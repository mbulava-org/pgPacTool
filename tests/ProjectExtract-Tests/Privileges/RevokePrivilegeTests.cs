using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests.Privileges
{
    /// <summary>
    /// Tests for REVOKE operations and privilege cascading
    /// Ensures privileges can be revoked and changes are detected correctly
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Privileges")]
    [Category("Revoke")]
    public class RevokePrivilegeTests
    {
        private PostgreSqlContainer _pgContainer = default!;
        private string _connectionString = default!;

        [SetUp]
        public async Task Setup()
        {
            // Start fresh PostgreSQL container for each test
            _pgContainer = new PostgreSqlBuilder("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("testpass")
                .Build();

            await _pgContainer.StartAsync();

            // Configure connection string with connection pool limits
            var builder = new NpgsqlConnectionStringBuilder(_pgContainer.GetConnectionString())
            {
                MaxPoolSize = 15,            // Reasonable size now that leaks are fixed
                MinPoolSize = 0,             // Start with no connections
                ConnectionIdleLifetime = 30, // Close idle connections after 30s
                Pooling = true,              // Enable pooling
                Timeout = 30                 // Connection timeout
            };
            _connectionString = builder.ToString();
        }

        [TearDown]
        public async Task Teardown()
        {
            // Clear connection pools before disposing container
            NpgsqlConnection.ClearAllPools();
            await _pgContainer.DisposeAsync();
        }

        private async Task ExecuteSqlAsync(string sql)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        [Test]
        public async Task RevokePrivilege_SchemaUsage_PrivilegeRemoved()
        {
            // Arrange - Grant then revoke
            await ExecuteSqlAsync(@"
                CREATE ROLE test_user LOGIN;
                CREATE SCHEMA test_schema;
                GRANT USAGE ON SCHEMA test_schema TO test_user;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            // Verify privilege exists
            var schemaBefore = projectBefore.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            var privBefore = schemaBefore?.Privileges.FirstOrDefault(p => p.Grantee == "test_user");
            Assert.That(privBefore, Is.Not.Null, "Privilege should exist before REVOKE");

            // Act - Revoke the privilege
            await ExecuteSqlAsync("REVOKE USAGE ON SCHEMA test_schema FROM test_user;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert - Privilege should be gone
            var schemaAfter = projectAfter.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            var privAfter = schemaAfter?.Privileges.FirstOrDefault(p => p.Grantee == "test_user");
            Assert.That(privAfter, Is.Null, "Privilege should be removed after REVOKE");

            TestContext.Out.WriteLine("✓ REVOKE correctly removed privilege");
        }

        [Test]
        public async Task RevokePrivilege_TableSelect_PrivilegeRemoved()
        {
            // Arrange
            await ExecuteSqlAsync(@"
                CREATE ROLE test_user LOGIN;
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT, name TEXT);
                GRANT SELECT ON test_schema.test_table TO test_user;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var tableBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var privBefore = tableBefore?.Privileges.FirstOrDefault(p => 
                p.Grantee == "test_user" && p.PrivilegeType == "SELECT");
            Assert.That(privBefore, Is.Not.Null, "SELECT privilege should exist");

            // Act
            await ExecuteSqlAsync("REVOKE SELECT ON test_schema.test_table FROM test_user;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert
            var tableAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var privAfter = tableAfter?.Privileges.FirstOrDefault(p => 
                p.Grantee == "test_user" && p.PrivilegeType == "SELECT");
            Assert.That(privAfter, Is.Null, "SELECT privilege should be revoked");

            TestContext.Out.WriteLine("✓ Table SELECT privilege revoked successfully");
        }

        [Test]
        public async Task RevokePrivilege_MultiplePrivileges_OnlySpecifiedRevoked()
        {
            // Arrange - Grant multiple privileges
            await ExecuteSqlAsync(@"
                CREATE ROLE test_user LOGIN;
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT);
                GRANT SELECT, INSERT, UPDATE, DELETE ON test_schema.test_table TO test_user;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var tableBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            Assert.That(tableBefore?.Privileges.Count, Is.EqualTo(4), "Should have 4 privileges initially");

            // Act - Revoke only INSERT and UPDATE
            await ExecuteSqlAsync("REVOKE INSERT, UPDATE ON test_schema.test_table FROM test_user;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert
            var tableAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            
            var remainingPrivs = tableAfter?.Privileges.Where(p => p.Grantee == "test_user").ToList();
            Assert.That(remainingPrivs?.Count, Is.EqualTo(2), "Should have 2 privileges remaining");
            
            var privTypes = remainingPrivs?.Select(p => p.PrivilegeType).ToList();
            Assert.That(privTypes, Does.Contain("SELECT"), "SELECT should remain");
            Assert.That(privTypes, Does.Contain("DELETE"), "DELETE should remain");
            Assert.That(privTypes, Does.Not.Contain("INSERT"), "INSERT should be revoked");
            Assert.That(privTypes, Does.Not.Contain("UPDATE"), "UPDATE should be revoked");

            TestContext.Out.WriteLine($"✓ Partial revoke successful: {remainingPrivs?.Count} privileges remain");
        }

        [Test]
        public async Task RevokePrivilege_AllPrivileges_RemovesAllFromGrantee()
        {
            // Arrange
            await ExecuteSqlAsync(@"
                CREATE ROLE test_user LOGIN;
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT);
                GRANT ALL PRIVILEGES ON test_schema.test_table TO test_user;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var tableBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var privCountBefore = tableBefore?.Privileges.Count(p => p.Grantee == "test_user");
            Assert.That(privCountBefore, Is.GreaterThan(0), "Should have privileges before revoke");

            // Act - Revoke ALL PRIVILEGES
            await ExecuteSqlAsync("REVOKE ALL PRIVILEGES ON test_schema.test_table FROM test_user;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert
            var tableAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var privCountAfter = tableAfter?.Privileges.Count(p => p.Grantee == "test_user");
            Assert.That(privCountAfter, Is.EqualTo(0), "All privileges should be revoked");

            TestContext.Out.WriteLine($"✓ REVOKE ALL PRIVILEGES removed {privCountBefore} privileges");
        }

        [Test]
        public async Task RevokePrivilege_FromPublic_PublicAccessRemoved()
        {
            // Arrange
            await ExecuteSqlAsync(@"
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT);
                GRANT SELECT ON test_schema.test_table TO PUBLIC;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var tableBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var publicPrivBefore = tableBefore?.Privileges.FirstOrDefault(p => p.Grantee == "PUBLIC");
            Assert.That(publicPrivBefore, Is.Not.Null, "PUBLIC should have SELECT");

            // Act - Revoke from PUBLIC
            await ExecuteSqlAsync("REVOKE SELECT ON test_schema.test_table FROM PUBLIC;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert
            var tableAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var publicPrivAfter = tableAfter?.Privileges.FirstOrDefault(p => p.Grantee == "PUBLIC");
            Assert.That(publicPrivAfter, Is.Null, "PUBLIC privilege should be revoked");

            TestContext.Out.WriteLine("✓ REVOKE from PUBLIC successful");
        }

        [Test]
        public async Task RevokePrivilege_SequenceUsage_PrivilegeRemoved()
        {
            // Arrange
            await ExecuteSqlAsync(@"
                CREATE ROLE test_user LOGIN;
                CREATE SCHEMA test_schema;
                CREATE SEQUENCE test_schema.test_seq;
                GRANT USAGE ON SEQUENCE test_schema.test_seq TO test_user;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var seqBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Sequences
                .FirstOrDefault(seq => seq.Name == "test_seq");
            var privBefore = seqBefore?.Privileges.FirstOrDefault(p => p.Grantee == "test_user");
            Assert.That(privBefore, Is.Not.Null, "USAGE on sequence should exist");

            // Act
            await ExecuteSqlAsync("REVOKE USAGE ON SEQUENCE test_schema.test_seq FROM test_user;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert
            var seqAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Sequences
                .FirstOrDefault(seq => seq.Name == "test_seq");
            var privAfter = seqAfter?.Privileges.FirstOrDefault(p => p.Grantee == "test_user");
            Assert.That(privAfter, Is.Null, "Sequence USAGE should be revoked");

            TestContext.Out.WriteLine("✓ Sequence privilege revoked successfully");
        }

        [Test]
        public async Task RevokeGrantOption_KeepsPrivilege_RemovesGrantability()
        {
            // Arrange - Grant with GRANT OPTION
            await ExecuteSqlAsync(@"
                CREATE ROLE test_user LOGIN;
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT);
                GRANT SELECT ON test_schema.test_table TO test_user WITH GRANT OPTION;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var tableBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var privBefore = tableBefore?.Privileges.FirstOrDefault(p => 
                p.Grantee == "test_user" && p.PrivilegeType == "SELECT");
            Assert.That(privBefore?.IsGrantable, Is.True, "Should have GRANT OPTION initially");

            // Act - Revoke only the GRANT OPTION
            await ExecuteSqlAsync("REVOKE GRANT OPTION FOR SELECT ON test_schema.test_table FROM test_user;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert - Privilege remains but not grantable
            var tableAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var privAfter = tableAfter?.Privileges.FirstOrDefault(p => 
                p.Grantee == "test_user" && p.PrivilegeType == "SELECT");
            
            Assert.That(privAfter, Is.Not.Null, "Privilege should still exist");
            Assert.That(privAfter.IsGrantable, Is.False, "GRANT OPTION should be removed");

            TestContext.Out.WriteLine("✓ REVOKE GRANT OPTION removed grantability while keeping privilege");
        }

        [Test]
        public async Task RevokePrivilege_CascadeOption_RemovesDependentGrants()
        {
            // Arrange - Create chain of grants (user1 grants to user2)
            await ExecuteSqlAsync(@"
                CREATE ROLE user1 LOGIN;
                CREATE ROLE user2 LOGIN;
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT);
                
                -- Grant to user1 with grant option
                GRANT SELECT ON test_schema.test_table TO user1 WITH GRANT OPTION;
            ");

            // user1 grants to user2
            await using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                // Switch to user1 and grant to user2
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SET ROLE user1;
                    GRANT SELECT ON test_schema.test_table TO user2;
                    RESET ROLE;
                ";
                await cmd.ExecuteNonQueryAsync();
            }

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var tableBefore = projectBefore.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var user2PrivBefore = tableBefore?.Privileges.FirstOrDefault(p => p.Grantee == "user2");
            Assert.That(user2PrivBefore, Is.Not.Null, "user2 should have privilege from user1");

            // Act - Revoke from user1 with CASCADE
            await ExecuteSqlAsync("REVOKE SELECT ON test_schema.test_table FROM user1 CASCADE;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert - user2's privilege should also be gone
            var tableAfter = projectAfter.Schemas
                .FirstOrDefault(s => s.Name == "test_schema")?.Tables
                .FirstOrDefault(t => t.Name == "test_table");
            var user1PrivAfter = tableAfter?.Privileges.FirstOrDefault(p => p.Grantee == "user1");
            var user2PrivAfter = tableAfter?.Privileges.FirstOrDefault(p => p.Grantee == "user2");
            
            Assert.That(user1PrivAfter, Is.Null, "user1 privilege should be revoked");
            Assert.That(user2PrivAfter, Is.Null, "user2 privilege should be cascaded away");

            TestContext.Out.WriteLine("✓ CASCADE successfully revoked dependent grants");
        }

        [Test]
        public async Task RevokePrivilege_Restrict_FailsWithDependentGrants()
        {
            // Arrange
            await ExecuteSqlAsync(@"
                CREATE ROLE user1 LOGIN;
                CREATE ROLE user2 LOGIN;
                CREATE SCHEMA test_schema;
                CREATE TABLE test_schema.test_table (id INT);
                
                GRANT SELECT ON test_schema.test_table TO user1 WITH GRANT OPTION;
            ");

            await using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SET ROLE user1;
                    GRANT SELECT ON test_schema.test_table TO user2;
                    RESET ROLE;
                ";
                await cmd.ExecuteNonQueryAsync();
            }

            // Act & Assert - RESTRICT should fail
            var exception = Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await ExecuteSqlAsync("REVOKE SELECT ON test_schema.test_table FROM user1 RESTRICT;");
            });

            Assert.That(exception?.SqlState, Is.EqualTo("2BP01"), "Should be dependent objects error");
            TestContext.Out.WriteLine("✓ RESTRICT correctly prevented revoke with dependent grants");
        }

        [Test]
        public async Task RevokePrivilege_RoleBasedPrivilege_RemovedFromRole()
        {
            // Arrange
            await ExecuteSqlAsync(@"
                CREATE ROLE test_role;
                CREATE SCHEMA test_schema;
                GRANT USAGE ON SCHEMA test_schema TO test_role;
            ");

            var extractor = new PgProjectExtractor(_connectionString);
            var projectBefore = await extractor.ExtractPgProject("testdb");
            
            var schemaBefore = projectBefore.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            var rolePrivBefore = schemaBefore?.Privileges.FirstOrDefault(p => p.Grantee == "test_role");
            Assert.That(rolePrivBefore, Is.Not.Null, "Role should have USAGE");

            // Act
            await ExecuteSqlAsync("REVOKE USAGE ON SCHEMA test_schema FROM test_role;");
            
            var projectAfter = await extractor.ExtractPgProject("testdb");

            // Assert
            var schemaAfter = projectAfter.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            var rolePrivAfter = schemaAfter?.Privileges.FirstOrDefault(p => p.Grantee == "test_role");
            Assert.That(rolePrivAfter, Is.Null, "Role privilege should be revoked");

            TestContext.Out.WriteLine("✓ Role-based privilege revoked successfully");
        }
    }
}
