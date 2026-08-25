namespace ContextSmith.Api;

public sealed record ChatResponse(string Answer, IReadOnlyList<ChatSource> Sources);

public sealed record ChatSource(IReadOnlyList<string> HeadingPath, string Text);
