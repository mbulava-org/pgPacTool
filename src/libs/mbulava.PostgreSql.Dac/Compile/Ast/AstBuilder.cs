using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Fluent API for building PostgreSQL Abstract Syntax Trees programmatically.
/// Uses SQL parsing to generate reliable AST structures.
/// </summary>
public static class AstBuilder
{
    /// <summary>
    /// Creates a DROP TABLE statement AST.
    /// </summary>
    public static JsonElement DropTable(string schema, string tableName, bool ifExists = true, bool cascade = false)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var cascadeClause = cascade ? " CASCADE" : "";
        var sql = $"DROP TABLE {ifExistsClause}{QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)}{cascadeClause};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a DROP VIEW statement AST.
    /// </summary>
    public static JsonElement DropView(string schema, string viewName, bool ifExists = true, bool cascade = false)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var cascadeClause = cascade ? " CASCADE" : "";
        var sql = $"DROP VIEW {ifExistsClause}{QuoteIdentifier(schema)}.{QuoteIdentifier(viewName)}{cascadeClause};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a DROP SEQUENCE statement AST.
    /// </summary>
    public static JsonElement DropSequence(string schema, string sequenceName, bool ifExists = true, bool cascade = false)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var cascadeClause = cascade ? " CASCADE" : "";
        var sql = $"DROP SEQUENCE {ifExistsClause}{QuoteIdentifier(schema)}.{QuoteIdentifier(sequenceName)}{cascadeClause};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a DROP FUNCTION statement AST.
    /// </summary>
    public static JsonElement DropFunction(string schema, string functionName, bool ifExists = true, bool cascade = false)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var cascadeClause = cascade ? " CASCADE" : "";
        var sql = $"DROP FUNCTION {ifExistsClause}{QuoteIdentifier(schema)}.{QuoteIdentifier(functionName)}{cascadeClause};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a DROP TRIGGER statement AST.
    /// </summary>
    public static JsonElement DropTrigger(string triggerName, string schema, string tableName, bool ifExists = true)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var sql = $"DROP TRIGGER {ifExistsClause}{QuoteIdentifier(triggerName)} ON {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a simple CREATE TABLE statement AST.
    /// </summary>
    public static JsonElement CreateTableSimple(string schema, string tableName, params (string columnName, string dataType)[] columns)
    {
        var columnDefs = string.Join(", ", columns.Select(c => $"{QuoteIdentifier(c.columnName)} {c.dataType}"));
        var sql = $"CREATE TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ({columnDefs});";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ADD COLUMN statement AST.
    /// </summary>
    public static JsonElement AlterTableAddColumn(string schema, string tableName, string columnName, string dataType, bool notNull = false, string? defaultValue = null)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ADD COLUMN {QuoteIdentifier(columnName)} {dataType}";
        if (notNull) sql += " NOT NULL";
        if (defaultValue != null) sql += $" DEFAULT {defaultValue}";
        sql += ";";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE DROP COLUMN statement AST.
    /// </summary>
    public static JsonElement AlterTableDropColumn(string schema, string tableName, string columnName, bool ifExists = true)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} DROP COLUMN {ifExistsClause}{QuoteIdentifier(columnName)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN TYPE statement AST.
    /// </summary>
    public static JsonElement AlterTableAlterColumnType(string schema, string tableName, string columnName, string newDataType)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} TYPE {newDataType};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN SET NOT NULL statement AST.
    /// </summary>
    public static JsonElement AlterTableAlterColumnSetNotNull(string schema, string tableName, string columnName)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} SET NOT NULL;";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN DROP NOT NULL statement AST.
    /// </summary>
    public static JsonElement AlterTableAlterColumnDropNotNull(string schema, string tableName, string columnName)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} DROP NOT NULL;";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN SET DEFAULT statement AST.
    /// </summary>
    public static JsonElement AlterTableAlterColumnSetDefault(string schema, string tableName, string columnName, string defaultValue)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} SET DEFAULT {defaultValue};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN DROP DEFAULT statement AST.
    /// </summary>
    public static JsonElement AlterTableAlterColumnDropDefault(string schema, string tableName, string columnName)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} DROP DEFAULT;";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ADD CONSTRAINT statement AST.
    /// </summary>
    public static JsonElement AlterTableAddConstraint(string schema, string tableName, string constraintName, string constraintDefinition)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ADD CONSTRAINT {QuoteIdentifier(constraintName)} {constraintDefinition};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE DROP CONSTRAINT statement AST.
    /// </summary>
    public static JsonElement AlterTableDropConstraint(string schema, string tableName, string constraintName, bool ifExists = true)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} DROP CONSTRAINT {ifExistsClause}{QuoteIdentifier(constraintName)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE OWNER TO statement AST.
    /// </summary>
    public static JsonElement AlterTableOwner(string schema, string tableName, string newOwner)
    {
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} OWNER TO {QuoteIdentifier(newOwner)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a GRANT statement AST.
    /// </summary>
    public static JsonElement Grant(string privileges, string objectType, string schema, string objectName, string grantee)
    {
        var sql = $"GRANT {privileges} ON {objectType} {QuoteIdentifier(schema)}.{QuoteIdentifier(objectName)} TO {QuoteIdentifier(grantee)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a REVOKE statement AST.
    /// </summary>
    public static JsonElement Revoke(string privileges, string objectType, string schema, string objectName, string grantee)
    {
        var sql = $"REVOKE {privileges} ON {objectType} {QuoteIdentifier(schema)}.{QuoteIdentifier(objectName)} FROM {QuoteIdentifier(grantee)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a CREATE INDEX statement AST.
    /// </summary>
    public static JsonElement CreateIndex(string indexName, string schema, string tableName, string[] columns, bool unique = false, bool ifNotExists = false)
    {
        var uniqueKeyword = unique ? "UNIQUE " : "";
        var ifNotExistsKeyword = ifNotExists ? "IF NOT EXISTS " : "";
        var columnList = string.Join(", ", columns.Select(QuoteIdentifier));
        var sql = $"CREATE {uniqueKeyword}INDEX {ifNotExistsKeyword}{QuoteIdentifier(indexName)} ON {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ({columnList});";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a DROP INDEX statement AST.
    /// </summary>
    public static JsonElement DropIndex(string schema, string indexName, bool ifExists = true, bool cascade = false)
    {
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var cascadeClause = cascade ? " CASCADE" : "";
        var sql = $"DROP INDEX {ifExistsClause}{QuoteIdentifier(schema)}.{QuoteIdentifier(indexName)}{cascadeClause};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a COMMENT ON statement AST.
    /// </summary>
    public static JsonElement CommentOn(string objectType, string schema, string objectName, string comment)
    {
        var escapedComment = comment.Replace("'", "''");
        var sql = $"COMMENT ON {objectType} {QuoteIdentifier(schema)}.{QuoteIdentifier(objectName)} IS '{escapedComment}';";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Helper to quote identifiers safely.
    /// </summary>
    public static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return identifier;

        // If already quoted, return as-is
        if (identifier.StartsWith("\"") && identifier.EndsWith("\""))
            return identifier;

        // Check if needs quoting
        if (NeedsQuoting(identifier))
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        return identifier;
    }

    private static bool NeedsQuoting(string identifier)
    {
        // Needs quoting if:
        // - Contains special characters
        // - Starts with digit
        // - Is a reserved word
        // - Contains uppercase letters (PostgreSQL folds to lowercase unless quoted)

        if (identifier.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            return true;

        if (char.IsDigit(identifier[0]))
            return true;

        if (identifier.Any(char.IsUpper))
            return true;

        if (IsReservedWord(identifier))
            return true;

        return false;
    }

    private static bool IsReservedWord(string word)
    {
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "select", "from", "where", "table", "create", "alter", "drop",
            "insert", "update", "delete", "grant", "revoke", "user", "role",
            "index", "view", "sequence", "function", "trigger", "constraint",
            "primary", "foreign", "key", "references", "check", "unique",
            "default", "null", "not", "and", "or", "in", "like", "between",
            "join", "inner", "left", "right", "outer", "on", "as", "order",
            "group", "by", "having", "limit", "offset", "union", "intersect",
            "except", "all", "distinct", "case", "when", "then", "else", "end"
        };
        return reserved.Contains(word);
    }
}
