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
        public List<ObjectDiff> CompareProjects(PgProject source, PgProject target)
        {
            var diffs = new List<ObjectDiff>();

            foreach (var srcSchema in source.Schemas)
            {
                var tgtSchema = target.Schemas.FirstOrDefault(s => s.Name == srcSchema.Name);
                if (tgtSchema == null)
                {
                    diffs.Add(new ObjectDiff
                    {
                        ObjectType = "Schema",
                        ObjectName = srcSchema.Name,
                        Action = "Create",
                        //TODO: Script = srcSchema.Definition // from AST
                    });
                    continue;
                }

                // Compare schema owner
                if (srcSchema.Owner != tgtSchema.Owner)
                {
                    diffs.Add(new ObjectDiff
                    {
                        ObjectType = "Schema",
                        ObjectName = srcSchema.Name,
                        Action = "Alter",
                        Script = $"ALTER SCHEMA {srcSchema.Name} OWNER TO {srcSchema.Owner};"
                    });
                }

                // Compare tables
                foreach (var srcTable in srcSchema.Tables)
                {
                    var tgtTable = tgtSchema.Tables.FirstOrDefault(t => t.Name == srcTable.Name);
                    if (tgtTable == null)
                    {
                        diffs.Add(new ObjectDiff
                        {
                            ObjectType = "Table",
                            ObjectName = $"{srcSchema.Name}.{srcTable.Name}",
                            Action = "Create",
                            Script = srcTable.Definition
                        });
                        continue;
                    }

                    // Column diffs
                    CompareColumns(srcSchema.Name, srcTable, tgtTable, diffs);

                    // Constraint diffs
                    CompareConstraints(srcSchema.Name, srcTable, tgtTable, diffs);

                    // Index diffs
                    CompareIndexes(srcSchema.Name, srcTable, tgtTable, diffs);
                }
            }

            return diffs;
        }
        private void CompareColumns(string schemaName, PgTable srcTable, PgTable tgtTable, List<ObjectDiff> diffs)
        {
            // Missing columns in target
            foreach (var srcCol in srcTable.Columns)
            {
                var tgtCol = tgtTable.Columns.FirstOrDefault(c => c.Name == srcCol.Name);
                if (tgtCol == null)
                {
                    diffs.Add(new ObjectDiff
                    {
                        ObjectType = "Column",
                        ObjectName = $"{schemaName}.{srcTable.Name}.{srcCol.Name}",
                        Action = "Add",
                        Script = $"ALTER TABLE {schemaName}.{srcTable.Name} ADD COLUMN {srcCol.Name} {srcCol.DataType}" +
                                 (srcCol.IsNotNull ? " NOT NULL" : "") +
                                 (srcCol.DefaultExpression != null ? $" DEFAULT {srcCol.DefaultExpression}" : "") + ";"
                    });
                }
                else
                {
                    // Compare datatype, nullability, default
                    if (srcCol.DataType != tgtCol.DataType)
                    {
                        diffs.Add(new ObjectDiff
                        {
                            ObjectType = "Column",
                            ObjectName = $"{schemaName}.{srcTable.Name}.{srcCol.Name}",
                            Action = "Alter",
                            Script = $"ALTER TABLE {schemaName}.{srcTable.Name} ALTER COLUMN {srcCol.Name} TYPE {srcCol.DataType};"
                        });
                    }
                    if (srcCol.IsNotNull != tgtCol.IsNotNull)
                    {
                        diffs.Add(new ObjectDiff
                        {
                            ObjectType = "Column",
                            ObjectName = $"{schemaName}.{srcTable.Name}.{srcCol.Name}",
                            Action = "Alter",
                            Script = $"ALTER TABLE {schemaName}.{srcTable.Name} ALTER COLUMN {srcCol.Name} {(srcCol.IsNotNull ? "SET NOT NULL" : "DROP NOT NULL")};"
                        });
                    }
                    if (srcCol.DefaultExpression != tgtCol.DefaultExpression)
                    {
                        diffs.Add(new ObjectDiff
                        {
                            ObjectType = "Column",
                            ObjectName = $"{schemaName}.{srcTable.Name}.{srcCol.Name}",
                            Action = "Alter",
                            Script = srcCol.DefaultExpression != null
                                ? $"ALTER TABLE {schemaName}.{srcTable.Name} ALTER COLUMN {srcCol.Name} SET DEFAULT {srcCol.DefaultExpression};"
                                : $"ALTER TABLE {schemaName}.{srcTable.Name} ALTER COLUMN {srcCol.Name} DROP DEFAULT;"
                        });
                    }
                }
            }

            // Extra columns in target
            foreach (var tgtCol in tgtTable.Columns.Where(c => !srcTable.Columns.Any(sc => sc.Name == c.Name)))
            {
                diffs.Add(new ObjectDiff
                {
                    ObjectType = "Column",
                    ObjectName = $"{schemaName}.{srcTable.Name}.{tgtCol.Name}",
                    Action = "Drop",
                    Script = $"ALTER TABLE {schemaName}.{srcTable.Name} DROP COLUMN {tgtCol.Name};"
                });
            }
        }

        private void CompareConstraints(string schemaName, PgTable srcTable, PgTable tgtTable, List<ObjectDiff> diffs)
        {
            foreach (var srcCon in srcTable.Constraints)
            {
                var tgtCon = tgtTable.Constraints.FirstOrDefault(c => c.Name == srcCon.Name);
                if (tgtCon == null)
                {
                    diffs.Add(new ObjectDiff
                    {
                        ObjectType = "Constraint",
                        ObjectName = $"{schemaName}.{srcTable.Name}.{srcCon.Name}",
                        Action = "Add",
                        Script = $"ALTER TABLE {schemaName}.{srcTable.Name} ADD CONSTRAINT {srcCon.Name} {srcCon.Type} {srcCon.CheckExpression ?? ""};"
                    });
                }
                else
                {
                    // Compare definition text (simplified)
                    if (srcCon.CheckExpression != tgtCon.CheckExpression ||
                        srcCon.ReferencedTable != tgtCon.ReferencedTable ||
                        !Enumerable.SequenceEqual(srcCon.ReferencedColumns ?? new(), tgtCon.ReferencedColumns ?? new()))
                    {
                        diffs.Add(new ObjectDiff
                        {
                            ObjectType = "Constraint",
                            ObjectName = $"{schemaName}.{srcTable.Name}.{srcCon.Name}",
                            Action = "Alter",
                            Script = $"ALTER TABLE {schemaName}.{srcTable.Name} DROP CONSTRAINT {srcCon.Name};\n" +
                                     $"ALTER TABLE {schemaName}.{srcTable.Name} ADD CONSTRAINT {srcCon.Name} {srcCon.Type} {srcCon.CheckExpression ?? ""};"
                        });
                    }
                }
            }

            // Extra constraints in target
            foreach (var tgtCon in tgtTable.Constraints.Where(c => !srcTable.Constraints.Any(sc => sc.Name == c.Name)))
            {
                diffs.Add(new ObjectDiff
                {
                    ObjectType = "Constraint",
                    ObjectName = $"{schemaName}.{srcTable.Name}.{tgtCon.Name}",
                    Action = "Drop",
                    Script = $"ALTER TABLE {schemaName}.{srcTable.Name} DROP CONSTRAINT {tgtCon.Name};"
                });
            }
        }

        private void CompareIndexes(string schemaName, PgTable srcTable, PgTable tgtTable, List<ObjectDiff> diffs)
        {
            foreach (var srcIdx in srcTable.Indexes)
            {
                var tgtIdx = tgtTable.Indexes.FirstOrDefault(i => i.Name == srcIdx.Name);
                if (tgtIdx == null)
                {
                    diffs.Add(new ObjectDiff
                    {
                        ObjectType = "Index",
                        ObjectName = $"{schemaName}.{srcTable.Name}.{srcIdx.Name}",
                        Action = "Add",
                        Script = srcIdx.Definition
                    });
                }
                else
                {
                    if (srcIdx.Definition != tgtIdx.Definition)
                    {
                        diffs.Add(new ObjectDiff
                        {
                            ObjectType = "Index",
                            ObjectName = $"{schemaName}.{srcTable.Name}.{srcIdx.Name}",
                            Action = "Alter",
                            Script = $"DROP INDEX {schemaName}.{srcIdx.Name};\n{srcIdx.Definition}"
                        });
                    }
                }
            }

            // Extra indexes in target
            foreach (var tgtIdx in tgtTable.Indexes.Where(i => !srcTable.Indexes.Any(si => si.Name == i.Name)))
            {
                diffs.Add(new ObjectDiff
                {
                    ObjectType = "Index",
                    ObjectName = $"{schemaName}.{srcTable.Name}.{tgtIdx.Name}",
                    Action = "Drop",
                    Script = $"DROP INDEX {schemaName}.{tgtIdx.Name};"
                });
            }
        }
    }
}
