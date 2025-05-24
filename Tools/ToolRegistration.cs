using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Tools;

public static class ToolRegistration
{
    // This class is not a tool, so do not annotate with tool attributes.
    // If you want to register tools manually, keep this as a static helper.
    public static IServiceCollection RegisterToolsManually(this IServiceCollection services)
    {
        Console.WriteLine("Manually registering MCP tools...");
        
        // Get the MCP server builder
        var mcpBuilder = services.AddMcpServer();        // Register all discovered tools by using reflection to scan for attributes
        var toolTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null);
          foreach (var toolType in toolTypes)
        {
            Console.WriteLine($"Registering tool: {toolType.Name}");
            var withToolsMethod = mcpBuilder.GetType().GetMethod("WithTools");
            if (withToolsMethod != null)
            {
                withToolsMethod.MakeGenericMethod(toolType)
                              .Invoke(mcpBuilder, Array.Empty<object>());
            }
            else
            {
                Console.WriteLine($"Warning: Could not find WithTools method on the MCP builder for {toolType.Name}");
            }
        }
        
        Console.WriteLine("Tool registration complete");
        return services;
    }
}
