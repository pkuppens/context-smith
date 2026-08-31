using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>Splits a parsed document into chunks ready for embedding and indexing.</summary>
public interface IChunkingStrategy
{
    /// <summary>Splits <paramref name="document"/> into chunks.</summary>
    /// <param name="document">Parsed document to split.</param>
    /// <returns>The chunks, in document order.</returns>
    IReadOnlyList<Chunk> Chunk(Document document);
}
