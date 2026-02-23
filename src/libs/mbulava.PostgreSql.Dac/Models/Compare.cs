using PgQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Models
{

    public class PgSchemaDiff
    {
        public string SchemaName { get; set; } = string.Empty;
        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }
        public List<PgPrivilegeDiff> PrivilegeChanges { get; set; } = new();
        public List<PgTableDiff> TableDiffs { get; set; } = new();
        public List<PgViewDiff> ViewDiffs { get; set; } = new();
        public List<PgFunctionDiff> FunctionDiffs { get; set; } = new();
        public List<PgTriggerDiff> TriggerDiffs { get; set; } = new();
        public List<PgTypeDiff> TypeDiffs { get; set; } = new();
        public List<PgSequenceDiff> SequenceDiffs { get; set; } = new();
    }

    public class PgPrivilegeDiff
    {
        public string Grantee { get; set; } = string.Empty;
        public string PrivilegeType { get; set; } = string.Empty;
        public PrivilegeChangeType ChangeType { get; set; }
    }

    public enum PrivilegeChangeType
    {
        MissingInTarget,
        ExtraInTarget
    }

    public class PgTableDiff
    {
        public string TableName { get; set; } = string.Empty;

        // Ownership changes
        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }

        // Structural changes
        public bool DefinitionChanged { get; set; }   // CREATE TABLE SQL differs
        public List<PgColumnDiff> ColumnDiffs { get; set; } = new();
        public List<PgConstraintDiff> ConstraintDiffs { get; set; } = new();
        public List<PgIndexDiff> IndexDiffs { get; set; } = new();

        // Privilege changes
        public List<PgPrivilegeDiff> PrivilegeChanges { get; set; } = new();
    }

    public class PgColumnDiff
    {
        public string ColumnName { get; set; } = string.Empty;
        public string? SourceDataType { get; set; }
        public string? TargetDataType { get; set; }
        public bool? SourceIsNotNull { get; set; }
        public bool? TargetIsNotNull { get; set; }
        public string? SourceDefault { get; set; }
        public string? TargetDefault { get; set; }
    }

    public class PgConstraintDiff
    {
        public string ConstraintName { get; set; } = string.Empty;
        public ConstrType SourceType { get; set; }
        public ConstrType TargetType { get; set; }
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }
    }

    public class PgIndexDiff
    {
        public string IndexName { get; set; } = string.Empty;
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }
    }

    public class PgTypeDiff
    {
        public string TypeName { get; set; } = string.Empty;
        public PgTypeKind SourceKind { get; set; }
        public PgTypeKind TargetKind { get; set; }

        // Ownership changes
        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }

        // Definition changes
        public bool DefinitionChanged { get; set; }

        // Source/target definitions for script generation
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }

        // Kind-specific differences
        public List<string>? SourceEnumLabels { get; set; }
        public List<string>? TargetEnumLabels { get; set; }

        public List<PgAttribute>? SourceCompositeAttributes { get; set; }
        public List<PgAttribute>? TargetCompositeAttributes { get; set; }

        // Privilege changes
        public List<PgPrivilegeDiff> PrivilegeChanges { get; set; } = new();
    }

    public class PgSequenceDiff
    {
        public string SequenceName { get; set; } = string.Empty;

        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }
        public bool DefinitionChanged { get; set; }

        // ✅ Option-level diffs
        public List<SeqOption>? SourceOptions { get; set; }
        public List<SeqOption>? TargetOptions { get; set; }

        public List<PgPrivilegeDiff> PrivilegeChanges { get; set; } = new();
    }

    public class PgViewDiff
    {
        public string ViewName { get; set; } = string.Empty;

        // Ownership changes
        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }

        // Definition changes
        public bool DefinitionChanged { get; set; }

        // Source/target definitions for comparison
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }

        // Materialized flag change
        public bool? SourceIsMaterialized { get; set; }
        public bool? TargetIsMaterialized { get; set; }

        // Privilege changes
        public List<PgPrivilegeDiff> PrivilegeChanges { get; set; } = new();
    }

    public class PgFunctionDiff
    {
        public string FunctionName { get; set; } = string.Empty;

        // Ownership changes
        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }

        // Definition changes
        public bool DefinitionChanged { get; set; }

        // Source/target definitions for comparison
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }

        // Privilege changes
        public List<PgPrivilegeDiff> PrivilegeChanges { get; set; } = new();
    }

    public class PgTriggerDiff
    {
        public string TriggerName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;

        // Ownership changes (triggers inherit table ownership)
        public (string SourceOwner, string TargetOwner)? OwnerChanged { get; set; }

        // Definition changes
        public bool DefinitionChanged { get; set; }

        // Source/target definitions for comparison
        public string? SourceDefinition { get; set; }
        public string? TargetDefinition { get; set; }
    }

}
