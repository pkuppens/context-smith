using System.Text.Json.Serialization;

namespace ContextSmith.Domain;

/// <summary>Base type for every node in a parsed document tree, such as a section, heading, or paragraph.</summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(Document), "document")]
[JsonDerivedType(typeof(Section), "section")]
[JsonDerivedType(typeof(Heading), "heading")]
[JsonDerivedType(typeof(Paragraph), "paragraph")]
[JsonDerivedType(typeof(ListBlock), "listBlock")]
public abstract class DocumentNode
{
    /// <summary>Origin of this node in the source document.</summary>
    public required Provenance Provenance { get; init; }

    /// <summary>Child nodes nested under this node, in document order. Empty when the node is a leaf.</summary>
    public IReadOnlyList<DocumentNode> Children { get; init; } = [];
}
