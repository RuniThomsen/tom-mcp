using ModelContextProtocol;
using ModelContextProtocol.Server;
using Tools;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq;

// Need to make this a proper program with a Main method since we're returning values
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Check if we're running with --list-tools for legacy tool discovery
        if (args.Length > 0 && args[0] == "--list-tools")
        {
            Console.WriteLine("test_echo_v2");
            Console.WriteLine("tmdl_list_tables");
            Console.WriteLine("tmdl_list_measures");
            Console.WriteLine("tmdl_detect_unused_columns");
            return 0;
        }

        // Redirect debug output to stderr to avoid interfering with JSON-RPC
        TextWriter originalOut = Console.Out;
        Console.SetOut(Console.Error);

        using var log = new StreamWriter("c:\\repos\\tom-mcp\\debug.log", true) { AutoFlush = true };
        log.WriteLine($"=== TOM-MCP startup: {DateTime.Now} ===");

        try {
            // Determine transport mode based on args or env variables
            bool useStdio = true; // Default to stdio for VS Code integration
            
            if (args.Contains("--http"))
            {
                useStdio = false;
                log.WriteLine("Using HTTP transport mode (from command line args)");
            }
            
            if (useStdio)
            {
                // Restore stdout for JSON-RPC communication
                Console.SetOut(originalOut);
                log.WriteLine("Using stdio transport mode");
                
                // Create host builder for stdio mode
                var hostBuilder = Host.CreateDefaultBuilder(args)
                    .ConfigureLogging(logging => 
                    {
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Debug);
                    })
                    .ConfigureServices((hostContext, services) => 
                    {
                        // Add MCP server with stdio transport
                        var mcpBuilder = services.AddMcpServer()
                            .WithStdioServerTransport();
                        
                        // Register tools
                        mcpBuilder.WithTools<TestToolv2>();
                        mcpBuilder.WithToolsFromAssembly(typeof(TmdlInfoTools).Assembly);
                        
                        log.WriteLine("Registered tools for stdio server");
                    });
                    
                var host = hostBuilder.Build();
                var server = host.Services.GetRequiredService<IMcpServer>();
                
                log.WriteLine("Starting stdio MCP server");
                await server.RunAsync();
                return 0;
            }
            
            // HTTP mode setup
            log.WriteLine("Setting up HTTP mode");
            var builder = WebApplication.CreateBuilder(args);
            
            // Add MCP server with HTTP transport
            var mcpBuilder = builder.Services.AddMcpServer()
                .WithHttpTransport();

// Register all the tool classes
mcpBuilder.WithTools<TestToolv2>();
mcpBuilder.WithToolsFromAssembly(typeof(TmdlInfoTools).Assembly); // This will register all tools in the assembly

Console.WriteLine("Successfully registered all tools");
Console.WriteLine($"Registered tools from assembly: {typeof(TmdlInfoTools).Assembly.FullName}");

var app = builder.Build();

// Map the MCP endpoints
Console.WriteLine("Mapping MCP endpoints");
app.MapMcp("/mcptools"); // Specify the endpoint explicitly

// Add some diagnostic endpoints
app.MapGet("/ping", () => "pong");
app.MapGet("/debug/routes", () => {
    return "Routes: /ping, /mcptools, /debug/routes, /debug/tools, /test-echo";
});

// Add a direct test endpoint for TestToolv2.Echo
app.MapGet("/test-echo", () => {
    var result = TestToolv2.Echo("Direct call without MCP");
    return result;
});

// Add a direct test for TMDL tools
app.MapGet("/test-tmdl", () => {
    try {
        var tmdlPath = @"c:\repos\Power BI\Common Semantic Model\Model.SemanticModel\definition";
        var tables = TmdlInfoTools.ListTables(tmdlPath, CancellationToken.None);
        return $"Found tables: {tables.Split('\n').Length}\nFirst few: {tables.Split('\n').Take(5).Aggregate((a, b) => $"{a}, {b}")}";
    }
    catch (Exception ex) {
        return $"Error: {ex.Message}\n{ex.StackTrace}";
    }
});

// Add a route to list known tools
app.MapGet("/debug/tools", () => {
    // Fall back to hard-coded list since we may not be able to get tools dynamically
    var toolNames = new List<string> {
        "test_echo_v2",
        "tmdl_list_tables",
        "tmdl_list_measures",
        "tmdl_detect_unused_columns"
    };
    
    return string.Join(Environment.NewLine, toolNames);
});

Console.WriteLine("Starting MCP Server");
app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            log.WriteLine($"[FATAL] Unhandled exception: {ex}");
            Console.Error.WriteLine($"[FATAL] Unhandled exception: {ex}");
            return 1;
        }
    }
}