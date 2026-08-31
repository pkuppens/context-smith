using System.ComponentModel;
using ContextSmith.Application;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

/// <summary>Exposes the <see cref="PromptCatalog"/> prompts as MCP server prompts.</summary>
[McpServerPromptType]
public sealed class ContextSmithPrompts
{
    /// <summary>Prompt that guides an agent to inspect a document's structure before changing or indexing it.</summary>
    /// <param name="documentId">The id returned by <c>prepare_document</c>.</param>
    /// <returns>The rendered prompt messages.</returns>
    [McpServerPrompt(Name = "analyze-document-structure")]
    [Description("Guide an agent to inspect document structure before it changes or indexes content.")]
    public static IEnumerable<ChatMessage> AnalyzeDocumentStructure(
        [Description("The id returned by prepare_document.")] string documentId) =>
        [new(ChatRole.User, Render(PromptCatalog.AnalyzeDocumentStructure, "{documentId}", documentId))];

    /// <summary>Prompt that walks an agent through document preparation, chunk creation, and quality checks.</summary>
    /// <param name="fileName">The file name of the document to prepare.</param>
    /// <returns>The rendered prompt messages.</returns>
    [McpServerPrompt(Name = "prepare-document-for-rag")]
    [Description("Guide an agent through document preparation, chunk creation, and quality checks.")]
    public static IEnumerable<ChatMessage> PrepareDocumentForRag(
        [Description("The file name of the document to prepare.")] string fileName) =>
        [new(ChatRole.User, Render(PromptCatalog.PrepareDocumentForRag, "{fileName}", fileName))];

    /// <summary>Prompt that guides an agent to inspect generated chunks and report likely retrieval problems.</summary>
    /// <param name="documentId">The id returned by <c>prepare_document</c>.</param>
    /// <returns>The rendered prompt messages.</returns>
    [McpServerPrompt(Name = "review-chunk-quality")]
    [Description("Guide an agent to inspect generated chunks and report likely retrieval problems.")]
    public static IEnumerable<ChatMessage> ReviewChunkQuality(
        [Description("The id returned by prepare_document.")] string documentId) =>
        [new(ChatRole.User, Render(PromptCatalog.ReviewChunkQuality, "{documentId}", documentId))];

    private static string Render(PromptDefinition definition, string placeholder, string value)
        => definition.Template.Replace(placeholder, value);
}
