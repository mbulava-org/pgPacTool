using mbulava.PostgreSql.Dac.Models;
using System.Text;

namespace mbulava.PostgreSql.Dac.Compile;

/// <summary>
/// Formats compiled project objects for CLI output.
/// </summary>
public static class CompileObjectTreeFormatter
{
    /// <summary>
    /// Formats the compiled project object tree including source locations when available.
    /// </summary>
    /// <param name="project">Project to render.</param>
    /// <returns>Formatted CLI output.</returns>
    public static string Format(PgProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var builder = new StringBuilder();
        builder.AppendLine("🌳 Compiled object tree:");

        foreach (var schema in project.Schemas.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"   Schema: {schema.Name}");

            foreach (var sequence in schema.Sequences.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"   Sequence: {schema.Name}.{sequence.Name}{FormatSourceLocation(project, $"{schema.Name}.{sequence.Name}")}");
            }

            foreach (var table in schema.Tables.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"   Table: {schema.Name}.{table.Name}{FormatSourceLocation(project, $"{schema.Name}.{table.Name}")}");
            }

            foreach (var view in schema.Views.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                var viewType = view.IsMaterialized ? "Materialized View" : "View";
                builder.AppendLine($"   {viewType}: {schema.Name}.{view.Name}{FormatSourceLocation(project, $"{schema.Name}.{view.Name}")}");
            }

            foreach (var function in schema.Functions.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                var objectType = function.Definition.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase)
                    ? "Procedure"
                    : "Function";
                var functionDisplayName = function.Name.Contains('.', StringComparison.OrdinalIgnoreCase)
                    ? function.Name
                    : $"{schema.Name}.{function.Name}";
                builder.AppendLine($"   {objectType}: {functionDisplayName}{FormatSourceLocation(project, functionDisplayName, function.Name)}");
            }

            foreach (var trigger in schema.Triggers.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"   Trigger: {schema.Name}.{trigger.TableName}.{trigger.Name}{FormatSourceLocation(project, $"{schema.Name}.{trigger.Name}")}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatSourceLocation(PgProject project, params string[] objectNames)
    {
        var sourceLocation = objectNames
            .Select(project.GetSourceLocation)
            .FirstOrDefault(location => !string.IsNullOrWhiteSpace(location));

        return string.IsNullOrWhiteSpace(sourceLocation) ? string.Empty : $" [{sourceLocation}]";
    }
}
