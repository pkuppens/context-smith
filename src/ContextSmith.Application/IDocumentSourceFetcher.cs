namespace ContextSmith.Application;

/// <summary>Downloads a remote document so it can be parsed.</summary>
public interface IDocumentSourceFetcher
{
    /// <summary>Fetches <paramref name="url"/> and returns its content as a <see cref="DocumentSource"/>.</summary>
    /// <param name="url">Absolute URL to fetch.</param>
    /// <param name="cancellationToken">Token to cancel the fetch.</param>
    /// <returns>The fetched source, with an identifier derived from <paramref name="url"/>.</returns>
    Task<DocumentSource> FetchAsync(Uri url, CancellationToken cancellationToken = default);
}
