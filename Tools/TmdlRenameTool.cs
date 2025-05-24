using ModelContextProtocol.Server;
using Microsoft.AnalysisServices.Tabular;
using ModelHelpers;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

namespace Tools;

// Ensure correct attribute and static class usage for MCP SDK 0.3.1-alpha
[McpServerToolType]
public static class TmdlRenameTool
{
    [McpServerTool(Name = "tmdl_rename_object")]
    [Description("Renames a table / column / measure and updates all DAX dependencies")]
    public static string RenameObject(
        string folderPath,
        string objectType,
        string table,
        string oldName,
        string newName,
        CancellationToken ct)
    {
        objectType = objectType.ToLowerInvariant().Trim();

        var db = TmdlIo.Load(folderPath);

        // 1. Perform the rename inside the model
        switch (objectType)
        {
            case "table":
                RenameTable(db, oldName, newName);
                break;

            case "column":
                RenameColumn(db, table, oldName, newName);
                break;

            case "measure":
                RenameMeasure(db, table, oldName, newName);
                break;

            default:
                throw new ArgumentException($"Unsupported objectType '{objectType}'. Expected table | column | measure.");
        }

        // 2. Rewrite DAX expressions everywhere
        foreach (var tbl in db.Model.Tables)
        {
            foreach (var m in tbl.Measures)
                m.Expression = Rewrite(m.Expression, objectType, table, oldName, newName);            foreach (var c in tbl.Columns)
                if (c.Type == ColumnType.Calculated && c is CalculatedColumn cc)
                    cc.Expression = Rewrite(cc.Expression, objectType, table, oldName, newName);
        }

        TmdlIo.Save(db, folderPath);
        return $"✔ Renamed {objectType} '{oldName}' → '{newName}'.";
    }

    // -------------------- helpers --------------------

    private static void RenameTable(Database db, string oldName, string newName)
    {
        if (!db.Model.Tables.Contains(oldName))
            throw new ArgumentException($"Table '{oldName}' not found.");
        db.Model.Tables[oldName].Name = newName;
    }

    private static void RenameColumn(Database db, string table, string oldName, string newName)
    {
        if (!db.Model.Tables.Contains(table))
            throw new ArgumentException($"Table '{table}' not found.");

        var col = db.Model.Tables[table].Columns[oldName]
                  ?? throw new ArgumentException($"Column '{oldName}' not found in table '{table}'.");
        col.Name = newName;
    }

    private static void RenameMeasure(Database db, string table, string oldName, string newName)
    {
        if (!db.Model.Tables.Contains(table))
            throw new ArgumentException($"Table '{table}' not found.");

        var msr = db.Model.Tables[table].Measures[oldName]
                  ?? throw new ArgumentException($"Measure '{oldName}' not found in table '{table}'.");
        msr.Name = newName;
    }

    // Creates a deterministic regex based on object type to rewrite all references.
    private static string Rewrite(
        string? expression,
        string objectType,
        string table,
        string oldName,
        string newName)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return expression ?? "";

        Regex rx;
        switch (objectType)
        {
            case "table":
                // Matches OldTable[Column]  OR  'Old Table'[Column]
                rx = new Regex($@"(?<![A-Za-z0-9_])('?{Regex.Escape(oldName)}'?)\s*\[",
                               RegexOptions.IgnoreCase | RegexOptions.Compiled);
                return rx.Replace(expression, m =>
                {
                    var prefix = m.Groups[1].Value.StartsWith('\'')
                        ? $"'{newName}'"
                        : newName;
                    return $"{prefix}[";
                });

            case "column":
                // Matches      Table[OldColumn]    or   'Table'[OldColumn]
                var tablePattern = Regex.Escape(table);
                rx = new Regex(
                    $@"('?{tablePattern}'?)\s*\[\s*{Regex.Escape(oldName)}\s*\]",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                return rx.Replace(expression, m => $"{m.Groups[1].Value}[{newName}]");

            case "measure":
                // Matches   [OldMeasure] (possibly qualified) but not part of longer identifiers.
                var measureQualified = $@"('?{Regex.Escape(table)}'?)?\s*\[\s*{Regex.Escape(oldName)}\s*\]";
                rx = new Regex(measureQualified,
                               RegexOptions.IgnoreCase | RegexOptions.Compiled);
                return rx.Replace(expression, m =>
                {
                    // Preserve any table qualifier that was present
                    var prefix = m.Groups[1].Success ? $"{m.Groups[1].Value}" : "";
                    return $"{prefix}[{newName}]";
                });

            default:
                return expression;
        }
    }
}
