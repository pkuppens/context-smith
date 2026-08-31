namespace ContextSmith.Application;

/// <summary>A reusable prompt that the server can offer to an agent.</summary>
/// <param name="Name">Stable prompt name used to request it.</param>
/// <param name="Goal">Short statement of what the prompt helps the agent do.</param>
/// <param name="Template">Prompt body. Placeholders in braces (for example <c>{documentId}</c>) are filled in by the caller.</param>
public sealed record PromptDefinition(string Name, string Goal, string Template);
