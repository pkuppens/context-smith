using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Retrieval.Local.Tests;

public class InMemoryRetrievalServiceTests
{
    [Fact]
    public async Task SearchAsync_ranks_chunks_by_cosine_similarity_to_the_query()
    {
        var service = new InMemoryRetrievalService();
        var provenance = new Provenance("handbook.md");

        var aboutLeave = new Chunk("Parental leave rules.", provenance, []);
        var aboutBenefits = new Chunk("Health and dental benefits.", provenance, []);
        var unrelated = new Chunk("Office parking policy.", provenance, []);

        await service.IndexAsync(aboutLeave, [1f, 0f, 0f]);
        await service.IndexAsync(aboutBenefits, [0f, 1f, 0f]);
        await service.IndexAsync(unrelated, [0f, 0f, 1f]);

        var results = await service.SearchAsync([0.9f, 0.1f, 0f], topK: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal(aboutLeave, results[0]);
        Assert.Equal(aboutBenefits, results[1]);
    }

    [Fact]
    public async Task SearchAsync_returns_at_most_topK_results()
    {
        var service = new InMemoryRetrievalService();
        var provenance = new Provenance("handbook.md");

        for (var i = 0; i < 5; i++)
        {
            await service.IndexAsync(new Chunk($"chunk {i}", provenance, []), [1f, i]);
        }

        var results = await service.SearchAsync([1f, 0f], topK: 3);

        Assert.Equal(3, results.Count);
    }
}
