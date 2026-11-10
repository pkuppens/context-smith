using ContextSmith.Persistence.Postgres;

namespace ContextSmith.Persistence.Postgres.Tests;

public class PostgresSqlTests
{
    [Fact]
    public void FormatVector_writes_the_pgvector_text_literal_with_invariant_culture()
    {
        // pgvector parses "[1,0.5,-2.25]". A comma decimal separator (nl-NL) would corrupt it.
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
        try
        {
            Assert.Equal("[1,0.5,-2.25]", PostgresSql.FormatVector([1f, 0.5f, -2.25f]));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void CreateSchemaScript_enables_pgvector_and_sizes_the_vector_column_from_the_dimension()
    {
        var script = PostgresSql.CreateSchemaScript(768);

        Assert.Contains("CREATE EXTENSION IF NOT EXISTS vector", script);
        Assert.Contains("embedding vector(768)", script);
        Assert.Contains("CREATE TABLE IF NOT EXISTS documents", script);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateSchemaScript_rejects_a_non_positive_dimension(int dimension)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PostgresSql.CreateSchemaScript(dimension));
    }
}
