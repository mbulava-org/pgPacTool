// Version Enforcement for PostgreSQL 16+

using Npgsql;
using System;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    /// <summary>
    /// Enforces minimum PostgreSQL version requirement
    /// </summary>
    public static class PostgreSqlVersionChecker
    {
        /// <summary>
        /// Minimum supported PostgreSQL major version
        /// </summary>
        public const int MinimumSupportedVersion = 16;
        
        /// <summary>
        /// Validates that the connected PostgreSQL instance meets minimum version requirements
        /// </summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <returns>Full version string (e.g., "16.1")</returns>
        /// <exception cref="NotSupportedException">Thrown when PostgreSQL version is below 16</exception>
        public static async Task<string> ValidateAndGetVersionAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand("SHOW server_version;", connection);
            var versionString = (string)await command.ExecuteScalarAsync() 
                ?? throw new InvalidOperationException("Could not determine PostgreSQL version");
            
            // Parse version (format: "16.1 (Debian 16.1-1.pgdg120+1)" or just "16.1")
            var versionPart = versionString.Split(' ')[0];
            var versionNumbers = versionPart.Split('.');
            
            if (versionNumbers.Length == 0 || !int.TryParse(versionNumbers[0], out int majorVersion))
            {
                throw new InvalidOperationException($"Could not parse PostgreSQL version: {versionString}");
            }
            
            // Enforce minimum version
            if (majorVersion < MinimumSupportedVersion)
            {
                throw new NotSupportedException(
                    $"PostgreSQL {majorVersion} is not supported. pgPacTool requires PostgreSQL {MinimumSupportedVersion} or higher.\n\n" +
                    $"Your PostgreSQL version: {versionString}\n" +
                    $"Minimum required version: {MinimumSupportedVersion}.0\n\n" +
                    "To upgrade your PostgreSQL instance:\n" +
                    "1. Backup your data: pg_dump mydb > backup.sql\n" +
                    "2. Install PostgreSQL 16: https://www.postgresql.org/download/\n" +
                    "3. Restore your data: psql -d mydb < backup.sql\n\n" +
                    "For help: https://github.com/mbulava-org/pgPacTool/wiki/postgresql-upgrade");
            }
            
            return versionPart;
        }
        
        /// <summary>
        /// Gets PostgreSQL version without validation (for display purposes)
        /// </summary>
        public static async Task<(int Major, int Minor, string Full)> GetVersionInfoAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand("SHOW server_version;", connection);
            var versionString = (string)await command.ExecuteScalarAsync() 
                ?? throw new InvalidOperationException("Could not determine PostgreSQL version");
            
            var versionPart = versionString.Split(' ')[0];
            var versionNumbers = versionPart.Split('.');
            
            int major = versionNumbers.Length > 0 && int.TryParse(versionNumbers[0], out int m) ? m : 0;
            int minor = versionNumbers.Length > 1 && int.TryParse(versionNumbers[1], out int n) ? n : 0;
            
            return (major, minor, versionPart);
        }
        
        /// <summary>
        /// Checks if version is supported without throwing exception
        /// </summary>
        public static async Task<(bool IsSupported, string Message)> CheckVersionSupportAsync(string connectionString)
        {
            try
            {
                var version = await GetVersionInfoAsync(connectionString);
                
                if (version.Major < MinimumSupportedVersion)
                {
                    return (false, 
                        $"PostgreSQL {version.Full} is not supported. " +
                        $"Please upgrade to PostgreSQL {MinimumSupportedVersion} or higher.");
                }
                
                return (true, $"PostgreSQL {version.Full} is supported.");
            }
            catch (Exception ex)
            {
                return (false, $"Error checking PostgreSQL version: {ex.Message}");
            }
        }
    }
}

