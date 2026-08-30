using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Retrieval.Azure.Tests;

// End-to-end coverage against real Azure OpenAI and Azure AI Search resources, per #19's
// acceptance criteria. Skipped (not failed) when the required environment variables are
// absent, using the pattern established in AzureCredentialsEnvironment.
public class AzureRetrievalIntegrationTests
{
    [SkippableFact]
    public async Task IndexAsync_then_SearchAsync_finds_the_most_relevant_fixture_chunk()
    {
        Skip.IfNot(
            AzureCredentialsEnvironment.OpenAiConfigured && AzureCredentialsEnvironment.SearchConfigured,
            "Set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_EMBEDDING_DEPLOYMENT, and AZURE_SEARCH_ENDPOINT to run this test.");

        var embeddingService = new AzureOpenAiEmbeddingService(
            new Uri(AzureCredentialsEnvironment.OpenAiEndpoint!),
            AzureCredentialsEnvironment.OpenAiEmbeddingDeployment!,
            AzureCredentialsEnvironment.OpenAiApiKey);

        var vectorDimension = EmbeddingDimensionResolver.Resolve(
            configuredDimension: null,
            AzureCredentialsEnvironment.OpenAiEmbeddingDeployment!);

        var documentId = $"integration-test-{Guid.NewGuid():N}";
        var retrievalService = new AzureAiSearchRetrievalService(
            new Uri(AzureCredentialsEnvironment.SearchEndpoint!),
            indexNamePrefix: "contextsmith-test",
            documentId,
            vectorDimension,
            AzureCredentialsEnvironment.SearchApiKey);

        var provenance = new Provenance("handbook.md");
        var fixtureChunks = new[]
        {
            new Chunk("leave", "Parental leave rules: employees qualify after twelve months.", provenance, ["Leave Policy"]),
            new Chunk("benefits", "Health and dental benefits are available to all full-time staff.", provenance, ["Benefits"]),
            new Chunk("parking", "Office parking is first-come, first-served.", provenance, ["Facilities"]),
        };

        foreach (var chunk in fixtureChunks)
        {
            var embedding = await embeddingService.EmbedAsync(chunk.Text);
            await retrievalService.IndexAsync(chunk, embedding);
        }

        // Azure AI Search indexing is eventually consistent; give it a moment to become searchable.
        await Task.Delay(TimeSpan.FromSeconds(2));

        var queryEmbedding = await embeddingService.EmbedAsync("How long before an employee can take parental leave?");
        var results = await retrievalService.SearchAsync(queryEmbedding, topK: 1);

        var topResult = Assert.Single(results);
        Assert.Equal("leave", topResult.Id);
    }
}
