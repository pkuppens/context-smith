# Issue body template

Use this template for every issue body in this repo. Keep the section order and the headings exactly as shown. `gh issue view` on any closed issue (for example #13, #14, #15) shows a finished example.

## Goal

State the outcome and the reason for it, in one or two sentences. Name a prior issue by number if this issue depends on it.

## Acceptance Criteria

List each outcome as one checkbox. Each item must state a testable result, not a task.

- [ ] `IExample` is defined in `ContextSmith.Application`, with a `LocalExample` implementation.
- [ ] Unit tests cover `LocalExample` against a fake dependency.

## Steps

List the planned steps as numbered checkboxes. Add a command or code block where a specific tool call matters.

- [ ] 1. Define `IExample` in `ContextSmith.Application`.
- [ ] 2. Implement `LocalExample`.
- [ ] 3. Add unit tests.

## Validation

List the commands or manual checks that prove the issue is done.

- [ ] `dotnet build` succeeds.
- [ ] `dotnet test --filter <project>` passes.
- [ ] `pre-commit run --all-files` passes.

## Out of Scope

List what this issue does not cover. Point to the issue that tracks it, if one exists.

- Example item not covered here — tracked in #N.

## Draft issues

Create an issue before all details are known, so the work is visible early. Add the `draft` label to it.

Keep every section heading in a draft issue. Replace unknown content with a placeholder — do not delete the section or leave it empty.

- `_TBD — <what is missing>_` for a section with no content yet.
- `- [ ] TBD: <question to resolve>` for one unresolved acceptance criterion or step, inside an otherwise-filled section.

Before work starts on a draft issue:

1. Resolve every `TBD` placeholder.
2. Remove the `draft` label.
