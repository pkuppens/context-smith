namespace ContextSmith.Application;

/// <summary>One node in a document outline tree, used to present the section hierarchy.</summary>
/// <param name="Title">Section title, or <see langword="null"/> for the document root or an untitled section.</param>
/// <param name="Level">Depth in the outline, where 0 is the document root.</param>
/// <param name="Children">Child outline nodes in document order.</param>
public sealed record DocumentOutlineNode(string? Title, int Level, IReadOnlyList<DocumentOutlineNode> Children);
