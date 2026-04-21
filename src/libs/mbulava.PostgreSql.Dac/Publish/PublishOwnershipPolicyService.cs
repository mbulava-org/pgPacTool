using mbulava.PostgreSql.Dac.Models;

namespace mbulava.PostgreSql.Dac.Publish;

/// <summary>
/// Applies publish-time ownership behavior and validates explicit source ownership declarations.
/// </summary>
public sealed class PublishOwnershipPolicyService
{
    /// <summary>
    /// Applies the requested ownership policy to publish comparison options.
    /// </summary>
    /// <param name="options">Publish options to mutate.</param>
    public void Apply(PublishOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.CompareOptions.CompareOwners = options.OwnershipMode == OwnershipMode.Enforce;
    }

    /// <summary>
    /// Validates that every explicit owner referenced in source is also defined as a source role.
    /// </summary>
    /// <param name="sourceProject">Source project to validate.</param>
    /// <returns>Validation errors for undefined explicit owners.</returns>
    public List<string> ValidateExplicitOwners(PgProject sourceProject)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);

        var definedRoles = sourceProject.Roles
            .Select(role => role.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();

        foreach (var schema in sourceProject.Schemas)
        {
            ValidateOwner(errors, definedRoles, $"schema '{schema.Name}'", schema.Owner);

            foreach (var table in schema.Tables)
            {
                ValidateOwner(errors, definedRoles, $"table '{schema.Name}.{table.Name}'", table.Owner);
            }

            foreach (var view in schema.Views)
            {
                ValidateOwner(errors, definedRoles, $"view '{schema.Name}.{view.Name}'", view.Owner);
            }

            foreach (var function in schema.Functions)
            {
                ValidateOwner(errors, definedRoles, $"function '{function.Name}'", function.Owner);
            }

            foreach (var type in schema.Types)
            {
                ValidateOwner(errors, definedRoles, $"type '{schema.Name}.{type.Name}'", type.Owner);
            }

            foreach (var sequence in schema.Sequences)
            {
                ValidateOwner(errors, definedRoles, $"sequence '{schema.Name}.{sequence.Name}'", sequence.Owner);
            }

            foreach (var trigger in schema.Triggers)
            {
                ValidateOwner(errors, definedRoles, $"trigger '{schema.Name}.{trigger.TableName}.{trigger.Name}'", trigger.Owner);
            }
        }

        return errors;
    }

    private static void ValidateOwner(List<string> errors, HashSet<string> definedRoles, string objectDisplayName, string? owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            return;
        }

        if (!definedRoles.Contains(owner))
        {
            errors.Add($"Explicit owner '{owner}' for {objectDisplayName} is not defined in the source project's roles.");
        }
    }
}
