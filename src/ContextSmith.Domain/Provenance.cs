namespace ContextSmith.Domain;

public sealed record Provenance(string SourceId, string? Location = null);
