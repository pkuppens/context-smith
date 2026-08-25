namespace ContextSmith.Application;

public sealed class ExtensionDocumentParserSelector(IReadOnlyDictionary<string, IDocumentParser> parsersByExtension)
    : IDocumentParserSelector
{
    public IDocumentParser GetParser(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (parsersByExtension.TryGetValue(extension, out var parser))
        {
            return parser;
        }

        throw new NotSupportedException($"No parser is registered for file extension '.{extension}'.");
    }
}
