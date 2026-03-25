using mbulava.PostgreSql.Dac.Models;
using mbulava.PostgreSql.Dac.Compare;
using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Deployment;
using mbulava.PostgreSql.Dac.Extract;
using System.Diagnostics;

namespace mbulava.PostgreSql.Dac.Publish;

/// <summary>
/// Publishes PostgreSQL database changes using comparison and script generation.
/// </summary>
public class ProjectPublisher
{
    private readonly PgSchemaComparer _comparer;
    private readonly ProjectCompiler _compiler;

    public ProjectPublisher()
    {
        _comparer = new PgSchemaComparer();
        _compiler = new ProjectCompiler();
    }

    /// <summary>
    /// Publishes a project to a target database.
    /// </summary>
    /// <param name="sourceProject">Source project with desired schema</param>
    /// <param name="targetConnectionString">Target database connection string</param>
    /// <param name="options">Publishing options</param>
    /// <returns>Publish result with script and statistics</returns>
    public async Task<PublishResult> PublishAsync(
        PgProject sourceProject,
        string targetConnectionString,
        PublishOptions options)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetConnectionString);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        var result = new PublishResult();

        try
        {
            // Step 1: Compile source project (validate dependencies)
            var compilationResult = _compiler.Compile(sourceProject);
            if (!compilationResult.IsSuccess)
            {
                result.Success = false;
                result.Errors.AddRange(compilationResult.Errors.Select(e => $"[{e.Code}] {e.Message}: {e.Location}"));
                return result;
            }

            // Add compilation warnings to result
            result.Warnings.AddRange(compilationResult.Warnings.Select(w => $"[{w.Code}] {w.Message}: {w.Location}"));

            // Step 2: Extract target database schema
            var extractor = new PgProjectExtractor(targetConnectionString);
            var targetProject = await extractor.ExtractPgProject(options.TargetDatabase ?? "target");

            // Step 3: Compare schemas
            var differences = new List<PgSchemaDiff>();
            
            // Compare each schema in source
            foreach (var sourceSchema in sourceProject.Schemas)
            {
                var targetSchema = targetProject.Schemas.FirstOrDefault(s => s.Name == sourceSchema.Name);
                
                if (targetSchema == null)
                {
                    // Schema doesn't exist in target - need to create everything
                    result.Warnings.Add($"Schema '{sourceSchema.Name}' does not exist in target database.");
                    // For now, we'll skip schemas that don't exist - they need special handling
                    continue;
                }

                var diff = _comparer.Compare(sourceSchema, targetSchema, options.CompareOptions);
                
                // Only add if there are differences
                if (HasDifferences(diff))
                {
                    differences.Add(diff);
                }
            }

            // Step 4: Validate pre/post deployment scripts
            if (options.PreDeploymentScripts.Any() || options.PostDeploymentScripts.Any())
            {
                var projectRoot = Environment.CurrentDirectory; // Could be configurable
                var scriptManager = new PrePostDeploymentScriptManager(projectRoot);

                var allScripts = options.PreDeploymentScripts.Concat(options.PostDeploymentScripts).ToList();
                var validationErrors = scriptManager.ValidateScripts(allScripts);
                
                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.Errors.AddRange(validationErrors);
                    return result;
                }

                // Load script contents
                options.PreDeploymentScripts = await scriptManager.LoadScriptsAsync(options.PreDeploymentScripts);
                options.PostDeploymentScripts = await scriptManager.LoadScriptsAsync(options.PostDeploymentScripts);

                // Validate script contents
                foreach (var script in allScripts)
                {
                    var contentWarnings = PrePostDeploymentScriptManager.ValidateScriptContent(script);
                    result.Warnings.AddRange(contentWarnings);
                }
            }

            // Step 5: Generate deployment script
            if (differences.Count == 0 && 
                !options.PreDeploymentScripts.Any() && 
                !options.PostDeploymentScripts.Any())
            {
                result.Success = true;
                result.Script = "-- No changes detected";
                result.Warnings.Add("No schema differences detected. Target database is already up to date.");
            }
            else
            {
                // Generate script for each schema
                var scriptParts = new List<string>();
                
                foreach (var diff in differences)
                {
                    var schemaScript = PublishScriptGenerator.Generate(diff, options);
                    scriptParts.Add(schemaScript);
                }

                result.Script = string.Join(Environment.NewLine + Environment.NewLine, scriptParts);

                // Count changes
                foreach (var diff in differences)
                {
                    result.ObjectsCreated += CountCreated(diff);
                    result.ObjectsAltered += CountAltered(diff);
                    result.ObjectsDropped += CountDropped(diff, options.DropObjectsNotInSource);
                }
            }

            // Step 6: Persist generated script for troubleshooting
            if (!string.IsNullOrWhiteSpace(options.OutputScriptPath))
            {
                var scriptDirectory = Path.GetDirectoryName(options.OutputScriptPath);
                if (!string.IsNullOrWhiteSpace(scriptDirectory))
                {
                    Directory.CreateDirectory(scriptDirectory);
                }

                await File.WriteAllTextAsync(options.OutputScriptPath, result.Script);
                result.ScriptFilePath = options.OutputScriptPath;
            }

            // Step 7: Execute script (if not GenerateScriptOnly)
            if (!options.GenerateScriptOnly && !string.IsNullOrWhiteSpace(result.Script))
            {
                try
                {
                    await ExecuteScriptAsync(targetConnectionString, result.Script, options.CommandTimeout);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Errors.Add($"Deployment failed: {ex.Message}");
                }
            }
            else
            {
                // Script generation only
                result.Success = true;
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Unexpected error during publish: {ex.Message}");
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Generates a deployment script without executing it.
    /// </summary>
    public async Task<PublishResult> GenerateScriptAsync(
        PgProject sourceProject,
        string targetConnectionString,
        PublishOptions? options = null)
    {
        options ??= new PublishOptions { ConnectionString = targetConnectionString };
        options.GenerateScriptOnly = true;

        return await PublishAsync(sourceProject, targetConnectionString, options);
    }

    private static bool HasDifferences(PgSchemaDiff diff)
    {
        return diff.OwnerChanged != null ||
               diff.PrivilegeChanges.Any() ||
               diff.TableDiffs.Any() ||
               diff.ViewDiffs.Any() ||
               diff.FunctionDiffs.Any() ||
               diff.TriggerDiffs.Any() ||
               diff.TypeDiffs.Any() ||
               diff.SequenceDiffs.Any();
    }

    private static int CountCreated(PgSchemaDiff diff)
    {
        int count = 0;

        count += diff.TableDiffs.Count(t => t.DefinitionChanged && t.ColumnDiffs.All(c => c.TargetDataType == null));
        count += diff.ViewDiffs.Count(v => v.TargetDefinition == null);
        count += diff.FunctionDiffs.Count(f => f.TargetDefinition == null);
        count += diff.TriggerDiffs.Count(t => t.TargetDefinition == null);
        count += diff.TypeDiffs.Count(t => t.TargetDefinition == null);
        count += diff.SequenceDiffs.Count(s => s.DefinitionChanged);

        return count;
    }

    private static int CountAltered(PgSchemaDiff diff)
    {
        int count = 0;

        count += diff.TableDiffs.Count(t => t.ColumnDiffs.Any(c => c.SourceDataType != null && c.TargetDataType != null));
        count += diff.ViewDiffs.Count(v => v.SourceDefinition != null && v.TargetDefinition != null && v.DefinitionChanged);
        count += diff.FunctionDiffs.Count(f => f.SourceDefinition != null && f.TargetDefinition != null && f.DefinitionChanged);
        count += diff.TriggerDiffs.Count(t => t.SourceDefinition != null && t.TargetDefinition != null && t.DefinitionChanged);
        count += diff.TypeDiffs.Count(t => t.SourceDefinition != null && t.TargetDefinition != null && t.DefinitionChanged);

        return count;
    }

    private static int CountDropped(PgSchemaDiff diff, bool dropObjectsNotInSource)
    {
        if (!dropObjectsNotInSource)
            return 0;

        int count = 0;

        count += diff.ViewDiffs.Count(v => v.SourceDefinition == null && v.TargetDefinition != null);
        count += diff.FunctionDiffs.Count(f => f.SourceDefinition == null && f.TargetDefinition != null);
        count += diff.TriggerDiffs.Count(t => t.SourceDefinition == null && t.TargetDefinition != null);
        count += diff.TypeDiffs.Count(t => t.SourceDefinition == null && t.TargetDefinition != null);

        return count;
    }

    private static async Task ExecuteScriptAsync(string connectionString, string script, int commandTimeout)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = script;
        command.CommandTimeout = commandTimeout;

        await command.ExecuteNonQueryAsync();
    }
}
