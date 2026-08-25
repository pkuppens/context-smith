using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ContextSmith.Api;

public sealed class OllamaChatClient(HttpClient httpClient, string model = "nemotron-3.5-lightning")
{
    public async Task<string> AskAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest(
            model,
            [
                new OllamaChatMessage("system", systemPrompt),
                new OllamaChatMessage("user", userPrompt),
            ],
            Stream: false);

        var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaChatResponse>(cancellationToken)
            .ConfigureAwait(false);

        return result?.Message.Content
            ?? throw new InvalidOperationException("Ollama returned no chat response.");
    }

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OllamaChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaChatMessage Message);
}
