namespace ContextSmith.Api;

/// <summary>Response body for the chat endpoint.</summary>
/// <param name="Answer">Generated answer text.</param>
/// <param name="Sources">Document excerpts the answer was grounded on.</param>
public sealed record ChatResponse(string Answer, IReadOnlyList<ChatSource> Sources);

/// <summary>One document excerpt cited in a <see cref="ChatResponse"/>.</summary>
/// <param name="HeadingPath">Titles of the enclosing sections, from the outermost to the innermost.</param>
/// <param name="Text">Excerpt text.</param>
public sealed record ChatSource(IReadOnlyList<string> HeadingPath, string Text);
