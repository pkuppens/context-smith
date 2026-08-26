using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Api;

public sealed class DocumentProcessingService(
    IDocumentParserSelector parserSelector,
    IDocumentSourceFetcher sourceFetcher,
    IDocumentStore documentStore,
    IEmbeddingService embeddingService,
    DocumentRetrievalRegistry retrievalRegistry,
    OllamaChatClient chatClient)
{
    private static readonly IChunkingStrategy Chunker = new StructureAwareChunker();

    public async Task<DocumentStructureSummary> PrepareFromUploadAsync(
        string fileName, Stream content, CancellationToken cancellationToken)
    {
        var parser = parserSelector.GetParser(fileName);
        var source = new DocumentSource(fileName, content);
        var document = await parser.ParseAsync(source, cancellationToken).ConfigureAwait(false);
        return await StoreAndIndexAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DocumentStructureSummary> PrepareFromUrlAsync(Uri url, CancellationToken cancellationToken)
    {
        var source = await sourceFetcher.FetchAsync(url, cancellationToken).ConfigureAwait(false);
        var parser = parserSelector.GetParser("page.html");
        var document = await parser.ParseAsync(source, cancellationToken).ConfigureAwait(false);
        return await StoreAndIndexAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(string Answer, IReadOnlyList<Chunk> Sources)> ChatAsync(
        string documentId, string message, CancellationToken cancellationToken)
    {
        if (documentStore.Get(documentId) is null)
        {
            throw new KeyNotFoundException($"No document is indexed under id '{documentId}'.");
        }

        var retrievalService = retrievalRegistry.GetOrCreate(documentId);
        var queryEmbedding = await embeddingService.EmbedAsync(message, cancellationToken).ConfigureAwait(false);
        var sources = await retrievalService.SearchAsync(queryEmbedding, topK: 4, cancellationToken).ConfigureAwait(false);

        var context = string.Join(
            "\n\n",
            sources.Select(chunk => $"[{string.Join(" > ", chunk.HeadingPath)}]\n{chunk.Text}"));

        const string systemPrompt =
            "Answer the user's question using only the provided document excerpts. " +
            "If the excerpts do not contain the answer, say so.";
        var userPrompt = $"Document excerpts:\n{context}\n\nQuestion: {message}";

        var answer = await chatClient.AskAsync(systemPrompt, userPrompt, cancellationToken).ConfigureAwait(false);
        return (answer, sources);
    }

    private async Task<DocumentStructureSummary> StoreAndIndexAsync(Document document, CancellationToken cancellationToken)
    {
        var documentId = documentStore.Store(document);
        var retrievalService = retrievalRegistry.GetOrCreate(documentId);

        foreach (var chunk in Chunker.Chunk(document))
        {
            var embedding = await embeddingService.EmbedAsync(chunk.Text, cancellationToken).ConfigureAwait(false);
            await retrievalService.IndexAsync(chunk, embedding, cancellationToken).ConfigureAwait(false);
        }

        return DocumentStructureSummaryBuilder.Build(documentId, document);
    }
}
