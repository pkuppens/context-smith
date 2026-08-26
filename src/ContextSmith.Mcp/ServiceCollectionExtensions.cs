using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Html;
using ContextSmith.Documents.Text;
using ContextSmith.Persistence.Local;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp;

public static class ServiceCollectionExtensions
{
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
