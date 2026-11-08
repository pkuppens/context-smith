using ContextSmith.Application;
using ContextSmith.Documents.Pdf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Api.Tests;

public class ParserRegistrationTests
{
    private static IDocumentParserSelector BuildSelector(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddContextSmithApi(configuration);
        return services.BuildServiceProvider().GetRequiredService<IDocumentParserSelector>();
    }

    [Fact]
    public void Pdf_parser_is_registered_when_a_Document_Intelligence_endpoint_is_configured()
    {
        var selector = BuildSelector(new() { ["DocumentIntelligence:Endpoint"] = "https://example.cognitiveservices.azure.com/" });

        Assert.IsType<PdfDocumentParser>(selector.GetParser("handbook.pdf"));
    }

    [Fact]
    public void Pdf_parser_is_not_registered_without_a_Document_Intelligence_endpoint()
    {
        var selector = BuildSelector([]);

        Assert.Throws<NotSupportedException>(() => selector.GetParser("handbook.pdf"));
    }
}
