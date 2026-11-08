namespace ContextSmith.Documents.Pdf;

/// <summary>Reading-order layout of a PDF, reduced to the parts the parser maps.</summary>
/// <param name="Elements">Paragraphs and tables in reading order.</param>
public sealed record PdfLayout(IReadOnlyList<PdfElement> Elements);

/// <summary>One layout element: a paragraph or a table.</summary>
/// <param name="PageNumber">One-based page the element starts on.</param>
public abstract record PdfElement(int PageNumber);

/// <summary>A paragraph of text with its Document Intelligence role.</summary>
/// <param name="Text">Paragraph text.</param>
/// <param name="Role">Role such as <c>title</c>, <c>sectionHeading</c>, <c>pageHeader</c>, or <see langword="null"/> for body text.</param>
/// <param name="PageNumber">One-based page number.</param>
public sealed record PdfParagraph(string Text, string? Role, int PageNumber) : PdfElement(PageNumber);

/// <summary>A table, with cell text row by row.</summary>
/// <param name="Rows">Rows of cell text, left to right.</param>
/// <param name="PageNumber">One-based page number.</param>
public sealed record PdfTable(IReadOnlyList<IReadOnlyList<string>> Rows, int PageNumber) : PdfElement(PageNumber);
