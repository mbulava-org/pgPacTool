using mbulava.PostgreSql.Dac.Models;
using PgQuery;

namespace pgPacTool.Benchmarks;

/// <summary>
/// Shared factory for building in-memory PgProject fixtures used across benchmarks.
/// </summary>
internal static class ProjectFactory
{
    /// <summary>
    /// Creates a PgProject with the requested number of objects.
    /// Tables form a linear FK chain so the dependency graph is non-trivial.
    /// Views reference the first table. Functions are independent.
    /// </summary>
    internal static PgProject Create(int tableCount, int viewCount, int functionCount)
    {
        var schema = new PgSchema { Name = "public" };

        // Tables: table_001 -> table_000, table_002 -> table_001, …
        for (int i = 0; i < tableCount; i++)
        {
            var table = new PgTable { Name = $"table_{i:D3}" };
            if (i > 0)
            {
                table.Constraints =
                [
                    new PgConstraint
                    {
                        Type = ConstrType.ConstrForeign,
                        ReferencedTable = $"table_{i - 1:D3}"
                    }
                ];
            }
            schema.Tables.Add(table);
        }

        // Views
        for (int i = 0; i < viewCount; i++)
        {
            schema.Views.Add(new PgView
            {
                Name = $"view_{i:D3}",
                Definition = $"SELECT * FROM table_000 WHERE id = {i}"
            });
        }

        // Functions
        for (int i = 0; i < functionCount; i++)
        {
            schema.Functions.Add(new PgFunction
            {
                Name = $"fn_{i:D3}",
                Definition = $"CREATE OR REPLACE FUNCTION public.fn_{i:D3}() RETURNS integer LANGUAGE plpgsql AS $$ BEGIN RETURN {i}; END; $$;"
            });
        }

        return new PgProject
        {
            DatabaseName = "benchmark_db",
            PostgresVersion = "16",
            Schemas = [schema]
        };
    }
}
