# Prerequisites

Read this file for the tools this project needs on a development machine.
Update it when a required tool or minimum version changes.

## .NET SDK

ContextSmith targets .NET 10.

Minimum SDK version: **10.0.400**.

Check the installed version:

```bash
dotnet --version
```

List every installed SDK, if more than one is present:

```bash
dotnet --list-sdks
```

Install or update the SDK with `winget`:

```bash
winget install --id Microsoft.DotNet.SDK.10
```

`winget` installs each SDK version side by side. It does not remove an
older version, and it does not change a Visual Studio-managed SDK. Download
links for every platform: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## GitHub CLI

Issue tracking uses the `gh` CLI — see `AGENTS.md` and
`docs/agents/issue-tracker.md`.

Check the installed version:

```bash
gh --version
```

Sign in, if not already signed in:

```bash
gh auth login
```

Install with `winget`:

```bash
winget install --id GitHub.cli
```

## pre-commit

Git hooks — formatting, tests, secret scanning, and NuGet vulnerability
scanning — run through [pre-commit](https://pre-commit.com/). Config lives
in `.pre-commit-config.yaml`; hook scripts live in `scripts/hooks/`.

Check the installed version:

```bash
pre-commit --version
```

Install with `winget` or `pip`:

```bash
winget install --id pre-commit.pre-commit
# or
pip install pre-commit
```

Install the git hooks once per clone:

```bash
pre-commit install
```

Run every hook against every file — useful after cloning, or after editing
`.pre-commit-config.yaml`:

```bash
pre-commit run --all-files
```

The `dotnet-format`, `dotnet-test`, and `dotnet-vulnerable-packages` hooks
skip with a message until a `.sln` file exists (Step 1 in `docs/PLAN.md`).
They activate automatically once the solution is scaffolded.
