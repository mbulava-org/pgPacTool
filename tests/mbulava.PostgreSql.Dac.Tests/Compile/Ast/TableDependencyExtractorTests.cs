using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;
using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for TableDependencyExtractor using AST parsing.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("AstExtraction")]
public class TableDependencyExtractorTests
{
    private TableDependencyExtractor _extractor = null!;

    [SetUp]
    public void SetUp()
    {
        _extractor = new TableDependencyExtractor();
    }

    [Test]
    [Category("Debug")]
    public void Debug_Table_WithSequence_Structure()
    {
        var sql = @"
            CREATE TABLE public.products (
                id integer DEFAULT nextval('public.products_id_seq'::regclass),
                name text
            );";

        using var parser = new Parser();
        var result = parser.Parse(sql);

        var root = result.ParseTree!.RootElement;
        if (root.TryGetProperty("stmts", out var stmts))
        {
            var firstStmt = stmts[0];
            if (firstStmt.TryGetProperty("stmt", out var stmt))
            {
                if (stmt.TryGetProperty("CreateStmt", out var createStmt))
                {
                    TestContext.WriteLine("\n=== CreateStmt (with Sequence) ===");
                    TestContext.WriteLine(createStmt.GetRawText());
                }
            }
        }
    }

    [Test]
    [Category("Debug")]
    public void Debug_Table_WithFK_Structure()
    {
        var sql = @"
            CREATE TABLE public.orders (
                id integer PRIMARY KEY,
                customer_id integer REFERENCES public.customers(id)
            );";

        using var parser = new Parser();
        var result = parser.Parse(sql);

        var root = result.ParseTree!.RootElement;
        if (root.TryGetProperty("stmts", out var stmts))
        {
            var firstStmt = stmts[0];
            if (firstStmt.TryGetProperty("stmt", out var stmt))
            {
                if (stmt.TryGetProperty("CreateStmt", out var createStmt))
                {
                    TestContext.WriteLine("\n=== CreateStmt (with FK) ===");
                    TestContext.WriteLine(createStmt.GetRawText());
                }
            }
        }
    }

    [Test]
    public void ExtractDependencies_WithForeignKey_ExtractsTableDependency()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.orders (
                id integer PRIMARY KEY,
                customer_id integer REFERENCES public.customers(id)
            );";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "orders", "TABLE");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].ObjectType, Is.EqualTo("TABLE"));
        Assert.That(dependencies[0].ObjectName, Is.EqualTo("orders"));
        Assert.That(dependencies[0].DependsOnType, Is.EqualTo("TABLE"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("customers"));
        Assert.That(dependencies[0].DependencyType, Is.EqualTo("FOREIGN_KEY"));
    }

    [Test]
    public void ExtractDependencies_WithInheritance_ExtractsParentDependency()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.employees (
                id integer PRIMARY KEY
            ) INHERITS (public.persons);";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "employees", "TABLE");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].ObjectType, Is.EqualTo("TABLE"));
        Assert.That(dependencies[0].ObjectName, Is.EqualTo("employees"));
        Assert.That(dependencies[0].DependsOnType, Is.EqualTo("TABLE"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("persons"));
        Assert.That(dependencies[0].DependencyType, Is.EqualTo("INHERITANCE"));
    }

    [Test]
    public void ExtractDependencies_WithSequenceDefault_ExtractsSequenceDependency()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.products (
                id integer DEFAULT nextval('public.products_id_seq'::regclass),
                name text
            );";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "products", "TABLE");

        // Assert
        var sequenceDep = dependencies.FirstOrDefault(d => d.DependencyType == "SEQUENCE_DEFAULT");
        Assert.That(sequenceDep, Is.Not.Null);
        Assert.That(sequenceDep!.DependsOnType, Is.EqualTo("SEQUENCE"));
        Assert.That(sequenceDep.DependsOnName, Is.EqualTo("products_id_seq"));
    }

    [Test]
    public void ExtractDependencies_WithUserDefinedType_ExtractsTypeDependency()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.addresses (
                id integer PRIMARY KEY,
                location public.point_type
            );";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "addresses", "TABLE");

        // Assert
        var typeDep = dependencies.FirstOrDefault(d => d.DependencyType == "COLUMN_TYPE");
        Assert.That(typeDep, Is.Not.Null);
        Assert.That(typeDep!.DependsOnType, Is.EqualTo("TYPE"));
        Assert.That(typeDep.DependsOnName, Is.EqualTo("point_type"));
    }

    [Test]
    public void ExtractDependencies_WithBuiltInType_DoesNotExtractDependency()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.simple (
                id integer PRIMARY KEY,
                name text,
                amount numeric(10,2)
            );";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "simple", "TABLE");

        // Assert - should not extract built-in types as dependencies
        var typeDeps = dependencies.Where(d => d.DependencyType == "COLUMN_TYPE");
        Assert.That(typeDeps, Is.Empty);
    }

    [Test]
    public void ExtractDependencies_WithMultipleForeignKeys_ExtractsAllDependencies()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.order_items (
                id integer PRIMARY KEY,
                order_id integer REFERENCES public.orders(id),
                product_id integer REFERENCES public.products(id)
            );";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "order_items", "TABLE");

        // Assert
        var fkDeps = dependencies.Where(d => d.DependencyType == "FOREIGN_KEY").ToList();
        Assert.That(fkDeps, Has.Count.EqualTo(2));
        Assert.That(fkDeps.Select(d => d.DependsOnName), Does.Contain("orders"));
        Assert.That(fkDeps.Select(d => d.DependsOnName), Does.Contain("products"));
    }

    [Test]
    public void ExtractDependencies_WithCrossSchemaReference_UsesCorrectSchema()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.orders (
                id integer PRIMARY KEY,
                customer_id integer REFERENCES customers_schema.customers(id)
            );";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "orders", "TABLE");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnSchema, Is.EqualTo("customers_schema"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("customers"));
    }

    [Test]
    public void ExtractDependencies_WithInvalidSql_ReturnsEmptyList()
    {
        // Arrange
        var sql = "INVALID SQL STATEMENT";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "test", "TABLE");

        // Assert
        Assert.That(dependencies, Is.Empty);
    }
}
