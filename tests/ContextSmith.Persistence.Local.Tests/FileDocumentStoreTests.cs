using ContextSmith.Domain;
using ContextSmith.Persistence.Local;

namespace ContextSmith.Persistence.Local.Tests;

public class FileDocumentStoreTests
{
    [Fact]
    public void Store_under_an_existing_id_overwrites_the_prior_document()
    {
        var directory = CreateTempDirectory();
        var store = new FileDocumentStore(directory);
        var first = new Document { Metadata = new DocumentMetadata { Title = "First" }, Provenance = new Provenance("handbook.md") };
        var second = new Document { Metadata = new DocumentMetadata { Title = "Second" }, Provenance = new Provenance("handbook.md") };

        store.Store(first, "handbook");
        store.Store(second, "handbook");

        Assert.Equal("Second", store.Get("handbook")?.Metadata.Title);
    }

    [Fact]
    public void Get_reloads_a_document_written_by_a_different_instance()
    {
        var directory = CreateTempDirectory();
        var document = new Document
        {
            Metadata = new DocumentMetadata { Title = "Handbook" },
            Provenance = new Provenance("handbook.md"),
            Children = [new Section { Title = "Leave Policy", Provenance = new Provenance("handbook.md", "s1") }],
        };

        new FileDocumentStore(directory).Store(document, "handbook");
        var reloaded = new FileDocumentStore(directory).Get("handbook");

        Assert.Equal("Handbook", reloaded?.Metadata.Title);
        var section = Assert.IsType<Section>(Assert.Single(reloaded!.Children));
        Assert.Equal("Leave Policy", section.Title);
    }

    [Fact]
    public void Get_reloads_a_table_written_by_a_different_instance()
    {
        // Regression: TableBlock was missing from DocumentNode's polymorphic JSON types,
        // so any document with a table (HTML, PDF, Excel, PowerPoint) failed to save.
        var directory = CreateTempDirectory();
        var document = new Document
        {
            Metadata = new DocumentMetadata(),
            Provenance = new Provenance("plans.html"),
            Children = [new TableBlock { Rows = [["Plan", "Cost"], ["Dental", "10"]], Provenance = new Provenance("plans.html", "table-1") }],
        };

        new FileDocumentStore(directory).Store(document, "plans");
        var reloaded = new FileDocumentStore(directory).Get("plans");

        var table = Assert.IsType<TableBlock>(Assert.Single(reloaded!.Children));
        Assert.Equal(["Dental", "10"], table.Rows[1]);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "context-smith-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
