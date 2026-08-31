namespace ContextSmith.Retrieval.Azure;

/// <summary>
/// Resolves the vector dimension for the Azure AI Search index. The vector field is created once
/// and its dimension is then fixed, so this picks the value from explicit configuration first and
/// falls back to a lookup by well-known Azure OpenAI embedding model name.
/// </summary>
public static class EmbeddingDimensionResolver
{
    private static readonly IReadOnlyDictionary<string, int> KnownModelDimensions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["text-embedding-3-small"] = 1536,
        ["text-embedding-3-large"] = 3072,
        ["text-embedding-ada-002"] = 1536,
    };

    /// <summary>Returns <paramref name="configuredDimension"/> when set; otherwise looks the dimension up by model name.</summary>
    /// <param name="configuredDimension">Explicit dimension from configuration, or <see langword="null"/>.</param>
    /// <param name="embeddingDeploymentName">Azure OpenAI deployment or model name used for the fallback lookup.</param>
    /// <returns>The vector dimension to create the index with.</returns>
    /// <exception cref="InvalidOperationException">No dimension is configured and the model name is not well known.</exception>
    public static int Resolve(int? configuredDimension, string embeddingDeploymentName)
    {
        if (configuredDimension is int dimension)
        {
            return dimension;
        }

        if (KnownModelDimensions.TryGetValue(embeddingDeploymentName, out var knownDimension))
        {
            return knownDimension;
        }

        throw new InvalidOperationException(
            $"Cannot resolve the vector dimension for embedding deployment '{embeddingDeploymentName}'. " +
            "Set AzureSearch:VectorDimension explicitly, or use a well-known model name.");
    }
}
