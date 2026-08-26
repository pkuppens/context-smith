using ContextSmith.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp.Tests;

public class PrepareDocumentToolTests
{
    [Fact]
    public async Task PrepareDocument_parses_by_extension_and_stores_the_result()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().AddContextSmithApplication(configuration);
        await using var provider = services.BuildServiceProvider();

        var parserSelector = provider.GetRequiredService<IDocumentParserSelector>();
        var documentStore = provider.GetRequiredService<IDocumentStore>();

        var contentBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync("sample.md"));

        var summary = await PrepareDocumentTool.PrepareDocument(
            parserSelector,
            documentStore,
            fileName: "sample.md",
            contentBase64: contentBase64,
            documentId: "test-doc");

        Assert.Equal("test-doc", summary.DocumentId);
        Assert.Equal(4, summary.HeadingCount);
        Assert.Equal(4, summary.SectionCount);

        var topSection = Assert.Single(summary.Outline.Children);
        Assert.Equal("Employee Handbook", topSection.Title);
        Assert.Equal(2, topSection.Children.Count);

        Assert.NotNull(documentStore.Get("test-doc"));
    }
}
