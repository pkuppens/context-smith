namespace ContextSmith.Retrieval.Azure.Tests;

public class EmbeddingDimensionResolverTests
{
    [Fact]
    public void Resolve_prefers_the_configured_dimension_over_the_model_lookup()
    {
        var dimension = EmbeddingDimensionResolver.Resolve(configuredDimension: 42, "text-embedding-3-small");

        Assert.Equal(42, dimension);
    }

    [Theory]
    [InlineData("text-embedding-3-small", 1536)]
    [InlineData("text-embedding-3-large", 3072)]
    [InlineData("text-embedding-ada-002", 1536)]
    public void Resolve_falls_back_to_the_known_dimension_for_well_known_models(string model, int expectedDimension)
    {
        var dimension = EmbeddingDimensionResolver.Resolve(configuredDimension: null, model);

        Assert.Equal(expectedDimension, dimension);
    }

    [Fact]
    public void Resolve_throws_for_an_unknown_model_with_no_configured_dimension()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EmbeddingDimensionResolver.Resolve(configuredDimension: null, "some-custom-deployment"));
    }
}
