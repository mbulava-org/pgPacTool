using mbulava.PostgreSql.Dac.Models;
using mbulava.PostgreSql.Dac.Deployment;
using mbulava.PostgreSql.Dac.Compile.Ast;
using System.Text;
using System.Text.Json;

namespace mbulava.PostgreSql.Dac.Compare;

/// <summary>
/// Generates SQL deployment scripts from schema differences.
/// </summary>
public static class PublishScriptGenerator
{
    /// <summary>
    /// Generates a complete deployment script from schema differences.
    /// </summary>
    /// <param name="diff">Schema differences to script</param>
    /// <param name="options">Publishing options (optional)</param>
    /// <returns>SQL deployment script</returns>
    public static string Generate(PgSchemaDiff diff, PublishOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(diff);
        options ??= new PublishOptions();

        var sb = new StringBuilder();

        // Header
        if (options.IncludeComments)
        {
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine($"-- PostgreSQL Deployment Script");
            sb.AppendLine($"-- Schema: {diff.SchemaName}");
            sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine();
        }

        // Transaction begin
        if (options.Transactional)
        {
            sb.AppendLine("BEGIN;");
            sb.AppendLine();
        }

        // Pre-deployment scripts
        if (options.PreDeploymentScripts.Count > 0)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- ============================================================================");
                sb.AppendLine("-- PRE-DEPLOYMENT SCRIPTS");
                sb.AppendLine("-- ============================================================================");
                sb.AppendLine();
            }

            var combined = PrePostDeploymentScriptManager.CombineScripts(
                options.PreDeploymentScripts,
                options.IncludeComments);

