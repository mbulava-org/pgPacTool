using Npgsql;

namespace mbulava.PostgreSql.Dac.Tests.Integration.SampleDatabases;

/// <summary>
/// Configuration for sample database integration tests.
/// </summary>
public static class SampleDbConfig
{
    // PostgreSQL 16 configuration
    public static string Pg16Host => Environment.GetEnvironmentVariable("PG16_HOST") ?? "localhost";
    public static int Pg16Port => int.TryParse(Environment.GetEnvironmentVariable("PG16_PORT"), out var port) ? port : 5416;
    
    // PostgreSQL 17 configuration
    public static string Pg17Host => Environment.GetEnvironmentVariable("PG17_HOST") ?? "localhost";
    public static int Pg17Port => int.TryParse(Environment.GetEnvironmentVariable("PG17_PORT"), out var port) ? port : 5417;
    
    // Common configuration
    public static string User => Environment.GetEnvironmentVariable("PG_USER") ?? "postgres";
    public static string Password => Environment.GetEnvironmentVariable("PG_PASSWORD") ?? "postgres";
    
    // Sample databases
    public static readonly string[] SampleDatabases = new[]
    {
        "chinook",      // Digital media store
        "dvdrental",    // DVD rental
        "employees",    // HR database
        "lego",         // LEGO sets
        "netflix",      // Netflix shows/movies
        "pagila",       // Extended DVD rental
        "periodic_table", // Periodic table
        "titanic",      // Titanic passenger data
        "world_happiness" // World Happiness Index
    };
    
    /// <summary>
    /// Gets connection string for PostgreSQL 16 sample database.
    /// </summary>
    public static string GetPg16ConnectionString(string database)
    {
        return $"Host={Pg16Host};Port={Pg16Port};Database={database};Username={User};Password={Password};";
    }
    
    /// <summary>
    /// Gets connection string for PostgreSQL 17 sample database.
    /// </summary>
    public static string GetPg17ConnectionString(string database)
    {
        return $"Host={Pg17Host};Port={Pg17Port};Database={database};Username={User};Password={Password};";
    }
    
    /// <summary>
    /// Tests if PostgreSQL 16 container is available.
    /// </summary>
    public static bool IsPg16Available()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetPg16ConnectionString("postgres"));
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Tests if PostgreSQL 17 container is available.
    /// </summary>
    public static bool IsPg17Available()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetPg17ConnectionString("postgres"));
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Tests if a specific database exists in PostgreSQL 16.
    /// </summary>
    public static bool IsDatabaseAvailable(string database, bool usePg17 = false)
    {
        try
        {
            var connString = usePg17 
                ? GetPg17ConnectionString(database)
                : GetPg16ConnectionString(database);
                
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets PostgreSQL version from connection.
    /// </summary>
    public static string GetPostgresVersion(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        
        using var cmd = new NpgsqlCommand("SELECT version()", conn);
        var version = cmd.ExecuteScalar()?.ToString() ?? "Unknown";
        
        return version;
    }
}
