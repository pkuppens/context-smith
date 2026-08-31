namespace ContextSmith.Application;

/// <summary>Selects a parser by matching the file name extension against a registered map.</summary>
/// <param name="parsersByExtension">Map from lower-case extension without the leading dot (for example <c>"pdf"</c>) to its parser.</param>
public sealed class ExtensionDocumentParserSelector(IReadOnlyDictionary<string, IDocumentParser> parsersByExtension)
    : IDocumentParserSelector
{
    /// <inheritdoc/>
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
