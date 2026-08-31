namespace ContextSmith.Application;

/// <summary>Compact overview of a parsed document: node counts plus its outline tree.</summary>
/// <param name="DocumentId">Identifier of the summarised document.</param>
/// <param name="SectionCount">Number of section nodes in the document.</param>
/// <param name="HeadingCount">Number of heading nodes in the document.</param>
/// <param name="ParagraphCount">Number of paragraph nodes in the document.</param>
/// <param name="Outline">Root of the document outline tree.</param>
public sealed record DocumentStructureSummary(
    string DocumentId,
    int SectionCount,
    int HeadingCount,
    int ParagraphCount,
    DocumentOutlineNode Outline);
