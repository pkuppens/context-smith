using System.ComponentModel;
using ContextSmith.Application;
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
        [new(ChatRole.User, Render(PromptCatalog.AnalyzeDocumentStructure, "{documentId}", documentId))];

    [McpServerPrompt(Name = "prepare-document-for-rag")]
    [Description("Guide an agent through document preparation, chunk creation, and quality checks.")]
    public static IEnumerable<ChatMessage> PrepareDocumentForRag(
        [Description("The file name of the document to prepare.")] string fileName) =>
        [new(ChatRole.User, Render(PromptCatalog.PrepareDocumentForRag, "{fileName}", fileName))];

    [McpServerPrompt(Name = "review-chunk-quality")]
    [Description("Guide an agent to inspect generated chunks and report likely retrieval problems.")]
    public static IEnumerable<ChatMessage> ReviewChunkQuality(
        [Description("The id returned by prepare_document.")] string documentId) =>
        [new(ChatRole.User, Render(PromptCatalog.ReviewChunkQuality, "{documentId}", documentId))];

    private static string Render(PromptDefinition definition, string placeholder, string value)
        => definition.Template.Replace(placeholder, value);
}
