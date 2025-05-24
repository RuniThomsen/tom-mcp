using ModelContextProtocol.Server;
using Microsoft.AnalysisServices.Tabular;
using ModelHelpers;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace Tools;

// Ensure correct attribute and static class usage for MCP SDK 0.3.1-alpha
[McpServerToolType]
public static class TmdlAnalysisTools
{
    private static readonly Regex ColumnRefRegex =
        // Matches  Table[Column]   or  'Table Name'[Column Name]
        new(@"'?([^'\[\]]+)'?\[([^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [McpServerTool(Name = "tmdl_detect_unused_columns")]
    [Description("Lists columns that are not referenced by any measure or calculated column")]
    public static string DetectUnusedColumns(
        string folderPath,
        CancellationToken ct)
    {
        var db = TmdlIo.Load(folderPath);

        // 1. Gather all column references used in expressions.
        var used = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var tbl in db.Model.Tables)
        {
            // Measures
            foreach (var m in tbl.Measures)
                AddRefs(m.Expression);            // Calculated columns
            foreach (var c in tbl.Columns)
                if (c.Type == ColumnType.Calculated)
                    AddRefs(c is CalculatedColumn cc ? cc.Expression : null);
        }

        void AddRefs(string? expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return;

            foreach (Match match in ColumnRefRegex.Matches(expr))
            {
                var table = match.Groups[1].Value.Trim();
                var col   = match.Groups[2].Value.Trim();
                used.Add($"{table}[{col}]");
            }
        }

        // 2. Determine unused columns.
        var unused = db.Model.Tables
            .SelectMany(t => t.Columns, (t, c) => new { t.Name, Column = c })
            .Where(tc => !used.Contains($"{tc.Name}[{tc.Column.Name}]"))
            .Select(tc => $"{tc.Name}[{tc.Column.Name}]")
            .OrderBy(x => x, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        return unused.Count == 0
            ? "✔ No unused columns detected."
            : string.Join('\n', unused);
    }
}
