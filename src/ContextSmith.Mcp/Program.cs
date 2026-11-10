using ContextSmith.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Mcp:Transport (or env Mcp__Transport) selects how the server talks: Stdio (default, for a local
// MCP client that starts this process) or Http (for a container; serves /mcp and /health).
var transport = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build()["Mcp:Transport"] ?? "Stdio";

if (string.Equals(transport, "Http", StringComparison.OrdinalIgnoreCase))
{
    await McpHost.CreateHttpApp(args).RunAsync();
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddContextSmithApplication(builder.Configuration);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<PrepareDocumentTool>()
    .WithResources<DocumentResources>()
    .WithResources<SkillResources>()
    .WithPrompts<ContextSmithPrompts>();

await builder.Build().RunAsync();
