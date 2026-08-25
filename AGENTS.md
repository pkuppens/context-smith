# AGENTS.md

Instructions for AI coding agents (Codex, Claude Code, and others) working in this repo.

## Writing style

Write in ASD-STE100 Simplified Technical English (STE): short sentences, one instruction per sentence, active voice, approved vocabulary. This applies to docs, comments, commit messages, issue/PR text, and any other prose an agent writes.

Terms or abbreviations outside STE (domain jargon, product names, acronyms) must be introduced with a definition in the domain docs — see `docs/agents/domain.md`.

## Agent skills

### Issue tracker

Issues and PRDs live as GitHub issues in `pkuppens/context-smith`, managed via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout — `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
