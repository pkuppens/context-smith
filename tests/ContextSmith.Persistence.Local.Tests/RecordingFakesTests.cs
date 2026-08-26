using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Persistence.Local.Tests;

public class RecordingFakesTests
{
    [Fact]
    public void RecordingDocumentStore_records_Store_calls_without_touching_real_storage()
    {
        var store = new RecordingDocumentStore();
        var document = new Document { Metadata = new DocumentMetadata { Title = "Handbook" }, Provenance = new Provenance("handbook.md") };

        var id = store.Store(document, "handbook");

        Assert.Equal("handbook", id);
        var call = Assert.Single(store.StoreCalls);
        Assert.Same(document, call.Document);
    }

    [Fact]
    public async Task RecordingRetrievalService_records_IndexAsync_calls_without_touching_real_storage()
    {
        var service = new RecordingRetrievalService();
        var chunk = new Chunk("chunk-1", "Parental leave rules.", new Provenance("handbook.md"), []);

        await service.IndexAsync(chunk, [1f, 0f]);

        var call = Assert.Single(service.IndexCalls);
        Assert.Same(chunk, call.Chunk);
    }
}
