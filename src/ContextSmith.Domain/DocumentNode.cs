using System.Text.Json.Serialization;

namespace ContextSmith.Domain;

[JsonPolymorphic]
[JsonDerivedType(typeof(Document), "document")]
[JsonDerivedType(typeof(Section), "section")]
[JsonDerivedType(typeof(Heading), "heading")]
[JsonDerivedType(typeof(Paragraph), "paragraph")]
[JsonDerivedType(typeof(ListBlock), "listBlock")]
public abstract class DocumentNode
{
    public required Provenance Provenance { get; init; }

    public IReadOnlyList<DocumentNode> Children { get; init; } = [];
}
