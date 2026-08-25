using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Html;
using ContextSmith.Documents.Text;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContextSmithApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
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
