namespace ContextSmith.Application;

public sealed record DocumentOutlineNode(string? Title, int Level, IReadOnlyList<DocumentOutlineNode> Children);
