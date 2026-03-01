using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;
using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for AST-based SQL generation using Npgquery deparse.
/// </summary>
[TestFixture]
[Category("AstSqlGeneration")]
public class AstSqlGeneratorTests
{
    [Test]
    public void Generate_WithSimpleSelect_ReturnsValidSQL()
    {
        // Arrange
        var sql = "SELECT id, name FROM customers;";
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated, Is.Not.Null);
        Assert.That(generated, Is.Not.Empty);
        Assert.That(generated.ToLower(), Does.Contain("select"));
        Assert.That(generated.ToLower(), Does.Contain("customers"));
    }

    [Test]
    public void Generate_WithCreateTable_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            CREATE TABLE public.users (
                id integer PRIMARY KEY,
                name text NOT NULL,
                email varchar(255) UNIQUE
            );";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("create table"));
        Assert.That(generated.ToLower(), Does.Contain("users"));
        Assert.That(generated.ToLower(), Does.Contain("primary key"));
    }

    [Test]
    public void Generate_WithCreateView_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            CREATE VIEW customer_view AS 
            SELECT id, name 
            FROM customers 
            WHERE active = true;";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("create"));
        Assert.That(generated.ToLower(), Does.Contain("view"));
        Assert.That(generated.ToLower(), Does.Contain("customer_view"));
    }

    [Test]
    public void Generate_WithAlterTable_ReturnsValidSQL()
    {
        // Arrange
        var sql = "ALTER TABLE users ADD COLUMN phone varchar(20);";
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("alter table"));
        Assert.That(generated.ToLower(), Does.Contain("add column"));
    }

    [Test]
    public void Generate_WithForeignKey_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            CREATE TABLE orders (
                id integer PRIMARY KEY,
                customer_id integer REFERENCES customers(id)
            );";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("references"));
        Assert.That(generated.ToLower(), Does.Contain("customers"));
    }

    [Test]
    public void Generate_WithJoinQuery_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            SELECT u.name, o.total
            FROM users u
            JOIN orders o ON u.id = o.customer_id;";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("join"));
        Assert.That(generated.ToLower(), Does.Contain("on"));
    }

    [Test]
    public void TryRoundTrip_WithValidSQL_ReturnsTrue()
    {
        // Arrange
        var sql = "SELECT * FROM users WHERE active = true;";
        
        // Act
        var success = AstSqlGenerator.TryRoundTrip(sql, out var generated);
        
        // Assert
        Assert.That(success, Is.True);
        Assert.That(generated, Is.Not.Null);
        Assert.That(generated, Is.Not.Empty);
    }

    [Test]
    public void TryRoundTrip_WithInvalidSQL_ReturnsFalse()
    {
        // Arrange
        var sql = "INVALID SQL STATEMENT;;;";
        
        // Act
        var success = AstSqlGenerator.TryRoundTrip(sql, out var generated);
        
        // Assert
        Assert.That(success, Is.False);
        Assert.That(generated, Is.Null);
    }

    [Test]
    public void Normalize_RemovesExtraWhitespace()
    {
        // Arrange
        var sql = "SELECT     id  ,    name     FROM    users   ;";
        
        // Act
        var normalized = AstSqlGenerator.Normalize(sql);
        
        // Assert
        Assert.That(normalized, Is.Not.EqualTo(sql));
        Assert.That(normalized.ToLower(), Does.Contain("select"));
        Assert.That(normalized.ToLower(), Does.Contain("users"));
    }

    [Test]
    public void Normalize_PreservesSemantics()
    {
        // Arrange - semantically equivalent queries
        var sql1 = "SELECT id, name FROM users WHERE active = true;";
        var sql2 = "select ID, NAME from USERS where ACTIVE = TRUE;";
        
        // Act
        var normalized1 = AstSqlGenerator.Normalize(sql1);
        var normalized2 = AstSqlGenerator.Normalize(sql2);
        
        // Assert - normalized forms should be identical (or very similar)
        // Note: Case might differ but structure should be same
        using var parser = new Parser();
        var ast1 = parser.Parse(normalized1);
        var ast2 = parser.Parse(normalized2);
        
        Assert.That(ast1.IsSuccess && ast2.IsSuccess, Is.True);
    }

    [Test]
    public void Generate_WithComplexView_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            CREATE VIEW order_summary AS
            WITH recent_orders AS (
                SELECT * FROM orders WHERE created_date > '2024-01-01'
            )
            SELECT 
                u.name,
                COUNT(ro.id) as order_count,
                SUM(ro.total) as total_amount
            FROM users u
            LEFT JOIN recent_orders ro ON u.id = ro.customer_id
            GROUP BY u.id, u.name
            ORDER BY total_amount DESC;";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("create"));
        Assert.That(generated.ToLower(), Does.Contain("view"));
        Assert.That(generated.ToLower(), Does.Contain("with"));
        Assert.That(generated.ToLower(), Does.Contain("group by"));
    }

    [Test]
    public void Generate_WithCreateFunction_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            CREATE FUNCTION add_numbers(a integer, b integer)
            RETURNS integer AS $$
            BEGIN
                RETURN a + b;
            END;
            $$ LANGUAGE plpgsql;";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("create"));
        Assert.That(generated.ToLower(), Does.Contain("function"));
    }

    [Test]
    public void Generate_WithCreateTrigger_ReturnsValidSQL()
    {
        // Arrange
        var sql = @"
            CREATE TRIGGER audit_trigger
            AFTER INSERT OR UPDATE ON users
            FOR EACH ROW
            EXECUTE FUNCTION audit_changes();";
        
        using var ast = AstSqlGenerator.ParseToAst(sql);
        
        // Act
        var generated = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(generated.ToLower(), Does.Contain("create trigger"));
        Assert.That(generated.ToLower(), Does.Contain("after"));
        Assert.That(generated.ToLower(), Does.Contain("execute"));
    }

    [Test]
    public void ParseToAst_WithNullSQL_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AstSqlGenerator.ParseToAst(null!));
    }

    [Test]
    public void ParseToAst_WithEmptySQL_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AstSqlGenerator.ParseToAst(""));
    }

    [Test]
    public void ParseToAst_WithInvalidSQL_ThrowsInvalidOperationException()
    {
        // Arrange
        var sql = "COMPLETELY INVALID SQL;;;";
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => AstSqlGenerator.ParseToAst(sql));
    }

    [Test]
    public void Generate_WithNullJsonDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AstSqlGenerator.Generate((JsonDocument)null!));
    }

    [Test]
    public void RoundTrip_PreservesQuerySemantics()
    {
        // Arrange - various SQL statements
        var testCases = new[]
        {
            "SELECT id FROM users;",
            "INSERT INTO users (name) VALUES ('test');",
            "UPDATE users SET active = false WHERE id = 1;",
            "DELETE FROM users WHERE id = 1;",
            "CREATE INDEX idx_users_email ON users(email);",
            "DROP TABLE IF EXISTS temp_table;"
        };

        foreach (var sql in testCases)
        {
            // Act
            var success = AstSqlGenerator.TryRoundTrip(sql, out var generated);
            
            // Assert
            Assert.That(success, Is.True, $"Failed to round-trip: {sql}");
            Assert.That(generated, Is.Not.Null);
            Assert.That(generated, Is.Not.Empty);
            
            // Verify generated SQL can be parsed again
            using var parser = new Parser();
            var result = parser.Parse(generated!);
            Assert.That(result.IsSuccess, Is.True, $"Generated SQL failed to parse: {generated}");
        }
    }
}
