using mbulava.PostgreSql.Dac.Extract;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectExtract_Tests.Integration
{
    /// <summary>
    /// Integration tests for PostgreSQL 17
    /// Tests forward compatibility with newer PostgreSQL version
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Postgres17")]
    public class Postgres17IntegrationTests : PostgresVersionTestBase
    {
        protected override string PostgreSqlVersion => "postgres:17";

        [Test]
        public async Task ExtractProject_Postgres17_Success()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            Assert.That(project, Is.Not.Null);
            // PostgreSQL version can be "17" or "17.x" depending on how it's reported
            Assert.That(project.PostgresVersion, Does.StartWith("17"), "Should be PostgreSQL 17.x");
            Assert.That(project.Schemas, Is.Not.Empty);

            TestContext.WriteLine($"✓ Extracted project from PostgreSQL {project.PostgresVersion}");
        }

        [Test]
        public async Task ExtractSchemaPrivileges_Postgres17_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null, "test_schema should exist");
            Assert.That(testSchema.Privileges, Is.Not.Empty, "Should have privileges");
            
            TestContext.WriteLine($"✓ Found {testSchema.Privileges.Count} privileges on test_schema");
        }

        [Test]
        public async Task VersionDetection_Postgres17_DetectsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var version = await extractor.DetectPostgresVersion();

            // Assert
            Assert.That(version, Does.StartWith("17."));
            TestContext.WriteLine($"✓ Detected PostgreSQL version: {version}");
        }

        [Test]
        public async Task CrossVersionCompatibility_Postgres17_WorksSameAsPostgres16()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert - Core features should work identically
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null);
            Assert.That(testSchema.Tables, Is.Not.Empty);
            Assert.That(testSchema.Sequences, Is.Not.Empty);
            Assert.That(testSchema.Privileges, Is.Not.Empty);
            
            TestContext.WriteLine("✓ PostgreSQL 17 behaves consistently with PostgreSQL 16");
        }
    }
}
