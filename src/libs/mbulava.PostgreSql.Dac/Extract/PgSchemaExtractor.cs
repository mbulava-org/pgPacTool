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

                schema.Sequences.AddRange(await ExtractSequencesAsync(schemaName));
                schema.Types.AddRange(await ExtractTypesAsync(schemaName));
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
            var tableNames = new List<string>();
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
            await reader.CloseAsync();

            foreach (var tableName in tableNames)
            {
                var table = new PgTable(tableName, $"CREATE TABLE {schema}.{tableName} (...)");

                // Columns
                table.Columns.AddRange(await ExtractColumnsAsync(schema, tableName));

                // Indexes
                table.Indexes.AddRange(await ExtractIndexesAsync(schema, tableName));

                // Constraints
                table.Constraints.AddRange(await ExtractConstraintsAsync(schema, tableName));

                tables.Add(table);
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

        private async Task<List<PgIndex>> ExtractIndexesAsync(string schema, string table)
        {
            var indexes = new List<PgIndex>();
            using var cmd = new NpgsqlCommand(
                @"SELECT indexname, indexdef
                  FROM pg_indexes
                  WHERE schemaname = @schema AND tablename = @table;", _conn);
            cmd.Parameters.AddWithValue("schema", schema);
            cmd.Parameters.AddWithValue("table", table);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                indexes.Add(new PgIndex(
                    reader.GetString(0),
                    table,
                    reader.GetString(1)
                ));
            }
            return indexes;
        }

        private async Task<List<PgConstraint>> ExtractConstraintsAsync(string schema, string table)
        {
            var constraints = new List<PgConstraint>();
            using var cmd = new NpgsqlCommand(
                @"SELECT conname,
                   contype,
                   pg_get_constraintdef(oid) AS definition,
                   confrelid::regclass::text AS referenced_table
                 FROM pg_constraint
                 WHERE conrelid = (
                   SELECT oid FROM pg_class
                   WHERE relname = @table
                   AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = @schema)
                 );", _conn);
            cmd.Parameters.AddWithValue("schema", schema);
            cmd.Parameters.AddWithValue("table", table);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                constraints.Add(new PgConstraint(
                    reader.GetString(0),
                    table,
                    reader.GetString(1),   // 'p' = primary key, 'f' = foreign key, 'u' = unique, etc.
                    reader.GetString(2))
                    {
                        ReferencedTable = reader.IsDBNull(3) ? null : reader.GetString(3)
                    }
                );
            }
            return constraints;
        }

        private async Task<List<PgColumn>> ExtractColumnsAsync(string schema, string table)
        {
            var columns = new List<PgColumn>();
            using var cmd = new NpgsqlCommand(
                @"SELECT column_name, data_type, is_nullable, column_default
                  FROM information_schema.columns
                  WHERE table_schema = @schema AND table_name = @table
                  ORDER BY ordinal_position;", _conn);
            cmd.Parameters.AddWithValue("schema", schema);
            cmd.Parameters.AddWithValue("table", table);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(new PgColumn(
                    Name: reader.GetString(0),
                    DataType: reader.GetString(1),
                    IsNullable: reader.GetString(2) == "YES",
                    DefaultValue: reader.IsDBNull(3) ? null : reader.GetString(3)
                ));
            }
            return columns;
        }

        private async Task<List<PgType>> ExtractTypesAsync(string schema)
        {
            var types = new List<PgType>();

            // Enums
            using var enumCmd = new NpgsqlCommand(@"SELECT t.typname, array_to_string(array_agg(e.enumlabel ORDER BY e.enumsortorder), ',')
                                            FROM pg_type t
                                            JOIN pg_enum e ON t.oid = e.enumtypid
                                            JOIN pg_namespace n ON n.oid = t.typnamespace
                                            WHERE n.nspname = @schema
                                            GROUP BY t.typname;", _conn);
            enumCmd.Parameters.AddWithValue("schema", schema);
            using var enumReader = await enumCmd.ExecuteReaderAsync();
            while (await enumReader.ReadAsync())
            {
                var name = enumReader.GetString(0);
                var labels = enumReader.GetString(1);
                var def = $"CREATE TYPE {schema}.{name} AS ENUM ({string.Join(", ", labels.Split(','))});";
                types.Add(new PgType(name, schema, "enum", def));
            }
            await enumReader.CloseAsync();

            // Domains, composites, ranges handled similarly...

            return types;
        }

        private async Task<List<PgSequence>> ExtractSequencesAsync(string schema)
        {
            var sequences = new List<PgSequence>();

            using var cmd = new NpgsqlCommand(@"
        SELECT c.relname AS seqname,
               n.nspname AS schemaname,
               pg_get_serial_sequence(n.nspname || '.' || c.relname, 'id') AS serialdef,
               s.start_value, s.increment_by, s.max_value, s.min_value, s.cache_size, s.is_cycled
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_sequence s ON s.seqrelid = c.oid
        WHERE c.relkind = 'S' AND n.nspname = @schema;", _conn);

            cmd.Parameters.AddWithValue("schema", schema);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var schemaname = reader.GetString(1);

                var start = reader.GetInt64(3);
                var inc = reader.GetInt64(4);
                var max = reader.GetInt64(5);
                var min = reader.GetInt64(6);
                var cache = reader.GetInt64(7);
                var cycle = reader.GetBoolean(8);

                var def = $"CREATE SEQUENCE {schemaname}.{name} " +
                          $"START WITH {start} INCREMENT BY {inc} " +
                          $"MINVALUE {min} MAXVALUE {max} CACHE {cache} " +
                          $"{(cycle ? "CYCLE" : "NO CYCLE")};";

                sequences.Add(new PgSequence(name, schemaname, def));
            }

            return sequences;
        }
    }


}
