using mbulava.PostgreSql.Dac.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Extract
{
    public class PgSchemaExtractor
    {
        private readonly NpgsqlConnection _conn;

        public PgSchemaExtractor(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task<PgProject> ExtractAllSchemasAsync(string databaseName, string postgresVersion)
        {
            var project = new PgProject
            {
                DatabaseName = databaseName,
                PostgresVersion = postgresVersion,
                ExtractionDate = DateTime.UtcNow
            };

            var schemas = await GetSchemasAsync();

            foreach (var schemaName in schemas)
            {
                var schema = new PgSchema { Name = schemaName };

                schema.Tables.AddRange(await ExtractTablesAsync(schemaName));
                schema.Views.AddRange(await ExtractViewsAsync(schemaName));
                schema.Functions.AddRange(await ExtractFunctionsAsync(schemaName));
                schema.Triggers.AddRange(await ExtractTriggersAsync(schemaName));

                project.Schemas.Add(schema);
            }

            return project;
        }

        private async Task<List<string>> GetSchemasAsync()
        {
            var schemas = new List<string>();
            using var cmd = new NpgsqlCommand(
                @"SELECT nspname
              FROM pg_namespace
              WHERE nspname NOT LIKE 'pg_%'
                AND nspname <> 'information_schema';", _conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                schemas.Add(reader.GetString(0));
            }
            return schemas;
        }

        private async Task<List<PgTable>> ExtractTablesAsync(string schema)
        {
            var tables = new List<PgTable>();
            using var cmd = new NpgsqlCommand(
                @"SELECT table_name
              FROM information_schema.tables
              WHERE table_schema = @schema AND table_type='BASE TABLE';", _conn);
            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string name = reader.GetString(0);
                tables.Add(new PgTable(name, $"CREATE TABLE {schema}.{name} (...)"));
            }
            return tables;
        }

        private async Task<List<PgView>> ExtractViewsAsync(string schema)
        {
            var views = new List<PgView>();
            using var cmd = new NpgsqlCommand(
                @"SELECT table_name, view_definition
              FROM information_schema.views
              WHERE table_schema = @schema;", _conn);
            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                views.Add(new PgView(reader.GetString(0), reader.GetString(1)));
            }
            return views;
        }

        private async Task<List<PgFunction>> ExtractFunctionsAsync(string schema)
        {
            var funcs = new List<PgFunction>();
            using var cmd = new NpgsqlCommand(
                @"SELECT proname, pg_get_functiondef(oid)
              FROM pg_proc
              WHERE pronamespace = @schema::regnamespace;", _conn);
            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                funcs.Add(new PgFunction(reader.GetString(0), reader.GetString(1)));
            }
            return funcs;
        }

        private async Task<List<PgTrigger>> ExtractTriggersAsync(string schema)
        {
            var triggers = new List<PgTrigger>();
            using var cmd = new NpgsqlCommand(
                @"SELECT tgname, relname, pg_get_triggerdef(oid)
              FROM pg_trigger
              JOIN pg_class ON pg_trigger.tgrelid = pg_class.oid
              JOIN pg_namespace ns ON pg_class.relnamespace = ns.oid
              WHERE ns.nspname = @schema;", _conn);
            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                triggers.Add(new PgTrigger(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            }
            return triggers;
        }
    }


}
