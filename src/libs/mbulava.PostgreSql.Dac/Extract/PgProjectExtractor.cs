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

    public class PgProjectExtractor
    {
        private readonly string _conn;
        private readonly bool _verbose;

        public PgProjectExtractor(string conString, bool verbose = false)
        {
            _conn = conString;
            _verbose = verbose;
        }

        /// <summary>
        /// Sanitizes connection string by removing password for documentation.
        /// </summary>
        private string SanitizeConnectionString(string connectionString)
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                builder.Password = "****";
                return builder.ToString();
            }
            catch
            {
                // If parsing fails, return generic sanitized string
                return connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase)
                    ? System.Text.RegularExpressions.Regex.Replace(connectionString, 
                        @"Password=[^;]*", "Password=****", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    : connectionString;
            }
        }

        /// <summary>
        /// Creates and opens a new database connection. MUST be disposed by caller using 'using' statement.
        /// </summary>
        private async Task<NpgsqlConnection> CreateConnectionAsync() 
        {             
            var conn = new NpgsqlConnection(_conn);
            await conn.OpenAsync();
            return conn;
        }

        /// <summary>
        /// Creates and opens a new database connection synchronously. MUST be disposed by caller using 'using' statement.
        /// </summary>
        private NpgsqlConnection CreateConnection() 
        {             
            var conn = new NpgsqlConnection(_conn);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Executes a query and returns a reader. Connection and command are automatically disposed.
        /// </summary>
        private async Task<T> ExecuteQueryAsync<T>(string sql, Func<NpgsqlDataReader, Task<T>> processReader, params (string name, object value)[] parameters)
        {
            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            return await processReader(reader);
        }

        /// <summary>
        /// Detects PostgreSQL version and validates it meets minimum requirements (PostgreSQL 16+)
        /// </summary>
        /// <returns>PostgreSQL version string (e.g., "16.1")</returns>
        /// <exception cref="NotSupportedException">Thrown when PostgreSQL version is below 16</exception>
        public async Task<string> DetectPostgresVersion()
        {
            // Use the version checker to validate and get version
            var version = await PostgreSqlVersionChecker.ValidateAndGetVersionAsync(_conn);
            return version;
        }

        /// <summary>
        /// Validates that the specified database exists in the PostgreSQL instance.
        /// </summary>
        /// <param name="databaseName">Name of the database to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when database doesn't exist</exception>
        private async Task ValidateDatabaseExistsAsync(string databaseName)
        {
            try
            {
                // Build a connection string to the 'postgres' database to check if target database exists
                var builder = new NpgsqlConnectionStringBuilder(_conn);
                var targetDatabase = builder.Database;

                // If no database specified in connection string, use the parameter
                if (string.IsNullOrEmpty(targetDatabase))
                {
                    targetDatabase = databaseName;
                }

                // Connect to 'postgres' database to query pg_database
                builder.Database = "postgres";
                var postgresConnString = builder.ToString();

                await using var conn = new NpgsqlConnection(postgresConnString);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbname;";
                cmd.Parameters.AddWithValue("dbname", targetDatabase);

                var result = await cmd.ExecuteScalarAsync();
                if (result == null)
                {
                    throw new InvalidOperationException(
                        $"Database '{targetDatabase}' does not exist in the PostgreSQL instance. " +
                        $"Please verify the database name or create the database before extraction.");
                }
            }
            catch (InvalidOperationException)
            {
                // Re-throw our validation exception
                throw;
            }
            catch (NpgsqlException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to validate database existence: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts PostgreSQL database project with version validation
        /// </summary>
        /// <param name="databaseName">Name of the database to extract</param>
        /// <returns>Complete PgProject with all database objects</returns>
        /// <exception cref="NotSupportedException">Thrown when PostgreSQL version is below 16</exception>
        /// <exception cref="InvalidOperationException">Thrown when database doesn't exist</exception>
        public async Task<PgProject> ExtractPgProject(string databaseName)
        {
            // Validate database exists first
            await ValidateDatabaseExistsAsync(databaseName);

            // Validate version - will throw NotSupportedException if < 16
            var postgresVersion = await DetectPostgresVersion();

            // Extract only major version (e.g., "16.12" -> "16")
            var majorVersion = postgresVersion.Split('.')[0];

            // Sanitize connection string for documentation (remove password)
            var sourceConnection = SanitizeConnectionString(_conn);

            // Extract username from connection string for default owner
            var defaultOwner = "postgres"; // fallback
            try
            {
                var connBuilder = new NpgsqlConnectionStringBuilder(_conn);
                defaultOwner = connBuilder.Username ?? "postgres";
            }
            catch
            {
                // If parsing fails, use default
            }

            var project = new PgProject
            {
                DatabaseName = databaseName,
                PostgresVersion = majorVersion, // Only major version
                SourceConnection = sourceConnection, // Sanitized connection string
                DefaultOwner = defaultOwner, // Use connection user as default
                DefaultTablespace = "pg_default"
            };



            var schemas = await ExtractSchemasAsync();
            foreach (var schema in schemas)
            {
                var pgSchema = new PgSchema 
                { 
                    Name = schema.Name, 
                    Owner = schema.Owner,
                    Definition = schema.Definition, // Copy the SQL definition
                    Ast = schema.Ast,
                    Privileges = schema.Privileges
                };

                pgSchema.Tables.AddRange(await ExtractTablesAsync(schema.Name));
                pgSchema.Views.AddRange(await ExtractViewsAsync(schema.Name));
                pgSchema.Functions.AddRange(await ExtractFunctionsAsync(schema.Name));
                pgSchema.Types.AddRange(await ExtractTypesAsync(schema.Name));
                pgSchema.Sequences.AddRange(await ExtractSequencesAsync(schema.Name));
                pgSchema.Triggers.AddRange(await ExtractTriggersAsync(schema.Name));

                project.Schemas.Add(pgSchema);
            }

            var roles = await ExtractRolesForProjectAsync(project);
            project.Roles = roles;

            return project;
        }

        private async Task<List<PgSchema>> ExtractSchemasAsync()
        {
            var schemas = new List<PgSchema>();

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT n.nspname, r.rolname
        FROM pg_namespace n
        JOIN pg_roles r ON r.oid = n.nspowner
        WHERE n.nspname NOT LIKE 'pg_%'
          AND n.nspname <> 'information_schema';";

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var owner = reader.GetString(1);

                if (_verbose)
                    Console.WriteLine($"   🔍 Found schema: {name} (owner: {owner})");

                // Build CREATE SCHEMA SQL
                var sql = $"CREATE SCHEMA {QuoteIdent(name)} AUTHORIZATION {QuoteIdent(owner)};";

                // Parse into AST
                var parser = new Parser();
                var result = parser.Parse(sql);

                if (!result.IsSuccess)
                {
                    if (_verbose)
                    {
                        Console.WriteLine($"   ⚠️ Warning: Failed to parse CREATE SCHEMA for '{name}': {result.Error}");
                        Console.WriteLine($"   Continuing extraction without AST for this schema...");
                    }
                    // Continue without AST instead of throwing
                }

                CreateSchemaStmt? ast = null;
                if (result.IsSuccess && result.ParseTree != null)
                {
                    var astJson = result.ParseTree.RootElement.GetRawText();
                    ast = JsonSerializer.Deserialize<CreateSchemaStmt>(astJson);
                }


                var privilegesSql = "SELECT n.nspacl::text[] FROM pg_namespace n WHERE n.nspname = @schema;";

                schemas.Add(new PgSchema
                {
                    Name = name,
                    Owner = owner,
                    Definition = sql, // Store the original SQL definition
                    Ast = ast,
                    Privileges = await ExtractPrivilegesAsync(privilegesSql, "schema", name)
                });
            }

            if (_verbose)
                Console.WriteLine($"   ✅ Total schemas found: {schemas.Count}");

            return schemas;
        }

        private async Task<List<PgPrivilege>> ExtractPrivilegesAsync(string sql, string paramName, object paramValue)
        {
            var privileges = new List<PgPrivilege>();

            try
            {
                using var conn = CreateConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue(paramName, paramValue);
                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return privileges;
                var aclArray = reader.IsDBNull(0) ? null : reader.GetFieldValue<string[]>(0);
                if (aclArray == null) return privileges;

                foreach (var acl in aclArray)
                {
                    // Example entries:
                    // Table: "grantee=arwdDxt/grantor" (lowercase = normal, uppercase = with grant option)
                    // Schema: "grantee=UC/grantor" or "grantee=U*C*/grantor" (* = with grant option)
                    var parts = acl.Split('=');
                    if (parts.Length < 2) continue;

                    var grantee = string.IsNullOrEmpty(parts[0]) ? "PUBLIC" : parts[0];
                    var rightsAndGrantor = parts[1].Split('/');
                    var rights = rightsAndGrantor[0];
                    var grantor = rightsAndGrantor.Length > 1 ? rightsAndGrantor[1] : string.Empty;

                    // Parse privileges - handle both formats:
                    // 1. Table format: lowercase=normal, uppercase=with grant option (e.g., "arwdDxt" or "ARWDXT")
                    // 2. Schema format: uppercase=normal, asterisk=with grant option (e.g., "UC" or "U*C*")
                    for (int i = 0; i < rights.Length; i++)
                    {
                        var ch = rights[i];

                        // Skip asterisks - they modify the previous character
                        if (ch == '*') continue;

                        // Check if next character is asterisk (GRANT OPTION for schemas)
                        var hasAsterisk = (i + 1 < rights.Length) && rights[i + 1] == '*';

                        // For tables: uppercase = GRANT OPTION
                        // For schemas: asterisk after privilege = GRANT OPTION
                        var isGrantable = hasAsterisk || (char.IsUpper(ch) && ch != 'U' && ch != 'C' && ch != 'D');

                        // Normalize to lowercase for privilege mapping
                        var privilegeCode = char.ToLower(ch);

                        privileges.Add(new PgPrivilege
                        {
                            Grantee = grantee,
                            PrivilegeType = MapPrivilege(ch), // Pass original char to handle U/C for schemas
                            IsGrantable = isGrantable,
                            Grantor = grantor
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExtractPrivilegesAsync: {ex.Message}");
                Console.WriteLine($"SQL: {sql}");
                Console.WriteLine($"ParamName: {paramName}, ParamValue: {paramValue}");
                throw;
            }

            return privileges;
        }

        private string MapPrivilege(char ch) =>
            ch switch
            {
                // Table privileges (lowercase = normal)
                'r' or 'R' => "SELECT",
                'w' or 'W' => "UPDATE",
                'a' or 'A' => "INSERT",
                'd' => "DELETE",             // lowercase d = DELETE
                'D' => "TRUNCATE",           // uppercase D = TRUNCATE
                'x' or 'X' => "REFERENCES",
                't' or 'T' => "TRIGGER",

                // Schema privileges (uppercase = normal)
                'U' or 'u' => "USAGE",       // U/u = USAGE
                'C' or 'c' => ch == 'C' ? "CREATE" : "CONNECT", // C = CREATE, c = CONNECT

                _ => $"Unknown({ch})"
            };


       
        private async Task<List<PgPrivilege>> ExtractSchemaPrivilegesAsync(string schemaName)
        {
            var privileges = new List<PgPrivilege>();

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT n.nspacl
        FROM pg_namespace n
        WHERE n.nspname = @schema;";

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
                await using var conn = await CreateConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            SELECT rolname, rolsuper, rolcanlogin, rolinherit, rolreplication, rolbypassrls
            FROM pg_roles
            WHERE rolname = @name;";
                cmd.Parameters.AddWithValue("name", roleName);

                await using var reader = await cmd.ExecuteReaderAsync();
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

                // Build CREATE ROLE SQL definition
                var attributes = new List<string>();
                if (role.IsSuperUser) attributes.Add("SUPERUSER");
                else attributes.Add("NOSUPERUSER");

                if (role.CanLogin) attributes.Add("LOGIN");
                else attributes.Add("NOLOGIN");

                if (role.Inherit) attributes.Add("INHERIT");
                else attributes.Add("NOINHERIT");

                if (role.Replication) attributes.Add("REPLICATION");
                else attributes.Add("NOREPLICATION");

                if (role.BypassRLS) attributes.Add("BYPASSRLS");
                else attributes.Add("NOBYPASSRLS");

                role.Definition = $"CREATE ROLE {QuoteIdent(roleName)} WITH {string.Join(" ", attributes)};";

                roles[role.Name] = role;
                reader.Close();

                // Step 4: resolve memberships
                await using var conn2 = await CreateConnectionAsync();
                await using var memCmd = conn2.CreateCommand();
                memCmd.CommandText = @"
            SELECT r.rolname
            FROM pg_auth_members m
            JOIN pg_roles r ON r.oid = m.roleid
            JOIN pg_roles u ON u.oid = m.member
            WHERE u.rolname = @name;";
                memCmd.Parameters.AddWithValue("name", roleName);

                await using var memReader = await memCmd.ExecuteReaderAsync();
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

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT c.oid, c.relname, r.rolname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_roles r ON r.oid = c.relowner
        WHERE c.relkind = 'r' AND n.nspname = @schema;";

            cmd.Parameters.AddWithValue("schema", schema);

            await using var reader = await cmd.ExecuteReaderAsync();
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
                    Definition = sql,
                    Ast = ast,
                    Owner = owner
                };

                var privilegesSql = "SELECT c.relacl::text[] FROM pg_class c WHERE c.oid = @oid;";
                table.Privileges = await ExtractPrivilegesAsync(privilegesSql, "oid", (int)oid);


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
            sb.AppendLine($"CREATE TABLE {QuoteIdent(schema)}.{QuoteIdent(name)} (");

            await using var conn = await CreateConnectionAsync();
            await using var colCmd = conn.CreateCommand();
            colCmd.CommandText = @"
        SELECT a.attname,
               pg_catalog.format_type(a.atttypid, a.atttypmod),
               a.attnotnull,
               pg_get_expr(d.adbin, d.adrelid)
        FROM pg_attribute a
        LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
        WHERE a.attrelid = @oid AND a.attnum > 0 AND NOT a.attisdropped;";

            colCmd.Parameters.AddWithValue("oid", (int)oid);

            await using var colReader = await colCmd.ExecuteReaderAsync();
            var cols = new List<string>();
            while (colReader.Read())
            {
                var colName = QuoteIdent(colReader.GetString(0));
                var colDef = $"{colName} {colReader.GetString(1)}";
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

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT a.attname,
               pg_catalog.format_type(a.atttypid, a.atttypmod),
               a.attnotnull,
               pg_get_expr(d.adbin, d.adrelid),
               col_description(a.attrelid, a.attnum)
        FROM pg_attribute a
        LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
        WHERE a.attrelid = @oid AND a.attnum > 0 AND NOT a.attisdropped;";

            cmd.Parameters.AddWithValue("oid", (int)oid);

            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                columns.Add(new PgColumn
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNotNull = reader.GetBoolean(2),
                    DefaultExpression = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Comment = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            await reader.CloseAsync();

            return columns;
        }

        private async Task<List<PgConstraint>> ExtractConstraintsAsync(UInt32 tableOid)
        {
            var constraints = new List<PgConstraint>();

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT conname, contype, pg_get_constraintdef(oid)
        FROM pg_constraint
        WHERE conrelid = @oid;";

            cmd.Parameters.AddWithValue("oid", (int)tableOid);

            await using var reader = await cmd.ExecuteReaderAsync();
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

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT i.indexrelid, c.relname, r.rolname
        FROM pg_index i
        JOIN pg_class c ON c.oid = i.indexrelid
        JOIN pg_roles r ON r.oid = c.relowner
        WHERE i.indrelid = @oid;";

            cmd.Parameters.AddWithValue("oid", (int)tableOid);

            await using var reader = await cmd.ExecuteReaderAsync();
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
                await using var conn2 = await CreateConnectionAsync();
                await using var defCmd = conn2.CreateCommand();
                defCmd.CommandText = "SELECT pg_get_indexdef(@oid);";
                defCmd.Parameters.AddWithValue("oid", (int)oid);
                var sql = (string?)await defCmd.ExecuteScalarAsync();

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

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        SELECT t.oid, t.typname, t.typtype, r.rolname
        FROM pg_type t
        JOIN pg_namespace n ON n.oid = t.typnamespace
        JOIN pg_roles r ON r.oid = t.typowner
        WHERE n.nspname = @schema
          AND t.typtype IN ('d','e','c');";

            cmd.Parameters.AddWithValue("schema", schema);

            await using var reader = await cmd.ExecuteReaderAsync();
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
                PgType pgType = new PgType { Name = name, Owner = owner };

                switch (typtype)
                {
                    case 'd': // Domain
                        await using (var conn2 = await CreateConnectionAsync())
                        await using (var domCmd = conn2.CreateCommand())
                        {
                            domCmd.CommandText = @"
                    SELECT pg_catalog.format_type(t.typbasetype, t.typtypmod) AS basetype,
                           t.typnotnull,
                           pg_get_constraintdef(c.oid) AS constraintdef
                    FROM pg_type t
                    LEFT JOIN pg_constraint c ON c.contypid = t.oid
                    WHERE t.oid = @oid;";
                            domCmd.Parameters.AddWithValue("oid", (int)oid);
                            await using var domReader = await domCmd.ExecuteReaderAsync();
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
                        var astJson = domResult.ParseTree?.RootElement.GetRawText();
                        pgType.Kind = PgTypeKind.Domain;
                        pgType.Definition = sql;
                        pgType.AstDomain = JsonSerializer.Deserialize<CreateDomainStmt>(astJson!);
                        break;

                    case 'e': // Enum
                        var labels = new List<string>();
                        await using (var conn3 = await CreateConnectionAsync())
                        await using (var enumCmd = conn3.CreateCommand())
                        {
                            enumCmd.CommandText = @"
                    SELECT e.enumlabel
                    FROM pg_enum e
                    WHERE e.enumtypid = @oid
                    ORDER BY e.enumsortorder;";
                            enumCmd.Parameters.AddWithValue("oid", (int)oid);
                            await using var enumReader = await enumCmd.ExecuteReaderAsync();
                            while (await enumReader.ReadAsync())
                                labels.Add(enumReader.GetString(0));
                        }

                        sql = $"CREATE TYPE {schema}.{name} AS ENUM ({string.Join(", ", labels.Select(l => $"'{l}'"))});";

                        var enumResult = new Parser().Parse(sql);
                        var enumAstJson = enumResult.ParseTree?.RootElement.GetRawText();
                        pgType.Kind = PgTypeKind.Enum;
                        pgType.Definition = sql;
                        pgType.EnumLabels = labels;
                        pgType.AstEnum = JsonSerializer.Deserialize<CreateEnumStmt>(enumAstJson!);
                        break;

                    case 'c': // Composite
                        var attrs = new List<PgAttribute>();
                        await using (var conn4 = await CreateConnectionAsync())
                        await using (var compCmd = conn4.CreateCommand())
                        {
                            compCmd.CommandText = @"
                    SELECT a.attname,
                           pg_catalog.format_type(a.atttypid, a.atttypmod) AS datatype,
                           a.attnotnull
                    FROM pg_attribute a
                    WHERE a.attrelid = @oid AND a.attnum > 0 AND NOT a.attisdropped
                    ORDER BY a.attnum;";
                            compCmd.Parameters.AddWithValue("oid", (int)oid);
                            await using var compReader = await compCmd.ExecuteReaderAsync();
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
                        var compAstJson = compResult.ParseTree?.RootElement.GetRawText();
                        pgType.Kind = PgTypeKind.Composite;
                        pgType.Definition = sql;
                        pgType.CompositeAttributes = attrs;
                        pgType.AstComposite = JsonSerializer.Deserialize<CompositeTypeStmt>(compAstJson!);
                        break;
                }

                types.Add(pgType);
            }

            return types;
        }


        private string BuildCreateSequenceSql(string schema, string name, string owner,
            long start, long increment, long min, long max, long cache, bool cycle)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE SEQUENCE {schema}.{name}");
            sb.AppendLine($"    START WITH {start}");
            sb.AppendLine($"    INCREMENT BY {increment}");
            sb.AppendLine($"    MINVALUE {min}");
            sb.AppendLine($"    MAXVALUE {max}");
            sb.AppendLine($"    CACHE {cache}");
            sb.AppendLine(cycle ? "    CYCLE;" : "    NO CYCLE;");
            sb.AppendLine($"ALTER SEQUENCE {schema}.{name} OWNER TO {owner};");
            return sb.ToString();
        }

        private async Task<List<PgSequence>> ExtractSequencesAsync(string schemaName)
        {
            var sequences = new List<PgSequence>();

            var sql = @"
        SELECT
            c.oid,
            c.relname,
            pg_get_userbyid(c.relowner) AS owner,
            s.seqstart,
            s.seqincrement,
            s.seqmin,
            s.seqmax,
            s.seqcache,
            s.seqcycle,
            c.relacl::text[]
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_sequence s ON s.seqrelid = c.oid
        WHERE c.relkind = 'S' AND n.nspname = @schema;
    ";


            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("schema", schemaName);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var oid = reader.GetFieldValue<UInt32>(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                var definition = BuildCreateSequenceSql(
                    schemaName,
                    name,
                    owner,
                    reader.GetInt64(3),
                    reader.GetInt64(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6),
                    reader.GetInt64(7),
                    reader.GetBoolean(8)
                );

                // Build options list
                var options = new List<SeqOption>
                {
                    new SeqOption { OptionName = "START",     OptionValue = reader.GetInt64(3).ToString() },
                    new SeqOption { OptionName = "INCREMENT", OptionValue = reader.GetInt64(4).ToString() },
                    new SeqOption { OptionName = "MINVALUE",  OptionValue = reader.GetInt64(5).ToString() },
                    new SeqOption { OptionName = "MAXVALUE",  OptionValue = reader.GetInt64(6).ToString() },
                    new SeqOption { OptionName = "CACHE",     OptionValue = reader.GetInt64(7).ToString() },
                    new SeqOption { OptionName = "CYCLE",     OptionValue = reader.GetBoolean(8).ToString() }
                };

                // Privileges
                var aclArray = reader.IsDBNull(9) ? null : reader.GetFieldValue<string[]>(9);
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
                    if (parseResult.IsSuccess && parseResult.ParseTree != null)
                    {
                        ast = JsonSerializer.Deserialize<CreateSeqStmt>(parseResult.ParseTree.RootElement.GetRawText());
                        astJson = parseResult.ParseTree.RootElement.GetRawText();
                    }
                    else if (_verbose)
                    {
                        Console.WriteLine($"   ⚠️  Warning: Failed to parse sequence definition for '{name}': {parseResult.Error}");
                    }
                }

                sequences.Add(new PgSequence
                {
                    Name = name,
                    Owner = owner,
                    Definition = definition,
                    Ast = ast,
                    Options = options,
                    Privileges = privileges
                });
            }

            return sequences;
        }

        private async Task<List<PgView>> ExtractViewsAsync(string schemaName)
        {
            var views = new List<PgView>();

            var sql = @"
        SELECT 
            c.oid,
            c.relname AS view_name,
            r.rolname AS owner,
            pg_get_viewdef(c.oid, true) AS definition,
            c.relkind = 'm' AS is_materialized,
            c.relacl::text[]
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_roles r ON r.oid = c.relowner
        WHERE n.nspname = @schema
          AND c.relkind IN ('v', 'm')  -- 'v' = view, 'm' = materialized view
        ORDER BY c.relname;
    ";

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("schema", schemaName);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetFieldValue<UInt32>(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                var definition = reader.GetString(3);
                var isMaterialized = reader.GetBoolean(4);
                var aclArray = reader.IsDBNull(5) ? null : reader.GetFieldValue<string[]>(5);

                // Build CREATE VIEW SQL for parsing
                var viewType = isMaterialized ? "MATERIALIZED VIEW" : "VIEW";
                var createViewSql = $"CREATE {viewType} {schemaName}.{name} AS\n{definition}";

                // Parse into AST
                var parser = new Parser();
                var result = parser.Parse(createViewSql);

                ViewStmt? ast = null;
                if (result.IsSuccess && result.ParseTree != null)
                {
                    try
                    {
                        var astJson = result.ParseTree.RootElement.GetRawText();
                        ast = System.Text.Json.JsonSerializer.Deserialize<ViewStmt>(astJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to deserialize AST for view {name}: {ex.Message}");
                        // Continue without AST - definition is still available
                    }
                }

                // Extract privileges
                var privileges = new List<PgPrivilege>();
                if (aclArray != null)
                {
                    var privilegesSql = "SELECT c.relacl::text[] FROM pg_class c WHERE c.oid = @oid;";
                    privileges = await ExtractPrivilegesAsync(privilegesSql, "oid", (int)oid);
                }

                views.Add(new PgView
                {
                    Name = name,
                    Owner = owner,
                    Definition = definition,
                    Ast = ast,
                    IsMaterialized = isMaterialized,
                    Privileges = privileges,
                    Dependencies = new List<string>() // TODO: Extract dependencies from pg_depend
                });
            }

            return views;
        }

        private async Task<List<PgFunction>> ExtractFunctionsAsync(string schemaName)
        {
            var functions = new List<PgFunction>();

            var sql = @"
        SELECT 
            p.oid,
            p.proname AS function_name,
            r.rolname AS owner,
            pg_get_functiondef(p.oid) AS definition,
            p.prokind AS kind  -- 'f' = function, 'p' = procedure, 'a' = aggregate, 'w' = window
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        JOIN pg_roles r ON r.oid = p.proowner
        WHERE n.nspname = @schema
        ORDER BY p.proname;
    ";

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("schema", schemaName);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetFieldValue<UInt32>(0);
                var name = reader.GetString(1);
                var owner = reader.GetString(2);
                var definition = reader.GetString(3);
                var kind = reader.GetChar(4);

                // Parse AST from function definition
                CreateFunctionStmt? ast = null;
                try
                {
                    var parser = new Parser();
                    var result = parser.Parse(definition);

                    if (result.IsSuccess && result.ParseTree != null)
                    {
                        var astJson = result.ParseTree.RootElement.GetRawText();
                        ast = JsonSerializer.Deserialize<CreateFunctionStmt>(astJson);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse AST for function {name}: {ex.Message}");
                    // Continue without AST - definition is still available
                }

                functions.Add(new PgFunction
                {
                    Name = name,
                    Owner = owner,
                    Definition = definition,
                    Ast = ast,
                    Privileges = new List<PgPrivilege>()  // TODO: Extract function privileges
                });
            }

            return functions;
        }

        private async Task<List<PgTrigger>> ExtractTriggersAsync(string schemaName)
        {
            var triggers = new List<PgTrigger>();

            var sql = @"
        SELECT 
            t.oid,
            t.tgname AS trigger_name,
            c.relname AS table_name,
            pg_get_triggerdef(t.oid) AS definition
        FROM pg_trigger t
        JOIN pg_class c ON c.oid = t.tgrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = @schema
          AND NOT t.tgisinternal
        ORDER BY c.relname, t.tgname;
    ";

            await using var conn = await CreateConnectionAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("schema", schemaName);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var oid = reader.GetFieldValue<UInt32>(0);
                var name = reader.GetString(1);
                var tableName = reader.GetString(2);
                var definition = reader.GetString(3);

                // Parse AST from trigger definition
                CreateTrigStmt? ast = null;
                try
                {
                    var parser = new Parser();
                    var result = parser.Parse(definition);

                    if (result.IsSuccess && result.ParseTree != null)
                    {
                        var astJson = result.ParseTree.RootElement.GetRawText();
                        ast = JsonSerializer.Deserialize<CreateTrigStmt>(astJson);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse AST for trigger {name}: {ex.Message}");
                    // Continue without AST - definition is still available
                }

                triggers.Add(new PgTrigger
                {
                    Name = name,
                    TableName = tableName,
                    Definition = definition,
                    Ast = ast,
                    Owner = "postgres"  // Triggers inherit table owner
                });
            }

            return triggers;
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
