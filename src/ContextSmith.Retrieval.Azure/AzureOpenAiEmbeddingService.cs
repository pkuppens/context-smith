using ContextSmith.Application;

namespace ContextSmith.Retrieval.Azure;

/// <summary>
/// A narrow seam over the Azure OpenAI SDK, so <see cref="AzureOpenAiEmbeddingService"/> can be
/// unit-tested against a fake gateway instead of a real Azure OpenAI resource.
/// </summary>
public interface IAzureEmbeddingGateway
{
    /// <summary>Calls Azure OpenAI to compute the embedding vector for <paramref name="text"/>.</summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>The embedding vector.</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
}

/// <summary><see cref="IEmbeddingService"/> backed by an Azure OpenAI embedding deployment.</summary>
public sealed class AzureOpenAiEmbeddingService : IEmbeddingService
{
    private readonly IAzureEmbeddingGateway _gateway;

    /// <summary>Creates a service that connects to Azure OpenAI directly.</summary>
    /// <param name="endpoint">Azure OpenAI resource endpoint.</param>
    /// <param name="deploymentName">Name of the embedding model deployment.</param>
    /// <param name="apiKey">API key, or <see langword="null"/> to authenticate with a managed identity.</param>
    public AzureOpenAiEmbeddingService(Uri endpoint, string deploymentName, string? apiKey = null)
        : this(new AzureOpenAiEmbeddingGateway(endpoint, deploymentName, apiKey))
    {
    }

    /// <summary>Creates a service backed by the given <paramref name="gateway"/>, used to inject a fake gateway in tests.</summary>
    /// <param name="gateway">Gateway used to talk to Azure OpenAI.</param>
    public AzureOpenAiEmbeddingService(IAzureEmbeddingGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc/>
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        _gateway.GenerateEmbeddingAsync(text, cancellationToken);
}
