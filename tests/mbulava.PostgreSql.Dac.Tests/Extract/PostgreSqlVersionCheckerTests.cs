using FluentAssertions;
using mbulava.PostgreSql.Dac.Extract;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace mbulava.PostgreSql.Dac.Tests.Extract
{
    [TestFixture]
    public class PostgreSqlVersionCheckerTests
    {
        [Test]
        public void MinimumSupportedVersion_IsSetTo16()
        {
            // Verify the minimum version constant
            PostgreSqlVersionChecker.MinimumSupportedVersion.Should().Be(16);
        }

        [Test]
        public async Task ValidateAndGetVersionAsync_PostgreSQL16_Succeeds()
        {
            // Arrange - Create PostgreSQL 16 container
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var version = await PostgreSqlVersionChecker.ValidateAndGetVersionAsync(connectionString);

            // Assert
            version.Should().StartWith("16.");
        }

        [Test]
        public async Task ValidateAndGetVersionAsync_PostgreSQL15_ThrowsNotSupported()
        {
            // Arrange - Create PostgreSQL 15 container
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            Func<Task> act = async () => await PostgreSqlVersionChecker.ValidateAndGetVersionAsync(connectionString);

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*PostgreSQL 15*not supported*")
                .WithMessage("*requires PostgreSQL 16 or higher*");
        }

        [Test]
        public async Task ValidateAndGetVersionAsync_PostgreSQL14_ThrowsNotSupported()
        {
            // Arrange - Create PostgreSQL 14 container
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:14")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            Func<Task> act = async () => await PostgreSqlVersionChecker.ValidateAndGetVersionAsync(connectionString);

            // Assert
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*PostgreSQL 14*not supported*");
        }

        [Test]
        public async Task GetVersionInfoAsync_PostgreSQL16_ReturnsCorrectVersion()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var (major, minor, full) = await PostgreSqlVersionChecker.GetVersionInfoAsync(connectionString);

            // Assert
            major.Should().Be(16);
            minor.Should().BeGreaterOrEqualTo(0);
            full.Should().StartWith("16.");
        }

        [Test]
        public async Task GetVersionInfoAsync_PostgreSQL15_ReturnsCorrectVersion()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var (major, minor, full) = await PostgreSqlVersionChecker.GetVersionInfoAsync(connectionString);

            // Assert
            major.Should().Be(15);
            full.Should().StartWith("15.");
        }

        [Test]
        public async Task CheckVersionSupportAsync_PostgreSQL16_ReturnsSupported()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var (isSupported, message) = await PostgreSqlVersionChecker.CheckVersionSupportAsync(connectionString);

            // Assert
            isSupported.Should().BeTrue();
            message.Should().Contain("16");
            message.Should().Contain("supported");
        }

        [Test]
        public async Task CheckVersionSupportAsync_PostgreSQL15_ReturnsNotSupported()
        {
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:15")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var (isSupported, message) = await PostgreSqlVersionChecker.CheckVersionSupportAsync(connectionString);

            // Assert
            isSupported.Should().BeFalse();
            message.Should().Contain("15");
            message.Should().Contain("not supported");
            message.Should().Contain("upgrade");
        }

        [Test]
        public async Task CheckVersionSupportAsync_InvalidConnectionString_ReturnsError()
        {
            // Arrange
            var connectionString = "Host=nonexistent;Database=test;Username=test;Password=test";

            // Act
            var (isSupported, message) = await PostgreSqlVersionChecker.CheckVersionSupportAsync(connectionString);

            // Assert
            isSupported.Should().BeFalse();
            message.Should().Contain("Error");
        }

        [Test]
        public async Task ValidateAndGetVersionAsync_PostgreSQL17_Succeeds()
        {
            // Arrange - Test forward compatibility with PostgreSQL 17+ (if available)
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:17")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var version = await PostgreSqlVersionChecker.ValidateAndGetVersionAsync(connectionString);

            // Assert
            version.Should().StartWith("17.");
        }

        [Test]
        public async Task ValidateAndGetVersionAsync_VersionInNonStandardFormat_ParsesCorrectly()
        {
            // This test verifies the version checker handles different version string formats
            // PostgreSQL can return: "16.1 (Debian 16.1-1.pgdg120+1)" or just "16.1"
            
            // Arrange
            await using var container = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .Build();

            await container.StartAsync();
            var connectionString = container.GetConnectionString();

            // Act
            var version = await PostgreSqlVersionChecker.ValidateAndGetVersionAsync(connectionString);

            // Assert
            version.Should().MatchRegex(@"^\d+\.\d+");  // Should be in format: major.minor
        }
    }
}
