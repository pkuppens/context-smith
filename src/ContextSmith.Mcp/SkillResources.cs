using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

// Name is the skill's identity: per SEP-2640, the final segment of a skill's URI
// path MUST equal its name, which makes the name unique by construction. See
// ADR-0001 for why this project addresses skills this way instead of a local id.
public sealed record SkillDefinition(string Name, string Description, string Body)
{
    public string Uri => $"skill://{Name}/SKILL.md";
}

public static class SkillCatalog
{
    public static readonly SkillDefinition PrepareDocumentForRag = new(
        Name: "prepare-document-for-rag",
        Description: "Guide an agent through document preparation, chunk creation, and quality checks.",
        Body: """
            ---
            name: prepare-document-for-rag
            description: Guide an agent through document preparation, chunk creation, and quality checks.
            ---

            Prepare a document for retrieval-augmented generation.

            1. Call `prepare_document` to parse the document.
            2. Read the returned `contextsmith://documents/{documentId}/structure` resource and inspect
               the section and heading hierarchy.
            3. Choose a chunking strategy appropriate to that structure.
            4. Create chunks for indexing.
            5. Review the generated chunks for lost context (missing heading path, or text too short
               to be meaningful on its own) and fix any problems found.
            """);

    public static IReadOnlyList<SkillDefinition> All { get; } = [PrepareDocumentForRag];

    public static SkillDefinition? Find(string name) =>
        All.FirstOrDefault(skill => skill.Name == name);
}

public sealed record SkillCatalogEntry(string Name, string Description, string Uri);

[McpServerResourceType]
public sealed class SkillResources
{
    [McpServerResource(UriTemplate = "contextsmith://skills", Name = "Skill catalog")]
    [Description("List available skills by name, description, and URI only (progressive disclosure).")]
    public static ResourceContents GetCatalog()
    {
        var entries = SkillCatalog.All
            .Select(skill => new SkillCatalogEntry(skill.Name, skill.Description, skill.Uri))
            .ToList();

        return new TextResourceContents
        {
            Uri = "contextsmith://skills",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(entries),
        };
    }

    [McpServerResource(UriTemplate = "skill://{skillName}/SKILL.md", Name = "Skill content")]
    [Description("Return the full body of one skill, for use once the catalog has matched it to the task.")]
    public static ResourceContents GetSkill(
        [Description("The skill name, matching a name returned by the skill catalog.")] string skillName)
    {
        var skill = SkillCatalog.Find(skillName)
            ?? throw new McpException($"No skill is registered under name '{skillName}'.");

        return new TextResourceContents
        {
            Uri = skill.Uri,
            MimeType = "text/markdown",
            Text = skill.Body,
        };
    }
}
