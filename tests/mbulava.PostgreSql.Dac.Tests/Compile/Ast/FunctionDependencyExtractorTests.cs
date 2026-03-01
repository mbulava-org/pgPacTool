using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;
using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for FunctionDependencyExtractor using AST parsing.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("AstExtraction")]
public class FunctionDependencyExtractorTests
{
    private FunctionDependencyExtractor _extractor = null!;

    [SetUp]
    public void SetUp()
    {
        _extractor = new FunctionDependencyExtractor();
    }

    [Test]
    [Category("Debug")]
    public void Debug_Function_Structure()
    {
        var sql = @"
            CREATE FUNCTION public.process_address(addr public.address_type)
            RETURNS public.result_type AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        using var parser = new Parser();
        var result = parser.Parse(sql);

        var root = result.ParseTree!.RootElement;
        if (root.TryGetProperty("stmts", out var stmts))
        {
            var firstStmt = stmts[0];
            if (firstStmt.TryGetProperty("stmt", out var stmt))
            {
                if (stmt.TryGetProperty("CreateFunctionStmt", out var funcStmt))
                {
                    TestContext.WriteLine("\n=== CreateFunctionStmt ===");
                    TestContext.WriteLine(funcStmt.GetRawText());
                }
            }
        }
    }

    [Test]
    public void ExtractDependencies_WithParameterType_ExtractsTypeDependency()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.get_customer(cust_id integer)
            RETURNS text AS $$
            BEGIN
                RETURN 'result';
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "get_customer", "FUNCTION");

        // Assert - integer is built-in, text is built-in, so no dependencies
        var typeDeps = dependencies.Where(d => d.DependencyType == "PARAMETER_TYPE" || d.DependencyType == "RETURN_TYPE");
        Assert.That(typeDeps, Is.Empty);
    }

    [Test]
    public void ExtractDependencies_WithUserDefinedParameterType_ExtractsTypeDependency()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.process_address(addr public.address_type)
            RETURNS void AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "process_address", "FUNCTION");

        // Assert
        var paramDep = dependencies.FirstOrDefault(d => d.DependencyType == "PARAMETER_TYPE");
        Assert.That(paramDep, Is.Not.Null);
        Assert.That(paramDep!.DependsOnType, Is.EqualTo("TYPE"));
        Assert.That(paramDep.DependsOnName, Is.EqualTo("address_type"));
    }

    [Test]
    public void ExtractDependencies_WithUserDefinedReturnType_ExtractsTypeDependency()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.get_address(id integer)
            RETURNS public.address_type AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "get_address", "FUNCTION");

        // Assert
        var returnDep = dependencies.FirstOrDefault(d => d.DependencyType == "RETURN_TYPE");
        Assert.That(returnDep, Is.Not.Null);
        Assert.That(returnDep!.DependsOnType, Is.EqualTo("TYPE"));
        Assert.That(returnDep.DependsOnName, Is.EqualTo("address_type"));
    }

    [Test]
    public void ExtractDependencies_WithMultipleParameters_ExtractsAllTypeDependencies()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.complex_func(
                addr public.address_type,
                contact public.contact_type
            )
            RETURNS public.result_type AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "complex_func", "FUNCTION");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(3)); // 2 params + 1 return
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("address_type"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("contact_type"));
        Assert.That(dependencies.Select(d => d.DependsOnName), Does.Contain("result_type"));
    }

    [Test]
    public void ExtractDependencies_WithVoidReturn_DoesNotExtractReturnDependency()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.simple_proc()
            RETURNS void AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "simple_proc", "FUNCTION");

        // Assert - void should not be extracted as a dependency
        var returnDeps = dependencies.Where(d => d.DependencyType == "RETURN_TYPE");
        Assert.That(returnDeps, Is.Empty);
    }

    [Test]
    public void ExtractDependencies_WithCrossSchemaType_UsesCorrectSchema()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.get_data(id integer)
            RETURNS other_schema.custom_type AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "get_data", "FUNCTION");

        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnSchema, Is.EqualTo("other_schema"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("custom_type"));
    }

    [Test]
    public void ExtractDependencies_WithInvalidSql_ReturnsEmptyList()
    {
        // Arrange
        var sql = "INVALID SQL STATEMENT";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "test_func", "FUNCTION");

        // Assert
        Assert.That(dependencies, Is.Empty);
    }

    [Test]
    public void ExtractDependencies_RemovesDuplicateTypes_ReturnsUniqueReferences()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION public.compare_addresses(
                addr1 public.address_type,
                addr2 public.address_type
            )
            RETURNS boolean AS $$
            BEGIN
                -- function body
            END;
            $$ LANGUAGE plpgsql;";

        // Act
        var dependencies = _extractor.ExtractDependencies(sql, "public", "compare_addresses", "FUNCTION");

        // Assert - should only have one reference to address_type, not two
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("address_type"));
    }
}
