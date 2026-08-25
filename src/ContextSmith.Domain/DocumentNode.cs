namespace ContextSmith.Domain;

public abstract class DocumentNode
{
    public required Provenance Provenance { get; init; }

    public IReadOnlyList<DocumentNode> Children { get; init; } = [];
}
