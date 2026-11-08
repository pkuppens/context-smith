using ContextSmith.Application;
using ContextSmith.Persistence.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Mcp.Tests;

public class StorageRegistrationTests
{
    [Fact]
    public void Storage_provider_Postgres_selects_the_PostgreSQL_document_store()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "Postgres",
            ["Postgres:ConnectionString"] = "Host=localhost;Database=contextsmith",
        }).Build();
        var services = new ServiceCollection();
        services.AddContextSmithApplication(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<PostgresDocumentStore>(provider.GetRequiredService<IDocumentStore>());
    }
}
