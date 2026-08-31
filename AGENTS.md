# AGENTS.md

Instructions for AI coding agents (Codex, Claude Code, and others) working in this repo.

## Writing style

Write in ASD-STE100 Simplified Technical English (STE): short sentences, one instruction per sentence, active voice, approved vocabulary. This applies to docs, comments, commit messages, issue/PR text, and any other prose an agent writes.

Terms or abbreviations outside STE (domain jargon, product names, acronyms) must be introduced with a definition in the domain docs — see `docs/agents/domain.md`.

## C# code style

Follow `.editorconfig`. Give every public type and member an XML doc comment
(`<summary>` at minimum; add `<param>`, `<returns>`, and `<exception>` where
they add information beyond the name). On an interface implementation, use
`<inheritdoc/>` instead of repeating the interface's doc comment, so the two
cannot drift.

`Directory.Build.props` enables `GenerateDocumentationFile`, so a missing
doc comment on a public member in `src/` shows up as a `CS1591` build
warning. Test projects (`tests/`) are exempt. The existing gap and the plan
to promote `CS1591` to a build error are tracked in
[#26](https://github.com/pkuppens/context-smith/issues/26).

## Agent skills

### Issue tracker

Issues and PRDs live as GitHub issues in `pkuppens/context-smith`, managed via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout — `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
