using ModelContextProtocol.Server;
using Microsoft.AnalysisServices.Tabular;
using ModelHelpers;
using System.ComponentModel;
using System.Linq;

namespace Tools;

[McpServerToolType]
public static class TmdlInfoTools
{
    // Lists every table name in the model.
    [McpServerTool(Name = "tmdl_list_tables")]
    [Description("Lists all tables in a TMDL file")]
    public static string ListTables(
        string tmdlPath,
        CancellationToken ct)
    {
        var db = TmdlIo.Load(tmdlPath);
        return string.Join('\n', db.Model.Tables.Select(t => t.Name));
    }

    // Lists every measure in the specified table.
    [McpServerTool(Name = "tmdl_list_measures")]
    [Description("Lists all measures in a specific table inside a TMDL file")]
    public static string ListMeasures(
        string tmdlPath,
        string table,
        CancellationToken ct)
    {
        var db = TmdlIo.Load(tmdlPath);

        if (!db.Model.Tables.Contains(table))
            return $"✖ Table '{table}' not found.";

        var tbl = db.Model.Tables[table];

        // No measures? – return an empty string rather than null.
        return string.Join('\n', tbl.Measures.Select(m => m.Name));
    }
}
