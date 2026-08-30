using Azure.Search.Documents.Models;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Retrieval.Azure.Tests;

public class AzureAiSearchRetrievalServiceTests
{
    [Fact]
    public async Task IndexAsync_ensures_the_index_and_uploads_a_document_carrying_the_chunk_and_embedding()
    {
        var gateway = new FakeAzureSearchGateway();
        var service = new AzureAiSearchRetrievalService(gateway, vectorDimension: 3);
        var chunk = new Chunk("leave", "Parental leave rules.", new Provenance("handbook.md", "p1"), ["Leave Policy"]);

        await service.IndexAsync(chunk, [1f, 0f, 0f]);

        Assert.Equal(3, gateway.EnsuredVectorDimension);
        var uploaded = Assert.Single(gateway.UploadedDocuments);
        Assert.Equal("leave", uploaded["chunkId"]);
        Assert.Equal("Parental leave rules.", uploaded["text"]);
        Assert.Equal("handbook.md", uploaded["sourceId"]);
        Assert.Equal("p1", uploaded["location"]);
        Assert.Equal(new[] { "Leave Policy" }, uploaded["headingPath"]);
        Assert.Equal(new[] { 1f, 0f, 0f }, uploaded[AzureSearchGateway.VectorFieldName]);
    }

    [Fact]
    public async Task SearchAsync_reconstructs_chunks_from_the_gateways_results()
    {
        var gateway = new FakeAzureSearchGateway();
        gateway.SearchResults.Add(new SearchDocument
        {
            ["chunkId"] = "benefits",
            ["text"] = "Health and dental benefits.",
            ["sourceId"] = "handbook.md",
            ["location"] = null,
            ["headingPath"] = new[] { "Benefits" },
        });

        var service = new AzureAiSearchRetrievalService(gateway, vectorDimension: 3);

        var results = await service.SearchAsync([0f, 1f, 0f], topK: 5);

        var result = Assert.Single(results);
        Assert.Equal("benefits", result.Id);
        Assert.Equal("Health and dental benefits.", result.Text);
        Assert.Equal("handbook.md", result.Provenance.SourceId);
        Assert.Null(result.Provenance.Location);
        Assert.Equal(["Benefits"], result.HeadingPath);
    }

    [Theory]
    [InlineData("chunk-1", "chunk-1")]
    [InlineData("chunk/with:special.chars", "chunk_with_special_chars")]
    public void SanitizeKey_keeps_the_allowed_character_set(string chunkId, string expectedKey)
    {
        Assert.Equal(expectedKey, AzureAiSearchRetrievalService.SanitizeKey(chunkId));
    }

    [Fact]
    public void BuildIndexName_lowercases_and_strips_disallowed_characters()
    {
        var indexName = AzureAiSearchRetrievalService.BuildIndexName("ContextSmith", "Doc_123!");

        Assert.Equal("contextsmith-doc-123", indexName);
    }

    private sealed class FakeAzureSearchGateway : IAzureSearchGateway
    {
        public int? EnsuredVectorDimension { get; private set; }
        public List<SearchDocument> UploadedDocuments { get; } = [];
        public List<SearchDocument> SearchResults { get; } = [];

        public Task EnsureIndexAsync(int vectorDimension, CancellationToken cancellationToken)
        {
            EnsuredVectorDimension = vectorDimension;
            return Task.CompletedTask;
        }

        public Task MergeOrUploadAsync(SearchDocument document, CancellationToken cancellationToken)
        {
            UploadedDocuments.Add(document);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SearchDocument>> VectorSearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SearchDocument>>(SearchResults);
    }
}
