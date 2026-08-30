using ContextSmith.Application;

namespace ContextSmith.Retrieval.Azure;

// A narrow seam over the Azure OpenAI SDK, so EmbedAsync's behavior can be unit-tested
// against a fake gateway instead of a real Azure OpenAI resource.
public interface IAzureEmbeddingGateway
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
}

public sealed class AzureOpenAiEmbeddingService : IEmbeddingService
{
    private readonly IAzureEmbeddingGateway _gateway;

    public AzureOpenAiEmbeddingService(Uri endpoint, string deploymentName, string? apiKey = null)
        : this(new AzureOpenAiEmbeddingGateway(endpoint, deploymentName, apiKey))
    {
    }

    public AzureOpenAiEmbeddingService(IAzureEmbeddingGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
        _gateway.GenerateEmbeddingAsync(text, cancellationToken);
}
