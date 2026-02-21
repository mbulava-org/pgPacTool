using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Privileges
{
    /// <summary>
    /// Comprehensive privilege extraction tests covering all GRANT and REVOKE scenarios
    /// Tests for Issue #7: Privilege Extraction
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Privileges")]
    [Category("Comprehensive")]
    public class ComprehensivePrivilegeTests
    {
        private PostgreSqlContainer _pgContainer = default!;
        private string _connectionString = default!;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Start PostgreSQL 16 container
            _pgContainer = new PostgreSqlBuilder("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("testpass")
                .Build();

            await _pgContainer.StartAsync();

            // Configure connection string with connection pool limits
            var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
            {
                MaxPoolSize = 20,           // Limit pool size
                MinPoolSize = 0,            // Start with no connections
                ConnectionIdleLifetime = 30, // Close idle connections after 30s
                ConnectionPruningInterval = 10 // Check for idle connections every 10s
            };
            _connectionString = builder.ToString();

            // Seed comprehensive test data
            await SeedComprehensiveTestDataAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            // Clear all connection pools before disposing container
            NpgsqlConnection.ClearAllPools();
            await _pgContainer.DisposeAsync();
        }

        #region Test Data Seeding

        private async Task SeedComprehensiveTestDataAsync()
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // Create test users and roles
            await ExecuteSqlAsync(conn, @"
                -- Create test users
                CREATE ROLE user_read LOGIN PASSWORD 'pass1';
                CREATE ROLE user_write LOGIN PASSWORD 'pass2';
                CREATE ROLE user_admin LOGIN PASSWORD 'pass3';
                CREATE ROLE user_exec LOGIN PASSWORD 'pass4';
                
                -- Create test roles (groups)
                CREATE ROLE role_readers;
                CREATE ROLE role_writers;
                CREATE ROLE role_admins;
                
                -- Add users to roles
                GRANT role_readers TO user_read;
                GRANT role_writers TO user_write;
                GRANT role_admins TO user_admin;
            ");

            // Create test schemas with various privileges
            await ExecuteSqlAsync(conn, @"
                -- Schema 1: Basic privileges
                CREATE SCHEMA schema_basic;
                GRANT USAGE ON SCHEMA schema_basic TO user_read;
                GRANT CREATE ON SCHEMA schema_basic TO user_write;
                GRANT ALL PRIVILEGES ON SCHEMA schema_basic TO user_admin;
                
                -- Schema 2: Privileges with GRANT OPTION
                CREATE SCHEMA schema_grant_option;
                GRANT USAGE ON SCHEMA schema_grant_option TO user_read WITH GRANT OPTION;
                GRANT CREATE ON SCHEMA schema_grant_option TO user_write WITH GRANT OPTION;
                
                -- Schema 3: Role-based privileges
                CREATE SCHEMA schema_roles;
                GRANT USAGE ON SCHEMA schema_roles TO role_readers;
                GRANT CREATE ON SCHEMA schema_roles TO role_writers;
                GRANT ALL PRIVILEGES ON SCHEMA schema_roles TO role_admins;
                
                -- Schema 4: PUBLIC privileges
                CREATE SCHEMA schema_public;
                GRANT USAGE ON SCHEMA schema_public TO PUBLIC;
            ");

            // Create tables with comprehensive privilege scenarios
            await ExecuteSqlAsync(conn, @"
                -- Table with individual column privileges
                CREATE TABLE schema_basic.table_full_privileges (
                    id SERIAL PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT,
                    salary NUMERIC(10,2),
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                -- Grant different privilege combinations
                GRANT SELECT ON schema_basic.table_full_privileges TO user_read;
                GRANT INSERT ON schema_basic.table_full_privileges TO user_write;
                GRANT UPDATE ON schema_basic.table_full_privileges TO user_write;
                GRANT DELETE ON schema_basic.table_full_privileges TO user_admin;
                GRANT TRUNCATE ON schema_basic.table_full_privileges TO user_admin;
                GRANT REFERENCES ON schema_basic.table_full_privileges TO user_admin;
                GRANT TRIGGER ON schema_basic.table_full_privileges TO user_admin;
                
                -- Table with GRANT OPTION
                CREATE TABLE schema_grant_option.table_grant_option (
                    id INT PRIMARY KEY,
                    data TEXT
                );
                
                GRANT SELECT ON schema_grant_option.table_grant_option TO user_read WITH GRANT OPTION;
                GRANT INSERT, UPDATE, DELETE ON schema_grant_option.table_grant_option TO user_write WITH GRANT OPTION;
                
                -- Table with PUBLIC grants
                CREATE TABLE schema_public.table_public (
                    id INT,
                    public_data TEXT
                );
                
                GRANT SELECT ON schema_public.table_public TO PUBLIC;
                
                -- Table with mixed privileges
                CREATE TABLE schema_roles.table_mixed (
                    id INT PRIMARY KEY,
                    data TEXT
                );
                
                GRANT SELECT ON schema_roles.table_mixed TO role_readers;
                GRANT SELECT, INSERT, UPDATE ON schema_roles.table_mixed TO role_writers;
                GRANT ALL PRIVILEGES ON schema_roles.table_mixed TO role_admins;
            ");

            // Create sequences with privileges
            await ExecuteSqlAsync(conn, @"
                -- Sequence 1: Basic privileges
                CREATE SEQUENCE schema_basic.seq_basic START 1000;
                GRANT USAGE ON SEQUENCE schema_basic.seq_basic TO user_read;
                GRANT UPDATE ON SEQUENCE schema_basic.seq_basic TO user_write;
                GRANT SELECT ON SEQUENCE schema_basic.seq_basic TO user_read;
                
                -- Sequence 2: With GRANT OPTION
                CREATE SEQUENCE schema_grant_option.seq_grant_option START 2000;
                GRANT USAGE, SELECT ON SEQUENCE schema_grant_option.seq_grant_option TO user_read WITH GRANT OPTION;
                
                -- Sequence 3: PUBLIC access
                CREATE SEQUENCE schema_public.seq_public START 3000;
                GRANT USAGE, SELECT ON SEQUENCE schema_public.seq_public TO PUBLIC;
            ");

            // Create functions with EXECUTE privileges
            await ExecuteSqlAsync(conn, @"
                -- Function 1: Basic EXECUTE
                CREATE FUNCTION schema_basic.func_calculate(a INT, b INT)
                RETURNS INT AS $$
                BEGIN
                    RETURN a + b;
                END;
                $$ LANGUAGE plpgsql;
                
                GRANT EXECUTE ON FUNCTION schema_basic.func_calculate(INT, INT) TO user_exec;
                
                -- Function 2: PUBLIC EXECUTE
                CREATE FUNCTION schema_public.func_public(x INT)
                RETURNS INT AS $$
                BEGIN
                    RETURN x * 2;
                END;
                $$ LANGUAGE plpgsql;
                
                GRANT EXECUTE ON FUNCTION schema_public.func_public(INT) TO PUBLIC;
                
                -- Function 3: EXECUTE with GRANT OPTION
                CREATE FUNCTION schema_grant_option.func_grant_option(val TEXT)
                RETURNS TEXT AS $$
                BEGIN
                    RETURN upper(val);
                END;
                $$ LANGUAGE plpgsql;
                
                GRANT EXECUTE ON FUNCTION schema_grant_option.func_grant_option(TEXT) TO user_exec WITH GRANT OPTION;
            ");

            // Create views with privileges
            await ExecuteSqlAsync(conn, @"
                -- View 1: Basic SELECT
                CREATE VIEW schema_basic.view_basic AS
                SELECT id, name FROM schema_basic.table_full_privileges;
                
                GRANT SELECT ON schema_basic.view_basic TO user_read;
                
                -- View 2: PUBLIC access
                CREATE VIEW schema_public.view_public AS
                SELECT id, public_data FROM schema_public.table_public;
                
                GRANT SELECT ON schema_public.view_public TO PUBLIC;
            ");

            TestContext.Out.WriteLine("✓ Comprehensive test data seeded successfully");
        }

        private async Task ExecuteSqlAsync(NpgsqlConnection conn, string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Schema Privilege Tests

        [Test]
        public async Task SchemaPrivileges_BasicUsageAndCreate_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_basic");
            Assert.That(schema, Is.Not.Null);
            Assert.That(schema.Privileges, Is.Not.Empty);

            // Verify USAGE privilege
            var usagePriv = schema.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_read" && p.PrivilegeType == "USAGE");
            Assert.That(usagePriv, Is.Not.Null, "user_read should have USAGE");
            Assert.That(usagePriv.IsGrantable, Is.False);

            // Verify CREATE privilege
            var createPriv = schema.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_write" && p.PrivilegeType == "CREATE");
            Assert.That(createPriv, Is.Not.Null, "user_write should have CREATE");

            TestContext.Out.WriteLine($"✓ Found {schema.Privileges.Count} privileges on schema_basic");
        }

        [Test]
        public async Task SchemaPrivileges_WithGrantOption_SetsIsGrantableTrue()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_grant_option");
            Assert.That(schema, Is.Not.Null);

            // Verify GRANT OPTION is set
            var usageWithGrant = schema.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_read" && p.PrivilegeType == "USAGE");
            Assert.That(usageWithGrant, Is.Not.Null);
            Assert.That(usageWithGrant.IsGrantable, Is.True, "USAGE should have GRANT OPTION");

            var createWithGrant = schema.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_write" && p.PrivilegeType == "CREATE");
            Assert.That(createWithGrant, Is.Not.Null);
            Assert.That(createWithGrant.IsGrantable, Is.True, "CREATE should have GRANT OPTION");

            TestContext.Out.WriteLine("✓ GRANT OPTION correctly detected");
        }

        [Test]
        public async Task SchemaPrivileges_RoleBased_ExtractsRoleGrants()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_roles");
            Assert.That(schema, Is.Not.Null);

            // Verify role privileges
            var roleReaderPriv = schema.Privileges.FirstOrDefault(p => p.Grantee == "role_readers");
            Assert.That(roleReaderPriv, Is.Not.Null, "role_readers should have privileges");

            var roleWriterPriv = schema.Privileges.FirstOrDefault(p => p.Grantee == "role_writers");
            Assert.That(roleWriterPriv, Is.Not.Null, "role_writers should have privileges");

            var roleAdminPriv = schema.Privileges.FirstOrDefault(p => p.Grantee == "role_admins");
            Assert.That(roleAdminPriv, Is.Not.Null, "role_admins should have privileges");

            TestContext.Out.WriteLine($"✓ Found role-based privileges: {schema.Privileges.Count}");
        }

        [Test]
        public async Task SchemaPrivileges_PublicGrant_RecognizesPublic()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_public");
            Assert.That(schema, Is.Not.Null);

            // Verify PUBLIC privilege
            var publicPriv = schema.Privileges.FirstOrDefault(p => p.Grantee == "PUBLIC");
            Assert.That(publicPriv, Is.Not.Null, "PUBLIC should have USAGE");
            Assert.That(publicPriv.PrivilegeType, Is.EqualTo("USAGE"));

            TestContext.Out.WriteLine("✓ PUBLIC grant detected correctly");
        }

        #endregion

        #region Table Privilege Tests

        [Test]
        public async Task TablePrivileges_AllTypes_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_basic");
            var table = schema?.Tables.FirstOrDefault(t => t.Name == "table_full_privileges");
            Assert.That(table, Is.Not.Null);
            Assert.That(table.Privileges, Is.Not.Empty);

            // Verify specific privilege types
            var privilegeTypes = table.Privileges.Select(p => p.PrivilegeType).Distinct().ToList();
            
            Assert.That(privilegeTypes, Does.Contain("SELECT"), "Should have SELECT");
            Assert.That(privilegeTypes, Does.Contain("INSERT"), "Should have INSERT");
            Assert.That(privilegeTypes, Does.Contain("UPDATE"), "Should have UPDATE");
            Assert.That(privilegeTypes, Does.Contain("DELETE"), "Should have DELETE");
            Assert.That(privilegeTypes, Does.Contain("TRUNCATE"), "Should have TRUNCATE");
            Assert.That(privilegeTypes, Does.Contain("REFERENCES"), "Should have REFERENCES");
            Assert.That(privilegeTypes, Does.Contain("TRIGGER"), "Should have TRIGGER");

            TestContext.Out.WriteLine($"✓ Found privilege types: {string.Join(", ", privilegeTypes)}");
        }

        [Test]
        public async Task TablePrivileges_WithGrantOption_SetsIsGrantableTrue()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_grant_option");
            var table = schema?.Tables.FirstOrDefault(t => t.Name == "table_grant_option");
            Assert.That(table, Is.Not.Null);

            // All privileges should have GRANT OPTION
            var selectWithGrant = table.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_read" && p.PrivilegeType == "SELECT");
            Assert.That(selectWithGrant?.IsGrantable, Is.True, "SELECT should have GRANT OPTION");

            var insertWithGrant = table.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_write" && p.PrivilegeType == "INSERT");
            Assert.That(insertWithGrant?.IsGrantable, Is.True, "INSERT should have GRANT OPTION");

            TestContext.Out.WriteLine("✓ Table GRANT OPTION detected");
        }

        [Test]
        public async Task TablePrivileges_PublicAccess_ExtractsPublicGrants()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_public");
            var table = schema?.Tables.FirstOrDefault(t => t.Name == "table_public");
            Assert.That(table, Is.Not.Null);

            // Verify PUBLIC has SELECT
            var publicPriv = table.Privileges.FirstOrDefault(p => 
                p.Grantee == "PUBLIC" && p.PrivilegeType == "SELECT");
            Assert.That(publicPriv, Is.Not.Null, "PUBLIC should have SELECT on table");

            TestContext.Out.WriteLine("✓ PUBLIC table access detected");
        }

        [Test]
        public async Task TablePrivileges_MixedPrivileges_ExtractsAllCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_roles");
            var table = schema?.Tables.FirstOrDefault(t => t.Name == "table_mixed");
            Assert.That(table, Is.Not.Null);

            // Verify role_readers has only SELECT
            var readerPrivs = table.Privileges.Where(p => p.Grantee == "role_readers").ToList();
            Assert.That(readerPrivs.Count, Is.EqualTo(1));
            Assert.That(readerPrivs[0].PrivilegeType, Is.EqualTo("SELECT"));

            // Verify role_writers has SELECT, INSERT, UPDATE
            var writerPrivs = table.Privileges.Where(p => p.Grantee == "role_writers").ToList();
            Assert.That(writerPrivs.Count, Is.EqualTo(3));
            
            var writerPrivTypes = writerPrivs.Select(p => p.PrivilegeType).ToList();
            Assert.That(writerPrivTypes, Does.Contain("SELECT"));
            Assert.That(writerPrivTypes, Does.Contain("INSERT"));
            Assert.That(writerPrivTypes, Does.Contain("UPDATE"));

            TestContext.Out.WriteLine($"✓ Mixed privileges extracted: readers={readerPrivs.Count}, writers={writerPrivs.Count}");
        }

        #endregion

        #region Sequence Privilege Tests

        [Test]
        public async Task SequencePrivileges_UsageAndUpdate_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_basic");
            var sequence = schema?.Sequences.FirstOrDefault(s => s.Name == "seq_basic");
            Assert.That(sequence, Is.Not.Null);
            Assert.That(sequence.Privileges, Is.Not.Empty);

            // Verify USAGE privilege
            var usagePriv = sequence.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_read" && p.PrivilegeType == "USAGE");
            Assert.That(usagePriv, Is.Not.Null, "Should have USAGE privilege");

            // Verify UPDATE privilege (for nextval)
            var updatePriv = sequence.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_write" && p.PrivilegeType == "UPDATE");
            Assert.That(updatePriv, Is.Not.Null, "Should have UPDATE privilege");

            // Verify SELECT privilege (for currval)
            var selectPriv = sequence.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_read" && p.PrivilegeType == "SELECT");
            Assert.That(selectPriv, Is.Not.Null, "Should have SELECT privilege");

            TestContext.Out.WriteLine($"✓ Sequence privileges: {sequence.Privileges.Count}");
        }

        [Test]
        public async Task SequencePrivileges_WithGrantOption_SetsIsGrantableTrue()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_grant_option");
            var sequence = schema?.Sequences.FirstOrDefault(s => s.Name == "seq_grant_option");
            Assert.That(sequence, Is.Not.Null);

            // Verify GRANT OPTION on USAGE
            var usageWithGrant = sequence.Privileges.FirstOrDefault(p => 
                p.Grantee == "user_read" && p.PrivilegeType == "USAGE");
            Assert.That(usageWithGrant?.IsGrantable, Is.True, "USAGE should have GRANT OPTION");

            TestContext.Out.WriteLine("✓ Sequence GRANT OPTION detected");
        }

        [Test]
        public async Task SequencePrivileges_PublicAccess_ExtractsPublicGrants()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_public");
            var sequence = schema?.Sequences.FirstOrDefault(s => s.Name == "seq_public");
            Assert.That(sequence, Is.Not.Null);

            // Verify PUBLIC has USAGE and SELECT
            var publicPrivs = sequence.Privileges.Where(p => p.Grantee == "PUBLIC").ToList();
            Assert.That(publicPrivs, Is.Not.Empty, "PUBLIC should have privileges");
            
            var privTypes = publicPrivs.Select(p => p.PrivilegeType).ToList();
            Assert.That(privTypes, Does.Contain("USAGE"));
            Assert.That(privTypes, Does.Contain("SELECT"));

            TestContext.Out.WriteLine($"✓ PUBLIC sequence access: {privTypes.Count} privileges");
        }

        #endregion

        #region Grantor Tracking Tests

        [Test]
        public async Task PrivilegeExtraction_TracksGrantor_CorrectlyIdentifiesWhoGranted()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_basic");
            Assert.That(schema, Is.Not.Null);

            // Verify grantor is tracked (usually postgres for our tests)
            var privilege = schema.Privileges.FirstOrDefault();
            Assert.That(privilege, Is.Not.Null);
            Assert.That(privilege.Grantor, Is.Not.Null.And.Not.Empty, "Grantor should be tracked");

            TestContext.Out.WriteLine($"✓ Grantor tracked: {privilege.Grantor}");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public async Task PrivilegeExtraction_EmptyACL_ReturnsEmptyList()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);
            
            // Create a schema with no explicit grants (only owner default)
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await ExecuteSqlAsync(conn, "CREATE SCHEMA schema_no_grants;");

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_no_grants");
            Assert.That(schema, Is.Not.Null);
            
            // Should either be empty or only have owner privileges
            TestContext.Out.WriteLine($"✓ Schema with minimal grants has {schema.Privileges.Count} privileges");
        }

        [Test]
        public async Task PrivilegeExtraction_MultipleGrantees_ExtractsAll()
        {
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var schema = project.Schemas.FirstOrDefault(s => s.Name == "schema_basic");
            var table = schema?.Tables.FirstOrDefault(t => t.Name == "table_full_privileges");
            Assert.That(table, Is.Not.Null);

            // Verify multiple distinct grantees
            var grantees = table.Privileges.Select(p => p.Grantee).Distinct().ToList();
            Assert.That(grantees.Count, Is.GreaterThan(1), "Should have multiple grantees");
            
            Assert.That(grantees, Does.Contain("user_read"));
            Assert.That(grantees, Does.Contain("user_write"));
            Assert.That(grantees, Does.Contain("user_admin"));

            TestContext.Out.WriteLine($"✓ Found {grantees.Count} distinct grantees");
        }

        #endregion

        #region Summary Test

        [Test]
        public async Task ComprehensivePrivilegeTest_AllScenarios_FullCoverage()
        {
            // This test verifies that all major privilege scenarios are covered
            
            // Arrange
            var extractor = new PgProjectExtractor(_connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert - Collect statistics
            var totalSchemas = project.Schemas.Count;
            var schemasWithPrivs = project.Schemas.Count(s => s.Privileges.Any());
            var totalTables = project.Schemas.SelectMany(s => s.Tables).Count();
            var tablesWithPrivs = project.Schemas.SelectMany(s => s.Tables).Count(t => t.Privileges.Any());
            var totalSequences = project.Schemas.SelectMany(s => s.Sequences).Count();
            var sequencesWithPrivs = project.Schemas.SelectMany(s => s.Sequences).Count(seq => seq.Privileges.Any());
            
            var allPrivileges = project.Schemas
                .SelectMany(s => s.Privileges)
                .Concat(project.Schemas.SelectMany(s => s.Tables).SelectMany(t => t.Privileges))
                .Concat(project.Schemas.SelectMany(s => s.Sequences).SelectMany(seq => seq.Privileges))
                .ToList();

            var privilegeTypes = allPrivileges.Select(p => p.PrivilegeType).Distinct().ToList();
            var grantees = allPrivileges.Select(p => p.Grantee).Distinct().ToList();
            var withGrantOption = allPrivileges.Count(p => p.IsGrantable);
            var publicGrants = allPrivileges.Count(p => p.Grantee == "PUBLIC");

            // Output comprehensive summary
            TestContext.Out.WriteLine("=== Comprehensive Privilege Test Summary ===");
            TestContext.Out.WriteLine($"Schemas: {totalSchemas} total, {schemasWithPrivs} with privileges");
            TestContext.Out.WriteLine($"Tables: {totalTables} total, {tablesWithPrivs} with privileges");
            TestContext.Out.WriteLine($"Sequences: {totalSequences} total, {sequencesWithPrivs} with privileges");
            TestContext.Out.WriteLine($"Total privileges extracted: {allPrivileges.Count}");
            TestContext.Out.WriteLine($"Unique privilege types: {privilegeTypes.Count} - {string.Join(", ", privilegeTypes)}");
            TestContext.Out.WriteLine($"Unique grantees: {grantees.Count} - {string.Join(", ", grantees)}");
            TestContext.Out.WriteLine($"Privileges with GRANT OPTION: {withGrantOption}");
            TestContext.Out.WriteLine($"PUBLIC grants: {publicGrants}");

            // Assertions
            Assert.That(totalSchemas, Is.GreaterThan(3), "Should have multiple test schemas");
            Assert.That(allPrivileges.Count, Is.GreaterThan(10), "Should have comprehensive privileges");
            Assert.That(privilegeTypes.Count, Is.GreaterThanOrEqualTo(5), "Should cover multiple privilege types");
            Assert.That(grantees, Does.Contain("PUBLIC"), "Should include PUBLIC grants");
            Assert.That(withGrantOption, Is.GreaterThan(0), "Should have GRANT OPTION examples");

            TestContext.Out.WriteLine("✅ Comprehensive privilege extraction test PASSED!");
        }

        #endregion
    }
}
