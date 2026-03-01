using NUnit.Framework;
using PgQuery;
using mbulava.PostgreSql.Dac.Compile.Ast;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

[TestFixture]
public class AstNavigationHelpersTests
{
    #region GetStringValue Tests

    [Test]
    public void GetStringValue_WithStringNode_ReturnsValue()
    {
        // Arrange
        var node = new Node
        {
            String = new PgQuery.String { Sval = "test_value" }
        };

        // Act
        var result = node.GetStringValue();

        // Assert
        Assert.That(result, Is.EqualTo("test_value"));
    }

    [Test]
    public void GetStringValue_WithNullNode_ReturnsNull()
    {
        // Arrange
        Node? node = null;

        // Act
        var result = node.GetStringValue();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetStringValue_WithNodeWithoutString_ReturnsNull()
    {
        // Arrange
        var node = new Node
        {
            Integer = new Integer { Ival = 42 }
        };

        // Act
        var result = node.GetStringValue();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetStringValue_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var node = new Node
        {
            String = new PgQuery.String { Sval = string.Empty }
        };

        // Act
        var result = node.GetStringValue();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region GetIntValue Tests

    [Test]
    public void GetIntValue_WithIntegerNode_ReturnsValue()
    {
        // Arrange
        var node = new Node
        {
            Integer = new Integer { Ival = 42 }
        };

        // Act
        var result = node.GetIntValue();

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetIntValue_WithNullNode_ReturnsNull()
    {
        // Arrange
        Node? node = null;

        // Act
        var result = node.GetIntValue();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetIntValue_WithNodeWithoutInteger_ReturnsNull()
    {
        // Arrange
        var node = new Node
        {
            String = new PgQuery.String { Sval = "test" }
        };

        // Act
        var result = node.GetIntValue();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetIntValue_WithZero_ReturnsZero()
    {
        // Arrange
        var node = new Node
        {
            Integer = new Integer { Ival = 0 }
        };

        // Act
        var result = node.GetIntValue();

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetIntValue_WithNegativeNumber_ReturnsNegativeValue()
    {
        // Arrange
        var node = new Node
        {
            Integer = new Integer { Ival = -100 }
        };

        // Act
        var result = node.GetIntValue();

        // Assert
        Assert.That(result, Is.EqualTo(-100));
    }

    #endregion

    #region GetQualifiedName Tests

    [Test]
    public void GetQualifiedName_WithNullNodes_ReturnsDefaultSchemaAndNull()
    {
        // Arrange
        IEnumerable<Node>? nodes = null;

        // Act
        var (schema, name) = nodes.GetQualifiedName();

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.Null);
    }

    [Test]
    public void GetQualifiedName_WithEmptyList_ReturnsDefaultSchemaAndNull()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act
        var (schema, name) = nodes.GetQualifiedName();

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.Null);
    }

    [Test]
    public void GetQualifiedName_WithSingleNode_ReturnsDefaultSchemaAndName()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "users" } }
        };

