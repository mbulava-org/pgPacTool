using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;


namespace ProjectExtract_Tests
{
    [TestFixture]
    public class ProjectExtractIntegrationTests
    {
        private PostgreSqlContainer _pgContainer = default!;
        private string _tempDir = default!;

        [OneTimeSetUp]
        public async Task Setup()
        {
            _pgContainer = new PostgreSqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            await _pgContainer.StartAsync();

            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);

            await SeedTestSchemaAsync();
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            await _pgContainer.DisposeAsync();
            Directory.Delete(_tempDir, recursive: true);
        }

        [Test]
        public async Task ExtractAsync_CreatesExpectedPgProjFiles()
        {
            await using var conn = new NpgsqlConnection(_pgContainer.GetConnectionString());
            await conn.OpenAsync();

            var extractor = new ProjectExtract(conn);
            await extractor.ExtractAsync(_tempDir);

            Assert.That(File.Exists(Path.Combine(_tempDir, "public", "Tables", "customers.sql")), Is.True);
            Assert.That(File.Exists(Path.Combine(_tempDir, "manifest.json")), Is.True);
        }

        private async Task SeedTestSchemaAsync()
        {
            await using var conn = new NpgsqlConnection(_pgContainer.GetConnectionString());
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
            CREATE TABLE customers (
                id SERIAL PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE VIEW active_customers AS
            SELECT * FROM customers WHERE name IS NOT NULL;

            CREATE FUNCTION calculate_discount(price numeric) RETURNS numeric AS $$
            BEGIN
                RETURN price * 0.9;
            END;
            $$ LANGUAGE plpgsql;
        ";
            await cmd.ExecuteNonQueryAsync();
        }
    }

}
