# Agent Skills: Landscape and Recommendation

This document explores where ContextSmith's MCP server should support Skills — a
fourth capability alongside Tools, Resources, and Prompts. It compares the
candidate placement layers, states where the PoC in this repo sits, and gives a
recommendation for follow-up work. See #21 for the issue that requested this
exploration.

For term definitions (Skill, Prompt, Skill Placement Layer), see `CONTEXT.md` at
the repo root.

## Summary

- ContextSmith should serve Skills over MCP using **layer 1: MCP-server-native,
  Resources-based** — the direction the industry working group has converged on
  as **SEP-2640**.
- ContextSmith should **not** build its own addressing scheme for skills. The
  PoC in this repo (`contextsmith://skills/{skillId}`) used a project-local,
  bare-id scheme for speed. That choice is now superseded: this repo adopts
  SEP-2640's full-URI addressing (`skill://<skill-path>/SKILL.md`) instead of
  maintaining a local variant.
- This matters because a bare id is ambiguous once skills come from more than
  one source, and a review comment on the PoC (PR #22) caught this. SEP-2640
  avoids the problem structurally, by construction, rather than by validation
  code this project would otherwise have to write and maintain.
- No other placement layer (agent/client-native, transport-agnostic content
  format) is recommended for build work in this repo at this time.
- Follow-up work — migrating the PoC's URI scheme to match SEP-2640 — is
  tracked in #23.

## Responsibilities: Tools, Resources, Prompts, Skills, MCP, Agent, LLM

Each part of the system has one job. The diagram shows who owns what, and where
a Skill would sit relative to the primitives ContextSmith already exposes.

```
+----------------------------------------------------------------+
| LLM                                                             |
| Generates text and tool calls from the context it is given.     |
| Has no direct access to the MCP server or the file system.      |
+----------------------------------------------------------------+
                              ^  context (prompts, resource text, tool results)
                              |  requests (tool calls)
+----------------------------------------------------------------+
| Agent / MCP client (e.g. Claude Code)                           |
| Decides what to fetch, when to fetch it, and what to inject     |
| into the LLM's context. Owns progressive disclosure: it may     |
| load a Skill's short description now and its full body later.   |
+----------------------------------------------------------------+
                              ^  MCP protocol calls (list, read, call, get)
                              |  MCP protocol results
+----------------------------------------------------------------+
| MCP server (ContextSmith.Mcp)                                   |
|                                                                  |
|  Tools      - actions the agent can invoke (e.g.                |
|               prepare_document). Owned by the server; changes    |
|               state or does work.                                |
|                                                                  |
|  Resources  - addressable, read-only data (e.g.                  |
|               contextsmith://documents/{id}/structure). Owned    |
|               by the server; describes state.                   |
|                                                                  |
|  Prompts    - static, hand-curated workflow instructions,        |
|               always registered, not dynamically discovered      |
|               (e.g. prepare-document-for-rag).                   |
|                                                                  |
|  Skills     - dynamically-discovered workflow instructions,      |
|               with optional bundled scripts/resources, using     |
|               progressive disclosure (name+description first,    |
|               full body on demand). Not yet implemented; this    |
|               PoC adds one placement layer's worth of support.   |
+----------------------------------------------------------------+
```

A Skill overlaps with a Prompt in purpose — both teach the agent a workflow —
but differs in two ways: a Skill is discovered dynamically (the agent sees a
name and description before deciding to load the rest) and a Skill can bundle
scripts or other resources alongside its instructions. A Prompt is fixed in
code and always fully present. See the `Skill` / `Prompt` distinction in
`CONTEXT.md` for the canonical definitions.

## Placement layers

A Skill needs three things done somewhere: **storage** (where the skill's
files live), **discovery / matching** (how the agent learns a skill exists and
decides it is relevant), and **context injection** (how the skill's content
enters the LLM's context). Three candidate layers split this responsibility
differently.

| Layer | Storage | Discovery / matching | Context injection |
|---|---|---|---|
| 1. MCP-server-native (SEP-2640, Resources-based) | On the MCP server, as files under a skill directory (e.g. `skill://<name>/SKILL.md`) | Server implements `skills/list` (catalog: name + description) and `skills/get` (single entry by URI); agent matches user intent against the catalog | Agent reads the matched skill's `SKILL.md` resource via `resources/read` and injects its content as context, same as any other resource |
| 2. Agent/client-native (local loading, no MCP protocol change) | On the user's machine or in the agent's own skill directory (e.g. `.claude/skills/`), independent of any MCP server | Agent's own skill-loading machinery scans local directories and matches on frontmatter, entirely outside MCP | Agent injects the skill's content directly; MCP is not involved at all |
| 3. Transport-agnostic content-format layer (agentskills.io-style) | Anywhere — a Git repo, a package registry, a plain URL — as a directory following the Agent Skills content format | A separate discovery mechanism (a registry, an index, a well-known URL) resolves a skill name to its location, independent of MCP or any one agent | Whichever host loaded the skill (an MCP server, an agent, a CLI) injects its content; this layer only standardizes the format and how it is found |

Layer 1 is what SEP-2640 defines: it reuses the existing MCP Resources
primitive (no new protocol methods for reading content, only `skills/list` and
`skills/get` for cataloging) rather than adding Skills as a distinct MCP
primitive. An earlier proposal, SEP-2076, took the alternative path of adding
Skills as a first-class primitive with dedicated `skills/list` / `skills/get`
protocol methods and a `skills` server capability; the working group closed it
in favor of the Resources-based convention in SEP-2640, judging that
convention could prove the pattern without a protocol extension. Layer 2 needs
no MCP involvement — this is how Claude Code and similar agents already load
locally-authored skills today. Layer 3 exists independent of MCP entirely: it
standardizes the skill content format and how a name resolves to a location,
so both layer 1 and layer 2 implementations can point at the same skill
without duplicating it.

These layers are not mutually exclusive. A skill could be authored once
against the layer-3 content format, then served by a layer-1 MCP server AND
loaded directly by a layer-2 agent from the same source directory.

## Skill sources

| Source | Description | Compatible layer(s) |
|---|---|---|
| Bundled with the server | Shipped inside the MCP server's own codebase, versioned with it (this PoC's sample skill) | 1 |
| User-provided | Authored or dropped in by the person running the agent, local to their machine | 2 (natively), or 1 if the server is configured to read from a user-supplied directory |
| Shared library | Published independently (a registry, a Git repo, a package) and referenced by name or URL | 3 (the layer this source depends on to be found and versioned at all); layers 1 and 2 can then fetch from it |

## PoC: `SkillResources.cs`

`src/ContextSmith.Mcp/SkillResources.cs` implements the layer-1 direction,
mirroring the existing `[McpServerResourceType]` pattern in
`DocumentResources.cs`:

- `contextsmith://skills` — catalog resource. Returns name and description
  only, for cheap progressive disclosure, for every bundled skill.
- `contextsmith://skills/{skillId}` — content resource. Returns one skill's
  full body by id.

The PoC serves one hardcoded sample skill, reusing the `prepare-document-for-rag`
prompt content as the pilot body. It anticipates the SEP-2640 direction
(Resources-based, catalog separate from content) without guaranteeing wire
compatibility with the SEP while it is still in Draft status — see Out of
Scope in #21.

The PoC addresses a skill by a bare `skillId` (`contextsmith://skills/{skillId}`)
rather than SEP-2640's full-URI addressing (`skill://<skill-path>/SKILL.md`), where
uniqueness is structural. The catalog validates id uniqueness at construction time as
a stopgap. See [Conclusion](#addressing-adopt-sep-2640s-scheme-dont-invent-one) — this
project has decided to migrate to SEP-2640's addressing rather than maintain the local
scheme; tracked in #23.

### Manual verification

Verified against the MCP Inspector (`npx @modelcontextprotocol/inspector`)
pointed at the local `ContextSmith.Mcp` server:

1. `resources/list` includes `contextsmith://skills` alongside the existing
   document-structure resources.
2. Reading `contextsmith://skills` returns the catalog: the sample skill's id,
   name, and description, with no full body.
3. Reading `contextsmith://skills/prepare-document-for-rag` returns the full
   skill body.

## Conclusion

Pursue **layer 1 (MCP-server-native, SEP-2640-aligned)** for ContextSmith's own
bundled skills — it needs no new client behavior beyond what MCP clients
already support for Resources, and this PoC shows it fits the existing
Resources pattern in `ContextSmith.Mcp` cleanly.

Do not build layer 2 or layer 3 support in this repo. Layer 2 is the
consuming agent's responsibility, not the server's — ContextSmith should not
try to author agent-side skill loading. Layer 3 is a distribution concern that
only matters once ContextSmith has more than one bundled skill and a reason to
share it outside this repo; revisit if that need appears, or once SEP-2640
finalizes and a reference client (e.g. Claude Code) supports reading
`skill://`-scheme resources, whichever comes first.

Responsibility should not split across layers for this repo's own bundled
skills — layer 1 alone covers storage, discovery, and injection for content
ContextSmith owns. Splitting only becomes useful once a shared-library source
(layer 3) is in play, which is out of scope here.

### Addressing: adopt SEP-2640's scheme, don't invent one

The PoC's `contextsmith://skills/{skillId}` addressing was a project-local
shortcut, chosen to get a working end-to-end resource pair quickly. A PR #22
review comment identified the cost of that shortcut: a bare id is ambiguous
the moment two skills share one, and the fix would have been project-specific
validation code to maintain.

SEP-2640 already solves this, by construction, with full-URI addressing:
`skill://<skill-path>/SKILL.md`, where `<skill-path>` ends in the skill's
`name` and any preceding segments are a server-chosen organizational prefix.
Two skills cannot collide under this scheme without also colliding in name and
prefix, which is a different, and already-necessary, uniqueness requirement —
not one this project has to invent or validate separately.

The decision: **adopt SEP-2640's addressing rather than maintain a local
alternative.** ContextSmith gains nothing by diverging from the public
standard here, and a divergent scheme is a cost the project would carry alone
— every future client integration, every future skill source, and every
future contributor reading this code would need to learn ContextSmith's
scheme instead of the one the wider ecosystem already uses. Migrating the PoC
to `skill://` addressing is tracked in #23.

## Follow or contribute

- [SEP-2076 — Agent Skills as a First-Class MCP Primitive](https://github.com/modelcontextprotocol/modelcontextprotocol/pull/2076) (closed in favor of SEP-2640)
- [SEP-2640 — Skills Extension (Resources-based, current WG direction)](https://github.com/modelcontextprotocol/modelcontextprotocol/pull/2640)
- [Skills Over MCP Working Group charter](https://modelcontextprotocol.io/community/working-groups/skills-over-mcp)
- [experimental-ext-skills reference implementation](https://github.com/modelcontextprotocol/experimental-ext-skills)
- [Skills Over MCP WG project board](https://github.com/orgs/modelcontextprotocol/projects/38/views/1)
- [agentskills.io — content format + discovery spec](https://agentskills.io/)
- Weekly Working Session: Tuesdays, meetings published at [meet.modelcontextprotocol.io](https://meet.modelcontextprotocol.io)
- Discord: `#skills-over-mcp-wg`
- [Meeting notes](https://github.com/modelcontextprotocol/modelcontextprotocol/discussions/categories/meeting-notes-skills-over-mcp-wg)
