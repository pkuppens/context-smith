namespace ContextSmith.Domain;

public sealed class Heading : DocumentNode
{
    public required string Text { get; init; }

    public required int Level { get; init; }
}
