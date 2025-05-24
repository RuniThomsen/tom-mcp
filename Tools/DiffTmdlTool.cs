using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tools;

// Ensure correct attribute and static class usage for MCP SDK 0.3.1-alpha
[McpServerToolType]
public static class DiffTmdlTool
{
    // Streams a Git-style unified diff (<1 KB per SSE message).
    [McpServerTool(Name = "diff_tmdl")]
    [Description("Streams a Git-style patch between two TMDL files (SSE)")]    public static async IAsyncEnumerable<string> DiffTmdl(
        string oldTmdlPath,
        string newTmdlPath,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"--no-pager diff --no-index -U0 \"{oldTmdlPath}\" \"{newTmdlPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        using var reader = proc.StandardOutput;

        const int MaxChunk = 1024;          // 1 KB
        var chunk = new StringBuilder(MaxChunk);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync() ?? "";
            if (chunk.Length + line.Length + 1 > MaxChunk)
            {
                yield return chunk.ToString();
                chunk.Clear();
            }
            chunk.AppendLine(line);
        }

        if (chunk.Length > 0)
            yield return chunk.ToString();

        // Ensure the process finishes
        await proc.WaitForExitAsync(ct);
    }
}
