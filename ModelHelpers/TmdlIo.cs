using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AnalysisServices.Tabular;

namespace ModelHelpers;

public static class TmdlIo
{
    public static Database Load(string folder)
    {
        // Connect to the Analysis Services instance
        var conn = new Server();
        // Create a new database with model
        var db = new Database();
        db.Model = new Model();
        db.Name = Path.GetFileName(folder);

        // Check if folder exists
        if (!Directory.Exists(folder))
        {
            Console.WriteLine($"Warning: Folder '{folder}' does not exist.");
            return db;
        }

        // Find model.tmdl file
        string modelFilePath = Path.Combine(folder, "model.tmdl");
        if (!File.Exists(modelFilePath))
        {
            Console.WriteLine($"Warning: model.tmdl not found in '{folder}'");
            return db;
        }

        try
        {
            // Read TMDL file content
            string tmdlContent = File.ReadAllText(modelFilePath);
            
            // Extract model name
            var modelNameMatch = Regex.Match(tmdlContent, @"model\s+(\w+)");
            if (modelNameMatch.Success)
            {
                db.Name = modelNameMatch.Groups[1].Value;
                db.Model.Name = db.Name;
            }

            // Parse tables and measures using regex
            // This is a simplified approach - a real implementation would be more robust
            var tableMatches = Regex.Matches(tmdlContent, @"table\s+(\w+)");
            foreach (Match tableMatch in tableMatches)
            {
                string tableName = tableMatch.Groups[1].Value;
                var table = new Table { Name = tableName };
                
                // Try to find measures for this table
                string tablePattern = $@"table\s+{tableName}[^{{]*{{([^}}]*?)}}";
                var tableContentMatch = Regex.Match(tmdlContent, tablePattern, RegexOptions.Singleline);
                if (tableContentMatch.Success)
                {
                    string tableContent = tableContentMatch.Groups[1].Value;
                    
                    // Extract measures
                    var measureMatches = Regex.Matches(tableContent, @"measure\s+'([^']+)'[^=]*=\s*{([^}]*)}", RegexOptions.Singleline);
                    foreach (Match measureMatch in measureMatches)
                    {
                        string measureName = measureMatch.Groups[1].Value;
                        string expression = measureMatch.Groups[2].Value.Trim();
                        
                        table.Measures.Add(new Measure { 
                            Name = measureName,
                            Expression = expression
                        });
                    }
                      // Extract columns (simplified)
                    var columnMatches = Regex.Matches(tableContent, @"column\s+(\w+)");
                    foreach (Match columnMatch in columnMatches)
                    {
                        string columnName = columnMatch.Groups[1].Value;
                        // Use DataColumn instead of the abstract Column type
                        table.Columns.Add(new DataColumn { Name = columnName });
                    }
                }
                
                db.Model.Tables.Add(table);
            }

            Console.WriteLine($"Successfully loaded model with {db.Model.Tables.Count} tables");
            return db;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing TMDL file: {ex.Message}");
            // Return empty model on error
            return db;
        }
    }

    public static void Save(Database db, string folder)
    {
        // Create the folder if it doesn't exist
        Directory.CreateDirectory(folder);
        
        // For now, just log what would be saved
        Console.WriteLine($"Saving database {db.Name} with {db.Model.Tables.Count} tables to {folder}");
        
        // In a real implementation, we would serialize the model to TMDL format
        // This would require building the TMDL syntax from the model
        string modelContent = $"model {db.Name}\n";
        foreach (var table in db.Model.Tables)
        {
            modelContent += $"\ntable {table.Name}\n{{\n";
            
            // Add columns
            foreach (var column in table.Columns)
            {
                modelContent += $"    column {column.Name}\n";
            }
            
            // Add measures
            foreach (var measure in table.Measures)
            {
                modelContent += $"    measure '{measure.Name}' = {measure.Expression}\n";
            }
            
            modelContent += "}\n";
        }
        
        // Write to model.tmdl
        string modelFilePath = Path.Combine(folder, "model.tmdl");
        File.WriteAllText(modelFilePath, modelContent);
        
        Console.WriteLine($"Saved model to {modelFilePath}");
    }
}
