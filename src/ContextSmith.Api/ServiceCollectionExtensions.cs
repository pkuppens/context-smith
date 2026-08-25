using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Html;
using ContextSmith.Documents.Text;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContextSmithApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
        services.AddSingleton<DocumentRetrievalRegistry>();
        services.AddScoped<DocumentProcessingService>();

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

        services.AddHttpClient<IDocumentSourceFetcher, HttpDocumentSourceFetcher>();

        var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/";
        var embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        var chatModel = configuration["Ollama:ChatModel"] ?? "nemotron-3.5-lightning";

        services.AddHttpClient("ollama", client => client.BaseAddress = new Uri(ollamaBaseUrl));

        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama");
            return new OllamaEmbeddingService(httpClient, embeddingModel);
        });

        services.AddSingleton(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama");
            return new OllamaChatClient(httpClient, chatModel);
        });

        return services;
    }
}
