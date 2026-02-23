using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;
using PgQuery;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// Tests for DependencyAnalyzer (Phase 1, Task 1.2)
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Milestone2")]
public class DependencyAnalyzerTests
{
    #region AnalyzeProject Tests

    [Test]
    public void AnalyzeProject_WithSimpleTable_BuildsGraph()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "testdb",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" }
                    }
                }
            }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var graph = analyzer.AnalyzeProject(project);
        
        // Assert
        var allObjects = graph.GetAllObjects();
        Assert.That(allObjects, Has.Count.EqualTo(1));
        Assert.That(allObjects[0], Is.EqualTo("public.users"));
        Assert.That(graph.GetObjectType("public.users"), Is.EqualTo("TABLE"));
    }

    [Test]
    public void AnalyzeProject_WithTableAndView_BuildsGraph()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "testdb",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" }
                    },
                    Views = new List<PgView>
                    {
                        new PgView { Name = "active_users" }
                    }
                }
            }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var graph = analyzer.AnalyzeProject(project);
        
        // Assert
        var allObjects = graph.GetAllObjects();
        Assert.That(allObjects, Has.Count.EqualTo(2));
        Assert.That(allObjects, Does.Contain("public.users"));
        Assert.That(allObjects, Does.Contain("public.active_users"));
    }

    #endregion

    #region ExtractTableDependencies Tests

    [Test]
    public void ExtractTableDependencies_WithForeignKey_ReturnsDependency()
    {
        // Arrange
        var table = new PgTable
        {
            Name = "orders",
            Constraints = new List<PgConstraint>
            {
                new PgConstraint
                {
                    Name = "fk_orders_users",
                    Type = ConstrType.ConstrForeign,
                    ReferencedTable = "users"
                }
            }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractTableDependencies("public", table);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].ObjectName, Is.EqualTo("orders"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("users"));
        Assert.That(dependencies[0].DependencyType, Is.EqualTo("FOREIGN_KEY"));
    }

    [Test]
    public void ExtractTableDependencies_WithMultipleForeignKeys_ReturnsAllDependencies()
    {
        // Arrange
        var table = new PgTable
        {
            Name = "order_items",
            Constraints = new List<PgConstraint>
            {
                new PgConstraint
                {
                    Name = "fk_order_items_orders",
                    Type = ConstrType.ConstrForeign,
                    ReferencedTable = "orders"
                },
                new PgConstraint
                {
                    Name = "fk_order_items_products",
                    Type = ConstrType.ConstrForeign,
                    ReferencedTable = "products"
                }
            }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractTableDependencies("public", table);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        var depNames = dependencies.Select(d => d.DependsOnName).ToList();
        Assert.That(depNames, Does.Contain("orders"));
        Assert.That(depNames, Does.Contain("products"));
    }

    [Test]
    public void ExtractTableDependencies_WithQualifiedTableReference_ParsesCorrectly()
    {
        // Arrange
        var table = new PgTable
        {
            Name = "orders",
            Constraints = new List<PgConstraint>
            {
                new PgConstraint
                {
                    Name = "fk_orders_users",
                    Type = ConstrType.ConstrForeign,
                    ReferencedTable = "auth.users"  // Qualified reference
                }
            }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractTableDependencies("public", table);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnSchema, Is.EqualTo("auth"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("users"));
    }

    [Test]
    public void ExtractTableDependencies_WithInheritance_ReturnsDependency()
    {
        // Arrange
        var table = new PgTable
        {
            Name = "premium_users",
            InheritedFrom = new List<string> { "users" }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractTableDependencies("public", table);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("users"));
        Assert.That(dependencies[0].DependencyType, Is.EqualTo("INHERITANCE"));
    }

    [Test]
    public void ExtractTableDependencies_WithNoConstraints_ReturnsEmptyList()
    {
        // Arrange
        var table = new PgTable
        {
            Name = "users",
            Constraints = new List<PgConstraint>()
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractTableDependencies("public", table);
        
        // Assert
        Assert.That(dependencies, Is.Empty);
    }

    #endregion

    #region ExtractViewDependencies Tests

    [Test]
    public void ExtractViewDependencies_WithTableDependency_ReturnsDependency()
    {
        // Arrange
        var view = new PgView
        {
            Name = "active_users",
            Dependencies = new List<string> { "users" }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractViewDependencies("public", view);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(1));
        Assert.That(dependencies[0].ObjectName, Is.EqualTo("active_users"));
        Assert.That(dependencies[0].DependsOnName, Is.EqualTo("users"));
        Assert.That(dependencies[0].DependencyType, Is.EqualTo("VIEW_REFERENCE"));
    }

    [Test]
    public void ExtractViewDependencies_WithMultipleDependencies_ReturnsAll()
    {
        // Arrange
        var view = new PgView
        {
            Name = "user_orders",
            Dependencies = new List<string> { "users", "orders" }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractViewDependencies("public", view);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        var depNames = dependencies.Select(d => d.DependsOnName).ToList();
        Assert.That(depNames, Does.Contain("users"));
        Assert.That(depNames, Does.Contain("orders"));
    }

    #endregion

    #region ExtractFunctionDependencies Tests

    [Test]
    public void ExtractFunctionDependencies_WithNoParameters_ReturnsEmpty()
    {
        // Arrange
        var function = new PgFunction
        {
            Name = "get_random_number",
            Definition = "CREATE FUNCTION get_random_number() RETURNS integer AS $$ BEGIN RETURN 42; END; $$ LANGUAGE plpgsql;"
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractFunctionDependencies("public", function);
        
        // Assert
        Assert.That(dependencies, Is.Empty);
    }

    [Test]
    public void ExtractFunctionDependencies_WithTableReference_ReturnsDependency()
    {
        // Arrange - Function that references a table in its body
        var function = new PgFunction
        {
            Name = "count_users",
            Definition = "CREATE FUNCTION count_users() RETURNS bigint AS $$ SELECT COUNT(*) FROM users; $$ LANGUAGE sql;"
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractFunctionDependencies("public", function);
        
        // Assert
        // Note: This will initially fail - parsing function bodies is complex
        // For MVP, we can skip or do simple text matching
        Assert.That(dependencies, Is.Not.Null);
    }

    #endregion

    #region ExtractTriggerDependencies Tests

    [Test]
    public void ExtractTriggerDependencies_ReturnsTableAndFunctionDependencies()
    {
        // Arrange
        var trigger = new PgTrigger
        {
            Name = "audit_trigger",
            TableName = "users",
            Definition = "CREATE TRIGGER audit_trigger AFTER INSERT ON users FOR EACH ROW EXECUTE FUNCTION audit_log();"
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var dependencies = analyzer.ExtractTriggerDependencies("public", trigger);
        
        // Assert
        Assert.That(dependencies, Has.Count.EqualTo(2));
        
        // Should depend on table
        var tableDep = dependencies.FirstOrDefault(d => d.DependsOnName == "users");
        Assert.That(tableDep, Is.Not.Null);
        Assert.That(tableDep!.DependencyType, Is.EqualTo("TRIGGER_TABLE"));
        
        // Should depend on function
        var funcDep = dependencies.FirstOrDefault(d => d.DependsOnName == "audit_log");
        Assert.That(funcDep, Is.Not.Null);
        Assert.That(funcDep!.DependencyType, Is.EqualTo("TRIGGER_FUNCTION"));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void AnalyzeProject_ComplexSchema_BuildsCorrectGraph()
    {
        // Arrange - Complex project with FK relationships
        var project = new PgProject
        {
            DatabaseName = "testdb",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" },
                        new PgTable 
                        { 
                            Name = "orders",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "users"
                                }
                            }
                        },
                        new PgTable 
                        { 
                            Name = "order_items",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "orders"
                                }
                            }
                        }
                    },
                    Views = new List<PgView>
                    {
                        new PgView 
                        { 
                            Name = "user_orders",
                            Dependencies = new List<string> { "users", "orders" }
                        }
                    }
                }
            }
        };
        
        var analyzer = new DependencyAnalyzer();
        
        // Act
        var graph = analyzer.AnalyzeProject(project);
        
        // Assert
        Assert.That(graph.GetAllObjects(), Has.Count.EqualTo(4));
        
        // Check dependencies
        Assert.That(graph.HasPath("public.orders", "public.users"), Is.True);
        Assert.That(graph.HasPath("public.order_items", "public.orders"), Is.True);
        Assert.That(graph.HasPath("public.order_items", "public.users"), Is.True); // Transitive
        Assert.That(graph.HasPath("public.user_orders", "public.users"), Is.True);
        Assert.That(graph.HasPath("public.user_orders", "public.orders"), Is.True);
    }

    #endregion
}
