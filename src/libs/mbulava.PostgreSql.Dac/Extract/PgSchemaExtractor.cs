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
    //TODO:  This will get the privileges for Functions & Procedures
    //var sql = "SELECT p.proacl FROM pg_proc p WHERE p.oid = @oid;";
    //pgFunction.Privileges = await ExtractPrivilegesAsync(sql, "oid", (int) oid);

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

        private async Task<List<PgSchema>> ExtractSchemasAsync()
        {
            var schemas = new List<PgSchema>();

            using var cmd = new NpgsqlCommand(@"
        SELECT n.nspname, r.rolname
        FROM pg_namespace n
        JOIN pg_roles r ON r.oid = n.nspowner
        WHERE n.nspname NOT LIKE 'pg_%'
          AND n.nspname <> 'information_schema';", _conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var owner = reader.GetString(1);

                // Build CREATE SCHEMA SQL
                var sql = $"CREATE SCHEMA {QuoteIdent(name)} AUTHORIZATION {QuoteIdent(owner)};";

                // Parse into AST
                var parser = new Parser();
                var result = parser.Parse(sql);

                if (!result.IsSuccess)
                    throw new InvalidOperationException($"Invalid SQL for schema {name}: {result.Error}");

                CreateSchemaStmt? ast = null;
                string? astJson = null;
                if (result.ParseTree != null)
                {
                    astJson = result.ParseTree.RootElement.GetRawText();
                    ast = JsonSerializer.Deserialize<CreateSchemaStmt>(astJson);
                }


                var privilegesSql = "SELECT n.nspacl FROM pg_namespace n WHERE n.nspname = @schema;";

                schemas.Add(new PgSchema
                {
                    Name = name,
                    Owner = owner,
                    Ast = ast,
                    AstJson = astJson,
                    Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
                });
            }

            return schemas;
        }

        private async Task<List<PgPrivilege>> ExtractPrivilegesAsync(string sql, string paramName, object paramValue)
        {
            var privileges = new List<PgPrivilege>();

            using var cmd = new NpgsqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue(paramName, paramValue);

            var aclArray = (string[]?)await cmd.ExecuteScalarAsync();
            if (aclArray == null) return privileges;

            foreach (var acl in aclArray)
            {
                // Example entry: "grantee=arwdDxt/grantor"
                var parts = acl.Split('=');
                if (parts.Length < 2) continue;

                var grantee = string.IsNullOrEmpty(parts[0]) ? "PUBLIC" : parts[0];
                var rightsAndGrantor = parts[1].Split('/');
                var rights = rightsAndGrantor[0];
                var grantor = rightsAndGrantor.Length > 1 ? rightsAndGrantor[1] : string.Empty;

                foreach (var ch in rights)
                {
                    privileges.Add(new PgPrivilege
                    {
                        Grantee = grantee,
                        PrivilegeType = MapPrivilege(ch),
                        IsGrantable = char.IsUpper(ch), // convention: uppercase = WITH GRANT OPTION
                        Grantor = grantor
                    });
                }
            }

            return privileges;
        }

        private string MapPrivilege(char ch) =>
            ch switch
            {
                'r' => "SELECT",
                'w' => "UPDATE",
                'a' => "INSERT",
                'd' => "DELETE",
                'D' => "TRUNCATE",
                'x' => "REFERENCES",
                't' => "TRIGGER",
                'U' => "USAGE",
                'C' => "CREATE",
                'c' => "CONNECT",
                'T' => "TEMPORARY",
                _ => $"Unknown({ch})"
            };

       
        private async Task<List<PgPrivilege>> ExtractSchemaPrivilegesAsync(string schemaName)
        {
            var privileges = new List<PgPrivilege>();

            using var cmd = new NpgsqlCommand(@"
        SELECT n.nspacl
        FROM pg_namespace n
        WHERE n.nspname = @schema;", _conn);

            cmd.Parameters.AddWithValue("schema", schemaName);

            var aclArray = (string[])await cmd.ExecuteScalarAsync();

            if (aclArray != null)
            {
                foreach (var acl in aclArray)
                {
                    // Example ACL entry: "grantee=UC/grantor"
                    // U = USAGE, C = CREATE
                    var parts = acl.Split('=');
                    if (parts.Length < 2) continue;

                    var grantee = parts[0];
                    var rights = parts[1];

                    // rights looks like "UC/grantor"
                    var privs = rights.Split('/');
                    var privString = privs[0];
                    var grantor = privs.Length > 1 ? privs[1] : null;

                    foreach (var ch in privString)
                    {
                        privileges.Add(new PgPrivilege
                        {
                            Grantee = string.IsNullOrEmpty(grantee) ? "PUBLIC" : grantee,
                            PrivilegeType = MapSchemaPrivilege(ch),
                            IsGrantable = false // Postgres encodes grantable separately; can extend later
                        });
                    }
                }
            }

            return privileges;
        }

        private string MapSchemaPrivilege(char ch) =>
            ch switch
            {
                'U' => "USAGE",
                'C' => "CREATE",
                _ => $"Unknown({ch})"
            };

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

                var priveilegesSql = "SELECT c.relacl FROM pg_class c WHERE c.oid = @oid;";
                table.Privileges = await ExtractPrivilegesAsync(priveilegesSql, "oid", (int)oid);


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
                    Definition = definition,
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
                'p' => ConstrType.ConstrPrimary,   // PRIMARY KEY
                'u' => ConstrType.ConstrUnique,    // UNIQUE
                'f' => ConstrType.ConstrForeign,   // FOREIGN KEY
                'c' => ConstrType.ConstrCheck,     // CHECK
                'x' => ConstrType.ConstrExclusion,       // EXCLUSION constraint (rare, but supported)
                't' => ConstrType.Undefined, // TODO: support constraint triggers
                'n' => ConstrType.ConstrNotnull,         // NOT NULL (stored as constraint in catalogs)
                _ => ConstrType.Undefined        // fallback
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
        SELECT t.oid, t.typname, t.typtype, r.rolname
        FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        JOIN pg_roles r ON r.oid = t.typowner
        WHERE n.nspname = @schema
          AND t.typtype IN ('d','e','c');", _conn);

            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            var typeInfos = new List<(uint oid, string name, char typtype, string owner)>();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetFieldValue<uint>(0);
                var name = reader.GetString(1);
                var typtype = reader.GetChar(2);
                var owner = reader.GetString(3);
                typeInfos.Add((oid, name, typtype, owner));
            }
            await reader.CloseAsync();

            foreach (var (oid, name, typtype, owner) in typeInfos)
            {
                string sql;
                string? astJson;
                PgType pgType = new PgType { Name = name, Owner = owner };

                switch (typtype)
                {
                    case 'd': // Domain
                        using (var domCmd = new NpgsqlCommand(@"
                    SELECT pg_catalog.format_type(t.typbasetype, t.typtypmod) AS basetype,
                           t.typnotnull,
                           pg_get_constraintdef(c.oid) AS constraintdef
                    FROM pg_type t
                    LEFT JOIN pg_constraint c ON c.contypid = t.oid
                    WHERE t.oid = @oid;", _conn))
                        {
                            domCmd.Parameters.AddWithValue("oid", (int)oid);
                            using var domReader = await domCmd.ExecuteReaderAsync();
                            await domReader.ReadAsync();
                            var basetype = domReader.GetString(0);
                            var notNull = domReader.GetBoolean(1);
                            var constraintDef = domReader.IsDBNull(2) ? null : domReader.GetString(2);

                            sql = $"CREATE DOMAIN {schema}.{name} AS {basetype}"
                                + (notNull ? " NOT NULL" : "")
                                + (constraintDef != null ? $" {constraintDef}" : "")
                                + ";";
                        }

                        var domResult = new Parser().Parse(sql);
                        astJson = domResult.ParseTree?.RootElement.GetRawText();
                        pgType.Kind = PgTypeKind.Domain;
                        pgType.Definition = sql;
                        pgType.AstDomain = JsonSerializer.Deserialize<CreateDomainStmt>(astJson!);
                        pgType.AstJson = astJson;
                        break;

                    case 'e': // Enum
                        var labels = new List<string>();
                        using (var enumCmd = new NpgsqlCommand(@"
                    SELECT e.enumlabel
                    FROM pg_enum e
                    WHERE e.enumtypid = @oid
                    ORDER BY e.enumsortorder;", _conn))
                        {
                            enumCmd.Parameters.AddWithValue("oid", (int)oid);
                            using var enumReader = await enumCmd.ExecuteReaderAsync();
                            while (await enumReader.ReadAsync())
                                labels.Add(enumReader.GetString(0));
                        }

                        sql = $"CREATE TYPE {schema}.{name} AS ENUM ({string.Join(", ", labels.Select(l => $"'{l}'"))});";

                        var enumResult = new Parser().Parse(sql);
                        astJson = enumResult.ParseTree?.RootElement.GetRawText();
                        pgType.Kind = PgTypeKind.Enum;
                        pgType.Definition = sql;
                        pgType.EnumLabels = labels;
                        pgType.AstEnum = JsonSerializer.Deserialize<CreateEnumStmt>(astJson!);
                        pgType.AstJson = astJson;
                        break;

                    case 'c': // Composite
                        var attrs = new List<PgAttribute>();
                        using (var compCmd = new NpgsqlCommand(@"
                    SELECT a.attname,
                           pg_catalog.format_type(a.atttypid, a.atttypmod) AS datatype,
                           a.attnotnull
                    FROM pg_attribute a
                    WHERE a.attrelid = @oid AND a.attnum > 0 AND NOT a.attisdropped
                    ORDER BY a.attnum;", _conn))
                        {
                            compCmd.Parameters.AddWithValue("oid", (int)oid);
                            using var compReader = await compCmd.ExecuteReaderAsync();
                            while (await compReader.ReadAsync())
                            {
                                attrs.Add(new PgAttribute
                                {
                                    Name = compReader.GetString(0),
                                    //DataType = compReader.GetString(1),
                                    // Composite attributes don’t support NOT NULL directly, but you can capture it
                                    DataType = compReader.GetString(1) + (compReader.GetBoolean(2) ? " NOT NULL" : "")
                                });
                            }
                        }

                        var attrSql = string.Join(", ", attrs.Select(a => $"{a.Name} {a.DataType}"));
                        sql = $"CREATE TYPE {schema}.{name} AS ({attrSql});";

                        var compResult = new Parser().Parse(sql);
                        astJson = compResult.ParseTree?.RootElement.GetRawText();
                        pgType.Kind = PgTypeKind.Composite;
                        pgType.Definition = sql;
                        pgType.CompositeAttributes = attrs;
                        pgType.AstComposite = JsonSerializer.Deserialize<CompositeTypeStmt>(astJson!);
                        pgType.AstJson = astJson;
                        break;
                }

                types.Add(pgType);
            }

            return types;
        }

        public async Task<List<PgSequence>> ExtractSequencesAsync(string schemaName)
        {
            var sequences = new List<PgSequence>();

            var sql = @"
        SELECT
            c.oid,
            c.relname,
            pg_get_userbyid(c.relowner) AS owner,
            pg_get_serial_sequence(n.nspname || '.' || c.relname, 'id') AS definition,
            s.seqstart,
            s.seqincrement,
            s.seqmin,
            s.seqmax,
            s.seqcache,
            s.seqcycle,
            c.relacl
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_sequence s ON s.seqrelid = c.oid
        WHERE c.relkind = 'S' AND n.nspname = @schema;
    ";

            using var cmd = new NpgsqlCommand(sql, _conn);
            cmd.Parameters.AddWithValue("schema", schemaName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetInt32(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                var definition = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

                // Build options list
                var options = new List<SeqOption>
        {
            new SeqOption { OptionName = "START",     OptionValue = reader["seqstart"].ToString() },
            new SeqOption { OptionName = "INCREMENT", OptionValue = reader["seqincrement"].ToString() },
            new SeqOption { OptionName = "MINVALUE",  OptionValue = reader["seqmin"].ToString() },
            new SeqOption { OptionName = "MAXVALUE",  OptionValue = reader["seqmax"].ToString() },
            new SeqOption { OptionName = "CACHE",     OptionValue = reader["seqcache"].ToString() },
            new SeqOption { OptionName = "CYCLE",     OptionValue = reader["seqcycle"].ToString() }
        };

                // Privileges
                var aclArray = reader.IsDBNull(10) ? null : reader.GetFieldValue<string[]>(10);
                var privileges = new List<PgPrivilege>();
                if (aclArray != null)
                {
                    foreach (var acl in aclArray)
                    {
                        privileges.AddRange(ParseAcl(acl));
                    }
                }

                // AST fidelity (optional, if you’re parsing CREATE SEQUENCE)
                string? astJson = null;
                CreateSeqStmt? ast = null;
                if (!string.IsNullOrEmpty(definition))
                {
                    var parseResult = new Parser().Parse(definition);
                    ast = JsonSerializer.Deserialize<CreateSeqStmt>(parseResult.ParseTree!.RootElement.GetRawText());
                    astJson = parseResult.ParseTree?.RootElement.GetRawText();
                }

                sequences.Add(new PgSequence
                {
                    Name = name,
                    Owner = owner,
                    Definition = definition,
                    Ast = ast,
                    AstJson = astJson,
                    Options = options,
                    Privileges = privileges
                });
            }

            return sequences;
        }

        private string QuoteIdent(string ident)
        {
            return "\"" + ident.Replace("\"", "\"\"") + "\"";
        }

        private IEnumerable<PgPrivilege> ParseAcl(string acl)
        {
            var privileges = new List<PgPrivilege>();
            var parts = acl.Split('=');
            if (parts.Length < 2) return privileges;

            var grantee = string.IsNullOrEmpty(parts[0]) ? "PUBLIC" : parts[0];
            var rightsAndGrantor = parts[1].Split('/');
            var rights = rightsAndGrantor[0];
            var grantor = rightsAndGrantor.Length > 1 ? rightsAndGrantor[1] : string.Empty;

            foreach (var ch in rights)
            {
                privileges.Add(new PgPrivilege
                {
                    Grantee = grantee,
                    PrivilegeType = MapPrivilege(ch),
                    IsGrantable = char.IsUpper(ch),
                    Grantor = grantor
                });
            }

            return privileges;
        }
    }

}
