namespace ContextSmith.Domain;

/// <summary>Points back to where a piece of content came from in its source document.</summary>
/// <param name="SourceId">Identifier of the source document.</param>
/// <param name="Location">Optional position within the source, such as a page number or an element path.</param>
public sealed record Provenance(string SourceId, string? Location = null);
