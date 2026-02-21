using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace ProjectExtract_Tests
{
    /// <summary>
    /// Simple smoke test to verify Testcontainers and basic privilege extraction works
    /// This is the fastest test to validate Issue #7 fix
    /// </summary>
    [TestFixture]
    [Category("Smoke")]
    public class SimplePrivilegeTest
    {
        [Test]
        public async Task SmokeTest_PrivilegeExtraction_Works()
        {
            // Arrange - Start PostgreSQL container
            await using var container = new PostgreSqlBuilder("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("testpass")
                .Build();

            await container.StartAsync();

            var connectionString = container.GetConnectionString();
            TestContext.Out.WriteLine($"✓ Container started: {connectionString}");

            // Create a simple table
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE test_table (id INT);";
            await cmd.ExecuteNonQueryAsync();

            TestContext.Out.WriteLine("✓ Table created");

            // Act - Extract the project
            var extractor = new PgProjectExtractor(connectionString);
            var version = await extractor.DetectPostgresVersion();
            TestContext.Out.WriteLine($"✓ PostgreSQL Version: {version}");

            var project = await extractor.ExtractPgProject("testdb");
            TestContext.Out.WriteLine("✓ Extraction complete");

            // Assert
            Assert.That(project, Is.Not.Null);
            Assert.That(project.Schemas, Is.Not.Empty);

            var publicSchema = project.Schemas[0];
            TestContext.Out.WriteLine($"✓ Schema: {publicSchema.Name}, Privileges: {publicSchema.Privileges.Count}");

            // The key assertion - we should be able to extract privileges without errors
            Assert.That(publicSchema.Privileges, Is.Not.Null, "Privileges should not be null");

            TestContext.Out.WriteLine("✅ Issue #7 Fix Verified: Privilege extraction works!");
        }
    }
}
