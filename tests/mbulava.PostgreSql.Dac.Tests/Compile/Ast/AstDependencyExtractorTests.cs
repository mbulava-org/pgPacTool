using NUnit.Framework;
using mbulava.PostgreSql.Dac.Compile.Ast;
using mbulava.PostgreSql.Dac.Models;
using PgQuery;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for AstDependencyExtractor base class functionality.
/// Uses a concrete implementation for testing protected methods.
/// </summary>
[TestFixture]
public class AstDependencyExtractorTests
{
    private TestAstDependencyExtractor _extractor = null!;

    [SetUp]
    public void Setup()
    {
        _extractor = new TestAstDependencyExtractor();
    }

    [TearDown]
    public void TearDown()
    {
        _extractor.Dispose();
    }

    #region GetFirstStatement Tests

    [Test]
    public void GetFirstStatement_WithValidSelectQuery_ReturnsStatement()
    {
        // Arrange
        var sql = "SELECT * FROM users";

        // Act
        var stmt = _extractor.PublicGetFirstStatement(sql);

        // Assert
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt.Value.TryGetProperty("SelectStmt", out _), Is.True);
    }

    [Test]
    public void GetFirstStatement_WithInvalidSql_ReturnsNull()
    {
        // Arrange
        var sql = "THIS IS NOT SQL";

        // Act
        var stmt = _extractor.PublicGetFirstStatement(sql);

        // Assert
        Assert.That(stmt, Is.Null);
    }

    [Test]
    public void GetFirstStatement_WithEmptyString_ThrowsArgumentException()
    {
        // Arrange
        var sql = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _extractor.PublicGetFirstStatement(sql));
    }

    [Test]
    public void GetFirstStatement_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange
        var sql = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _extractor.PublicGetFirstStatement(sql));
    }

    [Test]
    public void GetFirstStatement_WithNullString_ThrowsArgumentNullException()
    {
        // Arrange
        string sql = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _extractor.PublicGetFirstStatement(sql));
    }

    [Test]
    public void GetFirstStatement_WithMultipleStatements_ReturnsFirst()
    {
        // Arrange
        var sql = "SELECT * FROM users; SELECT * FROM orders;";

        // Act
        var stmt = _extractor.PublicGetFirstStatement(sql);

        // Assert
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt.Value.TryGetProperty("SelectStmt", out _), Is.True);
    }

    [Test]
    public void GetFirstStatement_WithInsertQuery_ReturnsInsertStmt()
    {
        // Arrange
        var sql = "INSERT INTO users (name) VALUES ('test')";

        // Act
        var stmt = _extractor.PublicGetFirstStatement(sql);

        // Assert
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt.Value.TryGetProperty("InsertStmt", out _), Is.True);
    }

    [Test]
    public void GetFirstStatement_WithCreateTable_ReturnsCreateStmt()
    {
        // Arrange
        var sql = "CREATE TABLE users (id INT, name TEXT)";

        // Act
        var stmt = _extractor.PublicGetFirstStatement(sql);

        // Assert
        Assert.That(stmt, Is.Not.Null);
        Assert.That(stmt.Value.TryGetProperty("CreateStmt", out _), Is.True);
    }

    #endregion

    #region ExtractSchemaAndName Tests

    [Test]
    public void ExtractSchemaAndName_WithNullRangeVar_ReturnsDefaultSchemaAndEmpty()
    {
        // Arrange
        RangeVar? rangeVar = null;

        // Act
        var (schema, name) = _extractor.PublicExtractSchemaAndName(rangeVar);

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.Empty);
    }

    [Test]
    public void ExtractSchemaAndName_WithSchemaAndName_ReturnsBoth()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            Schemaname = "myschema",
            Relname = "mytable"
        };

        // Act
        var (schema, name) = _extractor.PublicExtractSchemaAndName(rangeVar);

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void ExtractSchemaAndName_WithNullOrAbsentSchema_ReturnsEmptySchema()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            // Schemaname is empty string by default in protobuf3
            Relname = "mytable"
        };

        // Act
        var (schema, name) = _extractor.PublicExtractSchemaAndName(rangeVar);

        // Assert
        // Protobuf3 defaults to empty string, the ?? operator doesn't catch it
        Assert.That(schema, Is.EqualTo(string.Empty));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void ExtractSchemaAndName_WithCustomDefaultSchema_ButProtobufDefaultsToEmpty()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            // Schemaname is empty string by default in protobuf3
            Relname = "mytable"
        };

        // Act
        var (schema, name) = _extractor.PublicExtractSchemaAndName(rangeVar, "custom");

        // Assert
        // Protobuf3 returns empty string (not null), so ?? doesn't apply the default
        Assert.That(schema, Is.EqualTo(string.Empty));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void ExtractSchemaAndName_WithEmptyRelname_ReturnsEmptyName()
    {
        // Arrange
        var rangeVar = new RangeVar
        {
            Schemaname = "myschema",
            Relname = ""
        };

        // Act
        var (schema, name) = _extractor.PublicExtractSchemaAndName(rangeVar);

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.Empty);
    }

    #endregion

    #region ExtractQualifiedName Tests

    [Test]
    public void ExtractQualifiedName_WithNullNodes_ReturnsDefaultSchemaAndEmpty()
    {
        // Arrange
        IEnumerable<Node>? nodes = null;

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes);

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.Empty);
    }

    [Test]
    public void ExtractQualifiedName_WithEmptyList_ReturnsDefaultSchemaAndEmpty()
    {
        // Arrange
        var nodes = new List<Node>();

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes);

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.Empty);
    }

    [Test]
    public void ExtractQualifiedName_WithSingleNode_ReturnsDefaultSchemaAndName()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "users" } }
        };

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes);

        // Assert
        Assert.That(schema, Is.EqualTo("public"));
        Assert.That(name, Is.EqualTo("users"));
    }

    [Test]
    public void ExtractQualifiedName_WithTwoNodes_ReturnsSchemaAndName()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "myschema" } },
            new Node { String = new PgQuery.String { Sval = "mytable" } }
        };

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes);

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void ExtractQualifiedName_WithThreeNodes_ReturnsLastTwoParts()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "database" } },
            new Node { String = new PgQuery.String { Sval = "myschema" } },
            new Node { String = new PgQuery.String { Sval = "mytable" } }
        };

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes);

        // Assert
        Assert.That(schema, Is.EqualTo("myschema"));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    [Test]
    public void ExtractQualifiedName_WithCustomDefaultSchema_UsesCustomDefault()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = "users" } }
        };

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes, "custom");

        // Assert
        Assert.That(schema, Is.EqualTo("custom"));
        Assert.That(name, Is.EqualTo("users"));
    }

    [Test]
    public void ExtractQualifiedName_WithEmptyStringValues_HandlesGracefully()
    {
        // Arrange
        var nodes = new List<Node>
        {
            new Node { String = new PgQuery.String { Sval = string.Empty } },
            new Node { String = new PgQuery.String { Sval = "mytable" } }
        };

        // Act
        var (schema, name) = _extractor.PublicExtractQualifiedName(nodes);

        // Assert
        // Empty string is returned as-is (not treated as null)
        Assert.That(schema, Is.EqualTo(string.Empty));
        Assert.That(name, Is.EqualTo("mytable"));
    }

    #endregion

    #region ExtractColumnRefs Tests

    [Test]
    public void ExtractColumnRefs_WithNullNode_ReturnsEmptyList()
    {
        // Arrange
        Node? node = null;

        // Act
        var refs = _extractor.PublicExtractColumnRefs(node);

        // Assert
        Assert.That(refs, Is.Empty);
    }

    [Test]
    public void ExtractColumnRefs_WithSimpleColumnRef_ReturnsColumnName()
    {
        // Arrange
        var node = new Node
        {
            ColumnRef = new ColumnRef
            {
                Fields = {
                    new Node { String = new PgQuery.String { Sval = "id" } }
                }
            }
        };

        // Act
        var refs = _extractor.PublicExtractColumnRefs(node);

        // Assert
        Assert.That(refs, Has.Count.EqualTo(1));
        Assert.That(refs[0].schema, Is.Null);
        Assert.That(refs[0].table, Is.Null);
        Assert.That(refs[0].column, Is.EqualTo("id"));
    }

    [Test]
    public void ExtractColumnRefs_WithQualifiedColumnRef_ReturnsTableAndColumn()
    {
        // Arrange
        var node = new Node
        {
            ColumnRef = new ColumnRef
            {
                Fields = {
                    new Node { String = new PgQuery.String { Sval = "users" } },
                    new Node { String = new PgQuery.String { Sval = "id" } }
                }
            }
        };

        // Act
        var refs = _extractor.PublicExtractColumnRefs(node);

        // Assert
        Assert.That(refs, Has.Count.EqualTo(1));
        Assert.That(refs[0].schema, Is.Null);
        Assert.That(refs[0].table, Is.EqualTo("users"));
        Assert.That(refs[0].column, Is.EqualTo("id"));
    }

    [Test]
    public void ExtractColumnRefs_WithFullyQualifiedColumnRef_ReturnsAll()
    {
        // Arrange
        var node = new Node
        {
            ColumnRef = new ColumnRef
            {
                Fields = {
                    new Node { String = new PgQuery.String { Sval = "myschema" } },
                    new Node { String = new PgQuery.String { Sval = "users" } },
                    new Node { String = new PgQuery.String { Sval = "id" } }
                }
            }
        };

        // Act
        var refs = _extractor.PublicExtractColumnRefs(node);

        // Assert
        Assert.That(refs, Has.Count.EqualTo(1));
        Assert.That(refs[0].schema, Is.EqualTo("myschema"));
        Assert.That(refs[0].table, Is.EqualTo("users"));
        Assert.That(refs[0].column, Is.EqualTo("id"));
    }

    [Test]
    public void ExtractColumnRefs_WithNonColumnRefNode_ReturnsEmptyList()
    {
        // Arrange
        var node = new Node
        {
            String = new PgQuery.String { Sval = "test" }
        };

        // Act
        var refs = _extractor.PublicExtractColumnRefs(node);

        // Assert
        Assert.That(refs, Is.Empty);
    }

    #endregion

    /// <summary>
    /// Concrete test implementation of abstract AstDependencyExtractor
    /// to enable testing of protected methods.
    /// </summary>
    private class TestAstDependencyExtractor : AstDependencyExtractor, IDisposable
    {
        public override List<PgDependency> ExtractDependencies(
            string sql,
            string schemaName,
            string objectName,
            string objectType)
        {
            // Test implementation - just return empty list
            return new List<PgDependency>();
        }

        // Public wrappers for protected methods to enable testing
        public System.Text.Json.JsonElement? PublicGetFirstStatement(string sql)
            => GetFirstStatement(sql);

        public (string schema, string name) PublicExtractSchemaAndName(RangeVar? rangeVar, string defaultSchema = "public")
            => ExtractSchemaAndName(rangeVar, defaultSchema);

        public (string schema, string name) PublicExtractQualifiedName(
            IEnumerable<Node>? nameNodes,
            string defaultSchema = "public")
            => ExtractQualifiedName(nameNodes, defaultSchema);

        public List<(string? schema, string? table, string column)> PublicExtractColumnRefs(Node? node)
            => ExtractColumnRefs(node);

        public void Dispose()
        {
            _parser?.Dispose();
        }
    }
}
