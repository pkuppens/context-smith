using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Persistence.Postgres.Tests;

// Runs against a real PostgreSQL with the pgvector extension. Skipped (not failed) when
// CONTEXTSMITH_POSTGRES is unset. Start one locally with:
//   docker run -d -e POSTGRES_PASSWORD=test -e POSTGRES_DB=contextsmith -p 54329:5432 pgvector/pgvector:pg17
//   CONTEXTSMITH_POSTGRES="Host=localhost;Port=54329;Username=postgres;Password=test;Database=contextsmith"
public class PostgresIntegrationTests
{
    private const int Dimension = 3;

    private static string ConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONTEXTSMITH_POSTGRES");
        Skip.If(string.IsNullOrWhiteSpace(connectionString), "Set CONTEXTSMITH_POSTGRES to a pgvector-enabled PostgreSQL connection string to run this test.");
        return connectionString!;
    }

    private static string NewId() => $"it-{Guid.NewGuid():n}";

    [SkippableFact]
    public void Store_then_Get_round_trips_a_document_including_a_table()
    {
        var store = new PostgresDocumentStore(ConnectionString());
        var id = NewId();
        var document = new Document
        {
            Metadata = new DocumentMetadata { Title = "Handbook" },
            Provenance = new Provenance("handbook.md"),
            Children =
            [
                new Section { Title = "Benefits", Provenance = new Provenance("handbook.md", "s1") },
                new TableBlock { Rows = [["Plan", "Cost"], ["Dental", "10"]], Provenance = new Provenance("handbook.md", "t1") },
            ],
        };

        store.Store(document, id);
        var reloaded = store.Get(id);

        Assert.Equal("Handbook", reloaded?.Metadata.Title);
        Assert.Equal("Benefits", Assert.IsType<Section>(reloaded!.Children[0]).Title);
        Assert.Equal(["Dental", "10"], Assert.IsType<TableBlock>(reloaded.Children[1]).Rows[1]);
    }

    [SkippableFact]
    public void Store_under_an_existing_id_overwrites_the_prior_document()
    {
        var store = new PostgresDocumentStore(ConnectionString());
        var id = NewId();

        store.Store(new Document { Metadata = new DocumentMetadata { Title = "First" }, Provenance = new Provenance("a.md") }, id);
        store.Store(new Document { Metadata = new DocumentMetadata { Title = "Second" }, Provenance = new Provenance("a.md") }, id);

        Assert.Equal("Second", store.Get(id)?.Metadata.Title);
    }

    [SkippableFact]
    public void Get_returns_null_for_an_unknown_id()
    {
        Assert.Null(new PostgresDocumentStore(ConnectionString()).Get(NewId()));
    }

    [SkippableFact]
    public async Task SearchAsync_returns_chunks_nearest_first_and_only_for_its_own_document()
    {
        var connectionString = ConnectionString();
        var mine = new PostgresRetrievalService(connectionString, NewId(), Dimension);
        var other = new PostgresRetrievalService(connectionString, NewId(), Dimension);
        var provenance = new Provenance("handbook.md");

        await mine.IndexAsync(new Chunk("leave", "leave text", provenance, ["Leave"]), [1f, 0f, 0f]);
        await mine.IndexAsync(new Chunk("parking", "parking text", provenance, ["Parking"]), [0f, 1f, 0f]);
        await mine.IndexAsync(new Chunk("benefits", "benefits text", provenance, ["Benefits"]), [0.9f, 0.1f, 0f]);
        await other.IndexAsync(new Chunk("foreign", "foreign text", provenance, []), [1f, 0f, 0f]);

        var results = await mine.SearchAsync([1f, 0f, 0f], topK: 2);

        Assert.Equal(["leave", "benefits"], results.Select(chunk => chunk.Id));
        Assert.Equal(["Leave"], results[0].HeadingPath);
    }

    [SkippableFact]
    public async Task IndexAsync_with_an_existing_chunk_id_replaces_the_entry()
    {
        var service = new PostgresRetrievalService(ConnectionString(), NewId(), Dimension);
        var provenance = new Provenance("handbook.md");

        await service.IndexAsync(new Chunk("c1", "old text", provenance, []), [1f, 0f, 0f]);
        await service.IndexAsync(new Chunk("c1", "new text", provenance, []), [0f, 1f, 0f]);

        var results = await service.SearchAsync([0f, 1f, 0f], topK: 5);

        var chunk = Assert.Single(results);
        Assert.Equal("new text", chunk.Text);
    }
}
