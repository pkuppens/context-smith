using ContextSmith.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddContextSmithApplication();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<PrepareDocumentTool>()
    .WithResources<DocumentResources>()
    .WithPrompts<ContextSmithPrompts>();

await builder.Build().RunAsync();
