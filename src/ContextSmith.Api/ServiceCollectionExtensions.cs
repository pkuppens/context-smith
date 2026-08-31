using ContextSmith.Application;
using ContextSmith.Documents.Docx;
using ContextSmith.Documents.Html;
using ContextSmith.Documents.Text;
using ContextSmith.Persistence.Local;
using ContextSmith.Retrieval.Azure;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Api;

/// <summary>Registration helpers for the ContextSmith API services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the document store, parsers, embedding and retrieval services, and chat client from configuration.</summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="configuration">Configuration that selects providers (for example <c>Storage:Provider</c>, <c>Embedding:Provider</c>, <c>Retrieval:Provider</c>).</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
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
        var azureOpenAiEmbeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];

        services.AddSingleton<IEmbeddingService>(sp => embeddingProvider switch
        {
            "Ollama" => new OllamaEmbeddingService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama"), embeddingModel),
            "AzureOpenAI" => new AzureOpenAiEmbeddingService(
                new Uri(configuration["AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required when Embedding:Provider is 'AzureOpenAI'.")),
                azureOpenAiEmbeddingDeployment
                    ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment is required when Embedding:Provider is 'AzureOpenAI'."),
                configuration["AzureOpenAI:ApiKey"]),
            var other => throw new NotSupportedException($"Unknown Embedding:Provider '{other}'."),
        });

        services.AddSingleton(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ollama");
            return new OllamaChatClient(httpClient, chatModel);
        });

        // Retrieval:Provider is a separate axis from Storage:Provider: Azure AI Search is a
        // vector index, not a general document store, so it cannot fill the IDocumentStore
        // role above. When unset, retrieval defaults to following Storage:Provider, which
        // preserves the InMemory/File behavior from #8 unchanged.
        var retrievalProvider = configuration["Retrieval:Provider"] ?? storageProvider;

        services.AddSingleton(_ => new DocumentRetrievalRegistry(retrievalProvider switch
        {
            "InMemory" => documentId => new InMemoryRetrievalService(),
            "File" => documentId => new FileRetrievalService(storageDirectory, documentId),
            "AzureSearch" => documentId => new AzureAiSearchRetrievalService(
                new Uri(configuration["AzureSearch:Endpoint"]
                    ?? throw new InvalidOperationException("AzureSearch:Endpoint is required when Retrieval:Provider is 'AzureSearch'.")),
                configuration["AzureSearch:IndexPrefix"] ?? "contextsmith",
                documentId,
                EmbeddingDimensionResolver.Resolve(
                    int.TryParse(configuration["AzureSearch:VectorDimension"], out var configuredDimension) ? configuredDimension : null,
                    azureOpenAiEmbeddingDeployment ?? embeddingModel),
                configuration["AzureSearch:ApiKey"]),
            var other => throw new NotSupportedException($"Unknown Retrieval:Provider '{other}'."),
        }));

        return services;
    }
}
