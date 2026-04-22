using FluentAssertions;
using mbulava.PostgreSql.Dac.Extract;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace mbulava.PostgreSql.Dac.Tests.Extract
{
    [TestFixture]
    public class PgProjectExtractorVersionTests
    {
        [Test]
        public async Task ExtractPgProject_PostgreSQL16_Succeeds()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            project.Should().NotBeNull();
            project.DatabaseName.Should().Be("testdb");
            // Docker containers may return "16" or "16.x" format
            project.PostgresVersion.Should().MatchRegex(@"^16(\.|$)", "PostgreSQL version should be 16 or 16.x");
        }

        [Test]
        public async Task ExtractPgProject_PostgreSQL15_Succeeds()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            project.Should().NotBeNull();
            project.PostgresVersion.Should().MatchRegex(@"^15(\.|$)", "PostgreSQL version should be 15 or 15.x");
        }

        [Test]
        public async Task ExtractPgProject_PostgreSQL14_ThrowsNotSupported()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:14")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            Func<Task> act = async () => await extractor.ExtractPgProject("testdb");

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*PostgreSQL 14*not supported*");
        }

        [Test]
        public async Task DetectPostgresVersion_PostgreSQL16_ReturnsVersion()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            var version = await extractor.DetectPostgresVersion();

            // Assert
            // Docker containers may return "16" or "16.x" format
            version.Should().MatchRegex(@"^16(\.|$)", "PostgreSQL version should be 16 or 16.x");
        }

        [Test]
        public async Task DetectPostgresVersion_PostgreSQL15_ReturnsVersion()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            var version = await extractor.DetectPostgresVersion();

            // Assert
            version.Should().MatchRegex(@"^15(\.|$)", "PostgreSQL version should be 15 or 15.x");
        }

        [Test]
        public async Task ExtractPgProject_PostgreSQL17_Succeeds()
        {
            // Test forward compatibility with PostgreSQL 17+
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:17")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            project.Should().NotBeNull();
            // Docker containers may return "17" or "17.x" format
            project.PostgresVersion.Should().MatchRegex(@"^17(\.|$)", "PostgreSQL version should be 17 or 17.x");
        }

        [Test]
        public async Task ExtractPgProject_PostgreSQL18_Succeeds()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:18")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            var extractor = new PgProjectExtractor(connectionString);

            // Act
            var project = await extractor.ExtractPgProject("testdb");

            // Assert
            project.Should().NotBeNull();
            project.PostgresVersion.Should().MatchRegex(@"^18(\.|$)", "PostgreSQL version should be 18 or 18.x");
        }

        [Test]
        public async Task ExtractPgProject_InvalidConnection_ThrowsException()
        {
            // Arrange
            var connectionString = "Host=nonexistent;Database=test;Username=test;Password=test";
            var extractor = new PgProjectExtractor(connectionString);

            // Act
            Func<Task> act = async () => await extractor.ExtractPgProject("testdb");

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
    }
}
