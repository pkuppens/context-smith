namespace ContextSmith.Domain;

public sealed class ListBlock : DocumentNode
{
    public required bool Ordered { get; init; }
}
