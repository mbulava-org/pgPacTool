using mbulava.PostgreSql.Dac.Models;
using Npgsql;

namespace mbulava.PostgreSql.Dac.Publish;

/// <summary>
/// Resolves target database publish context and applies database-scoped SQLCMD metadata.
/// </summary>
public sealed class PublishTargetDatabaseContextService
{
    /// <summary>
    /// Applies the effective target database context to the source project and publish options.
    /// </summary>
    /// <param name="sourceProject">Source project being published.</param>
    /// <param name="targetConnectionString">Target database connection string.</param>
    /// <param name="options">Publish options to update.</param>
    public void Apply(PgProject sourceProject, string targetConnectionString, PublishOptions options)
    {
        ArgumentNullException.ThrowIfNull(sourceProject);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetConnectionString);
        ArgumentNullException.ThrowIfNull(options);

        options.SourceDatabase ??= sourceProject.DatabaseName;

        if (string.IsNullOrWhiteSpace(options.TargetDatabase))
        {
            options.TargetDatabase = GetDatabaseName(targetConnectionString);
        }

        if (string.IsNullOrWhiteSpace(options.TargetDatabase))
        {
            return;
        }

        sourceProject.DatabaseName = options.TargetDatabase;
        SetOrAddVariable(options.Variables, "DatabaseName", options.TargetDatabase);
        SetOrAddVariable(options.Variables, "TargetDatabase", options.TargetDatabase);

        if (!string.IsNullOrWhiteSpace(options.SourceDatabase))
        {
            SetOrAddVariable(options.Variables, "SourceDatabase", options.SourceDatabase);
        }
    }

    /// <summary>
    /// Builds a connection string that targets the effective publish database.
    /// </summary>
    /// <param name="connectionString">Base target connection string.</param>
    /// <param name="targetDatabase">Optional target database override.</param>
    /// <returns>The effective target connection string.</returns>
    public string BuildTargetConnectionString(string connectionString, string? targetDatabase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = targetDatabase
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Extracts the database name from a PostgreSQL connection string.
    /// </summary>
    /// <param name="connectionString">Connection string to inspect.</param>
    /// <returns>The database name when present; otherwise null.</returns>
    public string? GetDatabaseName(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return string.IsNullOrWhiteSpace(builder.Database) ? null : builder.Database;
    }

    private static void SetOrAddVariable(List<SqlCmdVariable> variables, string name, string value)
    {
        var existing = variables.FirstOrDefault(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Value = value;
            return;
        }

        variables.Add(new SqlCmdVariable
        {
            Name = name,
            Value = value
        });
    }
}
