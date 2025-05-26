using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using Microsoft.AnalysisServices.Tabular;
using System.ComponentModel;
using System.IO;
using ModelHelpers;

namespace Tools;

// [McpServerToolType] - Disabled due to Database parameter serialization issues
public static class TmdlSaveModelTool
{
    [McpServerTool(Name = "tmdl_save_model")]
    [Description("Serializes an in-memory Database back to TMDL (folder or single file)")]
    public static string SaveModel(
        string outputPath,
        Database database,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return "⏹ Cancelled";
        if (database == null)           return "❌ Database argument was null";

        // Folder → separated Power-BI structure, otherwise single-file
        if (Directory.Exists(outputPath) || Path.GetExtension(outputPath) == string.Empty)
        {
            TmdlSerializer.SerializeDatabaseToFolder(database, outputPath);
            return $"✅ Saved model to folder: {outputPath}";
        }

        TmdlIo.Save(database, outputPath);
        return $"✅ Saved model to file: {outputPath}";
    }
}