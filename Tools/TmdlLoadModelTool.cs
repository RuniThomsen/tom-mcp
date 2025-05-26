using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using System.ComponentModel;
using ModelHelpers;
using Microsoft.AnalysisServices.Tabular;

namespace Tools;

[McpServerToolType]
public static class TmdlLoadModelTool
{
    [McpServerTool(Name = "tmdl_load_model")]
    [Description("Load an existing TMDL model and return a summary of its contents")]
    public static Task<string> LoadModel(
        [Description("Path to the TMDL file to load")]
        string inputPath,
        [Description("Progress reporter for streaming load results")]
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        List<string> messages = new();        void Report(string message)
        {
            messages.Add(message);
            progress?.Report(new ProgressNotificationValue
            {
                Progress = messages.Count,
                Total = null,
                Message = message
            });
        }

        try
        {
            Report($"Loading model from '{inputPath}'...");

            Database database;
            
            // Handle both single file and directory (separated TMDL) scenarios
            if (Directory.Exists(inputPath))
            {
                // Separated TMDL structure - use TmdlSerializer.DeserializeDatabaseFromFolder
                try 
                {
                    database = TmdlSerializer.DeserializeDatabaseFromFolder(inputPath);
                    Report($"✅ Loaded separated TMDL structure from directory");
                }
                catch (Exception ex)
                {
                    Report($"⚠️ Failed to load as separated TMDL: {ex.Message}");
                    Report($"🔄 Attempting to load model.tmdl file directly...");
                    
                    var modelFile = Path.Combine(inputPath, "model.tmdl");
                    if (!File.Exists(modelFile))
                        throw new FileNotFoundException($"Neither separated TMDL structure nor model.tmdl found in: {inputPath}");
                    
                    database = TmdlIo.Load(modelFile);
                    Report($"✅ Loaded from model.tmdl (limited - tables/measures may not be included)");
                }
            }
            else if (File.Exists(inputPath))
            {
                // Single TMDL file
                database = TmdlIo.Load(inputPath);
                Report($"✅ Loaded single TMDL file");
            }
            else
            {
                throw new FileNotFoundException($"Path not found: {inputPath}");
            }
            
            var model = database.Model;

            Report($"✅ Model '{model.Name}' loaded");
            Report($"✅ Tables: {model.Tables.Count}");
            Report($"✅ Measures (total): {model.Tables.Sum(t => t.Measures.Count)}");

            return Task.FromResult(string.Join("\n", messages));
        }
        catch (Exception ex)
        {
            var error = $"❌ Error loading model: {ex.Message}";
            Report(error);
            return Task.FromResult(string.Join("\n", messages));
        }
    }
}
