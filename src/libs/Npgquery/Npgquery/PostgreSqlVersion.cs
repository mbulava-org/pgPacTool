namespace Npgquery;

/// <summary>
/// Represents a PostgreSQL major version for parsing queries.
/// Each version corresponds to a specific libpg_query implementation.
/// </summary>
public enum PostgreSqlVersion
{
    /// <summary>
    /// PostgreSQL 15.x - Uses libpg_query based on PostgreSQL 15 parser
    /// </summary>
    Postgres15 = 15,

    /// <summary>
    /// PostgreSQL 16.x - Uses libpg_query based on PostgreSQL 16 parser
    /// </summary>
    Postgres16 = 16,

    /// <summary>
    /// PostgreSQL 17.x - Uses libpg_query based on PostgreSQL 17 parser
    /// </summary>
    Postgres17 = 17,

    /// <summary>
    /// PostgreSQL 18.x - Uses libpg_query based on PostgreSQL 18 parser
    /// </summary>
    Postgres18 = 18
}

/// <summary>
/// Extension methods for PostgreSqlVersion enum
/// </summary>
public static class PostgreSqlVersionExtensions
{
    /// <summary>
    /// Gets the PostgreSQL versions currently supported by Npgquery.
    /// </summary>
    /// <returns>The supported PostgreSQL versions in ascending order.</returns>
    public static IReadOnlyList<PostgreSqlVersion> GetSupportedVersions()
    {
        return
        [
            PostgreSqlVersion.Postgres15,
            PostgreSqlVersion.Postgres16,
            PostgreSqlVersion.Postgres17,
            PostgreSqlVersion.Postgres18
        ];
    }

    /// <summary>
    /// Gets the library name suffix for the PostgreSQL version
    /// </summary>
    /// <param name="version">The PostgreSQL version</param>
    /// <returns>Library name suffix (e.g., "16", "17")</returns>
    /// <exception cref="ArgumentOutOfRangeException">When version is not supported</exception>
    public static string ToLibrarySuffix(this PostgreSqlVersion version)
    {
        return version switch
        {
            PostgreSqlVersion.Postgres15 => "15",
            PostgreSqlVersion.Postgres16 => "16",
            PostgreSqlVersion.Postgres17 => "17",
            PostgreSqlVersion.Postgres18 => "18",
            _ => ((int)version).ToString() // Allow any version number for forward compatibility
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
            PostgreSqlVersion.Postgres15 => "PostgreSQL 15",
            PostgreSqlVersion.Postgres16 => "PostgreSQL 16",
            PostgreSqlVersion.Postgres17 => "PostgreSQL 17",
            PostgreSqlVersion.Postgres18 => "PostgreSQL 18",
            _ => $"PostgreSQL {(int)version}" // Forward compatibility
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
            PostgreSqlVersion.Postgres15 => 150000,
            PostgreSqlVersion.Postgres16 => 160000,
            PostgreSqlVersion.Postgres17 => 170000,
            PostgreSqlVersion.Postgres18 => 180000,
            _ => (int)version * 10000 // Forward compatibility
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

    /// <summary>
    /// Determines whether the PostgreSQL version supports JSON_TABLE.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 17 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsJsonTable(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres17;
    }

    /// <summary>
    /// Determines whether the PostgreSQL version supports named <c>NOT NULL</c> constraints.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 16 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsNamedNotNullConstraints(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres16;
    }

    /// <summary>
    /// Determines whether the PostgreSQL version supports utility query normalization.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 16 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsNormalizeUtility(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres16;
    }

    /// <summary>
    /// Determines whether the PostgreSQL version supports utility statement detection APIs.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 17 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsUtilityStatementDetection(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres17;
    }

    /// <summary>
    /// Determines whether the PostgreSQL version supports summary APIs.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 17 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsSummaryApi(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres17;
    }

    /// <summary>
    /// Determines whether the PostgreSQL version supports virtual generated columns.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 17 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsVirtualGeneratedColumns(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres17;
    }

    /// <summary>
    /// Determines whether the PostgreSQL version supports <c>WITHOUT OVERLAPS</c> constraints.
    /// </summary>
    /// <param name="version">The PostgreSQL version.</param>
    /// <returns><see langword="true"/> for PostgreSQL 18 and later; otherwise, <see langword="false"/>.</returns>
    public static bool SupportsWithoutOverlaps(this PostgreSqlVersion version)
    {
        return version >= PostgreSqlVersion.Postgres18;
    }
}
