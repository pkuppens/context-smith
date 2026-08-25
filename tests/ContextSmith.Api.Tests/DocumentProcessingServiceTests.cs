using System.Net;
using System.Net.Http.Json;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Api.Tests;

public class DocumentProcessingServiceTests
{
    [Fact]
    public async Task PrepareFromUploadAsync_indexes_chunks_and_ChatAsync_retrieves_the_relevant_one()
    {
        var service = new DocumentProcessingService(
            new FakeParserSelector(),
            new ThrowingSourceFetcher(),
            new InMemoryDocumentStore(),
            new KeywordEmbeddingService(),
            new DocumentRetrievalRegistry(),
            new OllamaChatClient(new HttpClient(new StubChatHandler("The answer is twelve months.")) { BaseAddress = new Uri("http://localhost:11434/") }));

        using var content = new MemoryStream();
        var summary = await service.PrepareFromUploadAsync("handbook.md", content, CancellationToken.None);

        Assert.Equal(3, summary.SectionCount);

        var (answer, sources) = await service.ChatAsync(summary.DocumentId, "parental leave", CancellationToken.None);

        Assert.Equal("The answer is twelve months.", answer);
        Assert.Equal(["Handbook", "Parental Leave"], sources[0].HeadingPath);
    }

    [Fact]
    public async Task ChatAsync_throws_for_an_unknown_document_id()
    {
        var service = new DocumentProcessingService(
            new FakeParserSelector(),
            new ThrowingSourceFetcher(),
            new InMemoryDocumentStore(),
            new KeywordEmbeddingService(),
            new DocumentRetrievalRegistry(),
            new OllamaChatClient(new HttpClient(new StubChatHandler("unused")) { BaseAddress = new Uri("http://localhost:11434/") }));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ChatAsync("no-such-document", "anything", CancellationToken.None));
    }

    private sealed class FakeParserSelector : IDocumentParserSelector
    {
        public IDocumentParser GetParser(string fileName) => new FakeDocumentParser();
    }

    private sealed class FakeDocumentParser : IDocumentParser
    {
        public Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
        {
            static Provenance P(string location) => new("handbook.md", location);

            var document = new Document
            {
                Metadata = new DocumentMetadata { Title = "Handbook" },
                Provenance = P("document"),
                Children =
                [
                    new Section
                    {
                        Title = "Handbook",
                        Provenance = P("root"),
                        Children =
                        [
                            new Heading { Text = "Handbook", Level = 1, Provenance = P("h1") },
                            new Section
                            {
                                Title = "Parental Leave",
                                Provenance = P("parental-leave"),
                                Children =
                                [
                                    new Heading { Text = "Parental Leave", Level = 2, Provenance = P("h2a") },
                                    new Paragraph { Text = "Employees qualify after twelve months.", Provenance = P("p1") },
                                ],
                            },
                            new Section
                            {
                                Title = "Benefits",
                                Provenance = P("benefits"),
                                Children =
                                [
                                    new Heading { Text = "Benefits", Level = 2, Provenance = P("h2b") },
                                    new Paragraph { Text = "Health and dental insurance.", Provenance = P("p2") },
                                ],
                            },
                        ],
                    },
                ],
            };

            return Task.FromResult(document);
        }
    }

    private sealed class KeywordEmbeddingService : IEmbeddingService
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            var lower = text.ToLowerInvariant();
            float[] vector = lower.Contains("parental") ? [1f, 0f, 0f]
                : lower.Contains("benefit") ? [0f, 1f, 0f]
                : [0f, 0f, 1f];
            return Task.FromResult(vector);
        }
    }

    private sealed class ThrowingSourceFetcher : IDocumentSourceFetcher
    {
        public Task<DocumentSource> FetchAsync(Uri url, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("This test does not fetch URLs.");
    }

    private sealed class StubChatHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    message = new { role = "assistant", content },
                }),
            });
    }
}
