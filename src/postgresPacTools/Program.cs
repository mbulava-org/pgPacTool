using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Publish;
using mbulava.PostgreSql.Dac.Models;
using mbulava.PostgreSql.Dac.Compare;
using System.CommandLine;
using System.Text.Json;

namespace postgresPacTools;

/// <summary>
/// PostgreSQL Data-Tier Application Tools CLI
/// Inspired by SqlPackage: https://learn.microsoft.com/en-us/sql/tools/sqlpackage/cli-reference
/// </summary>
internal class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PostgreSQL Data-Tier Application Tools - Extract, compile, and publish PostgreSQL schemas");

        // Add Actions
        rootCommand.AddCommand(CreateExtractCommand());
        rootCommand.AddCommand(CreatePublishCommand());
        rootCommand.AddCommand(CreateScriptCommand());
        rootCommand.AddCommand(CreateCompileCommand());
        rootCommand.AddCommand(CreateDeployReportCommand());

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Extract: Creates a schema file (.pgproj.json) from a live PostgreSQL database
    /// Similar to: sqlpackage /Action:Extract
    /// </summary>
    static Command CreateExtractCommand()
    {
        var command = new Command("extract", "Extract database schema to a .pgproj.json file");

        var sourceConnectionOption = new Option<string>(
            name: "--source-connection-string",
            description: "Source PostgreSQL database connection string")
        {
            IsRequired = true
        };
        sourceConnectionOption.AddAlias("-scs");

        var targetFileOption = new Option<string>(
            name: "--target-file",
            description: "Path to output .pgproj.json file")
        {
            IsRequired = true
        };
        targetFileOption.AddAlias("-tf");

        var databaseNameOption = new Option<string>(
            name: "--database-name",
            description: "Database name (overrides connection string)")
        {
            IsRequired = false
        };
        databaseNameOption.AddAlias("-dn");

        command.AddOption(sourceConnectionOption);
        command.AddOption(targetFileOption);
        command.AddOption(databaseNameOption);

        command.SetHandler(async (sourceConnection, targetFile, databaseName) =>
        {
            await ExtractAction(sourceConnection, targetFile, databaseName);
        }, sourceConnectionOption, targetFileOption, databaseNameOption);

        return command;
    }

    /// <summary>
    /// Publish: Incrementally updates a database schema to match a source .pgproj.json file
    /// Similar to: sqlpackage /Action:Publish
    /// </summary>
    static Command CreatePublishCommand()
    {
        var command = new Command("publish", "Publish schema changes to target database");

        var sourceFileOption = new Option<string>(
            name: "--source-file",
            description: "Source .pgproj.json file")
        {
            IsRequired = true
        };
        sourceFileOption.AddAlias("-sf");

        var targetConnectionOption = new Option<string>(
            name: "--target-connection-string",
            description: "Target PostgreSQL database connection string")
        {
            IsRequired = true
        };
        targetConnectionOption.AddAlias("-tcs");

        var variablesOption = new Option<string[]>(
            name: "--variables",
            description: "SQLCMD variables in format Name=Value")
        {
            AllowMultipleArgumentsPerToken = true
        };
        variablesOption.AddAlias("-v");

        var dropObjectsOption = new Option<bool>(
            name: "--drop-objects-not-in-source",
            description: "Drop objects in target that don't exist in source",
            getDefaultValue: () => false);
        dropObjectsOption.AddAlias("-dons");

        var transactionalOption = new Option<bool>(
            name: "--transactional",
            description: "Execute deployment in a transaction",
            getDefaultValue: () => true);

        command.AddOption(sourceFileOption);
        command.AddOption(targetConnectionOption);
        command.AddOption(variablesOption);
        command.AddOption(dropObjectsOption);
        command.AddOption(transactionalOption);

        command.SetHandler(async (sourceFile, targetConnection, variables, dropObjects, transactional) =>
        {
            await PublishAction(sourceFile, targetConnection, variables, dropObjects, transactional);
        }, sourceFileOption, targetConnectionOption, variablesOption, dropObjectsOption, transactionalOption);

        return command;
    }

    /// <summary>
    /// Script: Creates a SQL deployment script without executing it
    /// Similar to: sqlpackage /Action:Script
    /// </summary>
    static Command CreateScriptCommand()
    {
        var command = new Command("script", "Generate deployment script without executing");

        var sourceFileOption = new Option<string>(
            name: "--source-file",
            description: "Source .pgproj.json file")
        {
            IsRequired = true
        };
        sourceFileOption.AddAlias("-sf");

        var targetConnectionOption = new Option<string>(
            name: "--target-connection-string",
            description: "Target PostgreSQL database connection string")
        {
            IsRequired = true
        };
        targetConnectionOption.AddAlias("-tcs");

        var outputFileOption = new Option<string>(
            name: "--output-file",
            description: "Path to output SQL script file")
        {
            IsRequired = true
        };
        outputFileOption.AddAlias("-of");

        var variablesOption = new Option<string[]>(
            name: "--variables",
            description: "SQLCMD variables in format Name=Value")
        {
            AllowMultipleArgumentsPerToken = true
        };
        variablesOption.AddAlias("-v");

        var dropObjectsOption = new Option<bool>(
            name: "--drop-objects-not-in-source",
            description: "Drop objects in target that don't exist in source",
            getDefaultValue: () => false);
        dropObjectsOption.AddAlias("-dons");

        command.AddOption(sourceFileOption);
        command.AddOption(targetConnectionOption);
        command.AddOption(outputFileOption);
        command.AddOption(variablesOption);
        command.AddOption(dropObjectsOption);

        command.SetHandler(async (sourceFile, targetConnection, outputFile, variables, dropObjects) =>
        {
            await ScriptAction(sourceFile, targetConnection, outputFile, variables, dropObjects);
        }, sourceFileOption, targetConnectionOption, outputFileOption, variablesOption, dropObjectsOption);

        return command;
    }

    /// <summary>
    /// Compile: Validates and compiles a project, checking dependencies and circular references
    /// Unique to pgPacTool (based on Milestone 2)
    /// </summary>
    static Command CreateCompileCommand()
    {
        var command = new Command("compile", "Compile and validate project dependencies");

        var sourceFileOption = new Option<string>(
            name: "--source-file",
            description: "Source .pgproj.json file")
        {
            IsRequired = true
        };
        sourceFileOption.AddAlias("-sf");

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Show detailed compilation output",
            getDefaultValue: () => false);
        verboseOption.AddAlias("-v");

        command.AddOption(sourceFileOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (sourceFile, verbose) =>
        {
            await CompileAction(sourceFile, verbose);
        }, sourceFileOption, verboseOption);

        return command;
    }

    /// <summary>
    /// DeployReport: Creates a report of changes that would be made by publish
    /// Similar to: sqlpackage /Action:DeployReport
    /// </summary>
    static Command CreateDeployReportCommand()
    {
        var command = new Command("deploy-report", "Generate report of deployment changes");

        var sourceFileOption = new Option<string>(
            name: "--source-file",
            description: "Source .pgproj.json file")
        {
            IsRequired = true
        };
        sourceFileOption.AddAlias("-sf");

        var targetConnectionOption = new Option<string>(
            name: "--target-connection-string",
            description: "Target PostgreSQL database connection string")
        {
            IsRequired = true
        };
        targetConnectionOption.AddAlias("-tcs");

        var outputFileOption = new Option<string>(
            name: "--output-file",
            description: "Path to output report file (JSON)")
        {
            IsRequired = true
        };
        outputFileOption.AddAlias("-of");

        command.AddOption(sourceFileOption);
        command.AddOption(targetConnectionOption);
        command.AddOption(outputFileOption);

        command.SetHandler(async (sourceFile, targetConnection, outputFile) =>
        {
            await DeployReportAction(sourceFile, targetConnection, outputFile);
        }, sourceFileOption, targetConnectionOption, outputFileOption);

        return command;
    }

    // ========================================================================
    // Action Implementations
    // ========================================================================

    static async Task ExtractAction(string sourceConnection, string targetFile, string? databaseName)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  PostgreSQL Schema Extraction                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine($"📋 Source: {MaskPassword(sourceConnection)}");
            Console.WriteLine($"💾 Target: {targetFile}");
            Console.WriteLine();

            var extractor = new PgProjectExtractor(sourceConnection);
            var dbName = databaseName ?? GetDatabaseFromConnection(sourceConnection);

            Console.WriteLine($"🔍 Extracting schema from database '{dbName}'...");
            var project = await extractor.ExtractPgProject(dbName);

            Console.WriteLine($"✅ Extracted {project.Schemas.Count} schema(s)");
            foreach (var schema in project.Schemas)
            {
                Console.WriteLine($"   📁 {schema.Name}: {schema.Tables.Count} tables, {schema.Views.Count} views, " +
                    $"{schema.Functions.Count} functions, {schema.Types.Count} types");
            }

            // Save to file
            Console.WriteLine();
            Console.WriteLine($"💾 Saving to {targetFile}...");
            await using var fileStream = File.Create(targetFile);
            await PgProject.Save(project, fileStream);

            Console.WriteLine();
            Console.WriteLine("✅ Extraction completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task PublishAction(string sourceFile, string targetConnection, string[]? variables,
        bool dropObjects, bool transactional)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  PostgreSQL Schema Publishing                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine($"📋 Source: {sourceFile}");
            Console.WriteLine($"🎯 Target: {MaskPassword(targetConnection)}");
            Console.WriteLine($"🔄 Transactional: {transactional}");
            Console.WriteLine($"🗑️  Drop extra objects: {dropObjects}");
            Console.WriteLine();

            // Load source project
            Console.WriteLine("📖 Loading source project...");
            await using var fileStream = File.OpenRead(sourceFile);
            var sourceProject = await PgProject.Load(fileStream);
            Console.WriteLine($"✅ Loaded {sourceProject.Schemas.Count} schema(s)");

            // Parse variables
            var sqlCmdVars = ParseVariables(variables);
            if (sqlCmdVars.Count > 0)
            {
                Console.WriteLine($"🔧 Variables: {string.Join(", ", sqlCmdVars.Select(v => v.Name))}");
            }

            // Publish
            Console.WriteLine();
            Console.WriteLine("🚀 Publishing changes...");
            var publisher = new ProjectPublisher();
            var options = new PublishOptions
            {
                ConnectionString = targetConnection,
                GenerateScriptOnly = false,
                DropObjectsNotInSource = dropObjects,
                Transactional = transactional,
                Variables = sqlCmdVars
            };

            var result = await publisher.PublishAsync(sourceProject, targetConnection, options);

            Console.WriteLine();
            if (result.Success)
            {
                Console.WriteLine("✅ Deployment successful!");
                Console.WriteLine($"   📊 Created: {result.ObjectsCreated}");
                Console.WriteLine($"   🔄 Altered: {result.ObjectsAltered}");
                Console.WriteLine($"   🗑️  Dropped: {result.ObjectsDropped}");
                Console.WriteLine($"   ⏱️  Time: {result.ExecutionTime.TotalSeconds:F2}s");
            }
            else
            {
                Console.WriteLine("❌ Deployment failed!");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   ❌ {error}");
                }
                Environment.Exit(1);
            }

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("⚠️  Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"   ⚠️  {warning}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task ScriptAction(string sourceFile, string targetConnection, string outputFile,
        string[]? variables, bool dropObjects)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  PostgreSQL Script Generation                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine($"📋 Source: {sourceFile}");
            Console.WriteLine($"🎯 Target: {MaskPassword(targetConnection)}");
            Console.WriteLine($"💾 Output: {outputFile}");
            Console.WriteLine();

            // Load source project
            Console.WriteLine("📖 Loading source project...");
            await using var fileStream = File.OpenRead(sourceFile);
            var sourceProject = await PgProject.Load(fileStream);

            // Parse variables
            var sqlCmdVars = ParseVariables(variables);

            // Generate script
            Console.WriteLine("⚙️  Generating deployment script...");
            var publisher = new ProjectPublisher();
            var options = new PublishOptions
            {
                ConnectionString = targetConnection,
                GenerateScriptOnly = true,
                OutputScriptPath = outputFile,
                DropObjectsNotInSource = dropObjects,
                IncludeComments = true,
                Variables = sqlCmdVars
            };

            var result = await publisher.GenerateScriptAsync(sourceProject, targetConnection, options);

            Console.WriteLine();
            if (result.Success)
            {
                Console.WriteLine("✅ Script generated successfully!");
                Console.WriteLine($"   💾 File: {result.ScriptFilePath}");
                Console.WriteLine($"   📊 Changes: {result.ObjectsCreated} created, {result.ObjectsAltered} altered, {result.ObjectsDropped} dropped");
            }
            else
            {
                Console.WriteLine("❌ Script generation failed!");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   ❌ {error}");
                }
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task CompileAction(string sourceFile, bool verbose)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  PostgreSQL Project Compilation                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine($"📋 Source: {sourceFile}");
            Console.WriteLine();

            // Load project
            Console.WriteLine("📖 Loading project...");
            await using var fileStream = File.OpenRead(sourceFile);
            var project = await PgProject.Load(fileStream);
            Console.WriteLine($"✅ Loaded {project.Schemas.Count} schema(s)");

            // Compile
            Console.WriteLine();
            Console.WriteLine("⚙️  Compiling and validating...");
            var compiler = new ProjectCompiler();
            var result = compiler.Compile(project);

            Console.WriteLine();
            if (result.IsSuccess)
            {
                Console.WriteLine("✅ Compilation successful!");
                Console.WriteLine($"   📊 Objects: {result.DeploymentOrder.Count}");
                Console.WriteLine($"   📦 Levels: {result.DeploymentLevels.Count}");
                Console.WriteLine($"   ⏱️  Time: {result.CompilationTime.TotalMilliseconds:F0}ms");

                if (verbose && result.DeploymentOrder.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("📋 Deployment order:");
                    var count = 0;
                    foreach (var obj in result.DeploymentOrder.Take(20))
                    {
                        Console.WriteLine($"   {++count}. {obj}");
                    }
                    if (result.DeploymentOrder.Count > 20)
                    {
                        Console.WriteLine($"   ... and {result.DeploymentOrder.Count - 20} more");
                    }
                }
            }
            else
            {
                Console.WriteLine("❌ Compilation failed!");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   ❌ [{error.Code}] {error.Message}");
                    if (verbose && !string.IsNullOrEmpty(error.Location))
                    {
                        Console.WriteLine($"      📍 {error.Location}");
                    }
                }
                Environment.Exit(1);
            }

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("⚠️  Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"   ⚠️  [{warning.Code}] {warning.Message}");
                    if (verbose && !string.IsNullOrEmpty(warning.Location))
                    {
                        Console.WriteLine($"      📍 {warning.Location}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task DeployReportAction(string sourceFile, string targetConnection, string outputFile)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  PostgreSQL Deployment Report                              ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Console.WriteLine($"📋 Source: {sourceFile}");
            Console.WriteLine($"🎯 Target: {MaskPassword(targetConnection)}");
            Console.WriteLine($"💾 Output: {outputFile}");
            Console.WriteLine();

            // Load source project
            await using var fileStream = File.OpenRead(sourceFile);
            var sourceProject = await PgProject.Load(fileStream);

            // Extract target
            Console.WriteLine("🔍 Analyzing target database...");
            var extractor = new PgProjectExtractor(targetConnection);
            var targetProject = await extractor.ExtractPgProject("target");

            // Compare
            Console.WriteLine("⚙️  Comparing schemas...");
            var comparer = new PgSchemaComparer();
            var diffs = new List<object>();

            foreach (var sourceSchema in sourceProject.Schemas)
            {
                var targetSchema = targetProject.Schemas.FirstOrDefault(s => s.Name == sourceSchema.Name);
                if (targetSchema != null)
                {
                    var diff = comparer.Compare(sourceSchema, targetSchema, new());
                    diffs.Add(new
                    {
                        SchemaName = diff.SchemaName,
                        Tables = diff.TableDiffs.Count,
                        Views = diff.ViewDiffs.Count,
                        Functions = diff.FunctionDiffs.Count,
                        Triggers = diff.TriggerDiffs.Count,
                        Types = diff.TypeDiffs.Count,
                        Sequences = diff.SequenceDiffs.Count
                    });
                }
            }

            // Save report
            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                SourceFile = sourceFile,
                TargetConnection = MaskPassword(targetConnection),
                TotalChanges = diffs.Count,
                Changes = diffs
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputFile, json);

            Console.WriteLine();
            Console.WriteLine("✅ Report generated successfully!");
            Console.WriteLine($"   💾 File: {outputFile}");
            Console.WriteLine($"   📊 Schemas analyzed: {diffs.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    static List<SqlCmdVariable> ParseVariables(string[]? variables)
    {
        var result = new List<SqlCmdVariable>();

        if (variables == null || variables.Length == 0)
            return result;

        foreach (var variable in variables)
        {
            var parts = variable.Split('=', 2);
            if (parts.Length == 2)
            {
                result.Add(new SqlCmdVariable
                {
                    Name = parts[0].Trim(),
                    Value = parts[1].Trim()
                });
            }
        }

        return result;
    }

    static string MaskPassword(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "****";
        }
        return builder.ToString();
    }

    static string GetDatabaseFromConnection(string connectionString)
    {
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        return builder.Database ?? "postgres";
    }
}
