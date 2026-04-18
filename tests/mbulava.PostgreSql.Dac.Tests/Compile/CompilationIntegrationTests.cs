using mbulava.PostgreSql.Dac.Compile;
using mbulava.PostgreSql.Dac.Models;
using NUnit.Framework;
using PgQuery;
using System.IO;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Tests.Compile;

/// <summary>
/// Integration tests for complete compilation workflow (Phase 1-5 integration)
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("Milestone2")]
public class CompilationIntegrationTests
{
    #region Complete Schema Tests

    [Test]
    public void IntegrationTest_CompleteEcommerceSchema_CompilationSucceeds()
    {
        // Arrange - Complete e-commerce schema
        var project = CreateEcommerceSchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True, result.GetSummary());
        Assert.That(result.DeploymentOrder.Count, Is.GreaterThanOrEqualTo(10), "Should have at least 10 objects");
        Assert.That(result.DependencyGraph, Is.Not.Null);
        Assert.That(result.CompilationTime, Is.GreaterThan(TimeSpan.Zero));
        
        // Verify key orderings
        var order = result.DeploymentOrder;
        VerifyDeploymentOrder(order, "public.users", "public.orders");
        VerifyDeploymentOrder(order, "public.products", "public.order_items");
        VerifyDeploymentOrder(order, "public.orders", "public.order_items");
        VerifyDeploymentOrder(order, "public.categories", "public.products");
        
