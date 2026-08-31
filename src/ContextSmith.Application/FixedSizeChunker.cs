using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>Chunking strategy that packs leaf text into chunks of a fixed maximum size, ignoring structure.</summary>
/// <param name="maxCharacters">Soft upper bound on chunk length in characters. A single leaf longer than this is still emitted whole.</param>
public sealed class FixedSizeChunker(int maxCharacters = 500) : IChunkingStrategy
{
    /// <inheritdoc/>
    public IReadOnlyList<Chunk> Chunk(Document document)
    {
        var chunks = new List<Chunk>();
        var buffer = new List<string>();
        var bufferLength = 0;
        Provenance? firstProvenance = null;
        IReadOnlyList<string> firstHeadingPath = [];

        void Flush()
        {
            if (buffer.Count == 0)
            {
                return;
            }

            chunks.Add(new Chunk(Guid.NewGuid().ToString("n"), string.Join(" ", buffer), firstProvenance!, firstHeadingPath));
            buffer.Clear();
            bufferLength = 0;
            firstProvenance = null;
        }

        foreach (var (text, provenance, headingPath) in DocumentTextWalker.WalkLeafText(document, []))
        {
            if (bufferLength > 0 && bufferLength + text.Length > maxCharacters)
            {
                Flush();
            }

            if (buffer.Count == 0)
            {
                firstProvenance = provenance;
                firstHeadingPath = headingPath;
            }

            buffer.Add(text);
            bufferLength += text.Length;
        }

        Flush();
        return chunks;
    }
}
