using mbulava.PostgreSql.Dac.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace mbulava.PostgreSql.Dac.Deployment;

/// <summary>
/// Parses and replaces SQLCMD-style variables in SQL scripts.
/// Supports $(VariableName) syntax similar to SQL Server SQLCMD.
/// </summary>
public partial class SqlCmdVariableParser
{
    // Regex to match $(VariableName) pattern
    // Matches: $(VarName), $(Database_Name), $(My.Var123)
    // Does not match: $$(Escaped), incomplete $(, or $(
    [GeneratedRegex(@"\$\(([a-zA-Z_][a-zA-Z0-9_.]*)\)", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    /// <summary>
    /// Extracts all variable references from a script.
    /// </summary>
    /// <param name="script">SQL script content</param>
    /// <returns>List of unique variable names found (without $() wrapper)</returns>
    public static List<string> ExtractVariableNames(string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        var matches = VariablePattern().Matches(script);
        return matches
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(v => v)
            .ToList();
    }

    /// <summary>
    /// Validates that all variables in the script have values defined.
    /// </summary>
    /// <param name="script">SQL script content</param>
    /// <param name="variables">Available variables</param>
    /// <returns>List of undefined variable names</returns>
    public static List<string> ValidateVariables(string script, List<SqlCmdVariable> variables)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(variables);

        var requiredVars = ExtractVariableNames(script);
        var definedVars = variables
            .Where(v => v.EffectiveValue != null)
            .Select(v => v.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requiredVars
            .Where(v => !definedVars.Contains(v))
            .ToList();
    }

    /// <summary>
    /// Replaces all variable references in a script with their values.
    /// </summary>
    /// <param name="script">SQL script content</param>
    /// <param name="variables">Variables to use for replacement</param>
    /// <param name="throwOnUndefined">Whether to throw exception if variable is undefined</param>
    /// <returns>Script with variables replaced</returns>
    /// <exception cref="InvalidOperationException">Thrown when variable is undefined and throwOnUndefined is true</exception>
    public static string ReplaceVariables(
        string script,
        List<SqlCmdVariable> variables,
        bool throwOnUndefined = true)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(variables);

        // Build lookup dictionary for fast access
        var varLookup = variables
            .Where(v => v.EffectiveValue != null)
            .ToDictionary(
                v => v.Name,
                v => v.EffectiveValue!,
                StringComparer.OrdinalIgnoreCase);

        // Track undefined variables
        var undefinedVars = new List<string>();

        var result = VariablePattern().Replace(script, match =>
        {
            var varName = match.Groups[1].Value;

            if (varLookup.TryGetValue(varName, out var value))
            {
                return value;
            }

            undefinedVars.Add(varName);
            return match.Value; // Keep original if not found
        });

        if (undefinedVars.Count > 0 && throwOnUndefined)
        {
            throw new InvalidOperationException(
                $"Undefined SQLCMD variables: {string.Join(", ", undefinedVars.Distinct())}");
        }

        return result;
    }

    /// <summary>
    /// Replaces variables in script with validation feedback.
    /// </summary>
    /// <param name="script">SQL script content</param>
    /// <param name="variables">Variables to use for replacement</param>
    /// <returns>Result containing processed script and any warnings</returns>
    public static VariableReplacementResult ReplaceVariablesWithResult(
        string script,
        List<SqlCmdVariable> variables)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(variables);

        var result = new VariableReplacementResult
        {
            OriginalScript = script
        };

        try
        {
            // Validate first
            var undefinedVars = ValidateVariables(script, variables);
            if (undefinedVars.Count > 0)
            {
                result.Warnings.AddRange(undefinedVars.Select(v =>
                    $"Variable '$({v})' is not defined and will remain unchanged"));
            }

            // Replace (don't throw on undefined)
            result.ProcessedScript = ReplaceVariables(script, variables, throwOnUndefined: false);
            result.Success = true;

            // Track which variables were used
            result.VariablesUsed.AddRange(
                ExtractVariableNames(script)
                    .Where(v => variables.Any(var => 
                        var.Name.Equals(v, StringComparison.OrdinalIgnoreCase) && 
                        var.EffectiveValue != null)));
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Warnings.Add($"Error processing variables: {ex.Message}");
            result.ProcessedScript = script; // Return original on error
        }

        return result;
    }

    /// <summary>
    /// Escapes a variable reference to prevent replacement.
    /// Converts $(VarName) to $$(VarName).
    /// </summary>
    public static string EscapeVariable(string variableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);
        return $"$$({variableName})";
    }

    /// <summary>
    /// Unescapes variable references in a script.
    /// Converts $$(VarName) back to $(VarName).
    /// </summary>
    public static string UnescapeVariables(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        return script.Replace("$$(", "$(");
    }
}

/// <summary>
/// Result of variable replacement operation.
/// </summary>
public class VariableReplacementResult
{
    /// <summary>
    /// Original script before replacement.
    /// </summary>
    public string OriginalScript { get; set; } = string.Empty;

    /// <summary>
    /// Processed script with variables replaced.
    /// </summary>
    public string ProcessedScript { get; set; } = string.Empty;

    /// <summary>
    /// Whether replacement was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Warning messages (e.g., undefined variables).
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Names of variables that were actually used in the script.
    /// </summary>
    public List<string> VariablesUsed { get; set; } = new();
}
