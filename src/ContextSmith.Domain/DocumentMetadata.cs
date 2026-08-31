namespace ContextSmith.Domain;

/// <summary>Descriptive metadata about a document that is not part of its body content.</summary>
public sealed class DocumentMetadata
{
    /// <summary>Document title, or <see langword="null"/> when the source gives no title.</summary>
    public string? Title { get; init; }
}
