using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for TriggerDependencyExtractor using AST parsing.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("AstExtraction")]
public class TriggerDependencyExtractorTests
{
    private TriggerDependencyExtractor _extractor = null!;

    [SetUp]
    public void SetUp()
    {
        _extractor = new TriggerDependencyExtractor();
    }

    [Test]
    public void ExtractDependencies_WithSimpleTrigger_ExtractsTableAndFunctionDependencies()
    {
        // Arrange
        var sql = @"
            CREATE TRIGGER audit_trigger
            AFTER INSERT OR UPDATE ON public.customers
            FOR EACH ROW
            EXECUTE FUNCTION public.audit_changes();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "audit_trigger", "TRIGGER");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        
        var tableDep = dependencies.FirstOrDefault(d => d.DependencyType == "TRIGGER_TABLE");
        Assert.That(tableDep, Is.Not.Null);
        Assert.That(tableDep!.DependsOnType, Is.EqualTo("TABLE"));
        Assert.That(tableDep.DependsOnName, Is.EqualTo("customers"));
        
        var funcDep = dependencies.FirstOrDefault(d => d.DependencyType == "TRIGGER_FUNCTION");
        Assert.That(funcDep, Is.Not.Null);
        Assert.That(funcDep!.DependsOnType, Is.EqualTo("FUNCTION"));
        Assert.That(funcDep.DependsOnName, Is.EqualTo("audit_changes"));
    }

    [Test]
    public void ExtractDependencies_WithBeforeTrigger_ExtractsCorrectDependencies()
    {
        // Arrange
        var sql = @"
            CREATE TRIGGER validate_trigger
            BEFORE INSERT ON public.orders
            FOR EACH ROW
            EXECUTE FUNCTION public.validate_order();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "validate_trigger", "TRIGGER");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("orders"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("validate_order"));
    }

    [Test]
    public void ExtractDependencies_WithCrossSchemaTable_UsesCorrectSchema()
    {
        // Arrange
        var sql = @"
            CREATE TRIGGER sync_trigger
            AFTER UPDATE ON sales_schema.products
            FOR EACH ROW
            EXECUTE FUNCTION public.sync_data();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "sync_trigger", "TRIGGER");

        // Assert
        var tableDep = dependencies.FirstOrDefault(d => d.DependencyType == "TRIGGER_TABLE");
        Assert.That(tableDep, Is.Not.Null);
        Assert.That(tableDep!.DependsOnSchema, Is.EqualTo("sales_schema"));
        Assert.That(tableDep.DependsOnName, Is.EqualTo("products"));
    }

    [Test]
    public void ExtractDependencies_WithCrossSchemaFunction_UsesCorrectSchema()
    {
        // Arrange
        var sql = @"
            CREATE TRIGGER process_trigger
            AFTER DELETE ON public.items
            FOR EACH ROW
            EXECUTE FUNCTION audit_schema.log_deletion();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "process_trigger", "TRIGGER");

        // Assert
        var funcDep = dependencies.FirstOrDefault(d => d.DependencyType == "TRIGGER_FUNCTION");
        Assert.That(funcDep, Is.Not.Null);
        Assert.That(funcDep!.DependsOnSchema, Is.EqualTo("audit_schema"));
        Assert.That(funcDep.DependsOnName, Is.EqualTo("log_deletion"));
    }

    [Test]
    public void ExtractDependencies_WithExecuteProcedure_ExtractsFunctionDependency()
    {
        // Arrange - older PostgreSQL syntax using PROCEDURE
        var sql = @"
            CREATE TRIGGER legacy_trigger
            AFTER INSERT ON public.logs
            FOR EACH ROW
            EXECUTE PROCEDURE public.process_log();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "legacy_trigger", "TRIGGER");

        // Assert
        var funcDep = dependencies.FirstOrDefault(d => d.DependencyType == "TRIGGER_FUNCTION");
        Assert.That(funcDep, Is.Not.Null);
        Assert.That(funcDep!.DependsOnName, Is.EqualTo("process_log"));
    }

    [Test]
    public void ExtractDependencies_WithStatementTrigger_ExtractsDependencies()
    {
        // Arrange
        var sql = @"
            CREATE TRIGGER statement_audit
            AFTER TRUNCATE ON public.data
            FOR EACH STATEMENT
            EXECUTE FUNCTION public.audit_truncate();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "statement_audit", "TRIGGER");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("data"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("audit_truncate"));
    }

    [Test]
    public void ExtractDependencies_WithInvalidSql_ReturnsEmptyList()
    {
        // Arrange
        var sql = "INVALID SQL STATEMENT";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "test_trigger", "TRIGGER");

        // Assert
        Assert.That(dependencies, Is.Empty);
    }

    [Test]
    public void ExtractDependencies_AlwaysExtractsTableDependency()
    {
        // Arrange - even without function, should extract table
        var sql = @"
            CREATE TRIGGER test_trigger
            AFTER INSERT ON public.test_table
            FOR EACH ROW
            EXECUTE FUNCTION public.test_func();";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "test_trigger", "TRIGGER");

        // Assert
        var tableDeps = dependencies.Where(d => d.DependencyType == "TRIGGER_TABLE").ToList();
        Assert.That(tableDeps, Has.Count.EqualTo(1));
        Assert.That(tableDeps.First().DependsOnType, Is.EqualTo("TABLE"));
    }
}
