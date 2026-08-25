namespace ContextSmith.Retrieval.Local.Tests;

public class OllamaEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_returns_a_non_empty_vector_when_Ollama_is_reachable()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434/") };

        if (!await IsReachableAsync(httpClient))
        {
            return;
        }

        var service = new OllamaEmbeddingService(httpClient);

        var embedding = await service.EmbedAsync("Employees qualify after twelve months of employment.");

        Assert.NotEmpty(embedding);
    }

    private static async Task<bool> IsReachableAsync(HttpClient httpClient)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var response = await httpClient.GetAsync("api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
