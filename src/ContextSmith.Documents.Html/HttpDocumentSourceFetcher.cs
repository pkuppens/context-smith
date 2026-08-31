using ContextSmith.Application;

namespace ContextSmith.Documents.Html;

/// <summary>Fetches <c>http</c> and <c>https</c> documents with an <see cref="HttpClient"/>.</summary>
/// <param name="httpClient">Client used for the download.</param>
public sealed class HttpDocumentSourceFetcher(HttpClient httpClient) : IDocumentSourceFetcher
{
    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">The URL scheme is not <c>http</c> or <c>https</c>.</exception>
    public async Task<DocumentSource> FetchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        if (url.Scheme is not ("http" or "https"))
        {
            throw new NotSupportedException(
                $"'{url.Scheme}' URLs cannot be fetched. Only http and https are supported.");
        }

        var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        return new DocumentSource(url.ToString(), new MemoryStream(bytes));
    }
}
