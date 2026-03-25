using Npgsql;
using System.Diagnostics;
using Testcontainers.PostgreSql;
using Xunit;
using Xunit.Abstractions;

namespace NugetPackage.Tests;

/// <summary>
/// Integration tests using PostgreSQL container to validate MSBuild.Sdk.PostgreSql workflow.
/// Tests the proper pgPacTool workflow: Extract to .csproj → Compile → Publish
/// 
/// REQUIRES DOCKER - Tests will fail if Docker is not available (this is intentional).
/// 
/// These tests validate:
/// 1. Extract database schema to MSBuild SDK-style .csproj project
/// 2. Compile the project to produce .pgpac file
/// 3. Publish the .pgpac file back to database
/// 4. Verify round-trip consistency
/// </summary>
public class PagilaIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _testWorkspace;
    private readonly string _solutionRoot;
    private readonly string _pgpacToolPath;
    
    private PostgreSqlContainer _postgresContainer = null!;
    private string _connectionString = string.Empty;

    public PagilaIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _solutionRoot = FindSolutionRoot();
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"pgpac-pagila-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);
        
        // Path to the pgpac tool (assuming it's built in Debug mode for tests)
        _pgpacToolPath = Path.Combine(_solutionRoot, "src", "postgresPacTools", "bin", "Debug", "net10.0", "postgresPacTools.dll");
    }

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container (will throw if Docker is not available - this is intentional)
        _output.WriteLine("🐳 Starting PostgreSQL container (requires Docker)...");
        _postgresContainer = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("pagila")
            .WithUsername("postgres")
            .WithPassword("postgres123")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();
        _connectionString = _postgresContainer.GetConnectionString();
        _output.WriteLine($"✅ PostgreSQL container started: {_connectionString}");
    }

    public async Task DisposeAsync()
    {
        _output.WriteLine("🧹 Stopping PostgreSQL container...");
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();

        if (Directory.Exists(_testWorkspace))
        {
            try
            {
                Directory.Delete(_testWorkspace, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task PagilaDatabase_CanBeExtractedToMSBuildProject()
    {
        // Arrange: Deploy Pagila schema
        var pagilaSchemaPath = Path.Combine(_solutionRoot, "tests", "NugetPackage.Tests", "TestData", "pagila-schema.sql");
        await DeploySchemaAsync(pagilaSchemaPath);
        _output.WriteLine($"✅ Pagila schema deployed ({await GetTableCountAsync()} tables)");

        // Act: Extract to MSBuild .csproj project
        _output.WriteLine("📥 Extracting to MSBuild SDK project...");
        var projectPath = Path.Combine(_testWorkspace, "Pagila.Database", "Pagila.Database.csproj");
        var projectDir = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(projectDir);
        
        var extractResult = await RunPgpacCommand($"extract --source-connection-string \"{_connectionString}\" --target-file \"{projectPath}\"");
        
        // Assert: Verify extraction succeeded
        Assert.Equal(0, extractResult.ExitCode);
        Assert.True(File.Exists(projectPath), "Project file should be created");
        
        // Verify project structure
        var projectContent = await File.ReadAllTextAsync(projectPath);
        Assert.Contains("MSBuild.Sdk.PostgreSql", projectContent);
        Assert.Contains("<DatabaseName>", projectContent);
        
        _output.WriteLine($"✅ Extracted to MSBuild project: {projectPath}");
        _output.WriteLine($"   Project content preview:\n{projectContent.Substring(0, Math.Min(500, projectContent.Length))}...");
    }

    [Fact]
    public async Task PagilaDatabase_MSBuildProject_CanBeCompiled()
    {
        // Arrange: Deploy and extract to MSBuild project
        var pagilaSchemaPath = Path.Combine(_solutionRoot, "tests", "NugetPackage.Tests", "TestData", "pagila-schema.sql");
        await DeploySchemaAsync(pagilaSchemaPath);
        
        var projectPath = Path.Combine(_testWorkspace, "Pagila.Database", "Pagila.Database.csproj");
        var projectDir = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(projectDir);
        
        await RunPgpacCommand($"extract --source-connection-string \"{_connectionString}\" --target-file \"{projectPath}\"");
        Assert.True(File.Exists(projectPath));
        
        // Act: Compile the MSBuild project
        _output.WriteLine("🔨 Compiling MSBuild project...");
        var compileResult = await RunPgpacCommand($"compile --source-file \"{projectPath}\" --verbose");
        
        // Assert: Verify compilation succeeded
        Assert.Equal(0, compileResult.ExitCode);
        
        // Verify .pgpac output was created
        var pgpacFile = Path.Combine(projectDir, "bin", "Debug", "net10.0", "pagila.pgpac");
        Assert.True(File.Exists(pgpacFile), $".pgpac file should be created at {pgpacFile}");
        
        _output.WriteLine($"✅ Compiled successfully: {pgpacFile}");
    }

    [Fact]
    public async Task PagilaDatabase_ExtractCompilePublish_RoundTripWorks()
    {
        // Arrange 1: Deploy Pagila schema to SOURCE database (pagila)
        _output.WriteLine("📦 Deploying Pagila schema to SOURCE database (pagila)...");
        var pagilaSchemaPath = Path.Combine(_solutionRoot, "tests", "NugetPackage.Tests", "TestData", "pagila-schema.sql");
        await DeploySchemaAsync(pagilaSchemaPath);
        var originalTableCount = await GetTableCountAsync();
        _output.WriteLine($"✅ SOURCE database ready ({originalTableCount} tables)");

        // Act 1: Extract from SOURCE database
        _output.WriteLine("📥 Extracting from SOURCE database (pagila)...");
        var projectPath = Path.Combine(_testWorkspace, "Pagila.Database", "Pagila.Database.csproj");
        var projectDir = Path.GetDirectoryName(projectPath)!;
        Directory.CreateDirectory(projectDir);

        var extractResult = await RunPgpacCommand($"extract --source-connection-string \"{_connectionString}\" --target-file \"{projectPath}\"");
        Assert.Equal(0, extractResult.ExitCode);
        Assert.True(File.Exists(projectPath));

        // Act 2: Compile the project to .pgpac format
        _output.WriteLine("🔨 Compiling project to .pgpac format...");
        var pgpacOutputPath = Path.Combine(projectDir, "bin", "Debug", "net10.0", "pagila.pgpac");
        Directory.CreateDirectory(Path.GetDirectoryName(pgpacOutputPath)!);
        var compileResult = await RunPgpacCommand($"compile --source-file \"{projectPath}\" --output-path \"{pgpacOutputPath}\" --output-format pgpac --verbose");
        Assert.Equal(0, compileResult.ExitCode);
        Assert.True(File.Exists(pgpacOutputPath), $".pgpac output should exist at {pgpacOutputPath}");

        // Arrange 2: Create NEW TARGET database for clean deployment test
        _output.WriteLine("🆕 Creating NEW TARGET database (pagila_target)...");
        var targetConnectionString = await CreateNewDatabaseAsync("pagila_target");
        _output.WriteLine($"✅ TARGET database created");

        // Verify target database is ACTUALLY empty
        var prePublishTableCount = await GetTableCountAsync(targetConnectionString);
        _output.WriteLine($"📊 PRE-PUBLISH: TARGET database tables: {prePublishTableCount}");
        Assert.Equal(0, prePublishTableCount); // Should be empty!

        // Act 3: Publish from .pgpac file to TARGET database (clean deployment)
        var publishScriptPath = Path.Combine(projectDir, "deployment_pagila_target_first.sql");
        _output.WriteLine("📤 Publishing to TARGET database (clean deployment)...");
        var publishResult = await RunPgpacCommand($"publish --source-file \"{pgpacOutputPath}\" --target-connection-string \"{targetConnectionString}\" --script-output \"{publishScriptPath}\"");
        await WriteDeploymentScriptDetailsAsync(publishScriptPath, "first publish");

        // Assert: Verify fresh deployment succeeded
        if (publishResult.ExitCode != 0)
        {
            _output.WriteLine("⚠️ Publish to TARGET failed. Output:");
            _output.WriteLine(publishResult.Output);
            _output.WriteLine("Error:");
            _output.WriteLine(publishResult.Error);
        }
        Assert.True(File.Exists(publishScriptPath), $"Deployment script should exist at {publishScriptPath}");
        Assert.Equal(0, publishResult.ExitCode);

        // Verify TARGET database has correct table count
        var targetTableCount = await GetTableCountAsync(targetConnectionString);
        _output.WriteLine($"📊 TARGET database tables: {targetTableCount}");
        Assert.Equal(originalTableCount, targetTableCount);

        // Act 4: Publish AGAIN to TARGET database (idempotent test - should detect NO CHANGES)
        var republishScriptPath = Path.Combine(projectDir, "deployment_pagila_target_second.sql");
        _output.WriteLine("🔄 Publishing AGAIN to TARGET database (idempotent test)...");
        var republishResult = await RunPgpacCommand($"publish --source-file \"{pgpacOutputPath}\" --target-connection-string \"{targetConnectionString}\" --script-output \"{republishScriptPath}\"");
        await WriteDeploymentScriptDetailsAsync(republishScriptPath, "second publish");

        Assert.True(File.Exists(republishScriptPath), $"Deployment script should exist at {republishScriptPath}");
        Assert.Equal(0, republishResult.ExitCode);

        // Parse output to verify no changes were made
        var noChangesDetected = republishResult.Output.Contains("Created: 0") || 
                                republishResult.Output.Contains("No changes detected");
        if (!noChangesDetected)
        {
            _output.WriteLine("⚠️ Expected NO CHANGES on second publish:");
            _output.WriteLine(republishResult.Output);
        }
        Assert.True(noChangesDetected, "Second publish should detect no changes (idempotent deployment)");

        _output.WriteLine($"✅ Round-trip successful: {originalTableCount} tables → extract → compile → publish (clean) → publish (idempotent)");
    }

    #region Helper Methods

    private string FindSolutionRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "src")) &&
                Directory.Exists(Path.Combine(current, "tests")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        throw new InvalidOperationException("Could not find solution root");
    }

    private async Task<(int ExitCode, string Output, string Error)> RunPgpacCommand(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_pgpacToolPath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _output.WriteLine($"▶️ Running: dotnet {startInfo.Arguments}");

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start pgpac process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(output))
        {
            _output.WriteLine($"📄 Output:\n{output}");
        }
        if (!string.IsNullOrWhiteSpace(error))
        {
            _output.WriteLine($"⚠️ Error:\n{error}");
        }

        return (process.ExitCode, output, error);
    }

    private async Task DeploySchemaAsync(string sqlFilePath)
    {
        var sql = await File.ReadAllTextAsync(sqlFilePath);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> GetTableCountAsync(string? connectionString = null)
    {
        await using var connection = new NpgsqlConnection(connectionString ?? _connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task<string> CreateNewDatabaseAsync(string databaseName)
    {
        // Connect to postgres database to create new database
        var builder = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Database = "postgres"
        };
        var adminConnectionString = builder.ConnectionString;

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        // Drop database if it exists
        await using (var dropCmd = connection.CreateCommand())
        {
            dropCmd.CommandText = $"DROP DATABASE IF EXISTS {databaseName}";
            await dropCmd.ExecuteNonQueryAsync();
        }

        // Create new database
        await using (var createCmd = connection.CreateCommand())
        {
            createCmd.CommandText = $"CREATE DATABASE {databaseName}";
            await createCmd.ExecuteNonQueryAsync();
        }

        // Return connection string for new database
        builder.Database = databaseName;
        return builder.ConnectionString;
    }

    private async Task WriteDeploymentScriptDetailsAsync(string scriptPath, string operationName)
    {
        if (!File.Exists(scriptPath))
        {
            _output.WriteLine($"⚠️ Deployment script for {operationName} was not created: {scriptPath}");
            return;
        }

        var scriptContent = await File.ReadAllTextAsync(scriptPath);
        _output.WriteLine($"💾 Deployment script for {operationName}: {scriptPath}");
        _output.WriteLine($"📏 Script size: {scriptContent.Length} characters");

        var previewLength = Math.Min(2000, scriptContent.Length);
        _output.WriteLine($"📄 Script preview ({previewLength} chars max):\n{scriptContent[..previewLength]}");
    }

    private async Task DropAllTablesAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // Disable foreign key checks temporarily
        await using (var disableCmd = connection.CreateCommand())
        {
            disableCmd.CommandText = @"
                DO $$ 
                DECLARE 
                    r RECORD;
                BEGIN
                    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
                        EXECUTE 'DROP TABLE IF EXISTS public.' || quote_ident(r.tablename) || ' CASCADE';
                    END LOOP;
                END $$;";
            await disableCmd.ExecuteNonQueryAsync();
        }
    }

    #endregion
}
