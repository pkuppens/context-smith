namespace ContextSmith.Application;

/// <summary>The built-in <see cref="PromptDefinition"/> values that ContextSmith publishes.</summary>
public static class PromptCatalog
{
    /// <summary>Prompt that guides an agent to inspect a document's structure before changing or indexing it.</summary>
    public static readonly PromptDefinition AnalyzeDocumentStructure = new(
        Name: "analyze-document-structure",
        Goal: "Guide an agent to inspect document structure before it changes or indexes content.",
        Template: """
            Inspect the structure of document '{documentId}' before making any changes to it or indexing it.
            Read the contextsmith://documents/{documentId}/structure resource and report the section and
            heading hierarchy, noting anything that looks malformed or unexpectedly flat.
            """);

    /// <summary>Prompt that walks an agent through document preparation, chunk creation, and quality checks.</summary>
    public static readonly PromptDefinition PrepareDocumentForRag = new(
        Name: "prepare-document-for-rag",
        Goal: "Guide an agent through document preparation, chunk creation, and quality checks.",
        Template: """
            Prepare '{fileName}' for retrieval-augmented generation.
            Call prepare_document to parse it, inspect the returned structure, then choose a chunking
            strategy appropriate to that structure before creating chunks for indexing.
            """);

    /// <summary>Prompt that guides an agent to inspect generated chunks and report likely retrieval problems.</summary>
    public static readonly PromptDefinition ReviewChunkQuality = new(
        Name: "review-chunk-quality",
        Goal: "Guide an agent to inspect generated chunks and report likely retrieval problems.",
        Template: """
            Review the chunks generated for document '{documentId}'.
            Flag chunks that lose necessary context (e.g. a chunk whose heading path is missing, or
            whose text is too short to be meaningful on its own) and suggest a fix.
            """);

    /// <summary>All published prompts, in a stable order.</summary>
    public static IReadOnlyList<PromptDefinition> All { get; } =
    [
        AnalyzeDocumentStructure,
        PrepareDocumentForRag,
        ReviewChunkQuality,
    ];
}
