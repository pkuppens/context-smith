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
