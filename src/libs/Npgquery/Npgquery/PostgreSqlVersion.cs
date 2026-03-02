namespace Npgquery;

/// <summary>
/// Represents a PostgreSQL major version for parsing queries.
/// Each version corresponds to a specific libpg_query implementation.
/// </summary>
public enum PostgreSqlVersion
{
    /// <summary>
    /// PostgreSQL 16.x - Uses libpg_query based on PostgreSQL 16 parser
    /// </summary>
    Postgres16 = 16,

    /// <summary>
    /// PostgreSQL 17.x - Uses libpg_query based on PostgreSQL 17 parser
    /// </summary>
    Postgres17 = 17
}

/// <summary>
/// Extension methods for PostgreSqlVersion enum
/// </summary>
public static class PostgreSqlVersionExtensions
{
    /// <summary>
    /// Gets the library name suffix for the PostgreSQL version
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Library name suffix (e.g., "16", "17")</returns>
    public static string ToLibrarySuffix(this PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres16 => "16",
            PostgreSqlVersion.Postgres17 => "17",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown PostgreSQL version")
        };
    }

    /// <summary>
    /// Gets the human-readable version string
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Version string (e.g., "PostgreSQL 16", "PostgreSQL 17")</returns>
    public static string ToVersionString(this PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres16 => "PostgreSQL 16",
            PostgreSqlVersion.Postgres17 => "PostgreSQL 17",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown PostgreSQL version")
        };
    }

    /// <summary>
    /// Gets the numeric version code used in the libpg_query API
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Numeric version code (e.g., 160000, 170000)</returns>
    public static int ToVersionNumber(this PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres16 => 160000,
            PostgreSqlVersion.Postgres17 => 170000,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown PostgreSQL version")
        };
    }

    /// <summary>
    /// Gets the major version number
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Major version number (e.g., 16, 17)</returns>
    public static int GetMajorVersion(this PostgreSqlVersion version)
    {
        return (int)version;
    }
}
