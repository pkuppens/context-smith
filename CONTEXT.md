# ContextSmith

ContextSmith prepares source documents into AI-ready context — parsing, chunking,
embedding, and retrieval — and exposes those capabilities to AI agents through
Tools, Resources, Prompts, and (explored, not yet implemented) Skills over the
Model Context Protocol.

## Language

**Skill**:
A reusable, dynamically-discovered package of agent instructions — plus optional
bundled scripts or resources — for a workflow. Uses progressive disclosure: only
a name and description are loaded up front; full content loads when relevant.
Can be authored locally, supplied by a user, or drawn from a shared library.
_Avoid_: Playbook, capability card, prompt

**Prompt** (MCP primitive):
A static, hand-curated, always-registered workflow instruction exposed by
ContextSmith's MCP server today (e.g. `prepare-document-for-rag`). Unlike a
Skill, a Prompt cannot bundle scripts and is not dynamically discovered — the
full set is fixed in code.
_Avoid_: Template, skill

**Skill Placement Layer**:
The architectural location responsible for a Skill's storage, discovery/matching,
and context injection. Candidate layers: the MCP server itself (a Resources-based
extension), the agent/client (native local loading), or a transport-agnostic
content/discovery layer decoupled from MCP entirely. Responsibility can split
across more than one layer.
_Avoid_: Skill layer, skill location
