namespace ContextSmith.Application;

/// <summary>Chooses the right <see cref="IDocumentParser"/> for a given file.</summary>
public interface IDocumentParserSelector
{
    /// <summary>Returns the parser that handles <paramref name="fileName"/>.</summary>
    /// <param name="fileName">File name whose extension selects the parser.</param>
    /// <returns>The matching parser.</returns>
    /// <exception cref="NotSupportedException">No parser is registered for the file extension.</exception>
    IDocumentParser GetParser(string fileName);
}
