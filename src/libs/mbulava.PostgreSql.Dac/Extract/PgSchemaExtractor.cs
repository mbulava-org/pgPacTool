using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using Npgsql;
using PgQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    public class PgSchemaExtractor
    {
        private readonly NpgsqlConnection _conn;

        public PgSchemaExtractor(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task<PgProject> ExtractAllSchemasAsync(string databaseName, string postgresVersion)
        {
            var project = new PgProject
            {
                DatabaseName = databaseName,
                PostgresVersion = postgresVersion,
            };

            var schemas = await ExtractSchemasAsync();
            foreach (var schema in schemas)
            {
                var pgSchema = new PgSchema { Name = schema.Name, Owner = schema.Owner, Ast = schema.Ast };

                pgSchema.Tables.AddRange(await ExtractTablesAsync(schema.Name));
                //pgSchema.Views.AddRange(await ExtractViewsAsync(schema.Name));
                //pgSchema.Functions.AddRange(await ExtractFunctionsAsync(schema.Name));
                pgSchema.Types.AddRange(await ExtractTypesAsync(schema.Name));
                pgSchema.Sequences.AddRange(await ExtractSequencesAsync(schema.Name));
                //pgSchema.Triggers.AddRange(await ExtractTriggersAsync(schema.Name));

                project.Schemas.Add(pgSchema);
            }

            var roles = await ExtractRolesForProjectAsync(project);
            project.Roles = roles;

            return project;
        }

        private async Task<List<PgRole>> ExtractRolesForProjectAsync(PgProject project)
        {
            var roles = new Dictionary<string, PgRole>(StringComparer.OrdinalIgnoreCase);

            // Step 1: collect owners
            var ownerNames = new HashSet<string>(
                project.Schemas.Select(s => s.Owner)
                .Concat(project.Schemas.SelectMany(s => s.Tables.Select(t => t.Owner)))
                .Concat(project.Schemas.SelectMany(s => s.Tables.SelectMany(t => t.Indexes.Select(i => i.Owner))))
                .Concat(project.Schemas.SelectMany(s => s.Sequences.Select(seq => seq.Owner)))
                .Concat(project.Schemas.SelectMany(s => s.Types.Select(ty => ty.Owner)))
                .Concat(project.Schemas.SelectMany(s => s.Triggers.Select(tr => tr.Owner)))
            );

            // Step 2: recursive lookup
            var queue = new Queue<string>(ownerNames);
            while (queue.Count > 0)
            {
                var roleName = queue.Dequeue();
                if (roles.ContainsKey(roleName)) continue;

                // Step 3: lookup role attributes
                using var cmd = new NpgsqlCommand(@"
            SELECT rolname, rolsuper, rolcanlogin, rolinherit, rolreplication, rolbypassrls
            FROM pg_roles
            WHERE rolname = @name;", _conn);
                cmd.Parameters.AddWithValue("name", roleName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) continue;

                var role = new PgRole
                {
                    Name = reader.GetString(0),
                    IsSuperUser = reader.GetBoolean(1),
                    CanLogin = reader.GetBoolean(2),
                    Inherit = reader.GetBoolean(3),
                    Replication = reader.GetBoolean(4),
                    BypassRLS = reader.GetBoolean(5),
                };

                roles[role.Name] = role;
                reader.Close();

                // Step 4: resolve memberships
                using var memCmd = new NpgsqlCommand(@"
            SELECT r.rolname
            FROM pg_auth_members m
            JOIN pg_roles r ON r.oid = m.roleid
            JOIN pg_roles u ON u.oid = m.member
            WHERE u.rolname = @name;", _conn);
                memCmd.Parameters.AddWithValue("name", roleName);

                using var memReader = await memCmd.ExecuteReaderAsync();
                while (await memReader.ReadAsync())
                {
                    var parentRole = memReader.GetString(0);
                    role.MemberOf.Add(parentRole);
                    if (!roles.ContainsKey(parentRole))
                        queue.Enqueue(parentRole);
                }
            }

            return roles.Values.ToList();
        }

        private async Task<List<PgSchema>> ExtractSchemasAsync()
        {
            var schemas = new List<PgSchema>();

            using var cmd = new NpgsqlCommand(
                "SELECT n.nspname, r.rolname " +
                "FROM pg_namespace n JOIN pg_roles r ON r.oid = n.nspowner " +
                "WHERE n.nspname NOT LIKE 'pg_%' " +
                "AND n.nspname <> 'information_schema';",
                _conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var owner = reader.GetString(1);
                //just create this AST for demonstration; in real scenarios, you might want to build it based on actual schema properties
                var ast = new CreateSchemaStmt
                {
                    Authrole = !string.IsNullOrEmpty(owner) ? new RoleSpec
                    {
                        Roletype = RoleSpecType.RolespecCstring,
                        Rolename = owner
                    } : null,
                    Schemaname = name,
                    IfNotExists = true,
                };
                schemas.Add(new PgSchema { Name = name, Owner = owner, Ast = ast, AstJson = JsonSerializer.Serialize(ast) });
            }

            return schemas;
        }

        private async Task<List<PgTable>> ExtractTablesAsync(string schema)
        {
            var tables = new List<PgTable>();

            using var cmd = new NpgsqlCommand(@"
        SELECT c.oid, c.relname, r.rolname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_roles r ON r.oid = c.relowner
        WHERE c.relkind = 'r' AND n.nspname = @schema;", _conn);

            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            var tableOids = new List<(UInt32 oid, string name, string owner)>();
            while (reader.Read())
            {
                tableOids.Add((reader.GetFieldValue<UInt32>(0), reader.GetString(1), reader.GetString(2)));
                //UInt32 oid = reader.GetDataTypeOID(0);   // ✅ OID-safe
                //var name = reader.GetString(1);
                //var owner = reader.GetString(2);
                //tableOids.Add((oid, name, owner));
            }
            await reader.CloseAsync();

            foreach (var (oid, name, owner) in tableOids)
            {
                // Build CREATE TABLE SQL
                var sql = await BuildCreateTableSqlAsync(oid, schema, name);

                // Parse into AST
                var parser = new Parser();
                var result = parser.Parse(sql);

                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Invalid SQL for table {schema}.{name}: {result.Error}");

                CreateStmt? ast = null;
                if (result.IsSuccess && result.ParseTree != null)
                {
                    // Assuming the ParseTree is a JsonDocument representing the parse tree,
                    // and you want to deserialize it to a CreateStmt.
                    // You may need to adjust this if your parser provides a different way to get the AST.
                    try
                    {
                        var json = result.ParseTree.RootElement.GetRawText();
                        ast = System.Text.Json.JsonSerializer.Deserialize<CreateStmt>(json);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Failed to deserialize AST for table {schema}.{name}");
                    }
                }

                var table = new PgTable
                {
                    Name = name,
                    Ast = ast,
                    AstJson = JsonSerializer.Serialize(ast),
                    Owner = owner
                };

                // Populate columns
                table.Columns.AddRange(await ExtractColumnsAsync(oid));

                // Populate constraints
                table.Constraints.AddRange(await ExtractConstraintsAsync(oid));

                // Populate indexes
                table.Indexes.AddRange(await ExtractIndexesAsync(oid));

                tables.Add(table);
            }

            return tables;
        }

        private async Task<string> BuildCreateTableSqlAsync(UInt32 oid, string schema, string name)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {schema}.{name} (");

            using var colCmd = new NpgsqlCommand(@"
        SELECT a.attname,
               pg_catalog.format_type(a.atttypid, a.atttypmod),
               a.attnotnull,
               pg_get_expr(d.adbin, d.adrelid)
        FROM pg_attribute a
        LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
        WHERE a.attrelid = @oid AND a.attnum > 0 AND NOT a.attisdropped;", _conn);

            colCmd.Parameters.AddWithValue("oid", (int)oid);

            using var colReader = await colCmd.ExecuteReaderAsync();
            var cols = new List<string>();
            while (colReader.Read())
            {
                var colDef = $"{colReader.GetString(0)} {colReader.GetString(1)}";
                if (colReader.GetBoolean(2)) colDef += " NOT NULL";
                if (!colReader.IsDBNull(3)) colDef += $" DEFAULT {colReader.GetString(3)}";
                cols.Add(colDef);
            }
            await colReader.CloseAsync();

            sb.AppendLine(string.Join(",\n", cols));
            sb.AppendLine(");");

            return sb.ToString();
        }

        private async Task<List<PgColumn>> ExtractColumnsAsync(UInt32 oid)
        {
            var columns = new List<PgColumn>();

            using var cmd = new NpgsqlCommand(@"
        SELECT a.attname,
               pg_catalog.format_type(a.atttypid, a.atttypmod),
               a.attnotnull,
               pg_get_expr(d.adbin, d.adrelid)
        FROM pg_attribute a
        LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
        WHERE a.attrelid = @oid AND a.attnum > 0 AND NOT a.attisdropped;", _conn);

            cmd.Parameters.AddWithValue("oid", (int)oid);

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                columns.Add(new PgColumn
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNotNull = reader.GetBoolean(2),
                    DefaultExpression = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
            await reader.CloseAsync();

            return columns;
        }

        private async Task<List<PgConstraint>> ExtractConstraintsAsync(UInt32 tableOid)
        {
            var constraints = new List<PgConstraint>();

            using var cmd = new NpgsqlCommand(@"
        SELECT conname, contype, pg_get_constraintdef(oid)
        FROM pg_constraint
        WHERE conrelid = @oid;", _conn);

            cmd.Parameters.AddWithValue("oid", (int)tableOid);

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var typeChar = reader.GetChar(1);
                var definition = reader.GetString(2);

                var constraint = new PgConstraint
                {
                    Name = name,
                    Type = MapConstraintType(typeChar),
                    CheckExpression = typeChar == 'c' ? definition : null,
                    ReferencedTable = typeChar == 'f' ? ExtractReferencedTable(definition) : null,
                    ReferencedColumns = typeChar == 'f' ? ExtractReferencedColumns(definition) : null
                };

                constraints.Add(constraint);
            }
            await reader.CloseAsync();

            return constraints;
        }

        private ConstrType MapConstraintType(char typeChar) =>
            typeChar switch
            {
                'p' => ConstrType.ConstrPrimary,
                'u' => ConstrType.ConstrUnique,
                'f' => ConstrType.ConstrForeign,
                'c' => ConstrType.ConstrCheck,
                _ => ConstrType.Undefined
            };

        // Helpers to parse FK definition text if needed
        private string ExtractReferencedTable(string definition)
        {
            // Example: FOREIGN KEY (col) REFERENCES other_table(col)
            var match = Regex.Match(definition, @"REFERENCES\s+(\S+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private List<string> ExtractReferencedColumns(string definition)
        {
            var match = Regex.Match(definition, @"\(([^)]+)\)$");
            return match.Success
                ? match.Groups[1].Value.Split(',').Select(c => c.Trim()).ToList()
                : new List<string>();
        }

        private async Task<List<PgIndex>> ExtractIndexesAsync(UInt32 tableOid)
        {
            var indexes = new List<PgIndex>();

            using var cmd = new NpgsqlCommand(@"
        SELECT i.indexrelid, c.relname, r.rolname
        FROM pg_index i
        JOIN pg_class c ON c.oid = i.indexrelid
        JOIN pg_roles r ON r.oid = c.relowner
        WHERE i.indrelid = @oid;", _conn);

            cmd.Parameters.AddWithValue("oid", (int)tableOid);

            using var reader = await cmd.ExecuteReaderAsync();
            var indexOids = new List<(UInt32 oid, string name, string owner)>();
            while (reader.Read())
            {
                var oid = reader.GetFieldValue<UInt32>(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                indexOids.Add((oid, name, owner));
            }
            await reader.CloseAsync();

            foreach (var (oid, name, owner) in indexOids)
            {
                using var defCmd = new NpgsqlCommand("SELECT pg_get_indexdef(@oid);", _conn);
                defCmd.Parameters.AddWithValue("oid", (int)oid);
                var sql = (string)await defCmd.ExecuteScalarAsync();

                var parser = new Parser();
                var result = parser.Parse(sql);

                IndexStmt? ast = null;
                if (result.IsSuccess && result.ParseTree != null)
                {
                    try
                    {
                        var json = result.ParseTree.RootElement.GetRawText();
                        ast = System.Text.Json.JsonSerializer.Deserialize<IndexStmt>(json);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Failed to deserialize AST for index {name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid SQL for index {name}: {result.Error}");
                }

                indexes.Add(new PgIndex
                {
                    Name = name,
                    Definition = sql,
                    Owner = owner
                });
            }

            return indexes;
        }

        private async Task<List<PgType>> ExtractTypesAsync(string schema)
        {
            var types = new List<PgType>();

            using var cmd = new NpgsqlCommand(@"
        SELECT t.oid, t.typname, r.rolname
        FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        JOIN pg_roles r ON r.oid = t.typowner
        WHERE n.nspname = @schema
          AND t.typtype IN ('d','e','c');", _conn);
            // d = domain, e = enum, c = composite

            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            var typeOids = new List<(uint oid, string name, string owner)>();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetFieldValue<UInt32>(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                typeOids.Add((oid, name, owner));
            }

            foreach (var (oid, name, owner) in typeOids)
            {
                using var defCmd = new NpgsqlCommand("SELECT pg_get_userbyid(typowner), pg_catalog.format_type(@oid, NULL);", _conn);
                defCmd.Parameters.AddWithValue("oid", (int)oid);
                var sql = $"CREATE TYPE {schema}.{name};"; // Simplified — you may need to reconstruct enum/domain details

                var parser = new Parser();
                var result = parser.Parse(sql);

                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Invalid SQL for type {name}: {result.Error}");

                CreateStmt? ast = null;
                if (result.IsSuccess && result.ParseTree != null)
                {
                    try
                    {
                        var json = result.ParseTree.RootElement.GetRawText();
                        ast = System.Text.Json.JsonSerializer.Deserialize<CreateStmt>(json);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Failed to deserialize AST for type {name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid SQL for type {name}: {result.Error}");
                }

                types.Add(new PgType
                {
                    Name = name,
                    Definition = sql,
                    Ast = ast,
                    AstJson = JsonSerializer.Serialize(ast),
                    Owner = owner
                });
            }

            return types;
        }

        private async Task<List<PgSequence>> ExtractSequencesAsync(string schema)
        {
            var sequences = new List<PgSequence>();

            using var cmd = new NpgsqlCommand(@"
        SELECT c.oid, c.relname, r.rolname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_roles r ON r.oid = c.relowner
        WHERE c.relkind = 'S' AND n.nspname = @schema;", _conn);

            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            var seqOids = new List<(uint oid, string name, string owner)>();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetDataTypeOID(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                seqOids.Add((oid, name, owner));
            }

            foreach (var (oid, name, owner) in seqOids)
            {
                using var defCmd = new NpgsqlCommand("SELECT pg_get_serial_sequence(@schema, @name);", _conn);
                defCmd.Parameters.AddWithValue("schema", schema);
                defCmd.Parameters.AddWithValue("name", name);

                // Build CREATE SEQUENCE SQL from pg_sequence
                using var seqCmd = new NpgsqlCommand(@"
            SELECT start_value, increment_by, max_value, min_value, cache_size, is_cycled
            FROM pg_sequence
            WHERE seqrelid = @oid;", _conn);
                seqCmd.Parameters.AddWithValue("oid", (int)oid);

                using var seqReader = await seqCmd.ExecuteReaderAsync();
                await seqReader.ReadAsync();

                var start = seqReader.GetInt64(0);
                var inc = seqReader.GetInt64(1);
                var max = seqReader.GetInt64(2);
                var min = seqReader.GetInt64(3);
                var cache = seqReader.GetInt64(4);
                var cycle = seqReader.GetBoolean(5);

                var sql = $@"CREATE SEQUENCE {schema}.{name}
            START WITH {start}
            INCREMENT BY {inc}
            MINVALUE {min}
            MAXVALUE {max}
            CACHE {cache}
            {(cycle ? "CYCLE" : "NO CYCLE")};";

                var parser = new Parser();
                var result = parser.Parse(sql);

                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Invalid SQL for sequence {name}: {result.Error}");

                CreateSeqStmt? ast = null;
                if (result.IsSuccess && result.ParseTree != null)
                {
                    try
                    {
                        var json = result.ParseTree.RootElement.GetRawText();
                        ast = System.Text.Json.JsonSerializer.Deserialize<CreateSeqStmt>(json);
                    }
                    catch
                    {
                        throw new InvalidOperationException($"Failed to deserialize AST for sequence {name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid SQL for sequence {name}: {result.Error}");
                }

                sequences.Add(new PgSequence
                {
                    Name = name,
                    Definition = sql,
                    Ast = ast,
                    AstJson = JsonSerializer.Serialize(ast),
                    Owner = owner
                });
            }

            return sequences;
        }
    }

}
