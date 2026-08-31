using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

/// <summary>One agent skill: its name, a one-line description, and the full <c>SKILL.md</c> body.</summary>
/// <remarks>
/// Name is the skill's identity: per SEP-2640, the final segment of a skill's URI path must equal
/// its name, which makes the name unique by construction. See ADR-0001 for why this project
/// addresses skills this way instead of a local id.
/// </remarks>
/// <param name="Name">Skill name. Also the addressable identity.</param>
/// <param name="Description">One-line summary used for progressive disclosure in the catalog.</param>
/// <param name="Body">Full skill content, including its front matter.</param>
public sealed record SkillDefinition(string Name, string Description, string Body)
{
    /// <summary>Canonical resource URI for this skill's body, of the form <c>skill://{Name}/SKILL.md</c>.</summary>
    public string Uri => $"skill://{Name}/SKILL.md";
}

/// <summary>The built-in <see cref="SkillDefinition"/> values that the MCP server publishes.</summary>
public static class SkillCatalog
{
    /// <summary>Skill that walks an agent through document preparation, chunk creation, and quality checks.</summary>
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

    /// <summary>All published skills, in a stable order.</summary>
    public static IReadOnlyList<SkillDefinition> All { get; } = [PrepareDocumentForRag];

    /// <summary>Returns the skill named <paramref name="name"/>, or <see langword="null"/> when none matches.</summary>
    /// <param name="name">Skill name to look up.</param>
    public static SkillDefinition? Find(string name) =>
        All.FirstOrDefault(skill => skill.Name == name);
}

/// <summary>Catalog row for one skill: name, description, and URI only, without the body.</summary>
/// <param name="Name">Skill name.</param>
/// <param name="Description">One-line summary.</param>
/// <param name="Uri">Resource URI for the skill's full body.</param>
public sealed record SkillCatalogEntry(string Name, string Description, string Uri);

/// <summary>Exposes the <see cref="SkillCatalog"/> as MCP resources, following progressive disclosure.</summary>
[McpServerResourceType]
public sealed class SkillResources
{
    /// <summary>Returns the skill catalog as JSON: one row per skill with name, description, and URI.</summary>
    /// <returns>The catalog as a JSON text resource.</returns>
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

    /// <summary>Returns the full <c>SKILL.md</c> body of the skill named <paramref name="skillName"/>.</summary>
    /// <param name="skillName">Skill name, matching a name returned by the skill catalog.</param>
    /// <returns>The skill body as a Markdown text resource.</returns>
    /// <exception cref="McpException">No skill is registered under <paramref name="skillName"/>.</exception>
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
