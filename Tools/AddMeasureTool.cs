using ModelContextProtocol.Server;
using Microsoft.AnalysisServices.Tabular;
using ModelHelpers;
using System.ComponentModel;

namespace Tools;

// Ensure correct attribute and static class usage for MCP SDK 0.3.1-alpha
[McpServerToolType]
public static class AddMeasureTool
{
    [McpServerTool(Name = "addMeasure")]
    [Description("Adds or updates a measure in a TMDL file")]
    public static string AddMeasure(
        string tmdlPath,
        string table,
        string measureName,
        string dax,
        CancellationToken ct)
    {
        var db = TmdlIo.Load(tmdlPath);
        var tbl = db.Model.Tables[table];

        if (tbl.Measures.Contains(measureName))
            tbl.Measures[measureName].Expression = dax;
        else
            tbl.Measures.Add(new Measure { Name = measureName, Expression = dax });

        TmdlIo.Save(db, tmdlPath);
        return $"✔ Measure '{measureName}' saved.";
    }
}
