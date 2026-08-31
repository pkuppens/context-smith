namespace ContextSmith.Domain;

/// <summary>A paragraph of body text.</summary>
public sealed class Paragraph : DocumentNode
{
    /// <summary>Paragraph text.</summary>
    public required string Text { get; init; }
}
