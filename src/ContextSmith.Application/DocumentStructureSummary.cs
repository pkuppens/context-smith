namespace ContextSmith.Application;

public sealed record DocumentStructureSummary(
    string DocumentId,
    int SectionCount,
    int HeadingCount,
    int ParagraphCount,
    DocumentOutlineNode Outline);
