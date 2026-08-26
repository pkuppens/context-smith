using ContextSmith.Application;
using ContextSmith.Domain;
using ContextSmith.Persistence.Local;

namespace ContextSmith.Persistence.Local.Tests;

public class FileRetrievalServiceTests
{
    [Fact]
    public async Task IndexAsync_replaces_the_existing_entry_for_the_same_chunk_id()
    {
        var directory = CreateTempDirectory();
        var provenance = new Provenance("handbook.md");
        var service = new FileRetrievalService(directory, "handbook");

        await service.IndexAsync(new Chunk("chunk-1", "Old text.", provenance, []), [1f, 0f]);
        await service.IndexAsync(new Chunk("chunk-1", "New text.", provenance, []), [0f, 1f]);

        var results = await service.SearchAsync([0f, 1f], topK: 10);

        var result = Assert.Single(results);
        Assert.Equal("New text.", result.Text);
    }

    [Fact]
    public async Task SearchAsync_ranks_chunks_by_cosine_similarity_after_reloading_from_disk()
    {
        var directory = CreateTempDirectory();
        var provenance = new Provenance("handbook.md");

        var first = new FileRetrievalService(directory, "handbook");
        await first.IndexAsync(new Chunk("leave", "Parental leave rules.", provenance, []), [1f, 0f, 0f]);
        await first.IndexAsync(new Chunk("benefits", "Health and dental benefits.", provenance, []), [0f, 1f, 0f]);

        var reloaded = new FileRetrievalService(directory, "handbook");
        var results = await reloaded.SearchAsync([0.9f, 0.1f, 0f], topK: 1);

        var result = Assert.Single(results);
        Assert.Equal("Parental leave rules.", result.Text);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "context-smith-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
