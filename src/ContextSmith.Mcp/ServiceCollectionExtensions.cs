using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Html;
using ContextSmith.Documents.Text;
using ContextSmith.Persistence.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp;

/// <summary>Registration helpers for the ContextSmith MCP server services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the document store and parser selector from configuration.</summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="configuration">Configuration that selects providers (for example <c>Storage:Provider</c>).</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddContextSmithApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var storageProvider = configuration["Storage:Provider"] ?? "InMemory";
        var storageDirectory = configuration["Storage:Directory"] ?? "data";

        services.AddSingleton<IDocumentStore>(_ => storageProvider switch
        {
            "InMemory" => new InMemoryDocumentStore(),
            "File" => new FileDocumentStore(storageDirectory),
            var other => throw new NotSupportedException($"Unknown Storage:Provider '{other}'."),
        });

        services.AddHttpClient<IDocumentSourceFetcher, HttpDocumentSourceFetcher>();

        services.AddSingleton<IDocumentParserSelector>(_ =>
        {
            var parsersByExtension = new Dictionary<string, IDocumentParser>
            {
                ["txt"] = new TextDocumentParser(),
                ["md"] = new MarkdownDocumentParser(),
                ["docx"] = new DocxDocumentParser(),
                ["html"] = new HtmlDocumentParser(),
                ["htm"] = new HtmlDocumentParser(),
            };

            return new ExtensionDocumentParserSelector(parsersByExtension);
        });

        return services;
    }
}
