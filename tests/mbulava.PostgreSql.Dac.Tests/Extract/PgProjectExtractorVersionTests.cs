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
            project.PostgresVersion.Should().StartWith("16.");
        }

        [Test]
        public async Task ExtractPgProject_PostgreSQL15_ThrowsNotSupported()
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
            Func<Task> act = async () => await extractor.ExtractPgProject("testdb");

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*PostgreSQL 15*not supported*")
                .WithMessage("*requires PostgreSQL 16 or higher*");
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
            version.Should().StartWith("16.");
        }

        [Test]
        public async Task DetectPostgresVersion_PostgreSQL15_ThrowsNotSupported()
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
            Func<Task> act = async () => await extractor.DetectPostgresVersion();

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>();
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
            project.PostgresVersion.Should().StartWith("17.");
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
