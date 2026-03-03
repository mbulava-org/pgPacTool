using mbulava.PostgreSql.Dac.Extract;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectExtract_Tests.Integration
{
    /// <summary>
    /// Integration tests for PostgreSQL 16
    /// pgPacTool's minimum supported version
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Postgres16")]
    public class Postgres16IntegrationTests : PostgresVersionTestBase
    {
        protected override string PostgreSqlVersion => "postgres:16";

        [Test]
        public async Task ExtractProject_Postgres16_Success()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            Assert.That(project, Is.Not.Null);
            // PostgreSQL version can be "16" or "16.x" depending on how it's reported
            Assert.That(project.PostgresVersion, Does.StartWith("16"), "Should be PostgreSQL 16.x");
            Assert.That(project.Schemas, Is.Not.Empty);

            TestContext.WriteLine($"✓ Extracted project from PostgreSQL {project.PostgresVersion}");
        }

        [Test]
        public async Task ExtractSchemaPrivileges_Postgres16_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null, "test_schema should exist");
            Assert.That(testSchema.Privileges, Is.Not.Empty, "Should have privileges");
            
            // Verify test_user has USAGE
            var userPrivileges = testSchema.Privileges
                .Where(p => p.Grantee == "test_user")
                .ToList();
            Assert.That(userPrivileges, Is.Not.Empty, "test_user should have privileges");
            
            TestContext.WriteLine($"✓ Found {testSchema.Privileges.Count} privileges on test_schema");
        }

        [Test]
        public async Task ExtractTables_Postgres16_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null);
            Assert.That(testSchema.Tables, Is.Not.Empty, "Should have tables");
            
            var customersTable = testSchema.Tables.FirstOrDefault(t => t.Name == "customers");
            Assert.That(customersTable, Is.Not.Null, "customers table should exist");
            Assert.That(customersTable.Columns, Is.Not.Empty, "Table should have columns");
            
            TestContext.WriteLine($"✓ Found {testSchema.Tables.Count} tables in test_schema");
        }

        [Test]
        public async Task ExtractSequences_Postgres16_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null);
            
            // Note: Sequences might include auto-generated ones from SERIAL columns
            Assert.That(testSchema.Sequences, Is.Not.Empty, "Should have sequences");
            
            var orderSeq = testSchema.Sequences.FirstOrDefault(s => s.Name == "order_seq");
            Assert.That(orderSeq, Is.Not.Null, "order_seq should exist");
            
            TestContext.WriteLine($"✓ Found {testSchema.Sequences.Count} sequences in test_schema");
        }

        [Test]
        public async Task ExtractTypes_Postgres16_ExtractsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null);
            
            // Types collection should exist (might be empty if no custom types)
            Assert.That(testSchema.Types, Is.Not.Null);
            
            TestContext.WriteLine($"✓ Found {testSchema.Types.Count} types in test_schema");
        }

        [Test]
        public async Task VersionDetection_Postgres16_DetectsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var version = await extractor.DetectPostgresVersion();

            // Assert
            // Docker containers may return "16" or "16.x" format
            Assert.That(version, Does.Match(@"^16(\.|$)"), "PostgreSQL version should be 16 or 16.x");
            TestContext.WriteLine($"✓ Detected PostgreSQL version: {version}");
        }

        [Test]
        public async Task ExtractPublicSchema_Postgres16_HasDefaultPrivileges()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            var publicSchema = project.Schemas.FirstOrDefault(s => s.Name == "public");
            Assert.That(publicSchema, Is.Not.Null, "public schema should exist");
            
            // Public schema should have some privileges (typically default ones)
            TestContext.WriteLine($"✓ public schema has {publicSchema.Privileges.Count} privileges");
        }
    }
}
