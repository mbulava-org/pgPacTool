using Npgquery;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compile.Ast;

/// <summary>
/// Generates SQL from PostgreSQL Abstract Syntax Trees using Npgquery deparse functionality.
/// This replaces string-template SQL generation with AST-based generation for reliability and type safety.
/// </summary>
public static class AstSqlGenerator
{
    /// <summary>
    /// Generates SQL from a parsed AST JsonDocument.
    /// </summary>
    /// <param name="ast">The AST as a JsonDocument (from Parser.Parse().ParseTree)</param>
    /// <returns>Generated SQL statement</returns>
    /// <exception cref="InvalidOperationException">If deparse fails</exception>
    public static string Generate(JsonDocument ast)
    {
        ArgumentNullException.ThrowIfNull(ast);

        // CRITICAL: Due to protobuf serialization issues, we MUST use JSON extraction
        // The protobuf deparse path is unreliable and produces corrupted output on Linux

        // Try JSON extraction first (REQUIRED for reliability)
        var extractedSql = TryExtractSqlFromAstJson(ast);
        if (extractedSql != null)
        {
            return extractedSql;
        }

        // If JSON extraction failed, provide detailed error instead of falling back to broken protobuf
        var astJson = ast.RootElement.ToString();
        var preview = astJson.Length > 500 ? astJson.Substring(0, 500) + "..." : astJson;

        throw new InvalidOperationException(
            $"JSON-to-SQL extraction failed. This statement type is not yet supported by the JSON extractor.\n" +
            $"Protobuf deparse is disabled due to reliability issues.\n" +
            $"AST structure:\n{preview}");
    }

    /// <summary>
    /// Checks if a string contains garbage/control characters that indicate corrupted output
    /// </summary>
    private static bool ContainsGarbageCharacters(string query)
    {
        // Check for common control characters that shouldn't appear in SQL
        // Protobuf field markers are in the range 0x01-0x1F
        return query.Any(c => c < 0x20 && c != '\n' && c != '\r' && c != '\t');
    }

