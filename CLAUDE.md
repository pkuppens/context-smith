# CLAUDE.md

Claude-specific guidance for this repo. Shared agent conventions (issue tracker, domain docs) live in [AGENTS.md](./AGENTS.md) — read that first.

## Test-driven development (trial)

Status: **on trial** since 2026-11. Decision date: after the next three feature PRs.

Use the `tdd` skill: agree the public seams first, then one failing test, one minimal
implementation, repeat. The seams used so far:

| Area | Seam |
| --- | --- |
| Document parsers | `IDocumentParser.ParseAsync` with fixtures built in code (Open XML SDK) or a fake gateway for Azure |
| Metrics and mappers | The pure static function, with expected values worked out by hand |
| Provider selection | `AddContextSmithApi` / `AddContextSmithApplication` resolved from in-memory configuration |
| Database stores | `IDocumentStore` and `IRetrievalService` against a real container, skipped when absent |
| HTTP host | The `/health` endpoint on a free port |

What worked (PDF, Excel, PowerPoint, evaluation metrics, host registration, HTTP host):

- The red test named the next type to write, so the interfaces stayed small.
- Hand-worked expected values (nDCG, recall) caught nothing wrong, but they could not
  pass by construction.
- A failing test for a bug found along the way (`TableBlock` missing from the JSON
  types) gave a ready regression test.

What did not work, so treat as a rule:

- Tests written *after* the code (Postgres integration tests) still found a real bug
  (`jsonb` reorders keys and breaks the `$type` discriminator). Run integration tests
  against a real container early, not last.
- Do not assert a behavior you have not read in the code. One evaluation test assumed
  `FixedSizeChunker` splits long leaves. It does not. Read the implementation first.
- Strict one-test-at-a-time slows a pure wiring change. For registration, write the
  test, then wire both hosts together.

**Dropping TDD:** if three feature PRs in a row need rework of the tests because the
seams moved, or the red step stops finding mistakes, stop requiring it. Record the
reason here and keep tests-with-the-change as the rule.

## Working notes

- **Before every commit** run `dotnet format ContextSmith.slnx`. The `dotnet format
  (verify)` hook fails on hand-written code that is not formatted.
- The `mixed-line-ending` hook rewrites files and fails once. Run `git add -A` and commit
  again. The second run passes.
- `detect-secrets` flags throwaway passwords in CI files. Add
  `# pragma: allowlist secret` on the line. Never do this for a real secret.
- Write C# files with the editor tool, not shell heredocs. Heredocs that contain quotes
  and braces can fail in the Windows shell without writing anything.
- Stack PRs one feature per PR, each based on the previous branch. Merge in order. GitHub
  retargets the next PR to `main` when its base merges.
- Optional services are skipped, not failed: a `[SkippableFact]` plus an environment
  variable (`AZURE_*`, `CONTEXTSMITH_POSTGRES`). Add the variable and the setup command
  to the test file comment.
- No Azure subscription is available in agent sessions, and the owner avoids Azure lock-in.
  Build against an interface, test with a fake, and leave deployment steps open.
