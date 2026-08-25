using ContextSmith.Application;

namespace ContextSmith.Documents.Html;

public sealed class HttpDocumentSourceFetcher(HttpClient httpClient) : IDocumentSourceFetcher
{
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
