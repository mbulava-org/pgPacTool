using mbulava.PostgreSql.Dac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mbulava.PostgreSql.Dac.Compare
{
    public static class PublishScriptGenerator
    {
        public static string Generate(SchemaDiff diff)
        {
            var sb = new StringBuilder();

            // Schemas
            foreach (var schema in diff.MissingSchemas)
                sb.AppendLine($"CREATE SCHEMA {schema.Name};");

            // Types
            AppendDiffs(sb, diff.TypeDiffs, "Types");

            // Sequences
            AppendDiffs(sb, diff.SequenceDiffs, "Sequences");

            // Tables
            AppendDiffs(sb, diff.TableDiffs, "Tables");

            // Column defaults (sequence bindings)
            AppendDiffs(sb, diff.ColumnDiffs, "Column Defaults");

            // Constraints (use specialized ordering)
            AppendConstraints(sb, diff.ConstraintDiffs);


            // Indexes
            AppendDiffs(sb, diff.IndexDiffs, "Indexes");

            // Views
            AppendDiffs(sb, diff.ViewDiffs, "Views");

            // Functions
            AppendDiffs(sb, diff.FunctionDiffs, "Functions");

            // Triggers
            AppendDiffs(sb, diff.TriggerDiffs, "Triggers");

            return sb.ToString();

        }

        private static void AppendDiffs(StringBuilder sb, List<ObjectDiff> diffs, string category)
        {
            if (diffs.Count == 0) return;

            sb.AppendLine($"-- {category}");
            foreach (var d in diffs)
            {
                sb.AppendLine($"-- {d.Type}: {d.Name}");
                sb.AppendLine(d.Script);
                sb.AppendLine();
            }
        }

        private static void AppendConstraints(StringBuilder sb, List<ObjectDiff> diffs)
        {
            var fkDiffs = diffs.Where(d => d.Script.Contains("FOREIGN KEY")).ToList();
            var otherDiffs = diffs.Except(fkDiffs).ToList();

            // Non-FK constraints first (PK, UNIQUE, CHECK)
            if (otherDiffs.Count > 0)
            {
                sb.AppendLine("-- Constraints (PK/UNIQUE/CHECK)");
                foreach (var d in otherDiffs)
                {
                    sb.AppendLine($"-- {d.Type}: {d.Name}");
                    sb.AppendLine(d.Script);
                    sb.AppendLine();
                }
            }

            // FK constraints last
            if (fkDiffs.Count > 0)
            {
                sb.AppendLine("-- Constraints (FOREIGN KEYS)");
                foreach (var d in fkDiffs)
                {
                    sb.AppendLine($"-- {d.Type}: {d.Name}");
                    sb.AppendLine(d.Script);
                    sb.AppendLine();
                }
            }
        }
    }
}
