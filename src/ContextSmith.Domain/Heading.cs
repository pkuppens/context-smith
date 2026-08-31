namespace ContextSmith.Domain;

/// <summary>A heading node that introduces a section of the document.</summary>
public sealed class Heading : DocumentNode
{
    /// <summary>Heading text.</summary>
    public required string Text { get; init; }

    /// <summary>Heading depth, where 1 is the top level.</summary>
    public required int Level { get; init; }
}
