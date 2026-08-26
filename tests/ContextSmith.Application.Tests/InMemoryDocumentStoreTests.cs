using ContextSmith.Domain;

namespace ContextSmith.Application.Tests;

public class InMemoryDocumentStoreTests
{
    [Fact]
    public void Store_under_an_existing_id_overwrites_the_prior_document()
    {
        var store = new InMemoryDocumentStore();
        var first = new Document { Metadata = new DocumentMetadata { Title = "First" }, Provenance = new Provenance("handbook.md") };
        var second = new Document { Metadata = new DocumentMetadata { Title = "Second" }, Provenance = new Provenance("handbook.md") };

        var id = store.Store(first, "handbook");
        store.Store(second, "handbook");

        Assert.Equal("handbook", id);
        Assert.Equal("Second", store.Get("handbook")?.Metadata.Title);
    }
}
