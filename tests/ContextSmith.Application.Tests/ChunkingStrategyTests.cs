using ContextSmith.Domain;

namespace ContextSmith.Application.Tests;

public class ChunkingStrategyTests
{
    [Fact]
    public void StructureAwareChunker_produces_one_chunk_per_section_with_heading_path()
    {
        var document = BuildHandbookFixture();

        var chunks = new StructureAwareChunker().Chunk(document);

        Assert.Equal(4, chunks.Count);
        Assert.Equal(["Employee Handbook"], chunks[0].HeadingPath);
        Assert.Contains("Welcome to the handbook.", chunks[0].Text);

        Assert.Equal(["Employee Handbook", "Leave Policy"], chunks[1].HeadingPath);
        Assert.Equal(["Employee Handbook", "Leave Policy", "Parental Leave"], chunks[2].HeadingPath);
        Assert.Contains("Eligibility requires continuous service.", chunks[2].Text);
        Assert.Contains("Apply at least four weeks in advance.", chunks[2].Text);

        Assert.Equal(["Employee Handbook", "Benefits"], chunks[3].HeadingPath);
    }

    [Fact]
    public void FixedSizeChunker_packs_leaf_text_by_character_budget_ignoring_structure()
    {
        var document = BuildHandbookFixture();

        var smallBudgetChunks = new FixedSizeChunker(maxCharacters: 40).Chunk(document);
        var largeBudgetChunks = new FixedSizeChunker(maxCharacters: 10_000).Chunk(document);

        Assert.True(smallBudgetChunks.Count > largeBudgetChunks.Count);
        Assert.Single(largeBudgetChunks);

        var reassembled = string.Join(" ", smallBudgetChunks.Select(chunk => chunk.Text));
        Assert.Equal(largeBudgetChunks[0].Text, reassembled);
    }

    private static Document BuildHandbookFixture()
    {
        static Provenance P(string location) => new("handbook.md", location);

        var parentalLeave = new Section
        {
            Title = "Parental Leave",
            Provenance = P("parental-leave"),
            Children =
            [
                new Heading { Text = "Parental Leave", Level = 3, Provenance = P("h3") },
                new Domain.Paragraph { Text = "Employees qualify after twelve months of employment.", Provenance = P("p1") },
                new ListBlock
                {
                    Ordered = false,
                    Provenance = P("list"),
                    Children =
                    [
                        new Domain.Paragraph { Text = "Eligibility requires continuous service.", Provenance = P("li1") },
                        new Domain.Paragraph { Text = "Apply at least four weeks in advance.", Provenance = P("li2") },
                    ],
                },
            ],
        };

        var leavePolicy = new Section
        {
            Title = "Leave Policy",
            Provenance = P("leave-policy"),
            Children =
            [
                new Heading { Text = "Leave Policy", Level = 2, Provenance = P("h2-leave") },
                new Domain.Paragraph { Text = "General leave rules apply.", Provenance = P("p2") },
                parentalLeave,
            ],
        };

        var benefits = new Section
        {
            Title = "Benefits",
            Provenance = P("benefits"),
            Children =
            [
                new Heading { Text = "Benefits", Level = 2, Provenance = P("h2-benefits") },
                new Domain.Paragraph { Text = "Benefits are described here.", Provenance = P("p3") },
            ],
        };

        var handbook = new Section
        {
            Title = "Employee Handbook",
            Provenance = P("root"),
            Children =
            [
                new Heading { Text = "Employee Handbook", Level = 1, Provenance = P("h1") },
                new Domain.Paragraph { Text = "Welcome to the handbook.", Provenance = P("p0") },
                leavePolicy,
                benefits,
            ],
        };

        return new Document
        {
            Metadata = new DocumentMetadata { Title = "Employee Handbook" },
            Provenance = P("document"),
            Children = [handbook],
        };
    }
}
