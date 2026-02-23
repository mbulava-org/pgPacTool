using mbulava.PostgreSql.Dac.Models;
using System.Text;

namespace mbulava.PostgreSql.Dac.Deployment;

/// <summary>
/// Manages pre-deployment and post-deployment scripts.
/// Handles loading, ordering, validation, and execution preparation.
/// </summary>
public class PrePostDeploymentScriptManager
{
    private readonly string _projectRoot;

    /// <summary>
    /// Initializes a new instance of the deployment script manager.
    /// </summary>
    /// <param name="projectRoot">Root directory of the project</param>
    public PrePostDeploymentScriptManager(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        _projectRoot = projectRoot;
    }

    /// <summary>
    /// Loads a deployment script from the file system.
    /// </summary>
    /// <param name="script">Script metadata with FilePath</param>
    /// <returns>Script with content loaded</returns>
    public async Task<DeploymentScript> LoadScriptAsync(DeploymentScript script)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentException.ThrowIfNullOrWhiteSpace(script.FilePath);

        var fullPath = Path.IsPathRooted(script.FilePath)
            ? script.FilePath
            : Path.Combine(_projectRoot, script.FilePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Deployment script not found: {script.FilePath}", fullPath);
        }

        script.Content = await File.ReadAllTextAsync(fullPath);
        return script;
    }

    /// <summary>
    /// Loads all deployment scripts from the file system.
    /// </summary>
    /// <param name="scripts">List of script metadata</param>
    /// <returns>Scripts with content loaded</returns>
    public async Task<List<DeploymentScript>> LoadScriptsAsync(List<DeploymentScript> scripts)
    {
        ArgumentNullException.ThrowIfNull(scripts);

        var tasks = scripts.Select(LoadScriptAsync);
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// Validates that all scripts exist and are accessible.
    /// </summary>
    /// <param name="scripts">Scripts to validate</param>
    /// <returns>List of validation errors (empty if all valid)</returns>
    public List<string> ValidateScripts(List<DeploymentScript> scripts)
    {
        ArgumentNullException.ThrowIfNull(scripts);

        var errors = new List<string>();

        foreach (var script in scripts)
        {
            if (string.IsNullOrWhiteSpace(script.FilePath))
            {
                errors.Add($"Script has empty FilePath (Type: {script.Type}, Order: {script.Order})");
                continue;
            }

            var fullPath = Path.IsPathRooted(script.FilePath)
                ? script.FilePath
                : Path.Combine(_projectRoot, script.FilePath);

            if (!File.Exists(fullPath))
            {
                errors.Add($"Script file not found: {script.FilePath}");
            }
        }

        // Check for duplicate orders within same type
        var duplicateOrders = scripts
            .GroupBy(s => new { s.Type, s.Order })
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.Type} scripts with duplicate order {g.Key.Order}: {string.Join(", ", g.Select(s => s.FilePath))}");

        errors.AddRange(duplicateOrders);

        return errors;
    }

    /// <summary>
    /// Orders scripts by their execution order.
    /// </summary>
    /// <param name="scripts">Scripts to order</param>
    /// <returns>Scripts sorted by Order property</returns>
    public static List<DeploymentScript> OrderScripts(List<DeploymentScript> scripts)
    {
        ArgumentNullException.ThrowIfNull(scripts);
        return scripts.OrderBy(s => s.Order).ToList();
    }

    /// <summary>
    /// Combines multiple scripts into a single SQL script.
    /// </summary>
    /// <param name="scripts">Scripts to combine</param>
    /// <param name="includeComments">Whether to include script file comments</param>
    /// <returns>Combined SQL script</returns>
    public static string CombineScripts(
        List<DeploymentScript> scripts,
        bool includeComments = true)
    {
        ArgumentNullException.ThrowIfNull(scripts);

        var sb = new StringBuilder();
        var orderedScripts = OrderScripts(scripts);

        foreach (var script in orderedScripts)
        {
            if (string.IsNullOrWhiteSpace(script.Content))
            {
                continue;
            }

            if (includeComments)
            {
                sb.AppendLine("-- ============================================================================");
                sb.AppendLine($"-- {script.Type} Script: {Path.GetFileName(script.FilePath)}");
                if (!string.IsNullOrWhiteSpace(script.Description))
                {
                    sb.AppendLine($"-- Description: {script.Description}");
                }
                sb.AppendLine($"-- Order: {script.Order}");
                sb.AppendLine($"-- Transactional: {script.Transactional}");
                sb.AppendLine("-- ============================================================================");
                sb.AppendLine();
            }

            sb.AppendLine(script.Content.TrimEnd());
            sb.AppendLine();
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Applies SQLCMD variables to all scripts.
    /// </summary>
    /// <param name="scripts">Scripts to process</param>
    /// <param name="variables">Variables to replace</param>
    /// <returns>Scripts with variables replaced</returns>
    public static List<DeploymentScript> ApplyVariables(
        List<DeploymentScript> scripts,
        List<SqlCmdVariable> variables)
    {
        ArgumentNullException.ThrowIfNull(scripts);
        ArgumentNullException.ThrowIfNull(variables);

        var processed = new List<DeploymentScript>();

        foreach (var script in scripts)
        {
            var copy = new DeploymentScript
            {
                FilePath = script.FilePath,
                Order = script.Order,
                Type = script.Type,
                Content = SqlCmdVariableParser.ReplaceVariables(
                    script.Content,
                    variables,
                    throwOnUndefined: true),
                Transactional = script.Transactional,
                Description = script.Description
            };

            processed.Add(copy);
        }

        return processed;
    }

    /// <summary>
    /// Discovers deployment scripts in a directory.
    /// </summary>
    /// <param name="directory">Directory to search</param>
    /// <param name="type">Type of scripts to discover</param>
    /// <param name="searchPattern">File search pattern (default: *.sql)</param>
    /// <param name="recursive">Whether to search subdirectories</param>
    /// <returns>List of discovered scripts</returns>
    public List<DeploymentScript> DiscoverScripts(
        string directory,
        DeploymentScriptType type,
        string searchPattern = "*.sql",
        bool recursive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var fullPath = Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(_projectRoot, directory);

        if (!Directory.Exists(fullPath))
        {
            return new List<DeploymentScript>();
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(fullPath, searchPattern, searchOption);

        var scripts = new List<DeploymentScript>();
        var order = 0;

        foreach (var file in files.OrderBy(f => f))
        {
            var relativePath = Path.GetRelativePath(_projectRoot, file);

            scripts.Add(new DeploymentScript
            {
                FilePath = relativePath,
                Order = order++,
                Type = type,
                Transactional = true, // Default to transactional
                Description = $"Auto-discovered: {Path.GetFileName(file)}"
            });
        }

        return scripts;
    }

    /// <summary>
    /// Validates script content for common issues.
    /// </summary>
    /// <param name="script">Script to validate</param>
    /// <returns>List of warnings (empty if no issues found)</returns>
    public static List<string> ValidateScriptContent(DeploymentScript script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(script.Content))
        {
            warnings.Add($"Script '{script.FilePath}' is empty");
            return warnings;
        }

        var content = script.Content;

        // Check for transaction control in transactional scripts
        if (script.Transactional)
        {
            if (content.Contains("BEGIN;", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("BEGIN TRANSACTION", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Script '{script.FilePath}' is marked as transactional but contains explicit BEGIN statement. This may cause nested transaction issues.");
            }

            if (content.Contains("COMMIT;", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("ROLLBACK;", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Script '{script.FilePath}' is marked as transactional but contains explicit COMMIT/ROLLBACK. This may interfere with automatic transaction management.");
            }
        }

        // Check for unreplaced variables
        var unreplacedVars = SqlCmdVariableParser.ExtractVariableNames(content);
        if (unreplacedVars.Count > 0)
        {
            warnings.Add($"Script '{script.FilePath}' contains SQLCMD variables that need to be replaced: {string.Join(", ", unreplacedVars.Select(v => $"$({v})"))}");
        }

        return warnings;
    }
}
