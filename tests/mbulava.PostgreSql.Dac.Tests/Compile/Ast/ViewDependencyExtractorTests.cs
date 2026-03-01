using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;
using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for ViewDependencyExtractor using AST parsing.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("AstExtraction")]
public class ViewDependencyExtractorTests
{
    private ViewDependencyExtractor _extractor = null!;

    [SetUp]
    public void SetUp()
    {
        _extractor = new ViewDependencyExtractor();
    }

    [Test]
    [Category("Debug")]
    public void Debug_CTE_Structure()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.customer_summary AS
            WITH active_customers AS (
                SELECT * FROM public.customers WHERE active = true
            )
            SELECT * FROM active_customers;";

        using var parser = new Parser();
        var result = parser.Parse(sql);

        var root = result.ParseTree!.RootElement;
        if (root.TryGetProperty("stmts", out var stmts))
        {
            var firstStmt = stmts[0];
            if (firstStmt.TryGetProperty("stmt", out var stmt))
            {
                if (stmt.TryGetProperty("ViewStmt", out var viewStmt))
                {
                    if (viewStmt.TryGetProperty("query", out var query))
                    {
                        if (query.TryGetProperty("SelectStmt", out var selectStmt))
                        {
                            TestContext.WriteLine("\n=== SelectStmt with CTE ===");
                            TestContext.WriteLine(selectStmt.GetRawText());
                        }
                    }
                }
            }
        }
    }

    [Test]
    [Category("Debug")]
    public void Debug_Join_Structure()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.order_details AS
            SELECT o.id, o.date, c.name
            FROM public.orders o
            JOIN public.customers c ON o.customer_id = c.id;";

        using var parser = new Parser();
        var result = parser.Parse(sql);

        var root = result.ParseTree!.RootElement;
        if (root.TryGetProperty("stmts", out var stmts))
        {
            var firstStmt = stmts[0];
            if (firstStmt.TryGetProperty("stmt", out var stmt))
            {
                if (stmt.TryGetProperty("ViewStmt", out var viewStmt))
                {
                    if (viewStmt.TryGetProperty("query", out var query))
                    {
                        if (query.TryGetProperty("SelectStmt", out var selectStmt))
                        {
                            if (selectStmt.TryGetProperty("fromClause", out var fromClause))
                            {
                                TestContext.WriteLine("\n=== FromClause (with JOIN) ===");
                                TestContext.WriteLine(fromClause.GetRawText());
                            }
                        }
                    }
                }
            }
        }
    }

    [Test]
    [Category("Debug")]
    public void Debug_ViewStmt_Structure()
    {
        // Arrange
        var sql = @"CREATE VIEW public.customer_view AS SELECT id, name FROM public.customers;";

        using var parser = new Parser();
        var result = parser.Parse(sql);

        // Print the entire AST
        TestContext.WriteLine("=== FULL AST ===");
        TestContext.WriteLine(result.ParseTree?.RootElement.GetRawText());

        // Navigate to ViewStmt
        var root = result.ParseTree!.RootElement;
        if (root.TryGetProperty("stmts", out var stmts))
        {
            TestContext.WriteLine("\n=== Found stmts array ===");
            var firstStmt = stmts[0];
            if (firstStmt.TryGetProperty("stmt", out var stmt))
            {
                TestContext.WriteLine("\n=== Found stmt object ===");
                if (stmt.TryGetProperty("ViewStmt", out var viewStmt))
                {
                    TestContext.WriteLine("\n=== ViewStmt ===");
                    TestContext.WriteLine(viewStmt.GetRawText());

                    if (viewStmt.TryGetProperty("query", out var query))
                    {
                        TestContext.WriteLine("\n=== Query ===");
                        TestContext.WriteLine(query.GetRawText());

                        if (query.TryGetProperty("SelectStmt", out var selectStmt))
                        {
                            TestContext.WriteLine("\n=== SelectStmt ===");
                            TestContext.WriteLine(selectStmt.GetRawText());

                            if (selectStmt.TryGetProperty("fromClause", out var fromClause))
                            {
                                TestContext.WriteLine("\n=== FromClause ===");
                                TestContext.WriteLine(fromClause.GetRawText());
                            }
                        }
                    }
                }
            }
        }
    }

    [Test]
    public void ExtractDependencies_WithSimpleSelect_ExtractsTableDependency()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.customer_view AS
            SELECT id, name FROM public.customers;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "customer_view", "VIEW");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].ObjectType, Is.EqualTo("VIEW"));
        Assert.That(dependencies[0].ObjectName, Is.EqualTo("customer_view"));
        Assert.That(dependencies[0].DependsOnType, Is.EqualTo("TABLE_OR_VIEW"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("customers"));
        Assert.That(dependencies[0].DependencyType, Is.EqualTo("VIEW_REFERENCE"));
    }

    [Test]
    public void ExtractDependencies_WithJoin_ExtractsMultipleDependencies()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.order_details AS
            SELECT o.id, o.date, c.name
            FROM public.orders o
            JOIN public.customers c ON o.customer_id = c.id;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "order_details", "VIEW");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("orders"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("customers"));
    }

    [Test]
    public void ExtractDependencies_WithCTE_ExtractsTableDependenciesNotCTEs()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.customer_summary AS
            WITH active_customers AS (
                SELECT * FROM public.customers WHERE active = true
            )
            SELECT * FROM active_customers;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "customer_summary", "VIEW");

        // Assert
        // Should extract customers table, not the CTE
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("customers"));
    }

    [Test]
    public void ExtractDependencies_WithUnion_ExtractsAllTableDependencies()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.all_people AS
            SELECT name FROM public.employees
            UNION
            SELECT name FROM public.customers;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "all_people", "VIEW");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("employees"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("customers"));
    }

    [Test]
    public void ExtractDependencies_WithSubquery_ExtractsTableDependency()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.top_customers AS
            SELECT * FROM (
                SELECT id, name FROM public.customers
                WHERE total_purchases > 1000
            ) AS rich_customers;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "top_customers", "VIEW");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("customers"));
    }

    [Test]
    public void ExtractDependencies_WithCrossSchemaReference_UsesCorrectSchema()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.combined_view AS
            SELECT * FROM sales_schema.orders;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "combined_view", "VIEW");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnSchema, Is.EqualTo("sales_schema"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("orders"));
    }

    [Test]
    public void ExtractDependencies_WithMultipleJoins_ExtractsAllDependencies()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.order_full_details AS
            SELECT o.id, c.name, p.name
            FROM public.orders o
            JOIN public.customers c ON o.customer_id = c.id
            JOIN public.order_items oi ON o.id = oi.order_id
            JOIN public.products p ON oi.product_id = p.id;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "order_full_details", "VIEW");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(4));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("orders"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("customers"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("order_items"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("products"));
    }

    [Test]
    public void ExtractDependencies_WithInvalidSql_ReturnsEmptyList()
    {
        // Arrange
        var sql = "INVALID SQL STATEMENT";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "test_view", "VIEW");

        // Assert
        Assert.That(dependencies, Is.Empty);
    }

    [Test]
    public void ExtractDependencies_RemovesDuplicates_ReturnsUniqueReferences()
    {
        // Arrange
        var sql = @"
            CREATE VIEW public.customer_analysis AS
            SELECT c1.id, c2.name
            FROM public.customers c1
            JOIN public.customers c2 ON c1.parent_id = c2.id;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "customer_analysis", "VIEW");

        // Assert - should only have one reference to customers, not two
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("customers"));
    }
}