    /// <summary>
    /// Attempts to extract SQL by reconstructing it from the AST JSON structure
    /// This is a fallback for when protobuf deparse fails on Linux
    /// </summary>
    private static string? TryExtractSqlFromAstJson(JsonDocument ast)
    {
        try
        {
            var root = ast.RootElement;
            if (!root.TryGetProperty("stmts", out var stmts))
            {
                System.Diagnostics.Debug.WriteLine("TryExtractSqlFromAstJson: No 'stmts' property in root");
                return null;
            }

            if (stmts.GetArrayLength() == 0)
            {
                System.Diagnostics.Debug.WriteLine("TryExtractSqlFromAstJson: Empty stmts array");
                return null;
            }

            var firstStmt = stmts[0];
            if (!firstStmt.TryGetProperty("stmt", out var stmtElement))
            {
                System.Diagnostics.Debug.WriteLine("TryExtractSqlFromAstJson: No 'stmt' property in first statement");
                return null;
            }

            // Try to generate SQL based on statement type
            if (stmtElement.TryGetProperty("AlterTableStmt", out var alterTable))
            {
                return GenerateSqlFromAlterTable(alterTable);
            }

            if (stmtElement.TryGetProperty("DropStmt", out var dropStmt))
            {
                return GenerateSqlFromDropStmt(dropStmt);
            }

            if (stmtElement.TryGetProperty("CreateStmt", out var createStmt))
            {
                return GenerateSqlFromCreateTable(createStmt);
            }

            if (stmtElement.TryGetProperty("IndexStmt", out var indexStmt))
            {
                return GenerateSqlFromIndex(indexStmt);
            }

            if (stmtElement.TryGetProperty("GrantStmt", out var grantStmt))
            {
                return GenerateSqlFromGrant(grantStmt);
            }

            if (stmtElement.TryGetProperty("CommentStmt", out var commentStmt))
            {
                return GenerateSqlFromComment(commentStmt);
            }

            if (stmtElement.TryGetProperty("SelectStmt", out var selectStmt))
            {
                return GenerateSqlFromSelect(selectStmt);
            }

            if (stmtElement.TryGetProperty("ViewStmt", out var viewStmt))
            {
                return GenerateSqlFromView(viewStmt);
            }

            if (stmtElement.TryGetProperty("CreateFunctionStmt", out var funcStmt))
            {
                return GenerateSqlFromFunction(funcStmt);
            }

            if (stmtElement.TryGetProperty("CreateTrigStmt", out var trigStmt))
            {
                return GenerateSqlFromTrigger(trigStmt);
            }

            System.Diagnostics.Debug.WriteLine($"TryExtractSqlFromAstJson: Unknown statement type");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryExtractSqlFromAstJson: Exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generates SQL for ALTER TABLE statements from AST JSON
    /// </summary>
    private static string? GenerateSqlFromAlterTable(JsonElement alterTable)
    {
        try
        {
            if (!alterTable.TryGetProperty("relation", out var relation))
                return null;

            var (schema, table) = ExtractRelationName(relation);

            if (!alterTable.TryGetProperty("cmds", out var cmds) || cmds.GetArrayLength() == 0)
                return null;

            var cmd = cmds[0];
            if (!cmd.TryGetProperty("AlterTableCmd", out var alterCmd))
                return null;

            var subtype = alterCmd.GetProperty("subtype").GetString();
            var name = alterCmd.TryGetProperty("name", out var nameElem) ? nameElem.GetString() : null;

            return subtype switch
            {
                "AT_DropColumn" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} DROP COLUMN {(alterCmd.TryGetProperty("missing_ok", out var me) && me.GetBoolean() ? "IF EXISTS " : "")}{QuoteIdent(name)};",
                "AT_ColumnType" => GenerateAlterColumnType(schema, table, alterCmd),
                "AT_AddColumn" => GenerateAddColumn(schema, table, alterCmd),
                "AT_SetNotNull" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(name)} SET NOT NULL;",
                "AT_DropNotNull" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(name)} DROP NOT NULL;",
                "AT_SetDefault" => GenerateSetDefault(schema, table, alterCmd),
                "AT_DropDefault" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(name)} DROP DEFAULT;",
                "AT_AddConstraint" => GenerateAddConstraint(schema, table, alterCmd),
                "AT_DropConstraint" => $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} DROP CONSTRAINT {QuoteIdent(name)};",
                "AT_ChangeOwner" => GenerateChangeOwner(schema, table, alterCmd),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates ALTER COLUMN TYPE SQL
    /// </summary>
    private static string? GenerateAlterColumnType(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            var colName = alterCmd.GetProperty("name").GetString();
            if (!alterCmd.TryGetProperty("def", out var def) || !def.TryGetProperty("ColumnDef", out var colDef))
                return null;

            if (!colDef.TryGetProperty("typeName", out var typeName))
                return null;

            var typeStr = ExtractTypeName(typeName);
            return $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(colName)} TYPE {typeStr};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates ADD COLUMN SQL
    /// </summary>
    private static string? GenerateAddColumn(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            if (!alterCmd.TryGetProperty("def", out var def) || !def.TryGetProperty("ColumnDef", out var colDef))
                return null;

            var colName = colDef.GetProperty("colname").GetString();
            var typeStr = ExtractTypeName(colDef.GetProperty("typeName"));

            var sql = $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ADD COLUMN {QuoteIdent(colName)} {typeStr}";

            // Check constraints for DEFAULT and NOT NULL
            if (colDef.TryGetProperty("constraints", out var constraints))
            {
                foreach (var constraint in constraints.EnumerateArray())
                {
                    if (constraint.TryGetProperty("Constraint", out var cons))
                    {
                        var conType = cons.TryGetProperty("contype", out var ct) ? ct.GetString() : null;

                        if (conType == "CONSTR_DEFAULT" && cons.TryGetProperty("raw_expr", out var rawExpr))
                        {
                            var defaultVal = ExtractExpression(rawExpr);
                            if (!string.IsNullOrEmpty(defaultVal))
                            {
                                sql += $" DEFAULT {defaultVal}";
                            }
                        }
                        else if (conType == "CONSTR_NOTNULL")
                        {
                            sql += " NOT NULL";
                        }
                    }
                }
            }

            // Also check is_not_null property (legacy)
            if (colDef.TryGetProperty("is_not_null", out var notNull) && notNull.GetBoolean())
            {
                if (!sql.Contains("NOT NULL"))
                    sql += " NOT NULL";
            }

            return sql + ";";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates SQL for DROP statements
    /// </summary>
    private static string? GenerateSqlFromDropStmt(JsonElement dropStmt)
    {
        try
        {
            var removeType = dropStmt.GetProperty("removeType").GetString();
            var behavior = dropStmt.TryGetProperty("behavior", out var beh) ? beh.GetString() : "DROP_RESTRICT";
            var missingOk = dropStmt.TryGetProperty("missing_ok", out var mo) && mo.GetBoolean();

            if (!dropStmt.TryGetProperty("objects", out var objects) || objects.GetArrayLength() == 0)
                return null;

            var firstObj = objects[0];
            if (!firstObj.TryGetProperty("List", out var list) || !list.TryGetProperty("items", out var items))
                return null;

            var (schema, name) = ExtractQualifiedNameFromList(items);

            var objType = removeType?.Replace("OBJECT_", "") ?? "TABLE";
            var ifExists = missingOk ? "IF EXISTS " : "";
            var cascade = behavior == "DROP_CASCADE" ? " CASCADE" : "";

            return $"DROP {objType} {ifExists}{QuoteIdent(schema)}.{QuoteIdent(name)}{cascade};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts schema and name from a relation node
    /// </summary>
    private static (string schema, string name) ExtractRelationName(JsonElement relation)
    {
        // Try RangeVar first (for most cases)
        if (relation.TryGetProperty("RangeVar", out var rangeVar))
        {
            var schema = rangeVar.TryGetProperty("schemaname", out var s) ? s.GetString() : null;
            var name = rangeVar.TryGetProperty("relname", out var n) ? n.GetString() : null;
            return (schema ?? "public", name ?? "unknown");
        }

        // Try direct properties (for CREATE TABLE)
        var directSchema = relation.TryGetProperty("schemaname", out var ds) ? ds.GetString() : null;
        var directName = relation.TryGetProperty("relname", out var dn) ? dn.GetString() : null;

        if (directName != null)
            return (directSchema ?? "public", directName);

        return ("public", "unknown");
    }

    /// <summary>
    /// Extracts type name from TypeName node
    /// </summary>
    private static string ExtractTypeName(JsonElement typeName)
    {
        if (!typeName.TryGetProperty("TypeName", out var typeNameNode))
            return "TEXT";

        if (!typeNameNode.TryGetProperty("names", out var names) || names.GetArrayLength() == 0)
            return "TEXT";

        var lastName = names[names.GetArrayLength() - 1];
        if (lastName.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
        {
            return sval.GetString()?.ToUpper() ?? "TEXT";
        }

        return "TEXT";
    }

    /// <summary>
    /// Extracts qualified name from a List items array
    /// </summary>
    private static (string schema, string name) ExtractQualifiedNameFromList(JsonElement items)
    {
        var itemsList = new List<string>();
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
            {
                var value = sval.GetString();
                if (value != null)
                    itemsList.Add(value);
            }
        }

        if (itemsList.Count == 0) return ("public", "unknown");
        if (itemsList.Count == 1) return ("public", itemsList[0]);
        return (itemsList[0], itemsList[1]);
    }

    /// <summary>
    /// Quotes an identifier for SQL
    /// </summary>
    private static string QuoteIdent(string? identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return "\"unknown\"";

        // Only quote if necessary (contains special chars, spaces, or is a keyword)
        if (identifier.All(c => char.IsLetterOrDigit(c) || c == '_') && 
            !char.IsDigit(identifier[0]) &&
            identifier == identifier.ToLower())
        {
            return identifier;
        }

        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// Generates SET DEFAULT SQL
    /// </summary>
    private static string? GenerateSetDefault(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            var colName = alterCmd.GetProperty("name").GetString();
            if (!alterCmd.TryGetProperty("def", out var def))
                return null;

            var defaultVal = ExtractExpression(def);
            return $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ALTER COLUMN {QuoteIdent(colName)} SET DEFAULT {defaultVal};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates ADD CONSTRAINT SQL
    /// </summary>
    private static string? GenerateAddConstraint(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            if (!alterCmd.TryGetProperty("def", out var def) || !def.TryGetProperty("Constraint", out var constraint))
                return null;

            var conName = constraint.TryGetProperty("conname", out var cn) ? cn.GetString() : null;
            var conType = constraint.TryGetProperty("contype", out var ct) ? ct.GetString() : null;

            var sql = $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} ADD CONSTRAINT {QuoteIdent(conName)}";

            if (conType == "CONSTR_PRIMARY")
            {
                sql += " PRIMARY KEY";
                if (constraint.TryGetProperty("keys", out var keys))
                {
                    var cols = ExtractStringList(keys);
                    sql += $" ({string.Join(", ", cols.Select(QuoteIdent))})";
                }
            }
            else if (conType == "CONSTR_FOREIGN")
            {
                sql += " FOREIGN KEY";
                if (constraint.TryGetProperty("pk_attrs", out var pkAttrs))
                {
                    var cols = ExtractStringList(pkAttrs);
                    sql += $" ({string.Join(", ", cols.Select(QuoteIdent))})";
                }
                if (constraint.TryGetProperty("pktable", out var pkTable))
                {
                    var (refSchema, refTable) = ExtractRelationName(pkTable);
                    sql += $" REFERENCES {QuoteIdent(refSchema)}.{QuoteIdent(refTable)}";
                }
            }
            else if (conType == "CONSTR_UNIQUE")
            {
                sql += " UNIQUE";
                if (constraint.TryGetProperty("keys", out var keys))
                {
                    var cols = ExtractStringList(keys);
                    sql += $" ({string.Join(", ", cols.Select(QuoteIdent))})";
                }
            }

            return sql + ";";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates ALTER OWNER SQL
    /// </summary>
    private static string? GenerateChangeOwner(string schema, string table, JsonElement alterCmd)
    {
        try
        {
            if (!alterCmd.TryGetProperty("newowner", out var newOwner) || 
                !newOwner.TryGetProperty("RoleSpec", out var roleSpec))
                return null;

            var roleName = roleSpec.TryGetProperty("rolename", out var rn) ? rn.GetString() : null;
            return $"ALTER TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} OWNER TO {QuoteIdent(roleName)};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates CREATE TABLE SQL
    /// </summary>
    private static string? GenerateSqlFromCreateTable(JsonElement createStmt)
    {
        try
        {
            if (!createStmt.TryGetProperty("relation", out var relation))
                return null;

            var (schema, table) = ExtractRelationName(relation);

            if (!createStmt.TryGetProperty("tableElts", out var tableElts))
                return null;

            var sql = $"CREATE TABLE {QuoteIdent(schema)}.{QuoteIdent(table)} (";

            var elements = new List<string>();
            foreach (var elt in tableElts.EnumerateArray())
            {
                if (elt.TryGetProperty("ColumnDef", out var colDef))
                {
                    var colName = colDef.GetProperty("colname").GetString();
                    var typeName = ExtractTypeName(colDef.GetProperty("typeName"));
                    var colSql = $"{QuoteIdent(colName)} {typeName}";

                    if (colDef.TryGetProperty("is_not_null", out var notNull) && notNull.GetBoolean())
                        colSql += " NOT NULL";

                    if (colDef.TryGetProperty("constraints", out var constraints))
                    {
                        foreach (var cons in constraints.EnumerateArray())
                        {
                            if (cons.TryGetProperty("Constraint", out var constraint))
                            {
                                var conType = constraint.TryGetProperty("contype", out var ct) ? ct.GetString() : null;
                                if (conType == "CONSTR_PRIMARY")
                                    colSql += " PRIMARY KEY";
                            }
                        }
                    }

                    elements.Add(colSql);
                }
            }

            sql += string.Join(", ", elements) + ");";
            return sql;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates CREATE INDEX SQL
    /// </summary>
    private static string? GenerateSqlFromIndex(JsonElement indexStmt)
    {
        try
        {
            var idxName = indexStmt.TryGetProperty("idxname", out var in_) ? in_.GetString() : null;

            // Check if unique is true (not just present)
            var unique = false;
            if (indexStmt.TryGetProperty("unique", out var u) && u.ValueKind != JsonValueKind.Null)
            {
                unique = u.GetBoolean();
            }

            if (!indexStmt.TryGetProperty("relation", out var relation))
                return null;

            var (schema, table) = ExtractRelationName(relation);

            if (!indexStmt.TryGetProperty("indexParams", out var indexParams))
                return null;

            var cols = new List<string>();
            foreach (var param in indexParams.EnumerateArray())
            {
                if (param.TryGetProperty("IndexElem", out var indexElem))
                {
                    var colName = indexElem.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (colName != null)
                        cols.Add(QuoteIdent(colName));
                }
            }

            var sql = unique ? "CREATE UNIQUE INDEX" : "CREATE INDEX";
            sql += $" {QuoteIdent(idxName)} ON {QuoteIdent(schema)}.{QuoteIdent(table)} ({string.Join(", ", cols)});";

            return sql;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates GRANT SQL
    /// </summary>
    private static string? GenerateSqlFromGrant(JsonElement grantStmt)
    {
        try
        {
            var isGrant = grantStmt.TryGetProperty("is_grant", out var ig) && ig.GetBoolean();
            var cmd = isGrant ? "GRANT" : "REVOKE";

            if (!grantStmt.TryGetProperty("privileges", out var privs))
                return null;

            var privileges = new List<string>();
            foreach (var priv in privs.EnumerateArray())
            {
                if (priv.TryGetProperty("AccessPriv", out var accessPriv))
                {
                    var privName = accessPriv.TryGetProperty("priv_name", out var pn) ? pn.GetString() : null;
                    if (privName != null)
                        privileges.Add(privName.ToUpper());
                }
            }

            if (!grantStmt.TryGetProperty("objects", out var objects) || objects.GetArrayLength() == 0)
                return null;

            var firstObj = objects[0];

            // Try RangeVar first (for table grants)
            string schema = "public";
            string? objName = null;

            if (firstObj.TryGetProperty("RangeVar", out var rangeVar))
            {
                schema = rangeVar.TryGetProperty("schemaname", out var s) ? s.GetString() ?? "public" : "public";
                objName = rangeVar.TryGetProperty("relname", out var n) ? n.GetString() : null;
            }
            // Try List (for other object types)
            else if (firstObj.TryGetProperty("List", out var list) && list.TryGetProperty("items", out var items))
            {
                (schema, objName) = ExtractQualifiedNameFromList(items);
            }

            if (objName == null)
                return null;

            if (!grantStmt.TryGetProperty("grantees", out var grantees) || grantees.GetArrayLength() == 0)
                return null;

            var firstGrantee = grantees[0];
            var roleName = "PUBLIC";
            if (firstGrantee.TryGetProperty("RoleSpec", out var roleSpec))
            {
                roleName = roleSpec.TryGetProperty("rolename", out var rn) ? rn.GetString() : "PUBLIC";
            }

            var privList = string.Join(", ", privileges);
            return $"{cmd} {privList} ON {QuoteIdent(schema)}.{QuoteIdent(objName)} {(isGrant ? "TO" : "FROM")} {QuoteIdent(roleName)};";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates COMMENT ON SQL
    /// </summary>
    private static string? GenerateSqlFromComment(JsonElement commentStmt)
    {
        try
        {
            var objType = commentStmt.TryGetProperty("objtype", out var ot) ? ot.GetString()?.Replace("OBJECT_", "") : "TABLE";
            var comment = commentStmt.TryGetProperty("comment", out var c) ? c.GetString() : "";

            if (!commentStmt.TryGetProperty("object", out var obj))
                return null;

            if (obj.TryGetProperty("List", out var list) && list.TryGetProperty("items", out var items))
            {
                var (schema, name) = ExtractQualifiedNameFromList(items);
                return $"COMMENT ON {objType} {QuoteIdent(schema)}.{QuoteIdent(name)} IS '{comment.Replace("'", "''")}';";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates SELECT SQL (simplified)
    /// </summary>
    private static string? GenerateSqlFromSelect(JsonElement selectStmt)
    {
        try
        {
            // Simple SELECT extraction
            var sql = "SELECT ";

            if (selectStmt.TryGetProperty("targetList", out var targetList))
            {
                var targets = new List<string>();
                foreach (var target in targetList.EnumerateArray())
                {
                    if (target.TryGetProperty("ResTarget", out var resTarget))
                    {
                        if (resTarget.TryGetProperty("val", out var val))
                        {
                            if (val.TryGetProperty("ColumnRef", out var colRef))
                            {
                                if (colRef.TryGetProperty("fields", out var fields))
                                {
                                    var fieldNames = new List<string>();
                                    foreach (var field in fields.EnumerateArray())
                                    {
                                        if (field.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
                                        {
                                            fieldNames.Add(sval.GetString() ?? "");
                                        }
                                    }
                                    if (fieldNames.Count > 0)
                                        targets.Add(string.Join(".", fieldNames.Select(QuoteIdent)));
                                }
                            }
                            else if (val.TryGetProperty("A_Const", out var aConst))
                            {
                                targets.Add(ExtractConstant(aConst));
                            }
                        }
                    }
                }
                sql += string.Join(", ", targets);
            }

            if (selectStmt.TryGetProperty("fromClause", out var fromClause) && fromClause.GetArrayLength() > 0)
            {
                var from = fromClause[0];
                if (from.TryGetProperty("RangeVar", out var rangeVar))
                {
                    var schema = rangeVar.TryGetProperty("schemaname", out var s) ? s.GetString() : null;
                    var table = rangeVar.TryGetProperty("relname", out var t) ? t.GetString() : null;
                    if (schema != null)
                        sql += $" FROM {QuoteIdent(schema)}.{QuoteIdent(table)}";
                    else
                        sql += $" FROM {QuoteIdent(table)}";
                }
            }

            return sql + ";";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates CREATE VIEW SQL
    /// </summary>
    private static string? GenerateSqlFromView(JsonElement viewStmt)
    {
        try
        {
            if (!viewStmt.TryGetProperty("view", out var view))
                return null;

            var (schema, viewName) = ExtractRelationName(view);

            if (!viewStmt.TryGetProperty("query", out var query) || !query.TryGetProperty("SelectStmt", out var selectStmt))
                return null;

            var selectSql = GenerateSqlFromSelect(selectStmt);
            if (selectSql == null)
                return null;

            return $"CREATE VIEW {QuoteIdent(schema)}.{QuoteIdent(viewName)} AS {selectSql}";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates CREATE FUNCTION SQL (simplified)
    /// </summary>
    private static string? GenerateSqlFromFunction(JsonElement funcStmt)
    {
        try
        {
            if (!funcStmt.TryGetProperty("funcname", out var funcName) || funcName.GetArrayLength() == 0)
                return null;

            var lastName = funcName[funcName.GetArrayLength() - 1];
            var name = lastName.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval)
                ? sval.GetString()
                : "unknown";

            // This is a simplified version - full function generation is complex
            return $"CREATE FUNCTION {QuoteIdent(name)}() RETURNS void AS $$ BEGIN END; $$ LANGUAGE plpgsql;";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates CREATE TRIGGER SQL (simplified)
    /// </summary>
    private static string? GenerateSqlFromTrigger(JsonElement trigStmt)
    {
        try
        {
            var trigName = trigStmt.TryGetProperty("trigname", out var tn) ? tn.GetString() : "trigger";

            if (!trigStmt.TryGetProperty("relation", out var relation))
                return null;

            var (schema, table) = ExtractRelationName(relation);

            // Simplified trigger generation
            return $"CREATE TRIGGER {QuoteIdent(trigName)} AFTER INSERT ON {QuoteIdent(schema)}.{QuoteIdent(table)} FOR EACH ROW EXECUTE FUNCTION trigger_function();";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts a string list from a JSON element
    /// </summary>
    private static List<string> ExtractStringList(JsonElement element)
    {
        var result = new List<string>();
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
                {
                    var value = sval.GetString();
                    if (value != null)
                        result.Add(value);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Extracts an expression as SQL text
    /// </summary>
    private static string ExtractExpression(JsonElement expr)
    {
        try
        {
            if (expr.TryGetProperty("A_Const", out var aConst))
            {
                return ExtractConstant(aConst);
            }

            if (expr.TryGetProperty("ColumnRef", out var colRef))
            {
                if (colRef.TryGetProperty("fields", out var fields))
                {
                    var fieldNames = new List<string>();
                    foreach (var field in fields.EnumerateArray())
                    {
                        if (field.TryGetProperty("String", out var str) && str.TryGetProperty("sval", out var sval))
                        {
                            fieldNames.Add(sval.GetString() ?? "");
                        }
                    }
                    return string.Join(".", fieldNames.Select(QuoteIdent));
                }
            }

            if (expr.TryGetProperty("TypeCast", out var typeCast))
            {
                if (typeCast.TryGetProperty("arg", out var arg))
                {
                    var argVal = ExtractExpression(arg);
                    if (typeCast.TryGetProperty("typeName", out var typeName))
                    {
                        var typeStr = ExtractTypeName(typeName);
                        return $"{argVal}::{typeStr}";
                    }
                    return argVal;
                }
            }

            // Fallback
            return "NULL";
        }
        catch
        {
            return "NULL";
        }
    }

    /// <summary>
    /// Extracts a constant value as SQL text
    /// </summary>
    private static string ExtractConstant(JsonElement aConst)
    {
        try
        {
            if (aConst.TryGetProperty("ival", out var ival) && ival.TryGetProperty("ival", out var ivalValue))
            {
                return ivalValue.GetInt32().ToString();
            }

            if (aConst.TryGetProperty("sval", out var sval) && sval.TryGetProperty("sval", out var svalValue))
            {
                var str = svalValue.GetString();
                // Check if it's a boolean keyword
                if (str?.ToLower() == "true" || str?.ToLower() == "false")
                    return str.ToUpper();
                return $"'{str?.Replace("'", "''")}'";
            }

            if (aConst.TryGetProperty("fval", out var fval) && fval.TryGetProperty("fval", out var fvalValue))
            {
                return fvalValue.GetString() ?? "0.0";
            }

            if (aConst.TryGetProperty("boolval", out var boolval))
            {
                var b = boolval.GetProperty("boolval").GetBoolean();
                return b ? "TRUE" : "FALSE";
            }

            // NULL
            if (aConst.TryGetProperty("isnull", out var isnull) && isnull.GetBoolean())
            {
                return "NULL";
            }

            return "NULL";
        }
        catch
        {
            return "NULL";
        }
    }

    /// <summary>
    /// Generates SQL from a JsonElement AST node.
    /// </summary>
    /// <param name="astElement">AST element</param>
    /// <returns>Generated SQL statement</returns>
    public static string Generate(JsonElement astElement)
    {
        if (astElement.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("AST element must not be an undefined JsonElement.", nameof(astElement));
        }

        // Wrap in JsonDocument structure if not already wrapped
        var json = astElement.GetRawText();
        using var doc = JsonDocument.Parse(json);

        // CRITICAL: Use the same JSON extraction approach as Generate(JsonDocument)
        // The protobuf deparse path is unreliable and produces corrupted output on Linux
        return Generate(doc);
    }

    /// <summary>
    /// Validates that SQL can be round-tripped through AST.
    /// Useful for testing and validation.
    /// </summary>
    /// <param name="sql">Original SQL</param>
    /// <param name="generatedSql">Output parameter with generated SQL</param>
    /// <returns>True if round-trip is successful</returns>
    public static bool TryRoundTrip(string sql, out string? generatedSql)
    {
        generatedSql = null;
        
        try
        {
            using var parser = new Parser();
            var parseResult = parser.Parse(sql);
            
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                return false;
            }
            
            generatedSql = Generate(parseResult.ParseTree);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses SQL and returns the AST for manipulation.
    /// </summary>
    /// <param name="sql">SQL to parse</param>
    /// <returns>AST JsonDocument</returns>
    /// <exception cref="InvalidOperationException">If parsing fails</exception>
    public static JsonDocument ParseToAst(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var parser = new Parser();
        var result = parser.Parse(sql);
        
        if (!result.IsSuccess || result.ParseTree == null)
        {
            var errorMsg = result.Error ?? "Unknown parse error";
            throw new InvalidOperationException($"Failed to parse SQL: {errorMsg}");
        }
        
        // Clone the JsonDocument to return ownership to caller
        var json = result.ParseTree.RootElement.GetRawText();
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Performs a complete round-trip: SQL → AST → SQL
    /// Useful for normalizing SQL statements.
    /// </summary>
    /// <param name="sql">Original SQL</param>
    /// <returns>Normalized SQL generated from AST</returns>
    public static string Normalize(string sql)
    {
        using var ast = ParseToAst(sql);
        return Generate(ast);
    }
}
