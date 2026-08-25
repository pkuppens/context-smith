using System.Net;

namespace ContextSmith.Documents.Html.Tests;

public class HttpDocumentSourceFetcherTests
{
    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/page.html")]
    public async Task FetchAsync_rejects_non_http_schemes(string url)
    {
        var fetcher = new HttpDocumentSourceFetcher(new HttpClient(new NeverCalledHandler()));

        await Assert.ThrowsAsync<NotSupportedException>(() => fetcher.FetchAsync(new Uri(url)));
    }

    [Fact]
    public async Task FetchAsync_downloads_content_from_an_http_url()
    {
        var handler = new StubHandler("<html><body><p>hello</p></body></html>");
        var fetcher = new HttpDocumentSourceFetcher(new HttpClient(handler));

        var source = await fetcher.FetchAsync(new Uri("https://example.com/page.html"));

        Assert.Equal("https://example.com/page.html", source.SourceId);
        using var reader = new StreamReader(source.Content);
        Assert.Equal("<html><body><p>hello</p></body></html>", await reader.ReadToEndAsync());
    }

    private sealed class NeverCalledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("The HTTP client should not be called for a rejected scheme.");
    }

    private sealed class StubHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content),
            });
    }
}
