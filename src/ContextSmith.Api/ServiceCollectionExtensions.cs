using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Html;
using ContextSmith.Documents.Text;
using ContextSmith.Persistence.Local;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContextSmithApi(this IServiceCollection services, IConfiguration configuration)
    {
        var storageProvider = configuration["Storage:Provider"] ?? "InMemory";
        var storageDirectory = configuration["Storage:Directory"] ?? "data";

        services.AddSingleton<IDocumentStore>(_ => storageProvider switch
        {
            "InMemory" => new InMemoryDocumentStore(),
            "File" => new FileDocumentStore(storageDirectory),
            var other => throw new NotSupportedException($"Unknown Storage:Provider '{other}'."),
        });

        services.AddSingleton(_ => new DocumentRetrievalRegistry(storageProvider switch
        {
            "InMemory" => documentId => new InMemoryRetrievalService(),
            "File" => documentId => new FileRetrievalService(storageDirectory, documentId),
            var other => throw new NotSupportedException($"Unknown Storage:Provider '{other}'."),
        }));

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

        var embeddingProvider = configuration["Embedding:Provider"] ?? "Ollama";

        services.AddSingleton<IEmbeddingService>(sp => embeddingProvider switch
        {
            "Ollama" => new OllamaEmbeddingService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama"), embeddingModel),
            var other => throw new NotSupportedException($"Unknown Embedding:Provider '{other}'."),
        });

        services.AddSingleton(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama");
            return new OllamaChatClient(httpClient, chatModel);
        });

        return services;
    }
}
