using System.Net;

namespace ContextSmith.Mcp.Tests;

public class HttpHostTests
{
    private static async Task<(Microsoft.AspNetCore.Builder.WebApplication App, HttpClient Client)> StartAsync()
    {
        // Port 0 lets the OS pick a free port, so parallel test runs never collide.
        var app = McpHost.CreateHttpApp(["--urls", "http://127.0.0.1:0"]);
        await app.StartAsync();
        var address = app.Urls.First();
        return (app, new HttpClient { BaseAddress = new Uri(address) });
    }

    [Fact]
    public async Task Health_endpoint_answers_200_Healthy()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mcp_endpoint_is_mapped_next_to_the_health_endpoint()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;
        using var __ = client;

        // A bare GET is not a valid MCP request, but it must reach the MCP handler, not a 404.
        var response = await client.GetAsync("/mcp");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
