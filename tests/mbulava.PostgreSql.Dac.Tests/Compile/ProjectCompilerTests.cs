using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;
using PgQuery;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// End-to-end tests for ProjectCompiler (Phase 5)
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Milestone2")]
public class ProjectCompilerTests
{
    #region Basic Compilation

    [Test]
    public void Compile_SimpleProject_Succeeds()
    {
        // Arrange
        var project = CreateSimpleProject();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.DeploymentOrder, Is.Not.Empty);
        Assert.That(result.DependencyGraph, Is.Not.Null);
    }

    [Test]
    public void Compile_EmptyProject_Succeeds()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "empty",
            Schemas = new List<PgSchema>()
        };
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeploymentOrder, Is.Empty);
    }

    [Test]
    public void Compile_ProjectWithDependencies_ReturnsCorrectOrder()
    {
        // Arrange - Users -> Orders
        var project = new PgProject
        {
            DatabaseName = "test",
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
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeploymentOrder, Has.Count.EqualTo(2));
        
        int usersIdx = result.DeploymentOrder.IndexOf("public.users");
        int ordersIdx = result.DeploymentOrder.IndexOf("public.orders");
        Assert.That(usersIdx, Is.LessThan(ordersIdx), "users should come before orders");
    }

    [Test]
    public void Compile_ProjectWithMissingReference_ReturnsErrorWithFirstUsageLocation()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "test",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Views = new List<PgView>
                    {
                        new PgView
                        {
                            Name = "active_orders",
                            Dependencies = new List<string> { "missing_orders" }
                        }
                    }
                }
            }
        };

        var compiler = new ProjectCompiler();

        // Act
        var result = compiler.Compile(project);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors.Any(e => e.Code == "REF001"), Is.True);
        Assert.That(result.Errors.Any(e => e.Message.Contains("public.missing_orders", StringComparison.Ordinal)), Is.True);
        Assert.That(result.Errors.Any(e => e.Location.Contains("first usage in view 'public.active_orders'", StringComparison.Ordinal)), Is.True);
    }

    #endregion

    #region Circular Dependency Detection

    [Test]
    public void Compile_WithCircularDependency_ReturnsError()
    {
        // Arrange - A -> B -> A (not allowed for tables)
        var project = new PgProject
        {
            DatabaseName = "test",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable
                        {
                            Name = "a",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "b"
                                }
                            }
                        },
                        new PgTable
                        {
                            Name = "b",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "a"
                                }
                            }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.HasCircularDependencies, Is.True);
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0].Code, Does.StartWith("CYCLE"));
    }

    [Test]
    public void Compile_WithViewCycle_ReturnsError()
    {
        // Arrange - View A -> View B -> View A (not allowed)
        var project = new PgProject
        {
            DatabaseName = "test",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Views = new List<PgView>
                    {
                        new PgView
                        {
                            Name = "view_a",
                            Dependencies = new List<string> { "view_b" }
                        },
                        new PgView
                        {
                            Name = "view_b",
                            Dependencies = new List<string> { "view_a" }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public void Compile_WithAllowedSelfReference_SucceedsWithWarning()
    {
        // Arrange - Self-referential FK (allowed)
        var project = new PgProject
        {
            DatabaseName = "test",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable
                        {
                            Name = "employees",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "employees" // Self-reference
                                }
                            }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert - Should succeed but with info/warning
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeploymentOrder, Has.Count.EqualTo(1));
    }

    #endregion

    #region Complex Scenarios

    [Test]
    public void Compile_ComplexProject_ReturnsValidOrder()
    {
        // Arrange - Complex schema with multiple dependencies
        var project = new PgProject
        {
            DatabaseName = "complex",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" },
                        new PgTable { Name = "products" },
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
                                },
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "products"
                                }
                            }
                        }
                    },
                    Views = new List<PgView>
                    {
                        new PgView
                        {
                            Name = "order_summary",
                            Dependencies = new List<string> { "orders", "order_items" }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeploymentOrder, Has.Count.EqualTo(5));
        
        // Verify key orderings
        var order = result.DeploymentOrder;
        int usersIdx = order.IndexOf("public.users");
        int productsIdx = order.IndexOf("public.products");
        int ordersIdx = order.IndexOf("public.orders");
        int orderItemsIdx = order.IndexOf("public.order_items");
        int viewIdx = order.IndexOf("public.order_summary");
        
        Assert.That(usersIdx, Is.LessThan(ordersIdx));
        Assert.That(ordersIdx, Is.LessThan(orderItemsIdx));
        Assert.That(productsIdx, Is.LessThan(orderItemsIdx));
        Assert.That(orderItemsIdx, Is.LessThan(viewIdx));
    }

    [Test]
    public void Compile_WithDeploymentLevels_GroupsParallelObjects()
    {
        // Arrange - Multiple tables depending on one root
        var project = new PgProject
        {
            DatabaseName = "test",
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
                            Name = "profiles",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "users"
                                }
                            }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeploymentLevels, Has.Count.EqualTo(2));
        Assert.That(result.DeploymentLevels[0], Does.Contain("public.users"));
        Assert.That(result.DeploymentLevels[1], Does.Contain("public.orders"));
        Assert.That(result.DeploymentLevels[1], Does.Contain("public.profiles"));
    }

    #endregion

    #region Result Properties

    [Test]
    public void Compile_TracksCompilationTime()
    {
        // Arrange
        var project = CreateSimpleProject();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.CompilationTime, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public void Compile_PopulatesDependencyGraph()
    {
        // Arrange
        var project = CreateSimpleProject();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.DependencyGraph, Is.Not.Null);
        Assert.That(result.DependencyGraph!.GetAllObjects(), Is.Not.Empty);
    }

    [Test]
    public void GetSummary_WithSuccess_ReturnsSuccessMessage()
    {
        // Arrange
        var project = CreateSimpleProject();
        var compiler = new ProjectCompiler();
        var result = compiler.Compile(project);
        
        // Act
        var summary = result.GetSummary();
        
        // Assert
        Assert.That(summary, Does.Contain("succeeded"));
        Assert.That(summary, Does.Contain("ready for deployment"));
    }

    [Test]
    public void GetSummary_WithErrors_ReturnsErrorMessage()
    {
        // Arrange - Project with circular dependency
        var project = new PgProject
        {
            DatabaseName = "test",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Views = new List<PgView>
                    {
                        new PgView
                        {
                            Name = "view_a",
                            Dependencies = new List<string> { "view_b" }
                        },
                        new PgView
                        {
                            Name = "view_b",
                            Dependencies = new List<string> { "view_a" }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        var result = compiler.Compile(project);
        
        // Act
        var summary = result.GetSummary();
        
        // Assert
        Assert.That(summary, Does.Contain("failed"));
        Assert.That(summary, Does.Contain("error"));
    }

    #endregion

    #region CanCompile Tests

    [Test]
    public void CanCompile_ValidProject_ReturnsTrue()
    {
        // Arrange
        var project = CreateSimpleProject();
        var compiler = new ProjectCompiler();
        
        // Act
        var canCompile = compiler.CanCompile(project);
        
        // Assert
        Assert.That(canCompile, Is.True);
    }

    [Test]
    public void CanCompile_ProjectWithCycles_ReturnsFalse()
    {
        // Arrange
        var project = new PgProject
        {
            DatabaseName = "test",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Views = new List<PgView>
                    {
                        new PgView
                        {
                            Name = "view_a",
                            Dependencies = new List<string> { "view_b" }
                        },
                        new PgView
                        {
                            Name = "view_b",
                            Dependencies = new List<string> { "view_a" }
                        }
                    }
                }
            }
        };
        
        var compiler = new ProjectCompiler();
        
        // Act
        var canCompile = compiler.CanCompile(project);
        
        // Assert
        Assert.That(canCompile, Is.False);
    }

    #endregion

    #region Helper Methods

    private PgProject CreateSimpleProject()
    {
        return new PgProject
        {
            DatabaseName = "simple",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" },
                        new PgTable { Name = "products" }
                    }
                }
            }
        };
    }

    #endregion
}
