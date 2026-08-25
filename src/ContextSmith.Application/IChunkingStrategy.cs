using ContextSmith.Domain;

namespace ContextSmith.Application;

public interface IChunkingStrategy
{
    IReadOnlyList<Chunk> Chunk(Document document);
}
