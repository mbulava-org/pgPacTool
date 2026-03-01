using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Builds PostgreSQL Abstract Syntax Trees programmatically using proper JSON AST structures.
/// All methods construct JSON AST nodes directly without string SQL templates.
/// See docs/architecture/AST_JSON_FORMAT.md for AST format documentation.
/// </summary>
public static class AstBuilder
{
    private const int PG_VERSION = 170004; // PostgreSQL 17.0.4

    /// <summary>
    /// Wraps a statement in the standard ParseResult structure.
    /// </summary>
    private static JsonElement WrapStatement(object stmtContent)
    {
        var wrapper = new
        {
            version = PG_VERSION,
            stmts = new[]
            {
                new
                {
                    stmt = stmtContent,
                    stmt_len = 0
                }
            }
        };

        var json = JsonSerializer.Serialize(wrapper);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a DROP TABLE statement AST.
    /// </summary>
    public static JsonElement DropTable(string schema, string tableName, bool ifExists = true, bool cascade = false)
    {
        var stmt = new
        {
            DropStmt = new
            {
                objects = new[]
                {
                    new
                    {
                        List = new
                        {
                            items = new object[]
                            {
                                new { String = new { sval = schema } },
                                new { String = new { sval = tableName } }
                            }
                        }
                    }
                },
                removeType = "OBJECT_TABLE",
                behavior = cascade ? "DROP_CASCADE" : "DROP_RESTRICT",
                missing_ok = ifExists
            }
        };

        return WrapStatement(stmt);
    }

    /// <summary>
    /// Creates a DROP VIEW statement AST.
    /// </summary>
    public static JsonElement DropView(string schema, string viewName, bool ifExists = true, bool cascade = false)
    {
        var stmt = new
        {
            DropStmt = new
            {
                objects = new[]
                {
                    new
                    {
                        List = new
                        {
                            items = new object[]
                            {
                                new { String = new { sval = schema } },
                                new { String = new { sval = viewName } }
                            }
                        }
                    }
                },
                removeType = "OBJECT_VIEW",
                behavior = cascade ? "DROP_CASCADE" : "DROP_RESTRICT",
                missing_ok = ifExists
            }
        };

        return WrapStatement(stmt);
    }

    /// <summary>
    /// Creates a DROP SEQUENCE statement AST.
    /// </summary>
    public static JsonElement DropSequence(string schema, string sequenceName, bool ifExists = true, bool cascade = false)
    {
        var stmt = new
        {
            DropStmt = new
            {
                objects = new[]
                {
                    new
                    {
                        List = new
                        {
                            items = new object[]
                            {
                                new { String = new { sval = schema } },
                                new { String = new { sval = sequenceName } }
                            }
                        }
                    }
                },
                removeType = "OBJECT_SEQUENCE",
                behavior = cascade ? "DROP_CASCADE" : "DROP_RESTRICT",
                missing_ok = ifExists
            }
        };

        return WrapStatement(stmt);
    }

    /// <summary>
    /// Creates a DROP FUNCTION statement AST.
    /// </summary>
    public static JsonElement DropFunction(string schema, string functionName, bool ifExists = true, bool cascade = false)
    {
        var stmt = new
        {
            DropStmt = new
            {
                objects = new[]
                {
                    new
                    {
                        List = new
                        {
                            items = new object[]
                            {
                                new { String = new { sval = schema } },
                                new { String = new { sval = functionName } }
                            }
                        }
                    }
                },
                removeType = "OBJECT_FUNCTION",
                behavior = cascade ? "DROP_CASCADE" : "DROP_RESTRICT",
                missing_ok = ifExists
            }
        };

        return WrapStatement(stmt);
    }

    /// <summary>
    /// Creates a DROP TRIGGER statement AST.
    /// </summary>
    public static JsonElement DropTrigger(string triggerName, string schema, string tableName, bool ifExists = true)
    {
        var stmt = new
        {
            DropStmt = new
            {
                objects = new[]
                {
                    new
                    {
                        List = new
                        {
                            items = new object[]
                            {
                                new { String = new { sval = schema } },
                                new { String = new { sval = tableName } },
                                new { String = new { sval = triggerName } }
                            }
                        }
                    }
                },
                removeType = "OBJECT_TRIGGER",
                behavior = "DROP_RESTRICT",
                missing_ok = ifExists
            }
        };

        return WrapStatement(stmt);
    }

    /// <summary>
    /// Creates a DROP INDEX statement AST.
    /// </summary>
    public static JsonElement DropIndex(string schema, string indexName, bool ifExists = true, bool cascade = false)
    {
        var stmt = new
        {
            DropStmt = new
            {
                objects = new[]
                {
                    new
                    {
                        List = new
                        {
                            items = new object[]
                            {
                                new { String = new { sval = schema } },
                                new { String = new { sval = indexName } }
                            }
                        }
                    }
                },
                removeType = "OBJECT_INDEX",
                behavior = cascade ? "DROP_CASCADE" : "DROP_RESTRICT",
                missing_ok = ifExists
            }
        };

        return WrapStatement(stmt);
    }

    /// <summary>
    /// For complex DDL operations not yet implemented as pure AST builders,
    /// we use parse-then-return pattern as a temporary bridge.
    /// TODO: Implement these as pure AST builders.
    /// </summary>

    /// <summary>
    /// Creates a simple CREATE TABLE statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement CreateTableSimple(string schema, string tableName, params (string columnName, string dataType)[] columns)
    {
        // Temporary: Use parse-then-return until we implement full CreateStmt builder
        var columnDefs = string.Join(", ", columns.Select(c => $"{QuoteIdentifier(c.columnName)} {c.dataType}"));
        var sql = $"CREATE TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ({columnDefs});";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ADD COLUMN statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAddColumn(string schema, string tableName, string columnName, string dataType, bool notNull = false, string? defaultValue = null)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ADD COLUMN {QuoteIdentifier(columnName)} {dataType}";
        if (notNull) sql += " NOT NULL";
        if (defaultValue != null) sql += $" DEFAULT {defaultValue}";
        sql += ";";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE DROP COLUMN statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableDropColumn(string schema, string tableName, string columnName, bool ifExists = true)
    {
        // Temporary: Use parse-then-return
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} DROP COLUMN {ifExistsClause}{QuoteIdentifier(columnName)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN TYPE statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAlterColumnType(string schema, string tableName, string columnName, string newDataType)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} TYPE {newDataType};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN SET NOT NULL statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAlterColumnSetNotNull(string schema, string tableName, string columnName)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} SET NOT NULL;";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN DROP NOT NULL statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAlterColumnDropNotNull(string schema, string tableName, string columnName)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} DROP NOT NULL;";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN SET DEFAULT statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAlterColumnSetDefault(string schema, string tableName, string columnName, string defaultValue)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} SET DEFAULT {defaultValue};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ALTER COLUMN DROP DEFAULT statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAlterColumnDropDefault(string schema, string tableName, string columnName)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ALTER COLUMN {QuoteIdentifier(columnName)} DROP DEFAULT;";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE ADD CONSTRAINT statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableAddConstraint(string schema, string tableName, string constraintName, string constraintDefinition)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ADD CONSTRAINT {QuoteIdentifier(constraintName)} {constraintDefinition};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE DROP CONSTRAINT statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableDropConstraint(string schema, string tableName, string constraintName, bool ifExists = true)
    {
        // Temporary: Use parse-then-return
        var ifExistsClause = ifExists ? "IF EXISTS " : "";
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} DROP CONSTRAINT {ifExistsClause}{QuoteIdentifier(constraintName)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates an ALTER TABLE OWNER TO statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement AlterTableOwner(string schema, string tableName, string newOwner)
    {
        // Temporary: Use parse-then-return
        var sql = $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} OWNER TO {QuoteIdentifier(newOwner)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a GRANT statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement Grant(string privileges, string objectType, string schema, string objectName, string grantee)
    {
        // Temporary: Use parse-then-return
        var sql = $"GRANT {privileges} ON {objectType} {QuoteIdentifier(schema)}.{QuoteIdentifier(objectName)} TO {QuoteIdentifier(grantee)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a REVOKE statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement Revoke(string privileges, string objectType, string schema, string objectName, string grantee)
    {
        // Temporary: Use parse-then-return
        var sql = $"REVOKE {privileges} ON {objectType} {QuoteIdentifier(schema)}.{QuoteIdentifier(objectName)} FROM {QuoteIdentifier(grantee)};";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a CREATE INDEX statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement CreateIndex(string indexName, string schema, string tableName, string[] columns, bool unique = false, bool ifNotExists = false)
    {
        // Temporary: Use parse-then-return
        var uniqueKeyword = unique ? "UNIQUE " : "";
        var ifNotExistsKeyword = ifNotExists ? "IF NOT EXISTS " : "";
        var columnList = string.Join(", ", columns.Select(QuoteIdentifier));
        var sql = $"CREATE {uniqueKeyword}INDEX {ifNotExistsKeyword}{QuoteIdentifier(indexName)} ON {QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)} ({columnList});";

        using var doc = AstSqlGenerator.ParseToAst(sql);
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Creates a COMMENT ON statement AST.
    /// TODO: Implement as pure AST builder.
    /// </summary>
    public static JsonElement CommentOn(string objectType, string schema, string objectName, string comment)
    {
        // Temporary: Use parse-then-return
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