            sb.AppendLine(combined);
        }

        // Schema changes header
        if (options.IncludeComments)
        {
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine("-- SCHEMA CHANGES");
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine();
        }

        // Generate SQL for each object type in dependency order
        // 1. Types (must come first - used by tables/functions)
        GenerateTypeScripts(diff.TypeDiffs, sb, options);

        // 2. Sequences (may be used in table defaults)
        GenerateSequenceScripts(diff.SequenceDiffs, sb, options);

        // 3. Tables (structure changes)
        GenerateTableScripts(diff.TableDiffs, sb, options);

        // 4. Views (depend on tables)
        GenerateViewScripts(diff.ViewDiffs, sb, options);

        // 5. Functions (may be used by triggers/views)
        GenerateFunctionScripts(diff.FunctionDiffs, sb, options);

        // 6. Triggers (depend on tables and functions)
        GenerateTriggerScripts(diff.TriggerDiffs, sb, options);

        // Post-deployment scripts
        if (options.PostDeploymentScripts.Count > 0)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine("-- ============================================================================");
                sb.AppendLine("-- POST-DEPLOYMENT SCRIPTS");
                sb.AppendLine("-- ============================================================================");
                sb.AppendLine();
            }

            var combined = PrePostDeploymentScriptManager.CombineScripts(
                options.PostDeploymentScripts,
                options.IncludeComments);

            sb.AppendLine(combined);
        }

        // Transaction commit
        if (options.Transactional)
        {
            sb.AppendLine();
            sb.AppendLine("COMMIT;");
        }

        // Footer
        if (options.IncludeComments)
        {
            sb.AppendLine();
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine("-- DEPLOYMENT COMPLETE");
            sb.AppendLine("-- ============================================================================");
        }

        var script = sb.ToString();

        // Apply SQLCMD variable replacement if variables are provided
        if (options.Variables.Count > 0)
        {
            script = SqlCmdVariableParser.ReplaceVariables(
                script,
                options.Variables,
                throwOnUndefined: false);
        }

        return script;
    }

    /// <summary>
    /// Helper method to append AST-generated SQL to the script.
    /// </summary>
    private static void AppendAstSql(StringBuilder sb, JsonElement ast)
    {
        var sql = AstSqlGenerator.Generate(ast);
        sb.AppendLine(sql);
    }

    /// <summary>
    /// Splits a qualified table name into schema and name parts.
    /// </summary>
    private static (string schema, string name) SplitQualifiedName(string qualifiedName)
    {
        var parts = qualifiedName.Split('.');
        return parts.Length == 2 
            ? (parts[0].Trim('"'), parts[1].Trim('"'))
            : ("public", parts[0].Trim('"'));
    }

    private static void GenerateTypeScripts(List<PgTypeDiff> diffs, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Types");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Type exists in target but not in source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    sb.AppendLine($"DROP TYPE IF EXISTS {QuoteIdentifier(diff.TypeName)} CASCADE;");
                }
            }
            else if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // Type missing in target - CREATE
                sb.AppendLine($"{diff.SourceDefinition};");
            }
            else if (diff.DefinitionChanged)
            {
                // Type changed - DROP and recreate (safest for types)
                if (options.IncludeComments)
                {
                    sb.AppendLine($"-- Recreating type {diff.TypeName} due to definition change");
                }
                sb.AppendLine($"DROP TYPE IF EXISTS {QuoteIdentifier(diff.TypeName)} CASCADE;");
                sb.AppendLine($"{diff.SourceDefinition};");
            }

            // Owner changes
            if (diff.OwnerChanged != null)
            {
                sb.AppendLine($"ALTER TYPE {QuoteIdentifier(diff.TypeName)} OWNER TO {QuoteIdentifier(diff.OwnerChanged.Value.SourceOwner)};");
            }

            // Privileges
            GeneratePrivilegeScripts(diff.PrivilegeChanges, "TYPE", diff.TypeName, sb);

            sb.AppendLine();
        }
    }

    private static void GenerateSequenceScripts(List<PgSequenceDiff> diffs, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Sequences");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            if (diff.DefinitionChanged)
            {
                if (options.IncludeComments)
                {
                    sb.AppendLine($"-- Altering sequence {diff.SequenceName}");
                }

                // Generate ALTER SEQUENCE for each changed option
                if (diff.SourceOptions != null)
                {
                    foreach (var opt in diff.SourceOptions)
                    {
                        sb.AppendLine($"ALTER SEQUENCE {QuoteIdentifier(diff.SequenceName)} {opt.OptionName} {opt.OptionValue};");
                    }
                }
            }

            // Owner changes
            if (diff.OwnerChanged != null)
            {
                sb.AppendLine($"ALTER SEQUENCE {QuoteIdentifier(diff.SequenceName)} OWNER TO {QuoteIdentifier(diff.OwnerChanged.Value.SourceOwner)};");
            }

            // Privileges
            GeneratePrivilegeScripts(diff.PrivilegeChanges, "SEQUENCE", diff.SequenceName, sb);

            sb.AppendLine();
        }
    }

    private static void GenerateTableScripts(List<PgTableDiff> diffs, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Tables");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            if (options.IncludeComments)
            {
                sb.AppendLine($"-- Table: {diff.TableName}");
            }

            // Column changes
            foreach (var colDiff in diff.ColumnDiffs)
            {
                var (schema, tableName) = SplitQualifiedName(diff.TableName);

                if (colDiff.SourceDataType == null && colDiff.TargetDataType != null)
                {
                    // Column exists in target but not in source - DROP if configured
                    if (options.DropObjectsNotInSource)
                    {
                        // ✅ Using AST builder
                        var ast = AstBuilder.AlterTableDropColumn(schema, tableName, colDiff.ColumnName, ifExists: true);
                        AppendAstSql(sb, ast);
                    }
                }
                else if (colDiff.SourceDataType != null && colDiff.TargetDataType == null)
                {
                    // Column missing in target - ADD
                    // ✅ Using AST builder
                    var notNull = colDiff.SourceIsNotNull == true;
                    var defaultValue = !string.IsNullOrEmpty(colDiff.SourceDefault) ? colDiff.SourceDefault : null;

                    var ast = AstBuilder.AlterTableAddColumn(
                        schema, 
                        tableName, 
                        colDiff.ColumnName, 
                        colDiff.SourceDataType!,
                        notNull,
                        defaultValue);
                    AppendAstSql(sb, ast);
                }
                else if (colDiff.SourceDataType != colDiff.TargetDataType ||
                         colDiff.SourceIsNotNull != colDiff.TargetIsNotNull ||
                         colDiff.SourceDefault != colDiff.TargetDefault)
                {
                    // Column changed - ALTER
                    if (colDiff.SourceDataType != colDiff.TargetDataType)
                    {
                        // TODO: AST builder for ALTER COLUMN TYPE needs better type handling
                        // Using string template for now until BuildTypeName is improved
                        sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ALTER COLUMN {QuoteIdentifier(colDiff.ColumnName)} TYPE {colDiff.SourceDataType};");
                    }
                    if (colDiff.SourceIsNotNull != colDiff.TargetIsNotNull)
                    {
                        // ✅ Using AST builder
                        if (colDiff.SourceIsNotNull == true)
                        {
                            var ast = AstBuilder.AlterTableAlterColumnSetNotNull(schema, tableName, colDiff.ColumnName);
                            AppendAstSql(sb, ast);
                        }
                        else
                        {
                            var ast = AstBuilder.AlterTableAlterColumnDropNotNull(schema, tableName, colDiff.ColumnName);
                            AppendAstSql(sb, ast);
                        }
                    }
                    if (colDiff.SourceDefault != colDiff.TargetDefault)
                    {
                        // ✅ Using AST builder
                        if (string.IsNullOrEmpty(colDiff.SourceDefault))
                        {
                            var ast = AstBuilder.AlterTableAlterColumnDropDefault(schema, tableName, colDiff.ColumnName);
                            AppendAstSql(sb, ast);
                        }
                        else
                        {
                            var ast = AstBuilder.AlterTableAlterColumnSetDefault(schema, tableName, colDiff.ColumnName, colDiff.SourceDefault);
                            AppendAstSql(sb, ast);
                        }
                    }
                }
            }

            // Constraint changes
            foreach (var constDiff in diff.ConstraintDiffs)
            {
                if (constDiff.SourceDefinition == null && constDiff.TargetDefinition != null)
                {
                    // Constraint in target but not source - DROP if configured
                    if (options.DropObjectsNotInSource)
                    {
                        sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} DROP CONSTRAINT IF EXISTS {QuoteIdentifier(constDiff.ConstraintName)};");
                    }
                }
                else if (constDiff.SourceDefinition != null && constDiff.TargetDefinition == null)
                {
                    // Constraint missing in target - ADD
                    sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ADD CONSTRAINT {QuoteIdentifier(constDiff.ConstraintName)} {constDiff.SourceDefinition};");
                }
                else if (constDiff.SourceDefinition != constDiff.TargetDefinition)
                {
                    // Constraint changed - DROP and recreate
                    sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} DROP CONSTRAINT IF EXISTS {QuoteIdentifier(constDiff.ConstraintName)};");
                    sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ADD CONSTRAINT {QuoteIdentifier(constDiff.ConstraintName)} {constDiff.SourceDefinition};");
                }
            }

            // Index changes
            foreach (var idxDiff in diff.IndexDiffs)
            {
                if (idxDiff.SourceDefinition == null && idxDiff.TargetDefinition != null)
                {
                    // Index in target but not source - DROP if configured
                    if (options.DropObjectsNotInSource)
                    {
                        sb.AppendLine($"DROP INDEX IF EXISTS {QuoteIdentifier(idxDiff.IndexName)};");
                    }
                }
                else if (idxDiff.SourceDefinition != null && idxDiff.TargetDefinition == null)
                {
                    // Index missing in target - CREATE
                    sb.AppendLine($"{idxDiff.SourceDefinition};");
                }
                else if (idxDiff.SourceDefinition != idxDiff.TargetDefinition)
                {
                    // Index changed - DROP and recreate
                    sb.AppendLine($"DROP INDEX IF EXISTS {QuoteIdentifier(idxDiff.IndexName)};");
                    sb.AppendLine($"{idxDiff.SourceDefinition};");
                }
            }

            // Owner changes
            if (diff.OwnerChanged != null)
            {
                sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} OWNER TO {QuoteIdentifier(diff.OwnerChanged.Value.SourceOwner)};");
            }

            // Privileges
            GeneratePrivilegeScripts(diff.PrivilegeChanges, "TABLE", diff.TableName, sb);

            sb.AppendLine();
        }
    }

    private static void GenerateViewScripts(List<PgViewDiff> diffs, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Views");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // View in target but not source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    var viewType = diff.TargetIsMaterialized == true ? "MATERIALIZED VIEW" : "VIEW";
                    sb.AppendLine($"DROP {viewType} IF EXISTS {QuoteIdentifier(diff.ViewName)} CASCADE;");
                }
            }
            else if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // View missing in target - CREATE
                sb.AppendLine($"{diff.SourceDefinition};");
            }
            else if (diff.DefinitionChanged)
            {
                // View changed - CREATE OR REPLACE (or DROP/CREATE for materialized views)
                if (diff.SourceIsMaterialized == true)
                {
                    sb.AppendLine($"DROP MATERIALIZED VIEW IF EXISTS {QuoteIdentifier(diff.ViewName)} CASCADE;");
                    sb.AppendLine($"{diff.SourceDefinition};");
                }
                else
                {
                    sb.AppendLine($"CREATE OR REPLACE {diff.SourceDefinition.Replace("CREATE VIEW", "VIEW")};");
                }
            }

            // Owner changes
            if (diff.OwnerChanged != null)
            {
                var viewType = diff.SourceIsMaterialized == true ? "MATERIALIZED VIEW" : "VIEW";
                sb.AppendLine($"ALTER {viewType} {QuoteIdentifier(diff.ViewName)} OWNER TO {QuoteIdentifier(diff.OwnerChanged.Value.SourceOwner)};");
            }

            // Privileges
            GeneratePrivilegeScripts(diff.PrivilegeChanges, "TABLE", diff.ViewName, sb);

            sb.AppendLine();
        }
    }

    private static void GenerateFunctionScripts(List<PgFunctionDiff> diffs, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Functions");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Function in target but not source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    sb.AppendLine($"DROP FUNCTION IF EXISTS {QuoteIdentifier(diff.FunctionName)} CASCADE;");
                }
            }
            else if (diff.SourceDefinition != null)
            {
                // Function missing or changed - CREATE OR REPLACE
                sb.AppendLine($"{diff.SourceDefinition};");
            }

            // Owner changes
            if (diff.OwnerChanged != null)
            {
                sb.AppendLine($"ALTER FUNCTION {QuoteIdentifier(diff.FunctionName)} OWNER TO {QuoteIdentifier(diff.OwnerChanged.Value.SourceOwner)};");
            }

            // Privileges
            GeneratePrivilegeScripts(diff.PrivilegeChanges, "FUNCTION", diff.FunctionName, sb);

            sb.AppendLine();
        }
    }

    private static void GenerateTriggerScripts(List<PgTriggerDiff> diffs, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Triggers");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Trigger in target but not source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    sb.AppendLine($"DROP TRIGGER IF EXISTS {QuoteIdentifier(diff.TriggerName)} ON {QuoteIdentifier(diff.TableName)};");
                }
            }
            else if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // Trigger missing in target - CREATE
                sb.AppendLine($"{diff.SourceDefinition};");
            }
            else if (diff.DefinitionChanged)
            {
                // Trigger changed - DROP and recreate
                sb.AppendLine($"DROP TRIGGER IF EXISTS {QuoteIdentifier(diff.TriggerName)} ON {QuoteIdentifier(diff.TableName)};");
                sb.AppendLine($"{diff.SourceDefinition};");
            }

            sb.AppendLine();
        }
    }

    private static void GeneratePrivilegeScripts(List<PgPrivilegeDiff> diffs, string objectType, string objectName, StringBuilder sb)
    {
        foreach (var privDiff in diffs)
        {
            if (privDiff.ChangeType == PrivilegeChangeType.MissingInTarget)
            {
                // Grant missing privilege
                sb.AppendLine($"GRANT {privDiff.PrivilegeType} ON {objectType} {QuoteIdentifier(objectName)} TO {QuoteIdentifier(privDiff.Grantee)};");
            }
            else if (privDiff.ChangeType == PrivilegeChangeType.ExtraInTarget)
            {
                // Revoke extra privilege
                sb.AppendLine($"REVOKE {privDiff.PrivilegeType} ON {objectType} {QuoteIdentifier(objectName)} FROM {QuoteIdentifier(privDiff.Grantee)};");
            }
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        // Simple identifier quoting - surround with double quotes if needed
        if (string.IsNullOrEmpty(identifier))
            return identifier;

        // If identifier contains schema.name, quote each part
        if (identifier.Contains('.'))
        {
            var parts = identifier.Split('.');
            return string.Join(".", parts.Select(p => $"\"{p}\""));
        }

        return $"\"{identifier}\"";
    }
}
