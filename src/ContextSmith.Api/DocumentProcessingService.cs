using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Api;

/// <summary>
/// Orchestrates document preparation and chat: parse a source, chunk it, embed and index the chunks,
/// then answer questions against the indexed chunks.
/// </summary>
/// <param name="parserSelector">Chooses a parser for the source file.</param>
/// <param name="sourceFetcher">Downloads a document from a URL.</param>
/// <param name="documentStore">Stores parsed documents.</param>
/// <param name="embeddingService">Computes embedding vectors for chunk and query text.</param>
/// <param name="retrievalRegistry">Provides the per-document retrieval service.</param>
/// <param name="chatClient">Chat model client used to generate answers.</param>
public sealed class DocumentProcessingService(
    IDocumentParserSelector parserSelector,
    IDocumentSourceFetcher sourceFetcher,
    IDocumentStore documentStore,
    IEmbeddingService embeddingService,
    DocumentRetrievalRegistry retrievalRegistry,
    OllamaChatClient chatClient)
{
    private static readonly IChunkingStrategy Chunker = new StructureAwareChunker();

    /// <summary>Parses an uploaded file, stores it, indexes its chunks, and returns a structure summary.</summary>
    /// <param name="fileName">Name of the uploaded file. Its extension selects the parser.</param>
    /// <param name="content">Readable stream of the file content.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the prepared document, including the id it is stored under.</returns>
    public async Task<DocumentStructureSummary> PrepareFromUploadAsync(
        string fileName, Stream content, CancellationToken cancellationToken)
    {
        var parser = parserSelector.GetParser(fileName);
        var source = new DocumentSource(fileName, content);
        var document = await parser.ParseAsync(source, cancellationToken).ConfigureAwait(false);
        return await StoreAndIndexAsync(document, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Fetches a URL, parses it as HTML, stores it, indexes its chunks, and returns a structure summary.</summary>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL to fetch.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the prepared document, including the id it is stored under.</returns>
    public async Task<DocumentStructureSummary> PrepareFromUrlAsync(Uri url, CancellationToken cancellationToken)
    {
        var source = await sourceFetcher.FetchAsync(url, cancellationToken).ConfigureAwait(false);
        var parser = parserSelector.GetParser("page.html");
        var document = await parser.ParseAsync(source, cancellationToken).ConfigureAwait(false);
        return await StoreAndIndexAsync(document, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Answers <paramref name="message"/> using the top chunks retrieved for the document.</summary>
    /// <param name="documentId">Identifier of a prepared document.</param>
    /// <param name="message">User question.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The generated answer and the chunks it was grounded on.</returns>
    /// <exception cref="KeyNotFoundException">No document is indexed under <paramref name="documentId"/>.</exception>
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
