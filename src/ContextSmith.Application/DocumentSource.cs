namespace ContextSmith.Application;

/// <summary>Raw input handed to a parser: an identifier and the byte stream to read.</summary>
/// <param name="SourceId">Identifier of the source, such as a file name or a URL. Used to pick a parser and to record provenance.</param>
/// <param name="Content">Readable stream of the source bytes. The caller owns the stream lifetime.</param>
public sealed record DocumentSource(string SourceId, Stream Content);
