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

`Directory.Build.props` enables `GenerateDocumentationFile` and `.editorconfig`
promotes `CS1591` to an error, so a missing doc comment on a public type or
member in `src/` fails the build. `pre-commit` (the `dotnet-build` hook) and CI
both enforce this.

Test projects (`tests/`) opt out of the `CS1591` build error through
`<NoWarn>$(NoWarn);CS1591</NoWarn>` in `Directory.Build.props`: xUnit types and
methods are all public, and a `<summary>` that restates a `Method_Scenario_Expected`
test name adds nothing. The opt-out is not a licence to skip readability. Shared
test infrastructure — fixtures, data builders, custom assertions, anything reused
across test classes — still gets a `<summary>` or at least a `//` comment on
intent. A test that encodes a non-obvious rule or pins a regression gets a short
`//` comment, or a reason string on `Skip` or `DisplayName`.

## Agent skills

### Issue tracker

Issues and PRDs live as GitHub issues in `pkuppens/context-smith`, managed via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout — `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
