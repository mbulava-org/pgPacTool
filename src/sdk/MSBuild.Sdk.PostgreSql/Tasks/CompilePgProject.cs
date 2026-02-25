using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using mbulava.PostgreSql.Dac.Compile;
using System;
using System.IO;
using OutputFormat = mbulava.PostgreSql.Dac.Compile.OutputFormat;

namespace MSBuild.Sdk.PostgreSql.Tasks;

/// <summary>
/// MSBuild task that compiles a PostgreSQL database project to a .pgpac file.
/// </summary>
public class CompilePgProject : Task
{
    /// <summary>
    /// The .csproj project file to compile.
    /// </summary>
    [Required]
    public string ProjectFile { get; set; } = string.Empty;

    /// <summary>
    /// Output path for the .pgpac file.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Output format: pgpac or json.
    /// </summary>
    public string OutputFormatString { get; set; } = "pgpac";

    /// <summary>
    /// Database name for the project.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to validate the project during build.
    /// </summary>
    public bool ValidateOnBuild { get; set; } = true;

    /// <summary>
    /// SQL files in the project.
    /// </summary>
    public ITaskItem[] SqlFiles { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Pre-deployment scripts.
    /// </summary>
    public ITaskItem[] PreDeploymentScripts { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Post-deployment scripts.
    /// </summary>
    public ITaskItem[] PostDeploymentScripts { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Output parameter indicating success.
    /// </summary>
    [Output]
    public bool Success { get; set; }

    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.High, $"🔨 Compiling PostgreSQL project: {DatabaseName}");
            Log.LogMessage(MessageImportance.Normal, $"   📁 Project: {ProjectFile}");
            Log.LogMessage(MessageImportance.Normal, $"   📄 SQL files: {SqlFiles.Length}");

            // Validate inputs
            if (!File.Exists(ProjectFile))
            {
                Log.LogError($"Project file not found: {ProjectFile}");
                Success = false;
                return false;
            }

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Parse output format
            var format = OutputFormatString.ToLowerInvariant() == "json" 
                ? OutputFormat.Json 
                : OutputFormat.DacPac;

            // Load and compile the project
            var loader = new CsprojProjectLoader(ProjectFile);
            
            Log.LogMessage(MessageImportance.Normal, "   ⚙️  Loading project...");
            var project = loader.LoadProjectAsync().GetAwaiter().GetResult();
            
            Log.LogMessage(MessageImportance.Normal, $"   ✅ Loaded {project.Schemas.Count} schema(s)");

            // Report object counts
            foreach (var schema in project.Schemas)
            {
                var objectCount = schema.Tables.Count + schema.Views.Count + schema.Functions.Count + 
                                schema.Types.Count + schema.Sequences.Count + schema.Triggers.Count;
                Log.LogMessage(MessageImportance.Normal, 
                    $"   📊 Schema '{schema.Name}': {objectCount} objects");
            }

            // Validate if requested
            if (ValidateOnBuild)
            {
                Log.LogMessage(MessageImportance.Normal, "   🔍 Validating project...");
                var compiler = new ProjectCompiler();
                var result = compiler.Compile(project);

                if (result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        Log.LogError(null, error.Code, null, error.Location ?? "", 0, 0, 0, 0, error.Message);
                    }
                    Success = false;
                    return false;
                }

                foreach (var warning in result.Warnings)
                {
                    Log.LogWarning(null, warning.Code, null, warning.Location ?? "", 0, 0, 0, 0, warning.Message);
                }

                Log.LogMessage(MessageImportance.Normal, 
                    $"   ✅ Validation passed: {result.DeploymentOrder.Count} objects in dependency order");
            }

            // Generate output
            Log.LogMessage(MessageImportance.Normal, $"   📦 Generating {format} output...");
            var actualOutput = loader.CompileAndGenerateOutputAsync(OutputPath, format).GetAwaiter().GetResult();

            var fileInfo = new FileInfo(actualOutput);
            Log.LogMessage(MessageImportance.High, 
                $"✅ Successfully created: {Path.GetFileName(actualOutput)} ({fileInfo.Length:N0} bytes)");

            Success = true;
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            Success = false;
            return false;
        }
    }
}
