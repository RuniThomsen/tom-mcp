using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Tools;

[McpServerToolType]
public static class ListToolsTool
{
    [McpServerTool(Name = "list_tools")]
    [Description("Lists available tools in the MCP server")]
    public static string ListTools()
    {
        var result = new List<string>();
        
        var assembly = typeof(AddMeasureTool).Assembly;
        
        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<McpServerToolTypeAttribute>() == null)
                continue;
                
            result.Add($"Tool type: {type.Name}");
            
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (attr == null)
                    continue;
                    
                result.Add($"  - {attr.Name ?? method.Name}: {method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description"}");
            }
        }
        
        return string.Join('\n', result);
    }
}
