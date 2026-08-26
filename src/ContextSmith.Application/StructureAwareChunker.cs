using ContextSmith.Domain;

namespace ContextSmith.Application;

public sealed class StructureAwareChunker : IChunkingStrategy
{
    public IReadOnlyList<Chunk> Chunk(Document document)
    {
        var chunks = new List<Chunk>();
        Visit(document, []);
        return chunks;

        void Visit(DocumentNode node, IReadOnlyList<string> headingPath)
        {
            var ownText = DocumentTextWalker.OwnText(node).ToList();
            if (ownText.Count > 0)
            {
                chunks.Add(new Chunk(Guid.NewGuid().ToString("n"), string.Join(" ", ownText), node.Provenance, headingPath));
            }

            foreach (var section in node.Children.OfType<Section>())
            {
                var childPath = section.Title is null ? headingPath : [.. headingPath, section.Title];
                Visit(section, childPath);
            }
        }
    }
}
