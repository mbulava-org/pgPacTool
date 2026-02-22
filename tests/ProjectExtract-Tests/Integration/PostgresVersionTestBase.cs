using mbulava.PostgreSql.Dac.Extract;
using Npgsql;
using NUnit.Framework;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;

namespace ProjectExtract_Tests.Integration
{
    /// <summary>
    /// Base class for PostgreSQL version-specific integration tests.
    /// Tests privilege extraction, schema extraction, and other features across different PostgreSQL versions.
    /// </summary>
    public abstract class PostgresVersionTestBase
    {
        protected PostgreSqlContainer Container { get; private set; } = default!;
        protected string ConnectionString { get; private set; } = default!;
        protected abstract string PostgreSqlVersion { get; }

        [OneTimeSetUp]
        public async Task BaseSetup()
        {
            Container = new PostgreSqlBuilder(PostgreSqlVersion)
                .WithDatabase("testdb")
                .WithUsername("postgres")
                .WithPassword("testpass")
                .Build();

            await Container.StartAsync();

            // Configure connection string with connection pool settings
            // Note: Large MaxPoolSize needed due to connection leaks in PgProjectExtractor
            var builder = new NpgsqlConnectionStringBuilder(Container.GetConnectionString())
            {
                MaxPoolSize = 100,
                MinPoolSize = 0,
                ConnectionIdleLifetime = 60,
                ConnectionPruningInterval = 10,
                Timeout = 30
            };
            ConnectionString = builder.ToString();

            // Verify connection
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            TestContext.Out.WriteLine($"✓ Connected to {PostgreSqlVersion}");

            // Seed common test data
            await SeedCommonTestDataAsync();
        }

        [OneTimeTearDown]
        public async Task BaseTeardown()
        {
            // Clear connection pools before disposing
            NpgsqlConnection.ClearAllPools();
            await Container.DisposeAsync();
            TestContext.Out.WriteLine($"✓ Disposed {PostgreSqlVersion} container");
        }

        /// <summary>
        /// Seeds common test data used across all version tests
        /// </summary>
        protected virtual async Task SeedCommonTestDataAsync()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                -- Create test schema
                CREATE SCHEMA test_schema;
                
                -- Create test table
                CREATE TABLE test_schema.customers (
                    id SERIAL PRIMARY KEY,
                    name TEXT NOT NULL,
                    email TEXT
                );
                
                -- Create test view
                CREATE VIEW test_schema.active_customers AS
                SELECT * FROM test_schema.customers WHERE email IS NOT NULL;
                
                -- Create test function
                CREATE FUNCTION test_schema.calculate_discount(price NUMERIC)
                RETURNS NUMERIC AS $$
                BEGIN
                    RETURN price * 0.9;
                END;
                $$ LANGUAGE plpgsql;
                
                -- Create test sequence
                CREATE SEQUENCE test_schema.order_seq START 1000;
                
                -- Grant some privileges
                CREATE ROLE test_user LOGIN PASSWORD 'test123';
                GRANT USAGE ON SCHEMA test_schema TO test_user;
                GRANT SELECT ON test_schema.customers TO test_user;
            ";
            await cmd.ExecuteNonQueryAsync();
            
            TestContext.WriteLine($"✓ Seeded test data for {PostgreSqlVersion}");
        }
    }
}
