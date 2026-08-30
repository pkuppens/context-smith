namespace ContextSmith.Retrieval.Azure.Tests;

public class AzureOpenAiEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_delegates_to_the_gateway_and_returns_its_vector()
    {
        var gateway = new FakeAzureEmbeddingGateway([1f, 2f, 3f]);
        var service = new AzureOpenAiEmbeddingService(gateway);

        var embedding = await service.EmbedAsync("Employees qualify after twelve months of employment.");

        Assert.Equal([1f, 2f, 3f], embedding);
        Assert.Equal("Employees qualify after twelve months of employment.", gateway.LastRequestedText);
    }

    [SkippableFact]
    public async Task EmbedAsync_returns_a_non_empty_vector_when_Azure_OpenAI_is_configured()
    {
        Skip.IfNot(
            AzureCredentialsEnvironment.OpenAiConfigured,
            "Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_EMBEDDING_DEPLOYMENT to run this test.");

        var service = new AzureOpenAiEmbeddingService(
            new Uri(AzureCredentialsEnvironment.OpenAiEndpoint!),
            AzureCredentialsEnvironment.OpenAiEmbeddingDeployment!,
            AzureCredentialsEnvironment.OpenAiApiKey);

        var embedding = await service.EmbedAsync("Employees qualify after twelve months of employment.");

        Assert.NotEmpty(embedding);
    }

    private sealed class FakeAzureEmbeddingGateway(float[] embedding) : IAzureEmbeddingGateway
    {
        public string? LastRequestedText { get; private set; }

        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
        {
            LastRequestedText = text;
            return Task.FromResult(embedding);
        }
    }
}
