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
            if (!string.IsNullOrWhiteSpace(options.SourceDatabase))
            {
                sb.AppendLine($"-- Source Database: {options.SourceDatabase}");
            }
            if (!string.IsNullOrWhiteSpace(options.TargetDatabase))
            {
                sb.AppendLine($"-- Target Database: {options.TargetDatabase}");
            }
            sb.AppendLine("-- ============================================================================");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(options.TargetDatabase))
        {
            AppendPublishMetadata(sb, options);
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

        var hasChanges = diff.TypeDiffs.Count > 0 ||
                         diff.SequenceDiffs.Count > 0 ||
                         diff.TableDiffs.Count > 0 ||
                         diff.ViewDiffs.Count > 0 ||
                         diff.FunctionDiffs.Count > 0 ||
                         diff.TriggerDiffs.Count > 0 ||
                         diff.PrivilegeChanges.Count > 0 ||
                         diff.OwnerChanged != null;

        // Initialize metadata table if tracking is enabled and there are schema changes
        if (hasChanges)
        {
            GenerateMetadataTableCreation(sb, options);
        }

        // Generate SQL for each object type in dependency order
        // 1. Types (must come first - used by tables/functions)
        GenerateTypeScripts(diff.TypeDiffs, diff.SchemaName, sb, options);

        // 2. Sequences (may be used in table defaults)
        GenerateSequenceScripts(diff.SequenceDiffs, diff.SchemaName, sb, options);

        // 3. Tables (structure changes)
        GenerateTableScripts(diff.TableDiffs, diff.SchemaName, sb, options);

        // 4. Views (depend on tables)
        GenerateViewScripts(diff.ViewDiffs, diff.SchemaName, sb, options);

        // 5. Functions (may be used by triggers/views)
        GenerateFunctionScripts(diff.FunctionDiffs, diff.SchemaName, sb, options);

        // 6. Triggers (depend on tables and functions)
        GenerateTriggerScripts(diff.TriggerDiffs, diff.SchemaName, sb, options);

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

    private static void AppendPublishMetadata(StringBuilder sb, PublishOptions options)
    {
        sb.AppendLine("-- SQLCMD-style deployment metadata");
        sb.AppendLine("--   $(TargetDatabase) validates the publish target");
        sb.AppendLine("--   $(DatabaseName) resolves to the effective target database name");
        sb.AppendLine();
        sb.AppendLine("DO $$");
        sb.AppendLine("BEGIN");
        sb.AppendLine("    IF current_database() <> '$(TargetDatabase)' THEN");
        sb.AppendLine("        RAISE EXCEPTION 'Deployment target database mismatch. Expected %, actual %.', '$(TargetDatabase)', current_database();");
        sb.AppendLine("    END IF;");
        sb.AppendLine("END $$;");
        sb.AppendLine();
    }

    /// <summary>
    /// Splits a qualified table name into schema and name parts.
    /// </summary>
    private static (string schema, string name) SplitQualifiedName(string qualifiedName, string defaultSchema = "public")
    {
        var parts = qualifiedName.Split('.');
        return parts.Length == 2 
            ? (parts[0].Trim('"'), parts[1].Trim('"'))
            : (defaultSchema, parts[0].Trim('"'));
    }

    private static void GenerateTypeScripts(List<PgTypeDiff> diffs, string schemaName, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Types");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            var (schema, typeName) = SplitQualifiedName(diff.TypeName, schemaName);

            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Type exists in target but not in source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    sb.AppendLine($"DROP TYPE IF EXISTS {QuoteIdentifier(diff.TypeName)} CASCADE;");
                    GenerateMetadataDelete(sb, schema, typeName, "TYPE", options);
                }
            }
            else if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // Type missing in target - CREATE
                sb.AppendLine($"{diff.SourceDefinition};");
                GenerateMetadataUpsert(sb, schema, typeName, "TYPE", diff.SourceFilePath, diff.SourceDefinition, options);
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
                GenerateMetadataUpsert(sb, schema, typeName, "TYPE", diff.SourceFilePath, diff.SourceDefinition, options);
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

    private static void GenerateSequenceScripts(List<PgSequenceDiff> diffs, string schemaName, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Sequences");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            var (schema, seqName) = SplitQualifiedName(diff.SequenceName, schemaName);

            if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                if (options.IncludeComments)
                {
                    sb.AppendLine($"-- Creating sequence {diff.SequenceName}");
                }

                sb.AppendLine(diff.SourceDefinition.TrimEnd());
                GenerateMetadataUpsert(sb, schema, seqName, "SEQUENCE", diff.SourceFilePath, diff.SourceDefinition, options);
            }
            else if (diff.DefinitionChanged)
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
                GenerateMetadataUpsert(sb, schema, seqName, "SEQUENCE", diff.SourceFilePath, diff.SourceDefinition, options);
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

    private static void GenerateTableScripts(List<PgTableDiff> diffs, string schemaName, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Tables");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            var (schema, tableName) = SplitQualifiedName(diff.TableName, schemaName);

            if (options.IncludeComments)
            {
                sb.AppendLine($"-- Table: {diff.TableName}");
            }

            // Table missing in target - CREATE table with original definition
            if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // Output original CREATE TABLE definition (don't modify it)
                sb.AppendLine(diff.SourceDefinition.TrimEnd());
                sb.AppendLine();
                GenerateMetadataUpsert(sb, schema, tableName, "TABLE", diff.SourceFilePath, diff.SourceDefinition, options);
            }
            else if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Table exists in target but not in source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    var ast = AstBuilder.DropTable(schema, tableName, ifExists: true, cascade: true);
                    AppendAstSql(sb, ast);
                    GenerateMetadataDelete(sb, schema, tableName, "TABLE", options);
                }
                sb.AppendLine();
                continue; // Skip column/constraint/index processing for dropped tables
            }
            else
            {
                // Table exists in both - process column changes
                foreach (var colDiff in diff.ColumnDiffs)
                {
                    if (colDiff.SourceDataType == null && colDiff.TargetDataType != null)
                    {
                        // Column exists in target but not in source - DROP if configured
                        if (options.DropObjectsNotInSource)
                        {
                            var ast = AstBuilder.AlterTableDropColumn(schema, tableName, colDiff.ColumnName, ifExists: true);
                            AppendAstSql(sb, ast);
                        }
                    }
                    else if (colDiff.SourceDataType != null && colDiff.TargetDataType == null)
                    {
                        // Column missing in target - ADD
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
                            var ast = AstBuilder.AlterTableAlterColumnType(schema, tableName, colDiff.ColumnName, colDiff.SourceDataType!);
                            AppendAstSql(sb, ast);
                        }
                        if (colDiff.SourceIsNotNull != colDiff.TargetIsNotNull)
                        {
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

                if (diff.SourceDefinition != null)
                {
                    GenerateMetadataUpsert(sb, schema, tableName, "TABLE", diff.SourceFilePath, diff.SourceDefinition, options);
                }
            }

            // Process table constraints
            foreach (var constraintDiff in diff.ConstraintDiffs)
            {
                if (constraintDiff.SourceDefinition != null && constraintDiff.TargetDefinition == null)
                {
                    sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} ADD CONSTRAINT {QuoteIdentifier(constraintDiff.ConstraintName)} {constraintDiff.SourceDefinition};");
                }
                else if (constraintDiff.SourceDefinition == null && constraintDiff.TargetDefinition != null && options.DropObjectsNotInSource)
                {
                    sb.AppendLine($"ALTER TABLE {QuoteIdentifier(diff.TableName)} DROP CONSTRAINT IF EXISTS {QuoteIdentifier(constraintDiff.ConstraintName)};");
                }
            }

            // Process table indexes
            foreach (var indexDiff in diff.IndexDiffs)
            {
                if (indexDiff.SourceDefinition != null && indexDiff.TargetDefinition == null)
                {
                    sb.AppendLine($"{indexDiff.SourceDefinition};");
                }
                else if (indexDiff.SourceDefinition == null && indexDiff.TargetDefinition != null && options.DropObjectsNotInSource)
                {
                    sb.AppendLine($"DROP INDEX IF EXISTS {QuoteIdentifier(indexDiff.IndexName)};");
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

    private static void GenerateViewScripts(List<PgViewDiff> diffs, string schemaName, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Views");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            var (schema, viewName) = SplitQualifiedName(diff.ViewName, schemaName);

            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // View in target but not source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    var ast = AstBuilder.DropView(schema, viewName, ifExists: true, cascade: true);
                    AppendAstSql(sb, ast);
                    GenerateMetadataDelete(sb, schema, viewName, diff.TargetIsMaterialized == true ? "MATERIALIZED VIEW" : "VIEW", options);
                }
            }
            else if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // View missing in target - CREATE
                sb.AppendLine($"{diff.SourceDefinition};");
                GenerateMetadataUpsert(sb, schema, viewName, diff.SourceIsMaterialized == true ? "MATERIALIZED VIEW" : "VIEW", diff.SourceFilePath, diff.SourceDefinition, options);
            }
            else if (diff.DefinitionChanged)
            {
                // View changed - CREATE OR REPLACE (or DROP/CREATE for materialized views)
                if (diff.SourceIsMaterialized == true)
                {
                    var ast = AstBuilder.DropView(schema, viewName, ifExists: true, cascade: true);
                    AppendAstSql(sb, ast);
                    sb.AppendLine($"{diff.SourceDefinition};");
                }
                else
                {
                    sb.AppendLine($"CREATE OR REPLACE {diff.SourceDefinition.Replace("CREATE VIEW", "VIEW")};");
                }
                GenerateMetadataUpsert(sb, schema, viewName, diff.SourceIsMaterialized == true ? "MATERIALIZED VIEW" : "VIEW", diff.SourceFilePath, diff.SourceDefinition, options);
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

    private static void GenerateFunctionScripts(List<PgFunctionDiff> diffs, string schemaName, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Functions");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            var (schema, functionName) = SplitQualifiedName(diff.FunctionName, schemaName);

            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Function in target but not source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    var ast = AstBuilder.DropFunction(schema, functionName, ifExists: true, cascade: true);
                    AppendAstSql(sb, ast);
                    GenerateMetadataDelete(sb, schema, functionName, "FUNCTION", options);
                }
            }
            else if (diff.SourceDefinition != null)
            {
                // Function missing or changed - CREATE OR REPLACE
                sb.AppendLine($"{diff.SourceDefinition};");
                GenerateMetadataUpsert(sb, schema, functionName, "FUNCTION", diff.SourceFilePath, diff.SourceDefinition, options);
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

    private static void GenerateTriggerScripts(List<PgTriggerDiff> diffs, string schemaName, StringBuilder sb, PublishOptions options)
    {
        if (diffs.Count == 0) return;

        if (options.IncludeComments)
        {
            sb.AppendLine("-- Triggers");
            sb.AppendLine();
        }

        foreach (var diff in diffs)
        {
            var (schema, tableName) = SplitQualifiedName(diff.TableName, schemaName);

            if (diff.SourceDefinition == null && diff.TargetDefinition != null)
            {
                // Trigger in target but not source - DROP if configured
                if (options.DropObjectsNotInSource)
                {
                    var ast = AstBuilder.DropTrigger(diff.TriggerName, schema, tableName, ifExists: true);
                    AppendAstSql(sb, ast);
                    GenerateMetadataDelete(sb, schema, diff.TriggerName, "TRIGGER", options);
                }
            }
            else if (diff.SourceDefinition != null && diff.TargetDefinition == null)
            {
                // Trigger missing in target - CREATE
                sb.AppendLine($"{diff.SourceDefinition};");
                GenerateMetadataUpsert(sb, schema, diff.TriggerName, "TRIGGER", diff.SourceFilePath, diff.SourceDefinition, options);
            }
            else if (diff.DefinitionChanged)
            {
                // Trigger changed - DROP and recreate
                var ast = AstBuilder.DropTrigger(diff.TriggerName, schema, tableName, ifExists: true);
                AppendAstSql(sb, ast);
                sb.AppendLine($"{diff.SourceDefinition};");
                GenerateMetadataUpsert(sb, schema, diff.TriggerName, "TRIGGER", diff.SourceFilePath, diff.SourceDefinition, options);
            }

            sb.AppendLine();
        }
    }

    private static void GenerateMetadataTableCreation(StringBuilder sb, PublishOptions options)
    {
        if (!options.TrackDeploymentMetadata) return;

        var metaTable = $"{QuoteIdentifier(options.MetadataSchema)}.{QuoteIdentifier(options.MetadataTableName)}";
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {metaTable} (");
        sb.AppendLine("    schema_name VARCHAR(128) NOT NULL,");
        sb.AppendLine("    object_name VARCHAR(128) NOT NULL,");
        sb.AppendLine("    object_type VARCHAR(32) NOT NULL,");
        sb.AppendLine("    file_path VARCHAR(512),");
        sb.AppendLine("    source_sql TEXT NOT NULL,");
        sb.AppendLine("    ast_hash VARCHAR(64),");
        sb.AppendLine("    updated_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp(),");
        sb.AppendLine("    PRIMARY KEY (schema_name, object_name, object_type)");
        sb.AppendLine(");");
        sb.AppendLine();
    }

    private static void GenerateMetadataUpsert(
        StringBuilder sb,
        string schemaName,
        string objectName,
        string objectType,
        string? filePath,
        string? sourceSql,
        PublishOptions options)
    {
        if (!options.TrackDeploymentMetadata || string.IsNullOrWhiteSpace(sourceSql)) return;

        var metaTable = $"{QuoteIdentifier(options.MetadataSchema)}.{QuoteIdentifier(options.MetadataTableName)}";
        var hash = ComputeSqlHash(sourceSql);

        sb.AppendLine($"INSERT INTO {metaTable} (schema_name, object_name, object_type, file_path, source_sql, ast_hash, updated_at)");
        sb.AppendLine($"VALUES ({EscapeSqlString(schemaName)}, {EscapeSqlString(objectName)}, {EscapeSqlString(objectType)}, {EscapeSqlString(filePath)}, {EscapeSqlString(sourceSql)}, {EscapeSqlString(hash)}, clock_timestamp())");
        sb.AppendLine($"ON CONFLICT (schema_name, object_name, object_type)");
        sb.AppendLine($"DO UPDATE SET file_path = EXCLUDED.file_path, source_sql = EXCLUDED.source_sql, ast_hash = EXCLUDED.ast_hash, updated_at = clock_timestamp();");
        sb.AppendLine();
    }

    private static void GenerateMetadataDelete(
        StringBuilder sb,
        string schemaName,
        string objectName,
        string objectType,
        PublishOptions options)
    {
        if (!options.TrackDeploymentMetadata) return;

        var metaTable = $"{QuoteIdentifier(options.MetadataSchema)}.{QuoteIdentifier(options.MetadataTableName)}";
        sb.AppendLine($"DELETE FROM {metaTable} WHERE schema_name = {EscapeSqlString(schemaName)} AND object_name = {EscapeSqlString(objectName)} AND object_type = {EscapeSqlString(objectType)};");
        sb.AppendLine();
    }

    private static string ComputeSqlHash(string sql)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(sql.Trim());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string EscapeSqlString(string? value)
    {
        if (value == null) return "NULL";
        return "'" + value.Replace("'", "''") + "'";
    }

    private static void GeneratePrivilegeScripts(List<PgPrivilegeDiff> diffs, string objectType, string objectName, StringBuilder sb)
    {
        foreach (var privDiff in diffs)
        {
            var (schema, name) = SplitQualifiedName(objectName);

            if (privDiff.ChangeType == PrivilegeChangeType.MissingInTarget)
            {
                // Grant missing privilege
                // ✅ Using AST builder
                var ast = AstBuilder.Grant(privDiff.PrivilegeType, objectType, schema, name, privDiff.Grantee);
                AppendAstSql(sb, ast);
            }
            else if (privDiff.ChangeType == PrivilegeChangeType.ExtraInTarget)
            {
                // Revoke extra privilege
                // ✅ Using AST builder
                var ast = AstBuilder.Revoke(privDiff.PrivilegeType, objectType, schema, name, privDiff.Grantee);
                AppendAstSql(sb, ast);
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
