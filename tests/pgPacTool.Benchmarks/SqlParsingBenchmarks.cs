using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Npgquery;

namespace pgPacTool.Benchmarks;

/// <summary>
/// Benchmarks for the Npgquery SQL parser (pg_query native wrapper).
/// Measures parse time for SQL statements of increasing complexity.
/// These map to the pgprojBuilder AST compilation path.
/// </summary>
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
public class SqlParsingBenchmarks
{
    // Simple SELECT
    private const string SimpleSql =
        "SELECT id, name FROM users WHERE active = true;";

    // Medium complexity: multi-join, subquery
    private const string MediumSql = """
        SELECT u.id, u.name, o.total
        FROM users u
        JOIN orders o ON o.user_id = u.id
        JOIN products p ON p.id = o.product_id
        WHERE u.active = true
          AND o.created_at > NOW() - INTERVAL '30 days'
        ORDER BY o.total DESC
        LIMIT 100;
        """;

    // Complex DDL: CREATE TABLE + constraints
    private const string ComplexDdl = """
        CREATE TABLE public.order_items (
            id          BIGSERIAL PRIMARY KEY,
            order_id    BIGINT      NOT NULL REFERENCES public.orders(id) ON DELETE CASCADE,
            product_id  BIGINT      NOT NULL REFERENCES public.products(id),
            quantity    INT         NOT NULL DEFAULT 1 CHECK (quantity > 0),
            unit_price  NUMERIC(12,2) NOT NULL,
            discount    NUMERIC(5,2)  NOT NULL DEFAULT 0,
            created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            CONSTRAINT order_items_price_check CHECK (unit_price >= 0),
            CONSTRAINT order_items_discount_check CHECK (discount BETWEEN 0 AND 100)
        );
        CREATE INDEX idx_order_items_order  ON public.order_items (order_id);
        CREATE INDEX idx_order_items_product ON public.order_items (product_id);
        """;

    // PL/pgSQL function body
    private const string PlpgsqlFunction = """
        CREATE OR REPLACE FUNCTION public.calculate_order_total(p_order_id BIGINT)
        RETURNS NUMERIC(12,2)
        LANGUAGE plpgsql
        AS $$
        DECLARE
            v_total NUMERIC(12,2) := 0;
            v_item  RECORD;
        BEGIN
            FOR v_item IN
                SELECT quantity, unit_price, discount
                FROM public.order_items
                WHERE order_id = p_order_id
            LOOP
                v_total := v_total + (v_item.quantity * v_item.unit_price * (1 - v_item.discount / 100));
            END LOOP;
            RETURN v_total;
        END;
        $$;
        """;

    // Batch: 50 simple statements (simulates multi-object compilation)
    private static readonly string BatchSql =
        string.Join("\n", Enumerable.Range(0, 50)
            .Select(i => $"SELECT {i} AS n, 'item_{i}' AS label;"));

    private Parser _parser = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parser = new Parser(PostgreSqlVersion.Postgres16);
    }

    [GlobalCleanup]
    public void Cleanup() => _parser.Dispose();

    [Benchmark(Description = "Parse simple SELECT")]
    public void ParseSimpleSql() => _parser.Parse(SimpleSql);

    [Benchmark(Description = "Parse medium multi-join query")]
    public void ParseMediumSql() => _parser.Parse(MediumSql);

    [Benchmark(Description = "Parse complex DDL (CREATE TABLE + indexes)")]
    public void ParseComplexDdl() => _parser.Parse(ComplexDdl);

    [Benchmark(Description = "Parse PL/pgSQL function")]
    public void ParsePlpgsqlFunction() => _parser.Parse(PlpgsqlFunction);

    [Benchmark(Description = "Parse batch of 50 statements")]
    public void ParseBatch() => _parser.Parse(BatchSql);
}