        // Verify levels exist
        Assert.That(result.DeploymentLevels, Is.Not.Empty);
        Assert.That(result.DeploymentLevels[0], Does.Contain("public.users")
            .Or.Contains("public.categories"));
    }

    [Test]
    public void IntegrationTest_SchemaWithInheritance_CompilesCorrectly()
    {
        // Arrange - Schema with table inheritance
        var project = CreateInheritanceSchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        // Parent table should come before child tables
        var order = result.DeploymentOrder;
        VerifyDeploymentOrder(order, "public.users", "public.premium_users");
        VerifyDeploymentOrder(order, "public.users", "public.admin_users");
    }

    [Test]
    public void IntegrationTest_ComplexViewHierarchy_CompilesCorrectly()
    {
        // Arrange - Multi-level view dependencies
        var project = CreateViewHierarchySchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        // Views should come after their base tables/views
        var order = result.DeploymentOrder;
        VerifyDeploymentOrder(order, "public.users", "public.user_summary");
        VerifyDeploymentOrder(order, "public.orders", "public.user_summary");
        VerifyDeploymentOrder(order, "public.user_summary", "public.user_report");
    }

    [Test]
    public async Task IntegrationTest_CsprojWithMissingReference_ReportsSourceFileLocation()
    {
        // Arrange
        var projectDirectory = Path.Combine(Path.GetTempPath(), $"pgpac-missing-ref-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectDirectory);
        TestContext.WriteLine($"Project directory: {projectDirectory}");

        var projectPath = Path.Combine(projectDirectory, "MissingRefs.csproj");
        await File.WriteAllTextAsync(projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <Sdk Name="MSBuild.Sdk.PostgreSql" Version="1.0.0-preview7" />
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <DatabaseName>MissingRefs</DatabaseName>
                <PostgresVersion>17</PostgresVersion>
              </PropertyGroup>
            </Project>
            """);

        var viewsDirectory = Path.Combine(projectDirectory, "Views");
        Directory.CreateDirectory(viewsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(viewsDirectory, "BrokenView.sql"),
            """
            CREATE VIEW public.broken_view AS
            SELECT *
            FROM public.missing_table;
            """);

        var loader = new CsprojProjectLoader(projectPath);
        var project = await loader.LoadProjectAsync();
        var compiler = new ProjectCompiler();

        // Act
        var result = compiler.Compile(project);

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors.Any(e => e.Code == "REF001"), Is.True);
        Assert.That(
            result.Errors.Any(e => e.Location.Replace('\\', '/').Contains("Views/BrokenView.sql", StringComparison.Ordinal)),
            Is.True);
    }

    [Test]
    [Ignore("TODO: Circular dependency detection issue - function body parsing may incorrectly identify dependencies")]
    public void IntegrationTest_FunctionsAndTriggers_CompileInCorrectOrder()
    {
        // Arrange - Functions and triggers with dependencies
        var project = CreateFunctionTriggerSchema();
        var compiler = new ProjectCompiler();

        // Act
        var result = compiler.Compile(project);

        // Assert
        if (!result.IsSuccess)
        {
            TestContext.WriteLine("Compilation errors:");
            foreach (var error in result.Errors)
            {
                TestContext.WriteLine($"  - {error.Message}");
            }
        }
        Assert.That(result.IsSuccess, Is.True, $"Compilation failed: {string.Join("; ", result.Errors.Select(e => e.Message))}");

        // Table before trigger, function before trigger
        var order = result.DeploymentOrder;
        VerifyDeploymentOrder(order, "public.audit_log", "public.log_changes");
        VerifyDeploymentOrder(order, "public.log_changes", "public.audit_trigger");
        VerifyDeploymentOrder(order, "public.users", "public.audit_trigger");
    }

    [Test]
    public void IntegrationTest_MultiSchemaProject_HandlesSchemaQualification()
    {
        // Arrange - Project with multiple schemas
        var project = CreateMultiSchemaProject();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        
        // Verify cross-schema dependencies
        var order = result.DeploymentOrder;
        VerifyDeploymentOrder(order, "auth.users", "public.orders");
    }

    #endregion

    #region Error Scenario Tests

    [Test]
    public void IntegrationTest_CircularViewDependencies_ReportsError()
    {
        // Arrange - Circular views (not allowed)
        var project = CreateCircularViewSchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Errors[0].Code, Does.StartWith("CYCLE"));
        Assert.That(result.HasCircularDependencies, Is.True);
        
        var errorCycle = result.CircularDependencies.First(c => c.Severity == CycleSeverity.Error);
        Assert.That(errorCycle.ObjectTypes, Does.Contain("VIEW"));
        Assert.That(errorCycle.Suggestion, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void IntegrationTest_ComplexTableCycle_ReportsErrorWithSuggestion()
    {
        // Arrange - Complex circular FK dependencies
        var project = CreateComplexCycleSchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.CircularDependencies, Is.Not.Empty);
        
        var cycle = result.CircularDependencies[0];
        Assert.That(cycle.Cycle.Count, Is.GreaterThan(2));
        Assert.That(cycle.Suggestion, Does.Contain("Break").Or.Contains("remove").Or.Contains("redesign"));
    }

    [Test]
    public void IntegrationTest_AllowedSelfReferences_CompileWithInfo()
    {
        // Arrange - Schema with allowed self-references
        var project = CreateSelfReferenceSchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.CircularDependencies, Is.Not.Empty);
        
        // Should have Info-level cycles (allowed)
        var infoCycles = result.CircularDependencies.Where(c => c.Severity == CycleSeverity.Info);
        Assert.That(infoCycles, Is.Not.Empty);
    }

    #endregion

    #region Performance Tests

    [Test]
    public void IntegrationTest_LargeSchema_CompletesInReasonableTime()
    {
        // Arrange - Large schema with 50+ objects
        var project = CreateLargeSchema();
        var compiler = new ProjectCompiler();
        
        // Act
        var result = compiler.Compile(project);
        
        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.DeploymentOrder.Count, Is.GreaterThanOrEqualTo(50));
        Assert.That(result.CompilationTime.TotalSeconds, Is.LessThan(5.0), 
            $"Compilation took {result.CompilationTime.TotalSeconds}s, expected < 5s");
    }

    #endregion

    #region Helper Methods

    private void VerifyDeploymentOrder(List<string> order, string before, string after)
    {
        int beforeIdx = order.IndexOf(before);
        int afterIdx = order.IndexOf(after);
        
        Assert.That(beforeIdx, Is.GreaterThanOrEqualTo(0), $"{before} not found in deployment order");
        Assert.That(afterIdx, Is.GreaterThanOrEqualTo(0), $"{after} not found in deployment order");
        Assert.That(beforeIdx, Is.LessThan(afterIdx), 
            $"{before} should come before {after} in deployment order");
    }

    private PgProject CreateEcommerceSchema()
    {
        return new PgProject
        {
            DatabaseName = "ecommerce",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" },
                        new PgTable { Name = "categories" },
                        new PgTable
                        {
                            Name = "products",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "categories"
                                }
                            }
                        },
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
                        },
                        new PgTable
                        {
                            Name = "reviews",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "users"
                                },
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "products"
                                }
                            }
                        },
                        new PgTable
                        {
                            Name = "shopping_cart",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "users"
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
                            Name = "user_orders",
                            Dependencies = new List<string> { "users", "orders" }
                        },
                        new PgView
                        {
                            Name = "product_reviews",
                            Dependencies = new List<string> { "products", "reviews", "users" }
                        },
                        new PgView
                        {
                            Name = "order_details",
                            Dependencies = new List<string> { "orders", "order_items", "products" }
                        }
                    },
                    Triggers = new List<PgTrigger>
                    {
                        new PgTrigger
                        {
                            Name = "update_timestamp",
                            TableName = "orders",
                            Definition = "CREATE TRIGGER update_timestamp AFTER UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION update_timestamp_func();"
                        },
                        new PgTrigger
                        {
                            Name = "validate_stock",
                            TableName = "order_items",
                            Definition = "CREATE TRIGGER validate_stock BEFORE INSERT ON order_items FOR EACH ROW EXECUTE FUNCTION validate_stock_func();"
                        }
                    },
                    Functions = new List<PgFunction>
                    {
                        new PgFunction { Name = "calculate_total" },
                        new PgFunction { Name = "apply_discount" },
                        new PgFunction { Name = "get_user_stats" },
                        new PgFunction { Name = "update_timestamp_func" },
                        new PgFunction { Name = "validate_stock_func" }
                    },
                    Sequences = new List<PgSequence>
                    {
                        new PgSequence { Name = "order_id_seq" }
                    }
                }
            }
        };
    }

    private PgProject CreateInheritanceSchema()
    {
        return new PgProject
        {
            DatabaseName = "inheritance_test",
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
                            Name = "premium_users",
                            InheritedFrom = new List<string> { "users" }
                        },
                        new PgTable
                        {
                            Name = "admin_users",
                            InheritedFrom = new List<string> { "users" }
                        }
                    }
                }
            }
        };
    }

    private PgProject CreateViewHierarchySchema()
    {
        return new PgProject
        {
            DatabaseName = "view_hierarchy",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" },
                        new PgTable { Name = "orders" }
                    },
                    Views = new List<PgView>
                    {
                        new PgView
                        {
                            Name = "user_summary",
                            Dependencies = new List<string> { "users", "orders" }
                        },
                        new PgView
                        {
                            Name = "user_report",
                            Dependencies = new List<string> { "user_summary" }
                        }
                    }
                }
            }
        };
    }

    private PgProject CreateFunctionTriggerSchema()
    {
        return new PgProject
        {
            DatabaseName = "function_trigger",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" },
                        new PgTable { Name = "audit_log" }
                    },
                    Functions = new List<PgFunction>
                    {
                        new PgFunction
                        {
                            Name = "log_changes",
                            Definition = "CREATE FUNCTION log_changes() RETURNS trigger AS $$ BEGIN INSERT INTO audit_log VALUES (NEW.*); RETURN NEW; END; $$ LANGUAGE plpgsql;"
                        }
                    },
                    Triggers = new List<PgTrigger>
                    {
                        new PgTrigger
                        {
                            Name = "audit_trigger",
                            TableName = "users",
                            Definition = "CREATE TRIGGER audit_trigger AFTER INSERT ON users FOR EACH ROW EXECUTE FUNCTION log_changes();"
                        }
                    }
                }
            }
        };
    }

    private PgProject CreateMultiSchemaProject()
    {
        return new PgProject
        {
            DatabaseName = "multi_schema",
            Schemas = new List<PgSchema>
            {
                new PgSchema
                {
                    Name = "auth",
                    Tables = new List<PgTable>
                    {
                        new PgTable { Name = "users" }
                    }
                },
                new PgSchema
                {
                    Name = "public",
                    Tables = new List<PgTable>
                    {
                        new PgTable
                        {
                            Name = "orders",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint
                                {
                                    Type = ConstrType.ConstrForeign,
                                    ReferencedTable = "auth.users"
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private PgProject CreateCircularViewSchema()
    {
        return new PgProject
        {
            DatabaseName = "circular_views",
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
    }

    private PgProject CreateComplexCycleSchema()
    {
        return new PgProject
        {
            DatabaseName = "complex_cycle",
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
                                new PgConstraint { Type = ConstrType.ConstrForeign, ReferencedTable = "b" }
                            }
                        },
                        new PgTable
                        {
                            Name = "b",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint { Type = ConstrType.ConstrForeign, ReferencedTable = "c" }
                            }
                        },
                        new PgTable
                        {
                            Name = "c",
                            Constraints = new List<PgConstraint>
                            {
                                new PgConstraint { Type = ConstrType.ConstrForeign, ReferencedTable = "a" }
                            }
                        }
                    }
                }
            }
        };
    }

    private PgProject CreateSelfReferenceSchema()
    {
        return new PgProject
        {
            DatabaseName = "self_reference",
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
                                    ReferencedTable = "employees"
                                }
                            }
                        }
                    },
                    Functions = new List<PgFunction>
                    {
                        new PgFunction
                        {
                            Name = "factorial",
                            Definition = "CREATE FUNCTION factorial(n integer) RETURNS integer AS $$ BEGIN IF n <= 1 THEN RETURN 1; ELSE RETURN n * factorial(n-1); END IF; END; $$ LANGUAGE plpgsql;"
                        }
                    }
                }
            }
        };
    }

    private PgProject CreateLargeSchema()
    {
        var schema = new PgSchema { Name = "public", Tables = new List<PgTable>() };
        
        // Create 50 tables with dependencies
        for (int i = 0; i < 50; i++)
        {
            var table = new PgTable { Name = $"table_{i:D2}" };
            
            // Create some dependencies
            if (i > 0 && i % 5 == 0)
            {
                table.Constraints = new List<PgConstraint>
                {
                    new PgConstraint
                    {
                        Type = ConstrType.ConstrForeign,
                        ReferencedTable = $"table_{i-5:D2}"
                    }
                };
            }
            
            schema.Tables.Add(table);
        }
        
        return new PgProject
        {
            DatabaseName = "large_schema",
            Schemas = new List<PgSchema> { schema }
        };
    }

    #endregion
}
