using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Embeddings;

namespace ContextSmith.Retrieval.Azure;

/// <summary><see cref="IAzureEmbeddingGateway"/> that calls a real Azure OpenAI embedding deployment through the Azure SDK.</summary>
public sealed class AzureOpenAiEmbeddingGateway : IAzureEmbeddingGateway
{
    private readonly EmbeddingClient _embeddingClient;

    /// <summary>Creates a gateway for an Azure OpenAI embedding deployment.</summary>
    /// <param name="endpoint">Azure OpenAI resource endpoint.</param>
    /// <param name="deploymentName">Name of the embedding model deployment.</param>
    /// <param name="apiKey">API key, or <see langword="null"/> to authenticate with a managed identity.</param>
    public AzureOpenAiEmbeddingGateway(Uri endpoint, string deploymentName, string? apiKey = null)
    {
        AzureOpenAIClient client = apiKey is null
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));

        _embeddingClient = client.GetEmbeddingClient(deploymentName);
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.ToFloats().ToArray();
    }
}
