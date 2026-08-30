using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace ContextSmith.Mcp.Tests;

public class SkillResourcesTests
{
    [Fact]
    public void GetCatalog_lists_skills_by_id_name_and_description_only()
    {
        var contents = Assert.IsType<TextResourceContents>(SkillResources.GetCatalog());

        Assert.Equal("contextsmith://skills", contents.Uri);
        var entries = JsonSerializer.Deserialize<List<SkillCatalogEntry>>(contents.Text);

        var entry = Assert.Single(entries!);
        Assert.Equal("prepare-document-for-rag", entry.Id);
        Assert.Equal("prepare-document-for-rag", entry.Name);
        Assert.False(string.IsNullOrWhiteSpace(entry.Description));
    }

    [Fact]
    public void GetSkill_returns_the_full_body_for_a_known_id()
    {
        var contents = Assert.IsType<TextResourceContents>(SkillResources.GetSkill("prepare-document-for-rag"));

        Assert.Equal("contextsmith://skills/prepare-document-for-rag", contents.Uri);
        Assert.Contains("prepare_document", contents.Text);
    }

    [Fact]
    public void GetSkill_throws_for_an_unknown_id()
    {
        Assert.Throws<McpException>(() => SkillResources.GetSkill("does-not-exist"));
    }
}
