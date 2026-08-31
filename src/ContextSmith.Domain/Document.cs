namespace ContextSmith.Domain;

/// <summary>Root node of a parsed document tree. Holds the document metadata and all child nodes.</summary>
public sealed class Document : DocumentNode
{
    /// <summary>Metadata for the document, such as its title.</summary>
    public required DocumentMetadata Metadata { get; init; }
}
