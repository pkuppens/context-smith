# Plan

Read this file for the implementation roadmap and current status. Update it
when a step starts, finishes, or the step order changes.

Each step below links to a GitHub issue. The issue is the place to track
progress, comments, and scope changes. This file stays a short index of
goals and status.

## Initial implementation strategy

The project starts with a small working path. Each step adds one capability
in a controlled way.

### Step 1 — Repository and application foundation

Issue: [#1](https://github.com/pkuppens/context-smith/issues/1)

See [docs/PREREQUISITES.md](PREREQUISITES.md) for the required .NET SDK
version before running any command below.

**Status: done.** Implemented directly on `main` (see commit history around
2026-08-25). Kept here, fully checked off, as the reference for how later
steps extend the solution.

**Goal.** Create a solution that builds, tests, and runs in CI.

**Acceptance criteria**

- [x] `dotnet build` builds the solution without error.
- [x] The solution has these projects: `ContextSmith.Domain`,
      `ContextSmith.Application`, `ContextSmith.Documents.Docx`,
      `ContextSmith.Mcp`, and a test project for `Domain`,
      `Documents.Docx`, and `Mcp`. `Application` gets a test project in
      Step 6, once it has chunking logic worth testing.
- [x] `ContextSmith.Domain` does not reference Open XML, Azure, MCP, or
      ASP.NET Core packages.
- [x] A GitHub Actions workflow runs build and test on push and pull
      request.
- [x] `.editorconfig`, `Directory.Build.props`, and
      `Directory.Packages.props` exist and set shared build rules and
      central package versions.
- [x] `git status` after setup shows no `bin/` or `obj/` folder as
      untracked.

**Steps**

Run these commands from the repository root. Each command creates or
changes files on disk — nothing here talks to GitHub or Azure.

- [x] 1. Create the solution file.

      ```bash
      dotnet new sln -n ContextSmith
      ```

      **Lesson learned:** on the .NET 10 SDK, `dotnet new sln` creates
      `ContextSmith.slnx` (the new XML solution format), not
      `ContextSmith.sln`. Every `dotnet sln ...` command below targets
      `ContextSmith.slnx`. If an older SDK creates a `.sln` file instead,
      substitute that filename.

- [x] 2. Create the four source projects. `-o` sets the output folder, so
      each project lands under `src/` instead of the repository root.

      ```bash
      dotnet new classlib -n ContextSmith.Domain          -o src/ContextSmith.Domain
      dotnet new classlib -n ContextSmith.Application      -o src/ContextSmith.Application
      dotnet new classlib -n ContextSmith.Documents.Docx   -o src/ContextSmith.Documents.Docx
      dotnet new console  -n ContextSmith.Mcp              -o src/ContextSmith.Mcp
      ```

      `ContextSmith.Mcp` is a console app because it will host the MCP
      server process (Step 7). The other three are class libraries — code
      with no entry point of its own.

- [x] 3. Create one xUnit test project per source project that has logic
      worth testing yet.

      ```bash
      dotnet new xunit -n ContextSmith.Domain.Tests          -o tests/ContextSmith.Domain.Tests
      dotnet new xunit -n ContextSmith.Documents.Docx.Tests   -o tests/ContextSmith.Documents.Docx.Tests
      dotnet new xunit -n ContextSmith.Mcp.Tests              -o tests/ContextSmith.Mcp.Tests
      ```

      `ContextSmith.Application` gets its chunking tests in Step 6 —
      skip its test project for now.

- [x] 4. Add every project to the solution.

      ```bash
      dotnet sln ContextSmith.slnx add src/ContextSmith.Domain
      dotnet sln ContextSmith.slnx add src/ContextSmith.Application
      dotnet sln ContextSmith.slnx add src/ContextSmith.Documents.Docx
      dotnet sln ContextSmith.slnx add src/ContextSmith.Mcp
      dotnet sln ContextSmith.slnx add tests/ContextSmith.Domain.Tests
      dotnet sln ContextSmith.slnx add tests/ContextSmith.Documents.Docx.Tests
      dotnet sln ContextSmith.slnx add tests/ContextSmith.Mcp.Tests
      ```

      A project only builds as part of `dotnet build` at the solution root
      if it is listed here. Creating the `.csproj` file in step 2 is not
      enough on its own.

- [x] 5. Wire up project references, so the dependency direction from
      `docs/ARCHITECTURE.md` is enforced by the compiler, not just by
      convention.

      ```bash
      dotnet add src/ContextSmith.Application reference src/ContextSmith.Domain
      dotnet add src/ContextSmith.Documents.Docx reference src/ContextSmith.Application
      dotnet add src/ContextSmith.Mcp reference src/ContextSmith.Application

      dotnet add tests/ContextSmith.Domain.Tests reference src/ContextSmith.Domain
      dotnet add tests/ContextSmith.Documents.Docx.Tests reference src/ContextSmith.Documents.Docx
      dotnet add tests/ContextSmith.Mcp.Tests reference src/ContextSmith.Mcp
      ```

      Notice `ContextSmith.Domain` never appears as the *first* argument
      here — nothing in `src/` should add a reference *from* Domain *to*
      anything else. That is what "Domain has no external dependency"
      means in practice.

- [x] 6. Add the two package dependencies this step's acceptance criteria
      assume, even though the code that uses them lands in later steps
      (Step 4 for Open XML, Step 7 for MCP). Adding the reference now,
      while the projects are still empty, keeps the plumbing separate
      from the parsing/serving logic.

      ```bash
      dotnet add src/ContextSmith.Documents.Docx package DocumentFormat.OpenXml

      dotnet add src/ContextSmith.Mcp package ModelContextProtocol
      dotnet add src/ContextSmith.Mcp package Microsoft.Extensions.Hosting
      ```

- [x] 7. Generate a .NET `.gitignore`, so build output never gets
      committed. This repository already had a one-line `.gitignore`
      (the `tmp/` scratch rule) from an earlier step — move it aside,
      generate the template, then append it back, instead of overwriting
      it.

      ```bash
      mv .gitignore .gitignore.custom
      dotnet new gitignore
      printf '\n# ContextSmith scratch directory (see AGENTS.md / CLAUDE.md)\ntmp/\n' >> .gitignore
      rm .gitignore.custom
      ```

- [x] 8. Move the settings that are now shared across every project out of
      the individual `.csproj` files:
      - `Directory.Build.props` — `TargetFramework`, `ImplicitUsings`,
        `Nullable`, `LangVersion`, `EnforceCodeStyleInBuild`. Generate a
        starting point with `dotnet new buildprops`, then fill in the
        `PropertyGroup`.
      - `Directory.Packages.props` — central package version management.
        Set `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
        and list every `<PackageVersion>` used anywhere in the solution.
        Once this file exists, drop the `Version="..."` attribute from
        every `<PackageReference>` in every `.csproj` — a version on both
        sides is a build error (`NU1008`).
      - `.editorconfig` — generate with `dotnet new editorconfig`; the
        default template is a reasonable starting point and does not need
        hand-editing yet.

- [x] 9. Add `.github/workflows/build.yml` that runs `dotnet restore`,
      `dotnet build`, and `dotnet test` on push and pull request.

- [x] 10. Inspect what was generated before committing anything.

      ```bash
      dotnet sln ContextSmith.slnx list
      git status
      ```

      `dotnet sln list` should print all seven projects from step 4. `git
      status` should show `.cs`, `.csproj`, `.slnx`, `.gitignore`, and the
      props/editorconfig/workflow files as new — and should **not** show
      any `bin/` or `obj/` folder. If a `bin/` or `obj/` folder does show
      up, the `.gitignore` from step 7 ran after files already existed, or
      is missing an entry — fix the `.gitignore`, not the folders.

- [x] 11. Run `pre-commit run --all-files` (see
      `docs/PREREQUISITES.md`). The generated `dotnet new` templates use
      CRLF line endings and no trailing newline; the `trailing-whitespace`,
      `end-of-file-fixer`, and `mixed-line-ending` hooks auto-fix these on
      the first run — re-run once to confirm a clean pass, then stage the
      fixes.

**Which files belong in git**

| Belongs in git | Generated — never commit |
| --- | --- |
| `ContextSmith.slnx`, every `*.csproj` | `bin/`, `obj/` under any project |
| Every `*.cs` file | `.vs/` (Visual Studio's own cache folder) |
| `.gitignore`, `.editorconfig` | NuGet package caches |
| `Directory.Build.props`, `Directory.Packages.props` | |
| `.github/workflows/build.yml` | |

`bin/` and `obj/` are the compiler's output — a fresh `dotnet build`
recreates them from the `.csproj` and `.cs` files every time. Committing
them adds noise and stale binaries with no benefit. The `dotnet new
gitignore` template from step 7 already excludes both.

**Validation**

- [x] `dotnet build` exits with code 0.
- [x] `dotnet test` exits with code 0.
- [x] `pre-commit run --all-files` passes.
- [x] The GitHub Actions run for the push to `main` shows a green check.

### Step 2 — Canonical document model

Issue: [#2](https://github.com/pkuppens/context-smith/issues/2)

**Goal.** Define one document model that does not depend on a source file
type.

**Acceptance criteria**

- [ ] `ContextSmith.Domain` defines types for Document, Section, Heading,
      Paragraph, List, Table, Metadata, and Provenance.
- [ ] Each type that represents document content carries a provenance
      reference (source id and source location).
- [ ] A unit test builds a sample tree and asserts the parent-child
      relations and the provenance fields.

**Steps**

1. Design the domain types from `docs/ARCHITECTURE.md`.
2. Add provenance fields to content-bearing types.
3. Write a unit test that constructs a small document tree.

**Validation**

- `dotnet test --filter ContextSmith.Domain.Tests` passes.

### Step 3 — Text and Markdown

Issue: [#3](https://github.com/pkuppens/context-smith/issues/3)

**Goal.** Parse `.txt` and `.md` files into the canonical document model.

**Acceptance criteria**

- [ ] An `IDocumentParser` implementation converts a plain text file into a
      Document with inferred Section and Paragraph elements.
- [ ] An `IDocumentParser` implementation converts a Markdown file into a
      Document, mapping Markdown headings and lists to the canonical model.
- [ ] A test fixture Markdown file produces a Document with the expected
      heading count and nesting depth.

**Steps**

1. Add the text and Markdown parser implementations.
2. Add fixture `.txt` and `.md` files under `samples/documents/`.
3. Add unit tests that assert structure counts against the fixtures.

**Validation**

- `dotnet test --filter "FullyQualifiedName~Text|FullyQualifiedName~Markdown"`
  passes.

### Step 4 — DOCX

Issue: [#4](https://github.com/pkuppens/context-smith/issues/4)

**Goal.** Parse `.docx` files with the Open XML SDK.

**Acceptance criteria**

- [ ] `ContextSmith.Documents.Docx` implements `IDocumentParser` using the
      Open XML SDK.
- [ ] Word styles, headings, and numbering map to Section, Heading, and
      List elements in the canonical model.
- [ ] A fixture `.docx` file produces a Document with the expected heading
      and paragraph counts.

**Steps**

1. Add the Open XML SDK package reference.
2. Implement the DOCX parser.
3. Add fixture `.docx` files under `samples/documents/`.
4. Add unit tests against the fixtures.

**Validation**

- `dotnet test --filter ContextSmith.Documents.Docx.Tests` passes.

### Step 5 — PDF

Issue: [#5](https://github.com/pkuppens/context-smith/issues/5)

**Goal.** Parse `.pdf` files, with Azure Document Intelligence as the first
implementation.

**Acceptance criteria**

- [ ] An `IDocumentParser` implementation for PDF exists.
- [ ] The implementation reads Azure credentials and endpoint from
      configuration, not from source code.
- [ ] A unit test verifies the parser contract with a mocked Azure
      response.
- [ ] An optional integration test, marked as requiring Azure credentials,
      runs against a fixture `.pdf` file when credentials are present.

**Steps**

1. Add `ContextSmith.Documents.Pdf` and the Azure Document Intelligence SDK
   package.
2. Implement the PDF parser against `IDocumentParser`.
3. Add a unit test with a mocked Azure response.
4. Add an integration test that skips when Azure credentials are absent.

**Validation**

- `dotnet test --filter ContextSmith.Documents.Pdf.Tests` passes without
  Azure credentials present.
- The integration test passes when Azure credentials are present, and is
  documented as optional in the test project README.

### Step 6 — Chunk strategies

Issue: [#6](https://github.com/pkuppens/context-smith/issues/6)

**Goal.** Create retrieval chunks from the canonical document model with a
shared strategy contract.

**Acceptance criteria**

- [ ] `IChunkingStrategy` is defined in `ContextSmith.Application`.
- [ ] `FixedSizeChunker` and `StructureAwareChunker` implement
      `IChunkingStrategy`.
- [ ] A unit test compares the chunk count and chunk boundaries produced by
      each strategy on the same fixture Document.

**Steps**

1. Define `IChunkingStrategy` and the `Chunk` domain type.
2. Implement `FixedSizeChunker`.
3. Implement `StructureAwareChunker`.
4. Add unit tests comparing both strategies on one fixture.

**Validation**

- `dotnet test --filter ContextSmith.Application.Tests` passes, including
  the chunking comparison test.

### Step 7 — MCP

Issue: [#7](https://github.com/pkuppens/context-smith/issues/7)

**Goal.** Expose the document preparation use case through MCP.

**Acceptance criteria**

- [ ] `ContextSmith.Mcp` hosts an MCP server that exposes the
      `prepare_document` tool, the
      `contextsmith://documents/{documentId}/structure` resource, and the
      `prepare-document-for-rag` prompt.
- [ ] `ContextSmith.Mcp` calls `ContextSmith.Application` only. It does not
      call `ContextSmith.Domain` directly.
- [ ] An in-process integration test calls `prepare_document` with a
      fixture document and asserts the returned structure.

**Steps**

1. Add the MCP C# SDK package to `ContextSmith.Mcp`.
2. Implement the server, the `prepare_document` tool, the structure
   resource, and the prompt.
3. Add an in-process integration test that invokes the tool.

**Validation**

- `dotnet test --filter ContextSmith.Mcp.Tests` passes, including the tool
  invocation test.

### Step 8 — Embeddings and retrieval

Issue: [#8](https://github.com/pkuppens/context-smith/issues/8)

**Goal.** Add embeddings and retrieval, with Azure AI Search as the first
managed implementation.

**Acceptance criteria**

- [ ] `IEmbeddingService` is defined in `ContextSmith.Application`, with an
      Azure OpenAI implementation.
- [ ] `IRetrievalService` is defined in `ContextSmith.Application`, with an
      Azure AI Search implementation for indexing and search.
- [ ] Unit tests verify both interfaces against a fake implementation
      without calling Azure.
- [ ] An optional integration test, marked as requiring Azure credentials,
      indexes fixture chunks and retrieves the top-K results for a known
      query.

**Steps**

1. Define `IEmbeddingService` and `IRetrievalService`.
2. Implement the Azure OpenAI embedding service.
3. Implement the Azure AI Search retrieval service.
4. Add unit tests with fake implementations.
5. Add an integration test that skips when Azure credentials are absent.

**Validation**

- `dotnet test --filter "FullyQualifiedName~Embedding|FullyQualifiedName~Retrieval"`
  passes without Azure credentials present.
- The integration test passes when Azure credentials are present.

### Step 9 — Azure deployment

Issue: [#9](https://github.com/pkuppens/context-smith/issues/9)

**Goal.** Deploy the MCP server and required services to Azure, using the
same application contracts as local development.

**Acceptance criteria**

- [ ] Infrastructure-as-code (Bicep or `azd`) provisions Azure Container
      Apps, Azure Document Intelligence, Azure OpenAI, Azure AI Search, and
      a managed identity.
- [ ] The deployed MCP server responds to a health check endpoint.
- [ ] No application code changes between local and Azure deployment;
      configuration values differ only.

**Steps**

1. Write the Bicep or `azd` templates for the required services.
2. Add a health check endpoint to `ContextSmith.Mcp`.
3. Deploy to a test resource group.
4. Run a smoke test against the deployed endpoint.

**Validation**

- `azd up` (or the chosen deployment command) completes with exit code 0.
- A scripted smoke test sends a request to the deployed health check
  endpoint and receives HTTP 200.

### Step 10 — Evaluation

Issue: [#10](https://github.com/pkuppens/context-smith/issues/10)

**Goal.** Compare parsing, chunking, embedding, and retrieval choices with
repeatable test data.

**Acceptance criteria**

- [ ] An evaluation dataset exists under `samples/documents/`, with fixture
      documents, queries, and relevance judgments.
- [ ] An evaluation harness computes Recall@K, Mean Reciprocal Rank (MRR),
      and normalized Discounted Cumulative Gain (nDCG) for one strategy
      combination.
- [ ] Running the harness against at least two chunking strategies produces
      a comparison report.

**Steps**

1. Curate the evaluation dataset with gold queries and relevance
   judgments.
2. Implement the Recall@K, MRR, and nDCG calculations.
3. Add a command or test that runs the comparison and writes the report.

**Validation**

- Running the evaluation command produces a report file.
- A test asserts that the report file exists and that each metric value is
  between 0 and 1.

### Step 11 — Additional Office formats

Issue: [#11](https://github.com/pkuppens/context-smith/issues/11)

**Goal.** Add Excel and PowerPoint parsing after the canonical model and
evaluation approach are stable.

**Acceptance criteria**

- [ ] An `IDocumentParser` implementation for `.xlsx` maps sheets, tables,
      and ranges to the canonical model.
- [ ] An `IDocumentParser` implementation for `.pptx` maps slides, text
      blocks, notes, and tables to the canonical model.
- [ ] Fixture-based tests validate the structural mapping for both
      formats.

**Steps**

1. Add `ContextSmith.Documents.Excel` and
   `ContextSmith.Documents.PowerPoint`.
2. Implement both parsers with the Open XML SDK.
3. Add fixture files and unit tests for both formats.

**Validation**

- `dotnet test --filter "FullyQualifiedName~Excel|FullyQualifiedName~PowerPoint"`
  passes.

## Status

ContextSmith is under active development.

The first goal is to create a small and testable document-processing core
(Steps 1-4). Later steps add chunking, MCP access, retrieval, Azure
deployment, comparative evaluation, and additional Office formats.
