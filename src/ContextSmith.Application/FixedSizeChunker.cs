using ContextSmith.Domain;

namespace ContextSmith.Application;

public sealed class FixedSizeChunker(int maxCharacters = 500) : IChunkingStrategy
{
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
