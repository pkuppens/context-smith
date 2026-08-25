using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Text;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContextSmithApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();

        services.AddSingleton<IDocumentParserSelector>(_ =>
        {
            var parsersByExtension = new Dictionary<string, IDocumentParser>
            {
                ["txt"] = new TextDocumentParser(),
                ["md"] = new MarkdownDocumentParser(),
                ["docx"] = new DocxDocumentParser(),
            };

            return new ExtensionDocumentParserSelector(parsersByExtension);
        });

        return services;
    }
}
