using ContextSmith.Application;
using ContextSmith.Persistence.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContextSmith.Api.Tests;

public class StorageRegistrationTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddContextSmithApi(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Storage_provider_Postgres_selects_the_PostgreSQL_document_store()
    {
        using var provider = Build(new()
        {
            ["Storage:Provider"] = "Postgres",
            ["Postgres:ConnectionString"] = "Host=localhost;Database=contextsmith",
        });

        Assert.IsType<PostgresDocumentStore>(provider.GetRequiredService<IDocumentStore>());
    }

    [Fact]
    public void Retrieval_follows_Storage_provider_Postgres_and_selects_the_PostgreSQL_retrieval_service()
    {
        using var provider = Build(new()
        {
            ["Storage:Provider"] = "Postgres",
            ["Postgres:ConnectionString"] = "Host=localhost;Database=contextsmith",
        });

        Assert.IsType<PostgresRetrievalService>(provider.GetRequiredService<DocumentRetrievalRegistry>().GetOrCreate("doc-1"));
    }

    [Fact]
    public void Storage_provider_Postgres_without_a_connection_string_fails_with_a_clear_message()
    {
        using var provider = Build(new() { ["Storage:Provider"] = "Postgres" });

        var error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IDocumentStore>());
        Assert.Contains("Postgres:ConnectionString", error.Message);
    }
}
