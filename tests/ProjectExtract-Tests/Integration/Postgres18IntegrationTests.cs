using mbulava.PostgreSql.Dac.Extract;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectExtract_Tests.Integration
{
    /// <summary>
    /// Integration tests for PostgreSQL 18 (Future-proofing)
    /// These tests will be enabled once PostgreSQL 18 is released
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Postgres18")]
    [Category("FutureVersion")]
    [Ignore("PostgreSQL 18 not yet released - Enable when available")]
    public class Postgres18IntegrationTests : PostgresVersionTestBase
    {
        protected override string PostgreSqlVersion => "postgres:18";

        [Test]
        public async Task ExtractProject_Postgres18_Success()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            Assert.That(project, Is.Not.Null);
            Assert.That(project.PostgresVersion, Does.StartWith("18."));
            Assert.That(project.Schemas, Is.Not.Empty);
            
            TestContext.WriteLine($"✓ Extracted project from PostgreSQL {project.PostgresVersion}");
        }

        [Test]
        public async Task VersionDetection_Postgres18_DetectsCorrectly()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var version = await extractor.DetectPostgresVersion();

            // Assert
            Assert.That(version, Does.StartWith("18."));
            TestContext.WriteLine($"✓ Detected PostgreSQL version: {version}");
        }

        [Test]
        public async Task ForwardCompatibility_Postgres18_AllFeaturesWork()
        {
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert - Verify all core features still work
            var testSchema = project.Schemas.FirstOrDefault(s => s.Name == "test_schema");
            Assert.That(testSchema, Is.Not.Null);
            Assert.That(testSchema.Tables, Is.Not.Empty, "Tables should be extracted");
            Assert.That(testSchema.Sequences, Is.Not.Empty, "Sequences should be extracted");
            Assert.That(testSchema.Privileges, Is.Not.Empty, "Privileges should be extracted");
            
            TestContext.WriteLine("✓ PostgreSQL 18 is fully compatible with pgPacTool");
        }

        [Test]
        public async Task NewFeatures_Postgres18_DoNotBreakExtraction()
        {
            // This test ensures that any new features in PostgreSQL 18
            // don't break our extraction logic
            
            // Arrange
            var extractor = new PgProjectExtractor(ConnectionString);

            // Act
            Exception? exception = null;
            try
            {
                await extractor.ExtractPgProject("testdb");
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Assert
            Assert.That(exception, Is.Null, 
                "PostgreSQL 18 new features should not break extraction");
        }
    }
}
