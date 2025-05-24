using ModelContextProtocol.Server;
using Microsoft.AnalysisServices.Tabular;
using ModelHelpers;
using System.ComponentModel;
using System.Linq;

namespace Tools;

// Ensure correct attribute and static class usage for MCP SDK 0.3.1-alpha
[McpServerToolType]
public static class TmdlFormatTool
{
    [McpServerTool(Name = "tmdl_format_model")]
    [Description("Deterministically re-serializes a TMDL folder so Git diffs remain minimal")]
    public static string FormatModel(
        string folderPath,
        CancellationToken ct)
    {
        var db = TmdlIo.Load(folderPath);

    // Sort tables, then columns / measures inside each table.
        SortNamedCollection<Table>(db.Model.Tables.ToList());

        foreach (var tbl in db.Model.Tables)
        {
            SortNamedCollection<Column>(tbl.Columns.ToList());
            SortNamedCollection<Measure>(tbl.Measures.ToList());
            SortNamedCollection<Hierarchy>(tbl.Hierarchies.ToList());
        }

        TmdlIo.Save(db, folderPath);
        return "✔ Model formatted.";
    }

    // Generic alphabetical reorder helper.
    private static void SortNamedCollection<T>(IList<T> collection)
        where T : NamedMetadataObject
    {
        var ordered = collection
            .OrderBy(x => x.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
        collection.Clear();
        foreach (var item in ordered)
            collection.Add(item);
    }
}
