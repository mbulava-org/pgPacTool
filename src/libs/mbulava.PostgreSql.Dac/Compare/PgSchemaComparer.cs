using mbulava.PostgreSql.Dac.Models;
using Npgquery;
using Npgsql;
using PgQuery;
using Npgquery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compare
{
    public class PgSchemaComparer
    {
        public PgSchemaDiff Compare(PgSchema source, PgSchema target, CompareOptions options)
        {
            var diff = new PgSchemaDiff
            {
                SchemaName = source.Name
            };

            // Compare ownership
            if (HasExplicitOwner(source.Owner) && source.Owner != target.Owner)
            {
                diff.OwnerChanged = (source.Owner, target.Owner);
            }

            // Compare privileges
            diff.PrivilegeChanges = ComparePrivileges(source.Privileges, target.Privileges);

            // Compare tables
            diff.TableDiffs = CompareTables(source.Tables, target.Tables);

            // Compare types
            diff.TypeDiffs = CompareTypes(source.Types, target.Types);

            // Compare sequences
            diff.SequenceDiffs = CompareSequences(source.Sequences, target.Sequences, options);

            // Compare views
            diff.ViewDiffs = CompareViews(source.Views, target.Views, options);

            // Compare functions
            diff.FunctionDiffs = CompareFunctions(source.Functions, target.Functions, options);

            // Compare triggers
            diff.TriggerDiffs = CompareTriggers(source.Triggers, target.Triggers, options);

            return diff;
        }

        private List<PgPrivilegeDiff> ComparePrivileges(List<PgPrivilege> sourcePrivs, List<PgPrivilege> targetPrivs)
        {
            var diffs = new List<PgPrivilegeDiff>();

            // Find missing grants
            foreach (var src in sourcePrivs)
            {
                if (!targetPrivs.Any(t => t.Grantee == src.Grantee &&
                                          t.PrivilegeType == src.PrivilegeType &&
                                          t.IsGrantable == src.IsGrantable))
                {
                    diffs.Add(new PgPrivilegeDiff
                    {
                        Grantee = src.Grantee,
                        PrivilegeType = src.PrivilegeType,
                        ChangeType = PrivilegeChangeType.MissingInTarget
                    });
                }
            }

            // Find extra grants
            foreach (var tgt in targetPrivs)
            {
                if (!sourcePrivs.Any(s => s.Grantee == tgt.Grantee &&
                                          s.PrivilegeType == tgt.PrivilegeType &&
                                          s.IsGrantable == tgt.IsGrantable))
                {
                    diffs.Add(new PgPrivilegeDiff
                    {
                        Grantee = tgt.Grantee,
                        PrivilegeType = tgt.PrivilegeType,
                        ChangeType = PrivilegeChangeType.ExtraInTarget
                    });
                }
            }

            return diffs;
        }

        private List<PgTableDiff> CompareTables(List<PgTable> sourceTables, List<PgTable> targetTables)
        {
            var diffs = new List<PgTableDiff>();

            foreach (var src in sourceTables)
            {
                var tgt = targetTables.FirstOrDefault(t => t.Name == src.Name);
                if (tgt == null)
                {
                    // Table missing in target - CREATE TABLE will include columns
                    // Only populate constraint/index diffs (not column diffs - columns are in CREATE TABLE)
                    diffs.Add(new PgTableDiff
                    {
                        TableName = src.Name,
                        DefinitionChanged = true,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null,
                        ColumnDiffs = new List<PgColumnDiff>(), // Empty - columns are in CREATE TABLE
                        ConstraintDiffs = CompareConstraints(src.Constraints, new List<PgConstraint>()),
                        IndexDiffs = CompareIndexes(src.Indexes, new List<PgIndex>()),
                        PrivilegeChanges = ComparePrivileges(src.Privileges, new List<PgPrivilege>())
                    });
                    continue;
                }

                var tableDiff = new PgTableDiff { TableName = src.Name };

                // Owner change
                if (HasExplicitOwner(src.Owner) && src.Owner != tgt.Owner)
                    tableDiff.OwnerChanged = (src.Owner, tgt.Owner);

                // Definition change (compare SQL text)
                if (src.Definition != tgt.Definition)
                    tableDiff.DefinitionChanged = true;

                // Column diffs
                tableDiff.ColumnDiffs = CompareColumns(src.Columns, tgt.Columns);

                // Constraint diffs
                tableDiff.ConstraintDiffs = CompareConstraints(src.Constraints, tgt.Constraints);

                // Index diffs
                tableDiff.IndexDiffs = CompareIndexes(src.Indexes, tgt.Indexes);

                // Privilege diffs
                tableDiff.PrivilegeChanges = ComparePrivileges(src.Privileges, tgt.Privileges);

                if (tableDiff.DefinitionChanged ||
                    tableDiff.OwnerChanged != null ||
                    tableDiff.ColumnDiffs.Any() ||
                    tableDiff.ConstraintDiffs.Any() ||
                    tableDiff.IndexDiffs.Any() ||
                    tableDiff.PrivilegeChanges.Any())
                {
                    diffs.Add(tableDiff);
                }
            }

            return diffs;
        }

        private List<PgTypeDiff> CompareTypes(List<PgType> sourceTypes, List<PgType> targetTypes)
        {
            var diffs = new List<PgTypeDiff>();

            foreach (var src in sourceTypes)
            {
                var tgt = targetTypes.FirstOrDefault(t => t.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgTypeDiff
                    {
                        TypeName = src.Name,
                        DefinitionChanged = true, // missing in target
                        SourceDefinition = src.Definition,
                        TargetDefinition = null,
                        SourceKind = src.Kind
                    });
                    continue;
                }

                var typeDiff = new PgTypeDiff { TypeName = src.Name };

                // Kind change
                if (src.Kind != tgt.Kind)
                {
                    typeDiff.SourceKind = src.Kind;
                    typeDiff.TargetKind = tgt.Kind;
                    typeDiff.DefinitionChanged = true;
                }

                // Owner change
                if (HasExplicitOwner(src.Owner) && src.Owner != tgt.Owner)
                    typeDiff.OwnerChanged = (src.Owner, tgt.Owner);

                // Definition change (compare SQL text)
                if (src.Definition != tgt.Definition)
                {
                    typeDiff.DefinitionChanged = true;
                    typeDiff.SourceDefinition = src.Definition;
                    typeDiff.TargetDefinition = tgt.Definition;
                }

                // Enum labels
                if (src.Kind == PgTypeKind.Enum)
                {
                    if (!Enumerable.SequenceEqual(src.EnumLabels ?? new(), tgt.EnumLabels ?? new()))
                    {
                        typeDiff.SourceEnumLabels = src.EnumLabels;
                        typeDiff.TargetEnumLabels = tgt.EnumLabels;
                        typeDiff.DefinitionChanged = true;
                    }
                }

                // Composite attributes
                if (src.Kind == PgTypeKind.Composite)
                {
                    if (!Enumerable.SequenceEqual(src.CompositeAttributes ?? new(), tgt.CompositeAttributes ?? new(),
                        new PgAttributeComparer()))
                    {
                        typeDiff.SourceCompositeAttributes = src.CompositeAttributes;
                        typeDiff.TargetCompositeAttributes = tgt.CompositeAttributes;
                        typeDiff.DefinitionChanged = true;
                    }
                }

                // Privileges
                typeDiff.PrivilegeChanges = ComparePrivileges(src.Privileges, tgt.Privileges);

                if (typeDiff.DefinitionChanged || typeDiff.OwnerChanged != null || typeDiff.PrivilegeChanges.Any())
                    diffs.Add(typeDiff);
            }

            return diffs;
        }

        private List<PgSequenceDiff> CompareSequences(
    List<PgSequence> sourceSeqs,
    List<PgSequence> targetSeqs,
    CompareOptions options)
        {
            var diffs = new List<PgSequenceDiff>();

            foreach (var src in sourceSeqs)
            {
                var tgt = targetSeqs.FirstOrDefault(s => s.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgSequenceDiff
                    {
                        SequenceName = src.Name,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null,
                        SourceOptions = src.Options,
                        DefinitionChanged = true
                    });
                    continue;
                }

                var seqDiff = new PgSequenceDiff
                {
                    SequenceName = src.Name,
                    SourceDefinition = src.Definition,
                    TargetDefinition = tgt.Definition
                };

                // Owner
                if (options.CompareOwners && HasExplicitOwner(src.Owner) && src.Owner != tgt.Owner)
                    seqDiff.OwnerChanged = (src.Owner, tgt.Owner);

                // Options
                foreach (var srcOpt in src.Options)
                {
                    var tgtOpt = tgt.Options.FirstOrDefault(o => o.OptionName == srcOpt.OptionName);
                    if (tgtOpt == null) continue;

                    bool shouldCompare = srcOpt.OptionName switch
                    {
                        "START" => options.CompareSequenceStart,
                        "INCREMENT" => options.CompareSequenceIncrement,
                        "MINVALUE" => options.CompareSequenceMinValue,
                        "MAXVALUE" => options.CompareSequenceMaxValue,
                        "CACHE" => options.CompareSequenceCache,
                        "CYCLE" => options.CompareSequenceCycle,
                        _ => true
                    };

                    if (shouldCompare && srcOpt.OptionValue != tgtOpt.OptionValue)
                    {
                        seqDiff.DefinitionChanged = true;
                        seqDiff.SourceOptions ??= new();
                        seqDiff.TargetOptions ??= new();
                        seqDiff.SourceOptions.Add(srcOpt);
                        seqDiff.TargetOptions.Add(tgtOpt);
                    }
                }

                // Privileges
                if (options.ComparePrivileges)
                    seqDiff.PrivilegeChanges = ComparePrivileges(src.Privileges, tgt.Privileges);

                if (seqDiff.DefinitionChanged || seqDiff.OwnerChanged != null || seqDiff.PrivilegeChanges.Any())
                    diffs.Add(seqDiff);
            }

            return diffs;
        }

        private List<PgColumnDiff> CompareColumns(List<PgColumn> sourceCols, List<PgColumn> targetCols)
        {
            var diffs = new List<PgColumnDiff>();

            foreach (var src in sourceCols)
            {
                var tgt = targetCols.FirstOrDefault(c => c.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgColumnDiff
                    {
                        ColumnName = src.Name,
                        SourceDataType = src.DataType,
                        TargetDataType = null,
                        SourceIsNotNull = src.IsNotNull,
                        TargetIsNotNull = null,
                        SourceDefault = src.DefaultExpression,
                        TargetDefault = null
                    });
                    continue;
                }

                if (src.DataType != tgt.DataType ||
                    src.IsNotNull != tgt.IsNotNull ||
                    src.DefaultExpression != tgt.DefaultExpression)
                {
                    diffs.Add(new PgColumnDiff
                    {
                        ColumnName = src.Name,
                        SourceDataType = src.DataType,
                        TargetDataType = tgt.DataType,
                        SourceIsNotNull = src.IsNotNull,
                        TargetIsNotNull = tgt.IsNotNull,
                        SourceDefault = src.DefaultExpression,
                        TargetDefault = tgt.DefaultExpression
                    });
                }
            }

            // Extra columns in target
            foreach (var tgt in targetCols)
            {
                if (!sourceCols.Any(c => c.Name == tgt.Name))
                {
                    diffs.Add(new PgColumnDiff
                    {
                        ColumnName = tgt.Name,
                        SourceDataType = null,
                        TargetDataType = tgt.DataType,
                        SourceIsNotNull = null,
                        TargetIsNotNull = tgt.IsNotNull,
                        SourceDefault = null,
                        TargetDefault = tgt.DefaultExpression
                    });
                }
            }

            return diffs;
        }

        private List<PgConstraintDiff> CompareConstraints(List<PgConstraint> sourceConstraints, List<PgConstraint> targetConstraints)
        {
            var diffs = new List<PgConstraintDiff>();

            foreach (var src in sourceConstraints)
            {
                var tgt = targetConstraints.FirstOrDefault(c => c.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgConstraintDiff
                    {
                        ConstraintName = src.Name,
                        SourceType = src.Type,
                        TargetType = ConstrType.Undefined,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null
                    });
                    continue;
                }

                if (src.Type != tgt.Type || src.Definition != tgt.Definition)
                {
                    diffs.Add(new PgConstraintDiff
                    {
                        ConstraintName = src.Name,
                        SourceType = src.Type,
                        TargetType = tgt.Type,
                        SourceDefinition = src.Definition,
                        TargetDefinition = tgt.Definition
                    });
                }
            }

            // Extra constraints in target
            foreach (var tgt in targetConstraints)
            {
                if (!sourceConstraints.Any(c => c.Name == tgt.Name))
                {
                    diffs.Add(new PgConstraintDiff
                    {
                        ConstraintName = tgt.Name,
                        SourceType = ConstrType.Undefined,
                        TargetType = tgt.Type,
                        SourceDefinition = null,
                        TargetDefinition = tgt.Definition
                    });
                }
            }

            return diffs;
        }

        private List<PgIndexDiff> CompareIndexes(List<PgIndex> sourceIndexes, List<PgIndex> targetIndexes)
        {
            var diffs = new List<PgIndexDiff>();

            foreach (var src in sourceIndexes)
            {
                var tgt = targetIndexes.FirstOrDefault(i => i.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgIndexDiff
                    {
                        IndexName = src.Name,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null
                    });
                    continue;
                }

                if (src.Definition != tgt.Definition)
                {
                    diffs.Add(new PgIndexDiff
                    {
                        IndexName = src.Name,
                        SourceDefinition = src.Definition,
                        TargetDefinition = tgt.Definition
                    });
                }
            }

            // Extra indexes in target
            foreach (var tgt in targetIndexes)
            {
                if (!sourceIndexes.Any(i => i.Name == tgt.Name))
                {
                    diffs.Add(new PgIndexDiff
                    {
                        IndexName = tgt.Name,
                        SourceDefinition = null,
                        TargetDefinition = tgt.Definition
                    });
                }
            }

            return diffs;
        }

        private List<PgViewDiff> CompareViews(List<PgView> sourceViews, List<PgView> targetViews, CompareOptions options)
        {
            var diffs = new List<PgViewDiff>();

            foreach (var src in sourceViews)
            {
                var tgt = targetViews.FirstOrDefault(v => v.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgViewDiff
                    {
                        ViewName = src.Name,
                        DefinitionChanged = true,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null,
                        SourceIsMaterialized = src.IsMaterialized,
                        TargetIsMaterialized = null
                    });
                    continue;
                }

                var viewDiff = new PgViewDiff { ViewName = src.Name };

                // Owner change
                if (options.CompareOwners && HasExplicitOwner(src.Owner) && src.Owner != tgt.Owner)
                    viewDiff.OwnerChanged = (src.Owner, tgt.Owner);

                // Materialized flag change
                if (src.IsMaterialized != tgt.IsMaterialized)
                {
                    viewDiff.SourceIsMaterialized = src.IsMaterialized;
                    viewDiff.TargetIsMaterialized = tgt.IsMaterialized;
                    viewDiff.DefinitionChanged = true;
                }

                // Definition change (compare normalized SQL)
                if (src.Definition != tgt.Definition)
                {
                    viewDiff.DefinitionChanged = true;
                    viewDiff.SourceDefinition = src.Definition;
                    viewDiff.TargetDefinition = tgt.Definition;
                }

                // Privileges
                if (options.ComparePrivileges)
                    viewDiff.PrivilegeChanges = ComparePrivileges(src.Privileges, tgt.Privileges);

                if (viewDiff.DefinitionChanged || viewDiff.OwnerChanged != null || viewDiff.PrivilegeChanges.Any())
                    diffs.Add(viewDiff);
            }

            // Extra views in target
            foreach (var tgt in targetViews)
            {
                if (!sourceViews.Any(v => v.Name == tgt.Name))
                {
                    diffs.Add(new PgViewDiff
                    {
                        ViewName = tgt.Name,
                        DefinitionChanged = true,
                        SourceDefinition = null,
                        TargetDefinition = tgt.Definition,
                        SourceIsMaterialized = null,
                        TargetIsMaterialized = tgt.IsMaterialized
                    });
                }
            }

            return diffs;
        }

        private List<PgFunctionDiff> CompareFunctions(List<PgFunction> sourceFunctions, List<PgFunction> targetFunctions, CompareOptions options)
        {
            var diffs = new List<PgFunctionDiff>();

            foreach (var src in sourceFunctions)
            {
                var tgt = targetFunctions.FirstOrDefault(f => f.Name == src.Name);
                if (tgt == null)
                {
                    diffs.Add(new PgFunctionDiff
                    {
                        FunctionName = src.Name,
                        DefinitionChanged = true,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null
                    });
                    continue;
                }

                var funcDiff = new PgFunctionDiff { FunctionName = src.Name };

                // Owner change
                if (options.CompareOwners && HasExplicitOwner(src.Owner) && src.Owner != tgt.Owner)
                    funcDiff.OwnerChanged = (src.Owner, tgt.Owner);

                // Definition change (compare SQL text)
                if (src.Definition != tgt.Definition)
                {
                    funcDiff.DefinitionChanged = true;
                    funcDiff.SourceDefinition = src.Definition;
                    funcDiff.TargetDefinition = tgt.Definition;
                }

                // Privileges
                if (options.ComparePrivileges)
                    funcDiff.PrivilegeChanges = ComparePrivileges(src.Privileges, tgt.Privileges);

                if (funcDiff.DefinitionChanged || funcDiff.OwnerChanged != null || funcDiff.PrivilegeChanges.Any())
                    diffs.Add(funcDiff);
            }

            // Extra functions in target
            foreach (var tgt in targetFunctions)
            {
                if (!sourceFunctions.Any(f => f.Name == tgt.Name))
                {
                    diffs.Add(new PgFunctionDiff
                    {
                        FunctionName = tgt.Name,
                        DefinitionChanged = true,
                        SourceDefinition = null,
                        TargetDefinition = tgt.Definition
                    });
                }
            }

            return diffs;
        }

        private static bool HasExplicitOwner(string? owner)
        {
            return !string.IsNullOrWhiteSpace(owner);
        }

        private List<PgTriggerDiff> CompareTriggers(List<PgTrigger> sourceTriggers, List<PgTrigger> targetTriggers, CompareOptions options)
        {
            var diffs = new List<PgTriggerDiff>();

            foreach (var src in sourceTriggers)
            {
                var tgt = targetTriggers.FirstOrDefault(t => t.Name == src.Name && t.TableName == src.TableName);
                if (tgt == null)
                {
                    diffs.Add(new PgTriggerDiff
                    {
                        TriggerName = src.Name,
                        TableName = src.TableName,
                        DefinitionChanged = true,
                        SourceDefinition = src.Definition,
                        TargetDefinition = null
                    });
                    continue;
                }

                var triggerDiff = new PgTriggerDiff
                {
                    TriggerName = src.Name,
                    TableName = src.TableName
                };

                // Owner change (triggers inherit table ownership)
                if (options.CompareOwners && HasExplicitOwner(src.Owner) && src.Owner != tgt.Owner)
                    triggerDiff.OwnerChanged = (src.Owner, tgt.Owner);

                // Definition change (compare SQL text)
                if (src.Definition != tgt.Definition)
                {
                    triggerDiff.DefinitionChanged = true;
                    triggerDiff.SourceDefinition = src.Definition;
                    triggerDiff.TargetDefinition = tgt.Definition;
                }

                if (triggerDiff.DefinitionChanged || triggerDiff.OwnerChanged != null)
                    diffs.Add(triggerDiff);
            }

            // Extra triggers in target
            foreach (var tgt in targetTriggers)
            {
                if (!sourceTriggers.Any(t => t.Name == tgt.Name && t.TableName == tgt.TableName))
                {
                    diffs.Add(new PgTriggerDiff
                    {
                        TriggerName = tgt.Name,
                        TableName = tgt.TableName,
                        DefinitionChanged = true,
                        SourceDefinition = null,
                        TargetDefinition = tgt.Definition
                    });
                }
            }

            return diffs;
        }
    }
}