        // Act
        var (schema, name) = nodes.GetQualifiedName();

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.EqualTo("users"));
    }

    [Test]
    public void GetQualifiedName_WithTwoNodes_ReturnsSchemaAndName()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "myschema" } },
            new Node { String = new PgQuery.String { Sval = "mytable" } }
        };

        // Act
        var (schema, name) = nodes.GetQualifiedName();

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void GetQualifiedName_WithThreeNodes_ReturnsLastTwoParts()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "database" } },
            new Node { String = new PgQuery.String { Sval = "myschema" } },
            new Node { String = new PgQuery.String { Sval = "mytable" } }
        };

        // Act
        var (schema, name) = nodes.GetQualifiedName();

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void GetQualifiedName_WithCustomDefaultSchema_UsesCustomDefault()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "users" } }
        };

        // Act
        var (schema, name) = nodes.GetQualifiedName("custom");

        // Assert
        Assert.That(schema, Is.EqualTo("custom"));
        Assert.That(name, Is.EqualTo("users"));
    }

    #endregion

    #region GetSchemaAndName Tests

    [Test]
    public void GetSchemaAndName_WithNullRangeVar_ReturnsDefaultSchemaAndEmpty()
    {
        // Arrange
        RangeVar? rangeVar = null;

        // Act
        var (schema, name) = rangeVar.GetSchemaAndName();

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.Empty);
    }

    [Test]
    public void GetSchemaAndName_WithSchemaAndName_ReturnsBoth()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            Schemaname = "myschema",
            Relname = "mytable"
        };

        // Act
        var (schema, name) = rangeVar.GetSchemaAndName();

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void GetSchemaAndName_WithNullOrAbsentSchema_ReturnsEmptySchema()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            // Schemaname will be empty string by default in protobuf3
            Relname = "mytable"
        };

        // Act
        var (schema, name) = rangeVar.GetSchemaAndName();

        // Assert
        // Protobuf3 defaults to empty string, not null
        // The ?? operator doesn't catch empty strings
        Assert.That(schema, Is.EqualTo(string.Empty));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void GetSchemaAndName_WithEmptySchema_ReturnsEmptySchema()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            Schemaname = string.Empty,
            Relname = "mytable"
        };

        // Act
        var (schema, name) = rangeVar.GetSchemaAndName();

        // Assert
        // Empty string is NOT null, so it's returned as-is
        Assert.That(schema, Is.EqualTo(string.Empty));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void GetSchemaAndName_WithCustomDefault_ButProtobufDefaultsToEmpty()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            // Schemaname will be empty string by default in protobuf3
            Relname = "mytable"
        };

        // Act
        var (schema, name) = rangeVar.GetSchemaAndName("custom");

        // Assert
        // Protobuf3 returns empty string (not null), so ?? doesn't apply
        Assert.That(schema, Is.EqualTo(string.Empty));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    #endregion

    #region ExtractRangeVars Tests

    [Test]
    public void ExtractRangeVars_WithNullFromClause_ReturnsEmptyList()
    {
        // Arrange
        IEnumerable<Node>? fromClause = null;

        // Act
        var result = AstNavigationHelpers.ExtractRangeVars(fromClause);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExtractRangeVars_WithEmptyFromClause_ReturnsEmptyList()
    {
        // Arrange
        var fromClause = new List<Node>();

        // Act
        var result = AstNavigationHelpers.ExtractRangeVars(fromClause);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExtractRangeVars_WithSingleTable_ReturnsOneRangeVar()
    {
        // Arrange
        var fromClause = new List<Node>
        {
            new Node
            {
                RangeVar = new RangeVar
                {
                    Schemaname = "public",
                    Relname = "users"
                }
            }
        };

        // Act
        var result = AstNavigationHelpers.ExtractRangeVars(fromClause);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Relname, Is.EqualTo("users"));
    }

    [Test]
    public void ExtractRangeVars_WithMultipleTables_ReturnsAllRangeVars()
    {
        // Arrange
        var fromClause = new List<Node>
        {
            new Node
            {
                RangeVar = new RangeVar
                {
                    Schemaname = "public",
                    Relname = "users"
                }
            },
            new Node
            {
                RangeVar = new RangeVar
                {
                    Schemaname = "public",
                    Relname = "orders"
                }
            }
        };

        // Act
        var result = AstNavigationHelpers.ExtractRangeVars(fromClause);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Relname, Is.EqualTo("users"));
        Assert.That(result[1].Relname, Is.EqualTo("orders"));
    }

    [Test]
    public void ExtractRangeVars_WithJoin_ExtractsBothSides()
    {
        // Arrange
        var fromClause = new List<Node>
        {
            new Node
            {
                JoinExpr = new JoinExpr
                {
                    Larg = new Node
                    {
                        RangeVar = new RangeVar
                        {
                            Schemaname = "public",
                            Relname = "users"
                        }
                    },
                    Rarg = new Node
                    {
                        RangeVar = new RangeVar
                        {
                            Schemaname = "public",
                            Relname = "orders"
                        }
                    }
                }
            }
        };

        // Act
        var result = AstNavigationHelpers.ExtractRangeVars(fromClause);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.Relname), Contains.Item("users"));
        Assert.That(result.Select(r => r.Relname), Contains.Item("orders"));
    }

    #endregion

    #region GetTypeName Tests

    [Test]
    public void GetTypeName_WithNullTypeName_ReturnsDefaultSchemaAndEmpty()
    {
        // Arrange
        TypeName? typeName = null;

        // Act
        var (schema, type) = typeName.GetTypeName();

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(type, Is.Empty);
    }

    [Test]
    public void GetTypeName_WithSingleNamePart_ReturnsDefaultSchemaAndType()
    {
        // Arrange
        var typeName = new TypeName
        {
            Names = {
                new Node { String = new PgQuery.String { Sval = "integer" } }
            }
        };

        // Act
        var (schema, type) = typeName.GetTypeName();

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(type, Is.EqualTo("integer"));
    }

    [Test]
    public void GetTypeName_WithQualifiedName_ReturnsSchemaAndType()
    {
        // Arrange
        var typeName = new TypeName
        {
            Names = {
                new Node { String = new PgQuery.String { Sval = "myschema" } },
                new Node { String = new PgQuery.String { Sval = "my_type" } }
            }
        };

        // Act
        var (schema, type) = typeName.GetTypeName();

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(type, Is.EqualTo("my_type"));
    }

    #endregion

    #region HasFromClause Tests

    [Test]
    public void HasFromClause_WithNullSelectStmt_ReturnsFalse()
    {
        // Arrange
        SelectStmt? selectStmt = null;

        // Act
        var result = selectStmt.HasFromClause();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasFromClause_WithNullFromClause_ReturnsFalse()
    {
        // Arrange
        var selectStmt = new SelectStmt();

        // Act
        var result = selectStmt.HasFromClause();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasFromClause_WithEmptyFromClause_ReturnsFalse()
    {
        // Arrange
        var selectStmt = new SelectStmt
        {
            FromClause = { }
        };

        // Act
        var result = selectStmt.HasFromClause();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasFromClause_WithFromClause_ReturnsTrue()
    {
        // Arrange
        var selectStmt = new SelectStmt
        {
            FromClause = {
                new Node { RangeVar = new RangeVar { Relname = "users" } }
            }
        };

        // Act
        var result = selectStmt.HasFromClause();

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region HasWithClause Tests

    [Test]
    public void HasWithClause_WithNullSelectStmt_ReturnsFalse()
    {
        // Arrange
        SelectStmt? selectStmt = null;

        // Act
        var result = selectStmt.HasWithClause();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasWithClause_WithNullWithClause_ReturnsFalse()
    {
        // Arrange
        var selectStmt = new SelectStmt();

        // Act
        var result = selectStmt.HasWithClause();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasWithClause_WithEmptyCtes_ReturnsFalse()
    {
        // Arrange
        var selectStmt = new SelectStmt
        {
            WithClause = new WithClause { Ctes = { } }
        };

        // Act
        var result = selectStmt.HasWithClause();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasWithClause_WithCtes_ReturnsTrue()
    {
        // Arrange
        var selectStmt = new SelectStmt
        {
            WithClause = new WithClause
            {
                Ctes = {
                    new Node
                    {
                        CommonTableExpr = new CommonTableExpr { Ctename = "temp_table" }
                    }
                }
            }
        };

        // Act
        var result = selectStmt.HasWithClause();

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region GetCteNames Tests

    [Test]
    public void GetCteNames_WithNullWithClause_ReturnsEmptyList()
    {
        // Arrange
        WithClause? withClause = null;

        // Act
        var result = withClause.GetCteNames();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetCteNames_WithNullCtes_ReturnsEmptyList()
    {
        // Arrange
        var withClause = new WithClause();

        // Act
        var result = withClause.GetCteNames();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetCteNames_WithEmptyCtes_ReturnsEmptyList()
    {
        // Arrange
        var withClause = new WithClause { Ctes = { } };

        // Act
        var result = withClause.GetCteNames();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetCteNames_WithSingleCte_ReturnsCteName()
    {
        // Arrange
        var withClause = new WithClause
        {
            Ctes = {
                new Node
                {
                    CommonTableExpr = new CommonTableExpr { Ctename = "temp_users" }
                }
            }
        };

        // Act
        var result = withClause.GetCteNames();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("temp_users"));
    }

    [Test]
    public void GetCteNames_WithMultipleCtes_ReturnsAllNames()
    {
        // Arrange
        var withClause = new WithClause
        {
            Ctes = {
                new Node
                {
                    CommonTableExpr = new CommonTableExpr { Ctename = "temp_users" }
                },
                new Node
                {
                    CommonTableExpr = new CommonTableExpr { Ctename = "temp_orders" }
                }
            }
        };

        // Act
        var result = withClause.GetCteNames();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Contains.Item("temp_users"));
        Assert.That(result, Contains.Item("temp_orders"));
    }

    [Test]
    public void GetCteNames_SkipsEmptyNames()
    {
        // Arrange
        var withClause = new WithClause
        {
            Ctes = {
                new Node
                {
                    CommonTableExpr = new CommonTableExpr { Ctename = "temp_users" }
                },
                new Node
                {
                    CommonTableExpr = new CommonTableExpr { Ctename = string.Empty }
                }
            }
        };

        // Act
        var result = withClause.GetCteNames();

        // Assert
        // Only non-empty names should be included
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("temp_users"));
    }

    #endregion

    #region IsCteReference Tests

    [Test]
    public void IsCteReference_WithMatchingName_ReturnsTrue()
    {
        // Arrange
        var cteNames = new List<string> { "temp_users", "temp_orders" };

        // Act
        var result = AstNavigationHelpers.IsCteReference("temp_users", cteNames);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsCteReference_WithNonMatchingName_ReturnsFalse()
    {
        // Arrange
        var cteNames = new List<string> { "temp_users", "temp_orders" };

        // Act
        var result = AstNavigationHelpers.IsCteReference("real_table", cteNames);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsCteReference_WithEmptyCteList_ReturnsFalse()
    {
        // Arrange
        var cteNames = new List<string>();

        // Act
        var result = AstNavigationHelpers.IsCteReference("temp_users", cteNames);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsCteReference_IsCaseInsensitive()
    {
        // Arrange
        var cteNames = new List<string> { "temp_users", "temp_orders" };

        // Act
        var result = AstNavigationHelpers.IsCteReference("TEMP_USERS", cteNames);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region FindNodesOfType Tests

    [Test]
    public void FindNodesOfType_WithNullRoot_ReturnsEmptyList()
    {
        // Arrange
        Node? root = null;

        // Act
        var result = AstNavigationHelpers.FindNodesOfType<RangeVar>(root);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindNodesOfType_WithNonMatchingType_ReturnsEmptyList()
    {
        // Arrange
        var root = new Node
        {
            String = new PgQuery.String { Sval = "test" }
        };

        // Act
        var result = AstNavigationHelpers.FindNodesOfType<RangeVar>(root);

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion
}
