using mbulava.PostgreSql.Dac.Compile.Ast;
using NUnit.Framework;
using Npgquery;

namespace mbulava.PostgreSql.Dac.Tests.Compile.Ast;

/// <summary>
/// Tests for AST builder fluent API.
/// </summary>
[TestFixture]
[Category("AstBuilder")]
public class AstBuilderTests
{
    [Test]
    public void DropTable_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropTable("public", "users", ifExists: true, cascade: false);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop table"));
        Assert.That(sql.ToLower(), Does.Contain("if exists"));
        Assert.That(sql.ToLower(), Does.Contain("users"));
        
        // Verify it parses back correctly
        using var parser = new Parser();
        var result = parser.Parse(sql);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void DropTable_WithCascade_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropTable("public", "users", ifExists: true, cascade: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("cascade"));
    }

    [Test]
    public void DropView_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropView("public", "customer_view", ifExists: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop"));
        Assert.That(sql.ToLower(), Does.Contain("view"));
        Assert.That(sql.ToLower(), Does.Contain("customer_view"));
    }

    [Test]
    public void DropSequence_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropSequence("public", "users_id_seq", ifExists: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop sequence"));
        Assert.That(sql.ToLower(), Does.Contain("users_id_seq"));
    }

    [Test]
    public void DropFunction_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropFunction("public", "calculate_total", ifExists: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop"));
        Assert.That(sql.ToLower(), Does.Contain("function"));
        Assert.That(sql.ToLower(), Does.Contain("calculate_total"));
    }

    [Test]
    public void DropTrigger_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropTrigger("audit_trigger", "public", "users", ifExists: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop trigger"));
        Assert.That(sql.ToLower(), Does.Contain("audit_trigger"));
    }

    [Test]
    public void CreateTableSimple_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.CreateTableSimple("public", "users",
            ("id", "integer"),
            ("name", "text"),
            ("email", "varchar(255)"));

        // Act
        var sql = AstSqlGenerator.Generate(ast);

        // Assert
        Assert.That(sql.ToLower(), Does.Contain("create table"));
        Assert.That(sql.ToLower(), Does.Contain("users"));
        Assert.That(sql.ToLower(), Does.Contain("id"));
        // Deparse may normalize "integer" to "int"
        Assert.That(sql.ToLower(), Does.Match("int(eger)?"));
    }

    [Test]
    public void AlterTableAddColumn_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAddColumn("public", "users", "phone", "varchar(20)", notNull: false);

        // Act
        var sql = AstSqlGenerator.Generate(ast);

        // Assert
        Assert.That(sql.ToLower(), Does.Contain("alter table"));
        Assert.That(sql.ToLower(), Does.Contain("add"));
        Assert.That(sql.ToLower(), Does.Contain("phone"));
    }

    [Test]
    public void AlterTableAddColumn_WithNotNull_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAddColumn("public", "users", "phone", "varchar(20)", notNull: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("not null"));
    }

    [Test]
    public void AlterTableAddColumn_WithDefault_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAddColumn("public", "users", "active", "boolean", defaultValue: "true");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("default"));
    }

    [Test]
    public void AlterTableDropColumn_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableDropColumn("public", "users", "old_column", ifExists: true);

        // Act
        var sql = AstSqlGenerator.Generate(ast);

        // Assert
        Assert.That(sql.ToLower(), Does.Contain("alter table"));
        Assert.That(sql.ToLower(), Does.Contain("drop"));
        Assert.That(sql.ToLower(), Does.Contain("if exists"));
        Assert.That(sql.ToLower(), Does.Contain("old_column"));
    }

    [Test]
    public void AlterTableAlterColumnType_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAlterColumnType("public", "users", "age", "bigint");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("alter column"));
        Assert.That(sql.ToLower(), Does.Contain("type"));
        Assert.That(sql.ToLower(), Does.Contain("bigint"));
    }

    [Test]
    public void AlterTableAlterColumnSetNotNull_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAlterColumnSetNotNull("public", "users", "email");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("set not null"));
    }

    [Test]
    public void AlterTableAlterColumnDropNotNull_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAlterColumnDropNotNull("public", "users", "phone");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop not null"));
    }

    [Test]
    public void AlterTableAlterColumnSetDefault_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAlterColumnSetDefault("public", "users", "active", "true");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("set default"));
    }

    [Test]
    public void AlterTableAlterColumnDropDefault_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAlterColumnDropDefault("public", "users", "active");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop default"));
    }

    [Test]
    public void AlterTableAddConstraint_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableAddConstraint("public", "users", "uk_email", "UNIQUE (email)");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("add constraint"));
        Assert.That(sql.ToLower(), Does.Contain("uk_email"));
        Assert.That(sql.ToLower(), Does.Contain("unique"));
    }

    [Test]
    public void AlterTableDropConstraint_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableDropConstraint("public", "users", "uk_email", ifExists: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop constraint"));
        Assert.That(sql.ToLower(), Does.Contain("uk_email"));
    }

    [Test]
    public void AlterTableOwner_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.AlterTableOwner("public", "users", "new_owner");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("owner to"));
        Assert.That(sql.ToLower(), Does.Contain("new_owner"));
    }

    [Test]
    public void Grant_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.Grant("SELECT, INSERT", "TABLE", "public", "users", "app_user");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("grant"));
        Assert.That(sql.ToLower(), Does.Contain("select"));
        Assert.That(sql.ToLower(), Does.Contain("to app_user"));
    }

    [Test]
    public void Revoke_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.Revoke("DELETE", "TABLE", "public", "users", "app_user");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("revoke"));
        Assert.That(sql.ToLower(), Does.Contain("delete"));
        Assert.That(sql.ToLower(), Does.Contain("from app_user"));
    }

    [Test]
    public void CreateIndex_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.CreateIndex("idx_users_email", "public", "users", new[] { "email" });
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("create index"));
        Assert.That(sql.ToLower(), Does.Contain("idx_users_email"));
        Assert.That(sql.ToLower(), Does.Contain("email"));
    }

    [Test]
    public void CreateIndex_WithUnique_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.CreateIndex("uk_users_email", "public", "users", new[] { "email" }, unique: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("unique"));
    }

    [Test]
    public void CreateIndex_WithMultipleColumns_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.CreateIndex("idx_users_name_email", "public", "users", new[] { "name", "email" });
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("name"));
        Assert.That(sql.ToLower(), Does.Contain("email"));
    }

    [Test]
    public void DropIndex_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.DropIndex("public", "idx_users_email", ifExists: true);
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("drop index"));
        Assert.That(sql.ToLower(), Does.Contain("idx_users_email"));
    }

    [Test]
    public void CommentOn_GeneratesValidSQL()
    {
        // Arrange
        var ast = AstBuilder.CommentOn("TABLE", "public", "users", "Stores user information");
        
        // Act
        var sql = AstSqlGenerator.Generate(ast);
        
        // Assert
        Assert.That(sql.ToLower(), Does.Contain("comment on"));
        Assert.That(sql, Does.Contain("Stores user information"));
    }

    [Test]
    public void QuoteIdentifier_WithSimpleName_ReturnsUnquoted()
    {
        // Act
        var quoted = AstBuilder.QuoteIdentifier("users");
        
        // Assert
        Assert.That(quoted, Is.EqualTo("users"));
    }

    [Test]
    public void QuoteIdentifier_WithSpaces_ReturnsQuoted()
    {
        // Act
        var quoted = AstBuilder.QuoteIdentifier("user name");
        
        // Assert
        Assert.That(quoted, Is.EqualTo("\"user name\""));
    }

    [Test]
    public void QuoteIdentifier_WithReservedWord_ReturnsQuoted()
    {
        // Act
        var quoted = AstBuilder.QuoteIdentifier("select");
        
        // Assert
        Assert.That(quoted, Is.EqualTo("\"select\""));
    }

    [Test]
    public void QuoteIdentifier_AlreadyQuoted_ReturnsAsIs()
    {
        // Act
        var quoted = AstBuilder.QuoteIdentifier("\"already quoted\"");
        
        // Assert
        Assert.That(quoted, Is.EqualTo("\"already quoted\""));
    }

    [Test]
    public void RoundTrip_ComplexScenario_PreservesSemantics()
    {
        // Arrange - create table, add column, create index
        var createTable = AstBuilder.CreateTableSimple("public", "test_table",
            ("id", "serial PRIMARY KEY"),
            ("name", "text NOT NULL"));
        
        var addColumn = AstBuilder.AlterTableAddColumn("public", "test_table", "email", "varchar(255)");
        var createIndex = AstBuilder.CreateIndex("idx_email", "public", "test_table", new[] { "email" });
        
        // Act
        var sql1 = AstSqlGenerator.Generate(createTable);
        var sql2 = AstSqlGenerator.Generate(addColumn);
        var sql3 = AstSqlGenerator.Generate(createIndex);
        
        // Assert - all should parse successfully
        using var parser = new Parser();
        Assert.That(parser.Parse(sql1).IsSuccess, Is.True);
        Assert.That(parser.Parse(sql2).IsSuccess, Is.True);
        Assert.That(parser.Parse(sql3).IsSuccess, Is.True);
    }
}
