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
        public SchemaDiff Compare(PgProject source, PgProject target)
        {
            var diff = new SchemaDiff();

            //foreach (var srcSchema in source.Schemas)
            //{
            //    var tgtSchema = target.Schemas.FirstOrDefault(s => s.Name == srcSchema.Name);
            //    if (tgtSchema == null)
            //    {
            //        diff.MissingSchemas.Add(srcSchema);
            //        continue;
            //    }

            //    CompareTypes(srcSchema, tgtSchema, diff);
            //    CompareTables(srcSchema, tgtSchema, diff);
                
            //    CompareSequences(srcSchema, tgtSchema, diff);
            //    CompareViews(srcSchema, tgtSchema, diff);
            //    CompareFunctions(srcSchema, tgtSchema, diff);
            //    CompareTriggers(srcSchema, tgtSchema, diff);
            //}

            return diff;
        }

        private void CompareTables(PgSchema srcSchema, PgSchema tgtSchema, SchemaDiff diff)
        {
            foreach (var srcTable in srcSchema.Tables)
            {
                var tgtTable = tgtSchema.Tables.FirstOrDefault(t => t.Name == srcTable.Name);
                if (tgtTable == null)
                {
                    diff.TableDiffs.Add(new ObjectDiff(srcTable.Name, DiffType.Missing, srcTable.Definition));
                    continue;
                }

                // Parse both definitions into AST
                var srcAst = new Parser().Parse(srcTable.Definition).OfType<CreateStmt>().First();
                var tgtAst = new Parser().Parse(tgtTable.Definition).OfType<CreateStmt>().First();

                CompareColumns(srcAst, tgtAst, srcSchema.Name, diff);
                CompareConstraints(srcAst, tgtAst, srcSchema.Name, diff);
            }

            foreach (var tgtTable in tgtSchema.Tables.Where(t => !srcSchema.Tables.Any(st => st.Name == t.Name)))
            {
                diff.TableDiffs.Add(new ObjectDiff(tgtTable.Name, DiffType.Extra, $"DROP TABLE {tgtSchema.Name}.{tgtTable.Name};"));
            }
        }

        private void CompareColumns(CreateStmt srcAst, CreateStmt tgtAst, string schemaName, SchemaDiff diff)
        {
            foreach (var srcCol in srcAst.TableElts.OfType<ColumnDef>())
            {
                var tgtCol = tgtAst.TableElts.OfType<ColumnDef>().FirstOrDefault(c => c.Colname == srcCol.Colname);
                if (tgtCol == null)
                {
                    diff.ColumnDiffs.Add(new ObjectDiff($"{schemaName}.{srcAst.Relation.Relname}.{srcCol.Colname}",
                        DiffType.Missing,
                        $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} ADD COLUMN {srcCol.Colname} {srcCol.TypeName};"));
                }
                else if (!ColumnEquals(srcCol, tgtCol))
                {
                    diff.ColumnDiffs.Add(new ObjectDiff($"{schemaName}.{srcAst.Relation.Relname}.{srcCol.Colname}",
                        DiffType.Changed,
                        $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} ALTER COLUMN {srcCol.Colname} TYPE {srcCol.TypeName};"));
                }
            }

            foreach (var tgtCol in tgtAst.TableElts.OfType<ColumnDef>().Where(c => !srcAst.TableElts.OfType<ColumnDef>().Any(sc => sc.Colname == c.Colname)))
            {
                diff.ColumnDiffs.Add(new ObjectDiff($"{schemaName}.{srcAst.Relation.Relname}.{tgtCol.Colname}",
                    DiffType.Extra,
                    $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} DROP COLUMN {tgtCol.Colname};"));
            }
        }

        private bool ColumnEquals(ColumnDef srcCol, ColumnDef tgtCol)
        {
            return srcCol.TypeName.ToString() == tgtCol.TypeName.ToString()
                && srcCol.IsNotNull == tgtCol.IsNotNull
                && (srcCol.RawDefault?.ToString() ?? "") == (tgtCol.RawDefault?.ToString() ?? "");
        }

        private void CompareConstraints(CreateStmt srcAst, CreateStmt tgtAst, string schemaName, SchemaDiff diff)
        {
            var srcConstraints = srcAst.TableElts.OfType<Constraint>().ToList();
            var tgtConstraints = tgtAst.TableElts.OfType<Constraint>().ToList();

            foreach (var srcCon in srcConstraints)
            {
                var tgtCon = tgtConstraints.FirstOrDefault(c => c.Conname == srcCon.Conname);
                if (tgtCon == null)
                {
                    diff.ConstraintDiffs.Add(new ObjectDiff(srcCon.Conname, DiffType.Missing,
                        $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} ADD CONSTRAINT {srcCon.Conname} {srcCon.Contype};"));
                }
                else if (!ConstraintEquals(srcCon, tgtCon))
                {
                    diff.ConstraintDiffs.Add(new ObjectDiff(srcCon.Conname, DiffType.Changed,
                        $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} DROP CONSTRAINT {srcCon.Conname};\n" +
                        $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} ADD CONSTRAINT {srcCon.Conname} {srcCon.Contype};"));
                }
            }

            foreach (var tgtCon in tgtConstraints.Where(c => !srcConstraints.Any(sc => sc.Conname == c.Conname)))
            {
                diff.ConstraintDiffs.Add(new ObjectDiff(tgtCon.Conname, DiffType.Extra,
                    $"ALTER TABLE {schemaName}.{srcAst.Relation.Relname} DROP CONSTRAINT {tgtCon.Conname};"));
            }
        }

        private bool ConstraintEquals(Constraint srcCon, Constraint tgtCon)
        {
            if (srcCon.Contype != tgtCon.Contype) return false;

            switch (srcCon.Contype)
            {
                case ConstrType.ConstrPrimary:
                case ConstrType.ConstrUnique:
                    return (srcCon.Keys?.ToString() ?? "") == (tgtCon.Keys?.ToString() ?? "");

                case ConstrType.ConstrForeign:
                    return (srcCon.Keys?.ToString() ?? "") == (tgtCon.Keys?.ToString() ?? "")
                        && (srcCon.Pktable?.Relname ?? "") == (tgtCon.Pktable?.Relname ?? "")
                        && (srcCon.PkAttrs?.ToString() ?? "") == (tgtCon.PkAttrs?.ToString() ?? "");

                case ConstrType.ConstrCheck:
                    return (srcCon.RawExpr?.ToString() ?? "") == (tgtCon.RawExpr?.ToString() ?? "");

                default:
                    return true;
            }

        }

    }
}
