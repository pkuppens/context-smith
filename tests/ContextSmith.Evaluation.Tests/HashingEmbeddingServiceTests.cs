using ContextSmith.Evaluation;

namespace ContextSmith.Evaluation.Tests;

public class HashingEmbeddingServiceTests
{
    private static float Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
    }

    [Fact]
    public async Task EmbedAsync_is_deterministic()
    {
        var service = new HashingEmbeddingService();

        Assert.Equal(await service.EmbedAsync("parental leave rules"), await service.EmbedAsync("parental leave rules"));
    }

    [Fact]
    public async Task EmbedAsync_places_texts_that_share_words_closer_than_unrelated_texts()
    {
        var service = new HashingEmbeddingService();
        var query = await service.EmbedAsync("parental leave");
        var related = await service.EmbedAsync("Parental leave starts after twelve months.");
        var unrelated = await service.EmbedAsync("Office parking is first come first served.");

        Assert.True(Cosine(query, related) > Cosine(query, unrelated));
    }
}
