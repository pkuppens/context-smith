using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ContextSmith.Api;

/// <summary>Calls a local Ollama server's <c>api/chat</c> endpoint to generate an answer.</summary>
/// <param name="httpClient">Client pointed at the Ollama base address.</param>
/// <param name="model">Ollama chat model name.</param>
public sealed class OllamaChatClient(HttpClient httpClient, string model = "nemotron-3.5-lightning")
{
    /// <summary>Sends <paramref name="systemPrompt"/> and <paramref name="userPrompt"/> to the model and returns its reply.</summary>
    /// <param name="systemPrompt">System instruction that frames the task.</param>
    /// <param name="userPrompt">User message, including any retrieved context.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>The model's reply text.</returns>
    /// <exception cref="InvalidOperationException">Ollama returned no chat response.</exception>
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
