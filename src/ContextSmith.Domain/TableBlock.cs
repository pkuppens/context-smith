namespace ContextSmith.Domain;

/// <summary>A table node. Cell text is stored row by row.</summary>
public sealed class TableBlock : DocumentNode
{
    /// <summary>Table rows in document order. Each inner list holds the cell text for one row, left to right.</summary>
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
