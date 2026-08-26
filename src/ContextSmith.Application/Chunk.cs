using ContextSmith.Domain;

namespace ContextSmith.Application;

public sealed record Chunk(string Id, string Text, Provenance Provenance, IReadOnlyList<string> HeadingPath);
