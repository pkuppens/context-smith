using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp;

/// <summary>Builds the HTTP form of the MCP server, for container hosting.</summary>
public static class McpHost
{
    /// <summary>
    /// Creates a web app that serves MCP over streamable HTTP at <c>/mcp</c> and a health check at
    /// <c>/health</c>. It registers the same services, tools, resources, and prompts as stdio mode.
    /// </summary>
    /// <param name="args">Command-line arguments, for example <c>--urls</c>.</param>
    /// <returns>The built, not yet started, web application.</returns>
    public static WebApplication CreateHttpApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddContextSmithApplication(builder.Configuration);
        builder.Services.AddHealthChecks();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<PrepareDocumentTool>()
            .WithResources<DocumentResources>()
            .WithResources<SkillResources>()
            .WithPrompts<ContextSmithPrompts>();

        var app = builder.Build();
        app.MapHealthChecks("/health");
        app.MapMcp("/mcp");
        return app;
    }
}
