---
status: accepted
---

# Address MCP Skills by SEP-2640's full skill:// URI, not a project-local id

When we added a Skills PoC to `ContextSmith.Mcp` (#21, PR #22), we addressed a
skill by a bare id under our own scheme: `contextsmith://skills/{skillId}`. A
review on PR #22 pointed out this is ambiguous — two skills sharing an id would
silently resolve to the wrong one — and asked whether that belongs to a general
skill-finder mechanism rather than being solved ad hoc in the PoC (tracked in
#23).

We decided to drop the local scheme and adopt SEP-2640's addressing instead:
`skill://<skill-path>/SKILL.md`, where `<skill-path>` ends in the skill's
`name` and any preceding segments are a server-chosen organizational prefix.
Uniqueness holds by construction — a skill's name and prefix must already be
unique for the URI to be well-formed — so no separate id-uniqueness policy is
needed. We are the party without a real trade-off to defend here: a project-local
scheme this small project would have to design and maintain, against a public
standard the working group has already converged on. There's nothing
ContextSmith needs from skill addressing that SEP-2640 doesn't already give
us, so divergence would be pure cost — for every future client integration,
skill source, or contributor who'd otherwise need to learn the standard
scheme anyway.

We considered whether serving `skill://` resources risks collision with other
MCP servers that also use it — since the scheme is meant to be shared across
the ecosystem rather than server-specific like `contextsmith://`. It does not:
an MCP client tracks resources per server connection, not in one global URI
namespace merged across servers, so identical `skill://` URIs on two different
servers never collide in a client's eyes. SEP-2640 also reserves the first
`skill-path` segment as a server-chosen organizational prefix for exactly this
reason, while explicitly ruling out DNS-style resolution of it.

See `docs/AGENT-SKILLS.md` for the full landscape and placement-layer
analysis. Migrating `SkillResources.cs` off `contextsmith://skills/{skillId}`
and onto `skill://` is tracked in #23.
