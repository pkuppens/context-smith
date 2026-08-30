using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

public sealed record SkillDefinition(string Id, string Name, string Description, string Body);

public static class SkillCatalog
{
    public static readonly SkillDefinition PrepareDocumentForRag = new(
        Id: "prepare-document-for-rag",
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

    public static SkillDefinition? Find(string skillId) =>
        All.FirstOrDefault(skill => skill.Id == skillId);
}

public sealed record SkillCatalogEntry(string Id, string Name, string Description);

[McpServerResourceType]
public sealed class SkillResources
{
    [McpServerResource(UriTemplate = "contextsmith://skills", Name = "Skill catalog")]
    [Description("List available skills by id, name, and description only (progressive disclosure).")]
    public static ResourceContents GetCatalog()
    {
        var entries = SkillCatalog.All
            .Select(skill => new SkillCatalogEntry(skill.Id, skill.Name, skill.Description))
            .ToList();

        return new TextResourceContents
        {
            Uri = "contextsmith://skills",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(entries),
        };
    }

    [McpServerResource(UriTemplate = "contextsmith://skills/{skillId}", Name = "Skill content")]
    [Description("Return the full body of one skill, for use once the catalog has matched it to the task.")]
    public static ResourceContents GetSkill(
        [Description("The id of a skill returned by the skill catalog.")] string skillId)
    {
        var skill = SkillCatalog.Find(skillId)
            ?? throw new McpException($"No skill is registered under id '{skillId}'.");

        return new TextResourceContents
        {
            Uri = $"contextsmith://skills/{skillId}",
            MimeType = "text/markdown",
            Text = skill.Body,
        };
    }
}
