using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>A unit of document text prepared for embedding and retrieval.</summary>
/// <param name="Id">Stable identifier for the chunk. Re-indexing the same id replaces the earlier entry.</param>
/// <param name="Text">Chunk body text.</param>
/// <param name="Provenance">Origin of the chunk text in its source document.</param>
/// <param name="HeadingPath">Titles of the enclosing sections, from the outermost to the innermost.</param>
public sealed record Chunk(string Id, string Text, Provenance Provenance, IReadOnlyList<string> HeadingPath);
