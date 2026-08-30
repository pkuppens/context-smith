namespace ContextSmith.Retrieval.Azure;

// The Azure AI Search vector field is created once, up front, and its dimension cannot
// change afterward. Resolve() picks that dimension from explicit configuration first,
// falling back to a lookup by deployment/model name for the well-known Azure OpenAI
// embedding models, so an operator does not have to know the number by heart.
public static class EmbeddingDimensionResolver
{
    private static readonly IReadOnlyDictionary<string, int> KnownModelDimensions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["text-embedding-3-small"] = 1536,
        ["text-embedding-3-large"] = 3072,
        ["text-embedding-ada-002"] = 1536,
    };

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
