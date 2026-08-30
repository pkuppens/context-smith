using ContextSmith.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
