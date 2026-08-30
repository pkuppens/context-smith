using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Embeddings;

namespace ContextSmith.Retrieval.Azure;

public sealed class AzureOpenAiEmbeddingGateway : IAzureEmbeddingGateway
{
    private readonly EmbeddingClient _embeddingClient;

    public AzureOpenAiEmbeddingGateway(Uri endpoint, string deploymentName, string? apiKey = null)
    {
        AzureOpenAIClient client = apiKey is null
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));

        _embeddingClient = client.GetEmbeddingClient(deploymentName);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.ToFloats().ToArray();
    }
}
