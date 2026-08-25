using ContextSmith.Domain;

namespace ContextSmith.Application;

internal static class DocumentTextWalker
{
    public static IEnumerable<(string Text, Provenance Provenance, IReadOnlyList<string> HeadingPath)> WalkLeafText(
        DocumentNode node, IReadOnlyList<string> headingPath)
    {
        foreach (var child in node.Children)
        {
            switch (child)
            {
                case Heading heading:
                    yield return (heading.Text, heading.Provenance, headingPath);
                    break;
                case Paragraph paragraph:
                    yield return (paragraph.Text, paragraph.Provenance, headingPath);
                    break;
                case ListBlock list:
                    foreach (var item in WalkLeafText(list, headingPath))
                    {
                        yield return item;
                    }

                    break;
                case TableBlock table:
                    yield return (FlattenTable(table), table.Provenance, headingPath);
                    break;
                case Section section:
                    var childPath = section.Title is null ? headingPath : [.. headingPath, section.Title];
                    foreach (var item in WalkLeafText(section, childPath))
                    {
                        yield return item;
                    }

                    break;
            }
        }
    }

    public static IEnumerable<string> OwnText(DocumentNode node)
    {
        foreach (var child in node.Children)
        {
            switch (child)
            {
                case Heading heading:
                    yield return heading.Text;
                    break;
                case Paragraph paragraph:
                    yield return paragraph.Text;
                    break;
                case ListBlock list:
                    foreach (var item in list.Children.OfType<Paragraph>())
                    {
                        yield return item.Text;
                    }

                    break;
                case TableBlock table:
                    yield return FlattenTable(table);
                    break;
            }
        }
    }

    private static string FlattenTable(TableBlock table)
        => string.Join(" | ", table.Rows.Select(row => string.Join(" | ", row)));
}
