namespace ContextSmith.Api;

/// <summary>Request body for the chat endpoint.</summary>
/// <param name="DocumentId">Identifier of the prepared document to ask about.</param>
/// <param name="Message">User question.</param>
public sealed record ChatRequest(string DocumentId, string Message);
