using ModelContextProtocol;
using ModelContextProtocol.Server;
using Tools;
using TomMcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Need to make this a proper program with a Main method since we're returning values
public class Program
{    public static async Task<int> Main(string[] args)
    {        // Debug: log arguments to a file to understand what VS Code is passing
        try
        {
            var debugInfo = $"{DateTime.Now}: Args count: {args.Length}, Args: [{string.Join(", ", args)}], Process: {Environment.ProcessId}\n";
            File.AppendAllText(@"c:\repos\tom-mcp\args_debug.log", debugInfo);
        }
        catch { /* ignore errors */ }
          // Check if we're running with --list-tools for legacy tool discovery        
        // DISABLED: MCP protocol doesn't support this legacy mode
        if (false && args.Length > 0 && args[0] == "--list-tools")
        {
            try { File.AppendAllText(@"c:\repos\tom-mcp\args_debug.log", $"{DateTime.Now}: Entering --list-tools mode\n"); } catch { }
            Console.Error.WriteLine("DEBUG: --list-tools mode detected");
            Console.Error.WriteLine("test_echo_v2");
            Console.Error.WriteLine("tmdl_list_tables");
            Console.Error.WriteLine("tmdl_list_measures");
            Console.Error.WriteLine("tmdl_detect_unused_columns");
            Console.Error.WriteLine("tmdl_validate_model");
            Console.Error.WriteLine("xmla_list_databases");
            Console.Error.WriteLine("xmla_test_connection");
            Console.Error.WriteLine("xmla_test_connection_noauth");
            return 0;
        }
        
        // If we reach here, we should NOT be in --list-tools mode
        try { File.AppendAllText(@"c:\repos\tom-mcp\args_debug.log", $"{DateTime.Now}: Continuing to MCP server mode\n"); } catch { }// Redirect debug output to stderr to avoid interfering with JSON-RPC
        TextWriter originalOut = Console.Out;
        Console.SetOut(Console.Error);

        StreamWriter? log = null;
        try 
        {
            // Try to create debug log with retry mechanism
            var logPath = "c:\\repos\\tom-mcp\\debug.log";
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    log = new StreamWriter(logPath, true) { AutoFlush = true };
                    log.WriteLine($"=== TOM-MCP startup: {DateTime.Now} ===");
                    break;
                }
                catch (IOException) when (i < 2)
                {
                    // If file is locked, try with a timestamped name
                    logPath = $"c:\\repos\\tom-mcp\\debug_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                }
            }
            
            if (log == null)
            {
                Console.Error.WriteLine($"Warning: Could not create debug log file, continuing without logging");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Debug log setup failed: {ex.Message}");
        }

        try {
            // Determine transport mode based on args or env variables
            bool useStdio = true; // Default to stdio for VS Code integration
              if (args.Contains("--http"))
            {
                useStdio = false;
                log?.WriteLine("Using HTTP transport mode (from command line args)");
            }
            
            if (useStdio)
            {                // Restore stdout for JSON-RPC communication
                Console.SetOut(originalOut);
                log?.WriteLine("Using stdio transport mode");
                  // Create host builder for stdio mode
                var hostBuilder = Host.CreateDefaultBuilder(args)
                    .ConfigureLogging(logging => 
                    {
                        // Disable logging for stdio mode to avoid interfering with JSON-RPC
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Critical);
                    })                    .ConfigureServices((hostContext, services) => 
                    {
                        // Add authentication services
                        services.AddSingleton<AuthManager>();
                        services.AddTransient<SsasConnector>();
                        
                        // Add MCP server with stdio transport
                        var mcpBuilder = services.AddMcpServer()
                            .WithStdioServerTransport();                        // Register tools using wrapper classes for JSON-RPC compatibility
              
                        mcpBuilder.WithTools<Tools.WrapperClasses.TmdlValidateModelWrapper>();
                        mcpBuilder.WithTools<Tools.WrapperClasses.TmdlInfoWrapper>();
                        mcpBuilder.WithTools<Tools.WrapperClasses.TestToolWrapper>();
                        mcpBuilder.WithTools<Tools.WrapperClasses.XmlaInfoWrapper>();
                        
                        // Also register from assembly for any other tools
                        mcpBuilder.WithToolsFromAssembly(typeof(TmdlInfoTools).Assembly);
                        log?.WriteLine("Registered tools for stdio server");
                    });
                    
                var host = hostBuilder.Build();
                var server = host.Services.GetRequiredService<IMcpServer>();                
                log?.WriteLine("Starting stdio MCP server");
                await server.RunAsync();
                return 0;
            }            // HTTP mode setup
            log?.WriteLine("Setting up HTTP mode");
            var builder = WebApplication.CreateBuilder(args);
            
            // Add authentication services
            builder.Services.AddSingleton<AuthManager>();
            builder.Services.AddTransient<SsasConnector>();
            
            // Add MCP server with HTTP transport
            var mcpBuilder = builder.Services.AddMcpServer()
                .WithHttpTransport();

// Register tools using wrapper classes for JSON-RPC compatibility

mcpBuilder.WithTools<Tools.WrapperClasses.TmdlValidateModelWrapper>();
mcpBuilder.WithTools<Tools.WrapperClasses.TmdlInfoWrapper>();
mcpBuilder.WithTools<Tools.WrapperClasses.TestToolWrapper>();
mcpBuilder.WithTools<Tools.WrapperClasses.BpaValidateWrapper>();
mcpBuilder.WithTools<Tools.WrapperClasses.XmlaInfoWrapper>();

// Also register from assembly for backwards compatibility
mcpBuilder.WithToolsFromAssembly(typeof(TmdlInfoTools).Assembly);
mcpBuilder.WithToolsFromAssembly(typeof(TmdlInfoTools).Assembly);

var app = builder.Build();

// Map the MCP endpoints
log?.WriteLine("Mapping MCP endpoints");
app.MapMcp("/mcptools"); // Specify the endpoint explicitly

// Add some diagnostic endpoints
app.MapGet("/ping", () => "pong");
app.MapGet("/debug/routes", () => {
    return "Routes: /ping, /mcptools, /debug/routes, /test-echo, /test-tmdl, /test-validate";
});

// Add a direct test endpoint for TestToolv2.Echo
app.MapGet("/test-echo", () => {
    var result = TestTool.Echo("Direct call without MCP");
    return result;
});

// Add a direct test for TMDL tools
app.MapGet("/test-tmdl", () => {
    try {
        var tmdlPath = @"c:\repos\Power BI\Common Semantic Model\Model.SemanticModel\definition";
        var tables = TmdlInfoTools.ListTables(tmdlPath, CancellationToken.None);
        var tableArray = tables.Split('\n');
        return $"Found tables: {tableArray.Length}\nFirst few: {string.Join(", ", tableArray.Take(5))}";
    }
    catch (Exception ex) {
        return $"Error: {ex.Message}";
    }
});

// Add a direct test for TMDL validation
app.MapGet("/test-validate", async (HttpContext context) => {
    try {
        var tmdlPath = @"c:\repos\backup_company1.tmdl";
        context.Response.ContentType = "text/event-stream";
        
        var syncObj = new object();
        var progress = new Progress<ProgressNotificationValue>(value => {
            try {
                lock(syncObj) {
                    var message = value.Message;
                    var writeTask = context.Response.WriteAsync($"data: {message}\n\n");
                    writeTask.Wait();
                    var flushTask = context.Response.Body.FlushAsync();
                    flushTask.Wait();
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error in progress handler: {ex}");
            }
        });
        
        string result = await Tools.TmdlValidateModelTools.ValidateModel(tmdlPath, null, progress, context.RequestAborted);
        await context.Response.WriteAsync($"data: Complete - {result.Split('\n').Length} total messages\n\n");
        await context.Response.Body.FlushAsync();
    }
    catch (Exception ex) {
        await context.Response.WriteAsync($"data: Error: {ex.Message}\n\n");
        await context.Response.Body.FlushAsync();
    }
});

log?.WriteLine("Starting MCP Server");
app.Run();
            return 0;
        }        catch (Exception ex)
        {
            log?.WriteLine($"[FATAL] Unhandled exception: {ex}");
            Console.Error.WriteLine($"[FATAL] Unhandled exception: {ex}");
            return 1;
        }
        finally
        {
            // Ensure log is properly disposed
            log?.Dispose();
        }
    }
}