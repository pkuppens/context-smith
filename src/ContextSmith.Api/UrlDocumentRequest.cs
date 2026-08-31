namespace ContextSmith.Api;

/// <summary>Request body for preparing a document from a URL.</summary>
/// <param name="Url">Absolute <c>http</c> or <c>https</c> URL of the page to fetch and prepare.</param>
public sealed record UrlDocumentRequest(string Url);
