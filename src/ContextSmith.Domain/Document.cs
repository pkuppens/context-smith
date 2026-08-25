namespace ContextSmith.Domain;

public sealed class Document : DocumentNode
{
    public required DocumentMetadata Metadata { get; init; }
}
