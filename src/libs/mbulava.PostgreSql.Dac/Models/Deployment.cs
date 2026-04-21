using System.Text.Json.Serialization;
using mbulava.PostgreSql.Dac.Compare;

namespace mbulava.PostgreSql.Dac.Models;

/// <summary>
/// Represents a SQLCMD-style variable used in deployment scripts.
/// Format: $(VariableName) in scripts will be replaced with Value.
/// </summary>
public class SqlCmdVariable
{
    /// <summary>
    /// Variable name (without $() wrapper).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default value for the variable.
    /// </summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Current value (overrides default if set).
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Gets the effective value (Value if set, otherwise DefaultValue).
    /// </summary>
    [JsonIgnore]
    public string? EffectiveValue => Value ?? DefaultValue;
}

/// <summary>
/// Represents a pre-deployment or post-deployment script.
/// </summary>
public class DeploymentScript
{
    /// <summary>
    /// Script file path (relative to project root).
    /// </summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Script execution order (lower executes first).
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <summary>
    /// Script type (PreDeployment or PostDeployment).
    /// </summary>
    [JsonPropertyName("type")]
    public DeploymentScriptType Type { get; set; }

    /// <summary>
    /// Script content (loaded from file).
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether to run this script in a transaction.
    /// </summary>
    [JsonPropertyName("transactional")]
    public bool Transactional { get; set; } = true;

    /// <summary>
    /// Optional description of what this script does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Type of deployment script.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeploymentScriptType
{
    /// <summary>
    /// Runs before schema changes (e.g., backup data, disable triggers).
    /// </summary>
    PreDeployment,

    /// <summary>
    /// Runs after schema changes (e.g., data migration, enable triggers, refresh views).
    /// </summary>
    PostDeployment
}

/// <summary>
/// Options for publishing/deploying database changes.
/// </summary>
public class PublishOptions
{
    /// <summary>
    /// Target database connection string.
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Target database name (optional, overrides connection string).
    /// </summary>
    [JsonPropertyName("targetDatabase")]
    public string? TargetDatabase { get; set; }

    /// <summary>
    /// Source database name before target override is applied.
    /// </summary>
    [JsonPropertyName("sourceDatabase")]
    public string? SourceDatabase { get; set; }

    /// <summary>
    /// Ownership handling policy during publish/deploy.
    /// </summary>
    [JsonPropertyName("ownershipMode")]
    public OwnershipMode OwnershipMode { get; set; } = OwnershipMode.Ignore;

    /// <summary>
    /// Whether to generate a script file instead of executing directly.
    /// </summary>
    [JsonPropertyName("generateScriptOnly")]
    public bool GenerateScriptOnly { get; set; } = false;

    /// <summary>
    /// Output path for generated script (if GenerateScriptOnly is true).
    /// </summary>
    [JsonPropertyName("outputScriptPath")]
    public string? OutputScriptPath { get; set; }

    /// <summary>
    /// SQLCMD variables for script replacement.
    /// </summary>
    [JsonPropertyName("variables")]
    public List<SqlCmdVariable> Variables { get; set; } = new();

    /// <summary>
    /// Pre-deployment scripts to execute.
    /// </summary>
    [JsonPropertyName("preDeploymentScripts")]
    public List<DeploymentScript> PreDeploymentScripts { get; set; } = new();

    /// <summary>
    /// Post-deployment scripts to execute.
    /// </summary>
    [JsonPropertyName("postDeploymentScripts")]
    public List<DeploymentScript> PostDeploymentScripts { get; set; } = new();

    /// <summary>
    /// Whether to include DROP statements for objects that exist in target but not in source.
    /// </summary>
    [JsonPropertyName("dropObjectsNotInSource")]
    public bool DropObjectsNotInSource { get; set; } = false;

    /// <summary>
    /// Whether to backup the database before deployment.
    /// </summary>
    [JsonPropertyName("backupBeforeDeployment")]
    public bool BackupBeforeDeployment { get; set; } = false;

    /// <summary>
    /// Backup file path (if BackupBeforeDeployment is true).
    /// </summary>
    [JsonPropertyName("backupPath")]
    public string? BackupPath { get; set; }

    /// <summary>
    /// Whether to wrap deployment in a transaction.
    /// </summary>
    [JsonPropertyName("transactional")]
    public bool Transactional { get; set; } = true;

    /// <summary>
    /// Comparison options for schema differences.
    /// </summary>
    [JsonPropertyName("compareOptions")]
    public CompareOptions CompareOptions { get; set; } = new();

    /// <summary>
    /// Whether to include comments in generated scripts.
    /// </summary>
    [JsonPropertyName("includeComments")]
    public bool IncludeComments { get; set; } = true;

    /// <summary>
    /// Command timeout in seconds (0 = infinite).
    /// </summary>
    [JsonPropertyName("commandTimeout")]
    public int CommandTimeout { get; set; } = 300; // 5 minutes default
}

/// <summary>
/// Controls how database and object ownership are handled during publish/deploy.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OwnershipMode
{
    /// <summary>
    /// Ignore database and object ownership differences.
    /// </summary>
    Ignore,

    /// <summary>
    /// Enforce database and object ownership differences from source.
    /// </summary>
    Enforce
}

/// <summary>
/// Result of a deployment operation.
/// </summary>
public class PublishResult
{
    /// <summary>
    /// Whether the deployment was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Generated SQL script (always populated).
    /// </summary>
    [JsonPropertyName("script")]
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// Error messages (if any).
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Warning messages (if any).
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Number of objects created.
    /// </summary>
    [JsonPropertyName("objectsCreated")]
    public int ObjectsCreated { get; set; }

    /// <summary>
    /// Number of objects altered.
    /// </summary>
    [JsonPropertyName("objectsAltered")]
    public int ObjectsAltered { get; set; }

    /// <summary>
    /// Number of objects dropped.
    /// </summary>
    [JsonPropertyName("objectsDropped")]
    public int ObjectsDropped { get; set; }

    /// <summary>
    /// Deployment execution time.
    /// </summary>
    [JsonPropertyName("executionTime")]
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Path to the generated deployment script file.
    /// </summary>
    [JsonPropertyName("scriptFilePath")]
    public string? ScriptFilePath { get; set; }
}
