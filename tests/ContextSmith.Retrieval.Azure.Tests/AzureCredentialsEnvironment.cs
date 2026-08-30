namespace ContextSmith.Retrieval.Azure.Tests;

// Establishes the skip-when-credentials-absent pattern this repo's Azure integration tests
// use: read a required environment variable and let [SkippableFact] + Skip.IfNot decide
// whether to run, so dotnet test reports Skipped rather than silently passing or failing.
public static class AzureCredentialsEnvironment
{
    public static string? OpenAiEndpoint => Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
    public static string? OpenAiApiKey => Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
    public static string? OpenAiEmbeddingDeployment => Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");

    public static string? SearchEndpoint => Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
    public static string? SearchApiKey => Environment.GetEnvironmentVariable("AZURE_SEARCH_API_KEY");

    public static bool OpenAiConfigured =>
        !string.IsNullOrWhiteSpace(OpenAiEndpoint) && !string.IsNullOrWhiteSpace(OpenAiEmbeddingDeployment);

    public static bool SearchConfigured =>
        !string.IsNullOrWhiteSpace(SearchEndpoint);
}
