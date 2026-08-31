namespace ContextSmith.Domain;

/// <summary>A section of the document that groups a heading with the nodes that follow it.</summary>
public sealed class Section : DocumentNode
{
    /// <summary>Section title, or <see langword="null"/> when the section has no heading.</summary>
    public string? Title { get; init; }
}
