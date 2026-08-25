namespace ContextSmith.Application;

public interface IDocumentSourceFetcher
{
    Task<DocumentSource> FetchAsync(Uri url, CancellationToken cancellationToken = default);
}
