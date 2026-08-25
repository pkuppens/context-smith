namespace ContextSmith.Domain;

public sealed class TableBlock : DocumentNode
{
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
