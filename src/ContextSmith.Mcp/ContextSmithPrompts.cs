using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

[McpServerPromptType]
public sealed class ContextSmithPrompts
{
    [McpServerPrompt(Name = "analyze-document-structure")]
    [Description("Guide an agent to inspect document structure before it changes or indexes content.")]
    public static IEnumerable<ChatMessage> AnalyzeDocumentStructure(
        [Description("The id returned by prepare_document.")] string documentId) =>
        [
            new(ChatRole.User, $"""
                Inspect the structure of document '{documentId}' before making any changes to it or indexing it.
                Read the contextsmith://documents/{documentId}/structure resource and report the section and
                heading hierarchy, noting anything that looks malformed or unexpectedly flat.
                """),
        ];

    [McpServerPrompt(Name = "prepare-document-for-rag")]
    [Description("Guide an agent through document preparation, chunk creation, and quality checks.")]
    public static IEnumerable<ChatMessage> PrepareDocumentForRag(
        [Description("The file name of the document to prepare.")] string fileName) =>
        [
            new(ChatRole.User, $"""
                Prepare '{fileName}' for retrieval-augmented generation.
                Call prepare_document to parse it, inspect the returned structure, then choose a chunking
                strategy appropriate to that structure before creating chunks for indexing.
                """),
        ];

    [McpServerPrompt(Name = "review-chunk-quality")]
    [Description("Guide an agent to inspect generated chunks and report likely retrieval problems.")]
    public static IEnumerable<ChatMessage> ReviewChunkQuality(
        [Description("The id returned by prepare_document.")] string documentId) =>
        [
            new(ChatRole.User, $"""
                Review the chunks generated for document '{documentId}'.
                Flag chunks that lose necessary context (e.g. a chunk whose heading path is missing, or
                whose text is too short to be meaningful on its own) and suggest a fix.
                """),
        ];
}
