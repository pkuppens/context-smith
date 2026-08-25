namespace ContextSmith.Application;

public interface IDocumentParserSelector
{
    IDocumentParser GetParser(string fileName);
}
