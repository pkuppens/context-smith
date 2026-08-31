namespace ContextSmith.Domain;

/// <summary>A list node. List items are held as child nodes.</summary>
public sealed class ListBlock : DocumentNode
{
    /// <summary><see langword="true"/> for an ordered (numbered) list; <see langword="false"/> for a bullet list.</summary>
    public required bool Ordered { get; init; }
}
