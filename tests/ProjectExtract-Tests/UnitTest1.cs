using mbulava.PostgreSql.Dac.Extract;
using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using Npgsql;
using PgQuery;
using System.Text;

namespace ProjectExtract_Tests
{
    public class Tests
    {

        private NpgsqlConnection _conn;
        private Parser _parser;

        [SetUp]
        public void Setup()
        {
            var connString = "Host=192.168.12.96;Port=5432;Database=lego;Username=postgres;Password=mysecretpassword";
            _conn = new NpgsqlConnection(connString);
            _conn.Open();
            _parser = new Parser();
        }

        [TearDown]
        public void TearDown()
        {
            _conn.Close();
            _conn.Dispose();
            _parser.Dispose();
        }

        [Test]
        public async Task Test1()
        {
            PgSchemaExtractor extractor = new PgSchemaExtractor(_conn);
            var project = await extractor.ExtractAllSchemasAsync("lego", "17.0");
        }
        

        

        private async Task<List<PgRole>> ExtractRolesAsync()
        {
            var roles = new List<PgRole>();

            using var cmd = new NpgsqlCommand(@"
        SELECT rolname, rolsuper, rolcanlogin, rolinherit, rolreplication, rolbypassrls
        FROM pg_roles;", _conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var role = new PgRole
                {
                    Name = reader.GetString(0),
                    IsSuperUser = reader.GetBoolean(1),
                    CanLogin = reader.GetBoolean(2),
                    Inherit = reader.GetBoolean(3),
                    Replication = reader.GetBoolean(4),
                    BypassRLS = reader.GetBoolean(5),
                };

                role.Definition = BuildCreateRoleSql(role);
                roles.Add(role);
            }

            return roles;
        }

        private string BuildCreateRoleSql(PgRole role)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE ROLE {role.Name}");

            if (role.IsSuperUser) sb.Append(" SUPERUSER");
            else sb.Append(" NOSUPERUSER");

            if (role.CanLogin) sb.Append(" LOGIN");
            else sb.Append(" NOLOGIN");

            if (role.Inherit) sb.Append(" INHERIT");
            else sb.Append(" NOINHERIT");

            if (role.Replication) sb.Append(" REPLICATION");
            else sb.Append(" NOREPLICATION");

            if (role.BypassRLS) sb.Append(" BYPASSRLS");
            else sb.Append(" NOBYPASSRLS");

            sb.Append(";");
            return sb.ToString();
        }


        
    }
}