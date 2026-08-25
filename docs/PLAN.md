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

**Status: done.** Merged via #12.

**Goal.** Define one document model that does not depend on a source file
type.

**Acceptance criteria**

- [x] `ContextSmith.Domain` defines types for Document, Section, Heading,
      Paragraph, List, Table, Metadata, and Provenance.
- [x] Each type that represents document content carries a provenance
      reference (source id and source location).
- [x] A unit test builds a sample tree and asserts the parent-child
      relations and the provenance fields.
- [x] `ContextSmith.Domain.csproj` still has zero `<PackageReference>`
      entries after this step.

**Steps**

- [x] 1. Design the domain types from `docs/ARCHITECTURE.md`'s canonical
      model diagram. Implemented as sketched below, one type per file:

      ```csharp
      namespace ContextSmith.Domain;

      public sealed record Provenance(string SourceId, string? Location = null);

      public abstract class DocumentNode
      {
          public required Provenance Provenance { get; init; }
          public IReadOnlyList<DocumentNode> Children { get; init; } = [];
      }

      public sealed class Document : DocumentNode
      {
          public required DocumentMetadata Metadata { get; init; }
      }

      public sealed class DocumentMetadata
      {
          public string? Title { get; init; }
      }

      public sealed class Section : DocumentNode
      {
          public string? Title { get; init; }
      }

      public sealed class Heading : DocumentNode
      {
          public required string Text { get; init; }
          public required int Level { get; init; }
      }

      public sealed class Paragraph : DocumentNode
      {
          public required string Text { get; init; }
      }

      public sealed class ListBlock : DocumentNode
      {
          public required bool Ordered { get; init; }
      }

      public sealed class TableBlock : DocumentNode
      {
          public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
      }
      ```

- [x] 2. Confirm every content-bearing type carries `Provenance` — done via
      the shared `DocumentNode` base type.
- [x] 3. Write a unit test in `ContextSmith.Domain.Tests` that constructs a
      small tree (`Document` → `Section` → `Heading`/`Paragraph`) and
      asserts the parent-child structure and the provenance fields. See
      `DocumentTreeTests.Document_tree_preserves_structure_and_provenance`.

**Validation**

- [x] `dotnet build` succeeds.
- [x] `dotnet test --filter ContextSmith.Domain.Tests` passes.
- [x] `pre-commit run --all-files` passes.

### Step 3 — Text and Markdown

Issue: [#3](https://github.com/pkuppens/context-smith/issues/3)

**Status: done.**

**Goal.** Parse `.txt` and `.md` files into the canonical document model.

**Acceptance criteria**

- [x] `IDocumentParser` and `DocumentSource` are defined in
      `ContextSmith.Application` — every parser project from this step
      onward (Docx, Pdf, Excel, PowerPoint) depends on this contract, and
      nothing defined it in Step 1 or 2.
- [x] A new project `ContextSmith.Documents.Text` implements
      `IDocumentParser` for `.txt`, converting a plain text file into a
      Document with inferred Section and Paragraph elements.
- [x] The same project implements `IDocumentParser` for `.md`, mapping
      Markdown headings and lists to the canonical model.
- [x] A test fixture Markdown file produces a Document with the expected
      heading count and nesting depth.

**Steps**

- [x] 1. Define the parsing contract in `ContextSmith.Application`:

      ```csharp
      namespace ContextSmith.Application;

      public sealed record DocumentSource(string SourceId, Stream Content);

      public interface IDocumentParser
      {
          Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default);
      }
      ```

- [x] 2. Create the project and its test project — Step 1 scaffolded four
      `src/` projects, not five; this is the first step that needs a new
      one:

      ```bash
      dotnet new classlib -n ContextSmith.Documents.Text        -o src/ContextSmith.Documents.Text
      dotnet new xunit     -n ContextSmith.Documents.Text.Tests -o tests/ContextSmith.Documents.Text.Tests
      dotnet sln ContextSmith.slnx add src/ContextSmith.Documents.Text
      dotnet sln ContextSmith.slnx add tests/ContextSmith.Documents.Text.Tests
      dotnet add src/ContextSmith.Documents.Text reference src/ContextSmith.Application
      dotnet add tests/ContextSmith.Documents.Text.Tests reference src/ContextSmith.Documents.Text
      ```

- [x] 3. Implement `TextDocumentParser` (infers `Section`/`Paragraph` from
      blank-line-separated blocks) and `MarkdownDocumentParser` (maps
      `#`/`##`/... headings and `-`/`1.` lists directly, since Markdown
      already states its structure explicitly — no inference needed).
- [x] 4. Add fixture files:

      ```bash
      mkdir -p samples/documents
      ```

      Add a small fixture `.txt` and `.md` file, each with a known and
      documented heading/paragraph count.
- [x] 5. Add unit tests in `ContextSmith.Documents.Text.Tests` asserting
      the parsed structure counts against each fixture.

**Validation**

- [x] `dotnet build` succeeds.
- [x] `dotnet test --filter ContextSmith.Documents.Text.Tests` passes.
- [x] `pre-commit run --all-files` passes.

**Out of Scope**

- DOCX, PDF, Excel, PowerPoint parsing. Tracked in Steps 4, 5, 11.

### Step 4 — DOCX

Issue: [#4](https://github.com/pkuppens/context-smith/issues/4)

**Status: done.**

**Goal.** Parse `.docx` files with the Open XML SDK.

**Acceptance criteria**

- [x] `ContextSmith.Documents.Docx` implements the `IDocumentParser`
      contract from Step 3 using the Open XML SDK (already referenced —
      see Step 1).
- [x] Word styles, headings, and numbering map to Section, Heading, and
      List elements in the canonical model.
- [x] A fixture `.docx` file produces a Document with the expected heading
      and paragraph counts.

**Steps**

- [x] 1. Implement `DocxDocumentParser : IDocumentParser` in
      `ContextSmith.Documents.Docx`: paragraphs with style `Heading1`
      through `Heading9` map to `Heading` with a matching `Level`;
      `NumberingProperties` maps to `ListBlock`; everything else maps to
      `Paragraph`.
- [x] 2. Add a fixture `.docx` file under `samples/documents/` with a
      known heading/paragraph/list count. Create it in Word or
      LibreOffice, or generate it with a one-off Open XML SDK script — do
      not hand-craft the underlying zip/XML.
- [x] 3. Copy the fixture to the test output directory:

      ```xml
      <ItemGroup>
        <None Include="..\..\samples\documents\*.docx" CopyToOutputDirectory="PreserveNewest" />
      </ItemGroup>
      ```

- [x] 4. Add unit tests asserting the parsed structure counts against the
      fixture.

**Validation**

- [x] `dotnet build` succeeds.
- [x] `dotnet test --filter ContextSmith.Documents.Docx.Tests` passes.
- [x] `pre-commit run --all-files` passes.

### Step 5 — PDF

Issue: [#5](https://github.com/pkuppens/context-smith/issues/5)

**Goal.** Parse `.pdf` files, with Azure Document Intelligence as the first
implementation.

**Acceptance criteria**

- [ ] An `IDocumentParser` implementation for PDF exists in a new
      `ContextSmith.Documents.Pdf` project.
- [ ] The implementation reads Azure credentials and endpoint from
      configuration (`IConfiguration`), not from source code.
- [ ] A unit test verifies the parser contract with a mocked Azure
      response.
- [ ] An optional integration test, marked as requiring Azure credentials,
      runs against a fixture `.pdf` file when credentials are present, and
      is skipped (not failed) when they are absent.

**Steps**

- [ ] 1. Create the project and test project, and add the Azure Document
      Intelligence package:

      ```bash
      dotnet new classlib -n ContextSmith.Documents.Pdf        -o src/ContextSmith.Documents.Pdf
      dotnet new xunit     -n ContextSmith.Documents.Pdf.Tests -o tests/ContextSmith.Documents.Pdf.Tests
      dotnet sln ContextSmith.slnx add src/ContextSmith.Documents.Pdf
      dotnet sln ContextSmith.slnx add tests/ContextSmith.Documents.Pdf.Tests
      dotnet add src/ContextSmith.Documents.Pdf reference src/ContextSmith.Application
      dotnet add tests/ContextSmith.Documents.Pdf.Tests reference src/ContextSmith.Documents.Pdf
      dotnet add src/ContextSmith.Documents.Pdf package Azure.AI.DocumentIntelligence
      ```

      Add the resulting `Azure.AI.DocumentIntelligence` version to
      `Directory.Packages.props` as a `<PackageVersion>`, and drop the
      `Version` attribute from the `<PackageReference>` — see the Step 1
      note on central package management.
- [ ] 2. Implement `PdfDocumentParser : IDocumentParser`, reading the
      endpoint and key from `IConfiguration`.
- [ ] 3. Add a unit test that injects a mocked Document Intelligence
      client (wrap the SDK client behind a small interface if it is not
      directly mockable) and asserts the mapping from Azure's layout
      result to the canonical model.
- [ ] 4. Add an integration test that is skipped when an environment
      variable such as `AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT` is unset,
      and otherwise runs the real parser against a fixture `.pdf`.

**Validation**

- [ ] `dotnet build` succeeds.
- [ ] `dotnet test --filter ContextSmith.Documents.Pdf.Tests` passes
      without Azure credentials present (the integration test reports
      Skipped, not Failed).
- [ ] The integration test passes when Azure credentials are present.
- [ ] `pre-commit run --all-files` passes.

### Step 6 — Chunk strategies

Issue: [#6](https://github.com/pkuppens/context-smith/issues/6)

**Status: done.**

**Goal.** Create retrieval chunks from the canonical document model with a
shared strategy contract.

**Acceptance criteria**

- [x] `IChunkingStrategy` is defined in `ContextSmith.Application`.
- [x] `FixedSizeChunker` and `StructureAwareChunker` implement
      `IChunkingStrategy`.
- [x] A unit test compares the chunk count and chunk boundaries produced by
      each strategy on the same fixture Document.

**Steps**

- [x] 1. Create the `ContextSmith.Application` test project Step 1
      deliberately skipped, now that there is logic worth testing:

      ```bash
      dotnet new xunit -n ContextSmith.Application.Tests -o tests/ContextSmith.Application.Tests
      dotnet sln ContextSmith.slnx add tests/ContextSmith.Application.Tests
      dotnet add tests/ContextSmith.Application.Tests reference src/ContextSmith.Application
      ```

- [x] 2. Define `IChunkingStrategy` and the `Chunk` domain type — `Chunk`
      should carry the extracted text, a reference back to its source
      node(s), and `Provenance`.
- [x] 3. Implement `FixedSizeChunker` (splits by a configurable
      character/token budget, ignoring structure).
- [x] 4. Implement `StructureAwareChunker` (chunks along `Section`/
      `Heading` boundaries, keeping each chunk's ancestor heading path as
      context).
- [x] 5. Add unit tests in `ContextSmith.Application.Tests` comparing
      chunk count and boundaries produced by both strategies on the same
      fixture `Document`.

**Validation**

- [x] `dotnet build` succeeds.
- [x] `dotnet test --filter ContextSmith.Application.Tests` passes,
      including the chunking comparison test.
- [x] `pre-commit run --all-files` passes.

### Step 7 — MCP

Issue: [#7](https://github.com/pkuppens/context-smith/issues/7)

**Status: done.**

**Goal.** Expose the document preparation use case through MCP.

**Acceptance criteria**

- [x] `ContextSmith.Mcp` hosts an MCP server that exposes the
      `prepare_document` tool, the
      `contextsmith://documents/{documentId}/structure` resource, and the
      `prepare-document-for-rag` prompt.
- [x] `ContextSmith.Mcp` calls `ContextSmith.Application` only. It does not
      call `ContextSmith.Domain` directly.
- [x] An in-process integration test calls `prepare_document` with a
      fixture document and asserts the returned structure.

**Steps**

- [x] 1. `ContextSmith.Mcp` already references `ModelContextProtocol` and
      `Microsoft.Extensions.Hosting` from Step 1. Confirm the installed
      version and check its current samples before writing server code —
      the tool/resource/prompt registration API can change between
      releases:

      ```bash
      dotnet list src/ContextSmith.Mcp package
      ```

      (Installed as of Step 1: `ModelContextProtocol` 2.2.0.)
- [x] 2. Build a `Host` (`Microsoft.Extensions.Hosting`) in `Program.cs`
      that registers the MCP server and the `ContextSmith.Application`
      services it calls.
- [x] 3. Implement the `prepare_document` tool: accepts a document source,
      calls the matching `IDocumentParser` (selected by file extension),
      and returns the canonical structure.
- [x] 4. Implement the `contextsmith://documents/{documentId}/structure`
      resource, returning the stored structure for a previously prepared
      document. An in-memory store is enough at this stage — no
      persistence requirement yet.
- [x] 5. Implement the `prepare-document-for-rag` prompt per its one-line
      goal in `docs/ARCHITECTURE.md`.
- [x] 6. Add an in-process integration test in `ContextSmith.Mcp.Tests`
      that invokes `prepare_document` in-process (no separate server
      process) with a fixture document and asserts the returned
      structure.

**Validation**

- [x] `dotnet build` succeeds.
- [x] `dotnet test --filter ContextSmith.Mcp.Tests` passes, including the
      tool invocation test.
- [x] `pre-commit run --all-files` passes.

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

- [ ] 1. Add the Azure SDK packages, with matching `Directory.Packages.props`
      entries (see Step 5 for the central-package-management pattern):

      ```bash
      dotnet add src/ContextSmith.Application package Azure.AI.OpenAI
      dotnet add src/ContextSmith.Application package Azure.Search.Documents
      ```

      Consider a dedicated `ContextSmith.Retrieval.Azure` project instead
      if `ContextSmith.Application` should not carry Azure SDK references
      directly — decide this while implementing, and update
      `docs/ARCHITECTURE.md`'s service-agnostic diagram to match whichever
      choice is made.
- [ ] 2. Define `IEmbeddingService` and `IRetrievalService` in
      `ContextSmith.Application`.
- [ ] 3. Implement the Azure OpenAI embedding service.
- [ ] 4. Implement the Azure AI Search retrieval service.
- [ ] 5. Add unit tests against fake implementations of both interfaces
      (no Azure call).
- [ ] 6. Add an integration test, skipped when Azure credentials are
      absent (same pattern as Step 5), that indexes fixture chunks and
      retrieves the top-K results for a known query.

**Validation**

- [ ] `dotnet build` succeeds.
- [ ] `dotnet test --filter "FullyQualifiedName~Embedding|FullyQualifiedName~Retrieval"`
      passes without Azure credentials present.
- [ ] The integration test passes when Azure credentials are present.
- [ ] `pre-commit run --all-files` passes.

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

- [ ] 1. Run `azd init` (or hand-write Bicep under `infra/`) to provision
      Azure Container Apps, Azure Document Intelligence, Azure OpenAI,
      Azure AI Search, and a managed identity.
- [ ] 2. Add a health check endpoint to `ContextSmith.Mcp`
      (`Microsoft.Extensions.Diagnostics.HealthChecks`, exposed over HTTP
      alongside the MCP transport).
- [ ] 3. Deploy to a test resource group (`azd up` or
      `az deployment group create`).
- [ ] 4. Run a smoke test against the deployed endpoint.

**Validation**

- [ ] `azd up` (or the chosen deployment command) completes with exit code
      0.
- [ ] A scripted smoke test sends a request to the deployed health check
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

- [ ] 1. Curate the evaluation dataset under `samples/documents/`: fixture
      documents, a fixed set of queries, and manually judged relevant
      chunks per query.
- [ ] 2. Implement Recall@K, MRR, and nDCG. Put them in
      `ContextSmith.Application`, or in a new `ContextSmith.Evaluation`
      project if the metrics don't belong in the application layer —
      decide while implementing.
- [ ] 3. Add a command (console entry point or a dedicated test) that runs
      the comparison across at least two chunking strategies and writes a
      report (CSV or console table) with the metric values.

**Validation**

- [ ] Running the evaluation command produces a report file.
- [ ] A test asserts that the report file exists and that each metric
      value is between 0 and 1.

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

- [ ] 1. Create both projects and their test projects, following the same
      pattern as Steps 3-5:

      ```bash
      dotnet new classlib -n ContextSmith.Documents.Excel               -o src/ContextSmith.Documents.Excel
      dotnet new classlib -n ContextSmith.Documents.PowerPoint          -o src/ContextSmith.Documents.PowerPoint
      dotnet new xunit     -n ContextSmith.Documents.Excel.Tests        -o tests/ContextSmith.Documents.Excel.Tests
      dotnet new xunit     -n ContextSmith.Documents.PowerPoint.Tests   -o tests/ContextSmith.Documents.PowerPoint.Tests
      dotnet sln ContextSmith.slnx add src/ContextSmith.Documents.Excel src/ContextSmith.Documents.PowerPoint tests/ContextSmith.Documents.Excel.Tests tests/ContextSmith.Documents.PowerPoint.Tests
      dotnet add src/ContextSmith.Documents.Excel reference src/ContextSmith.Application
      dotnet add src/ContextSmith.Documents.PowerPoint reference src/ContextSmith.Application
      dotnet add tests/ContextSmith.Documents.Excel.Tests reference src/ContextSmith.Documents.Excel
      dotnet add tests/ContextSmith.Documents.PowerPoint.Tests reference src/ContextSmith.Documents.PowerPoint
      ```

- [ ] 2. Implement `ExcelDocumentParser` (sheets → `Section`, tables/ranges
      → `TableBlock`) and `PowerPointDocumentParser` (slides → `Section`,
      titles → `Heading`, text blocks → `Paragraph`, notes → metadata,
      tables → `TableBlock`) with the Open XML SDK.
- [ ] 3. Add fixture `.xlsx`/`.pptx` files and unit tests validating the
      structural mapping for both formats.

**Validation**

- [ ] `dotnet build` succeeds.
- [ ] `dotnet test --filter "FullyQualifiedName~Excel|FullyQualifiedName~PowerPoint"`
      passes.
- [ ] `pre-commit run --all-files` passes.

## Demo track

The steps above are the original roadmap, in dependency order. The three
steps below were added to reach a concrete, runnable demo: an Angular app
where a user uploads a document or pastes a URL, watches it get parsed, and
asks questions about it in a chat window, answered by a local Ollama model
grounded in the retrieved chunks — with the MCP prompts from Step 7 offered
as sample prompts. They do not replace Steps 5, 8, 9, 10, 11 (Azure-backed
PDF, embeddings, deployment, evaluation, Excel/PowerPoint) — those stay on
the roadmap, just not required for this demo, since Azure credentials are
not available in this environment.

### Step 12 — HTML document parsing (file or URL)

Issue: [#13](https://github.com/pkuppens/context-smith/issues/13)

**Status: done.**

**Goal.** Parse HTML into the canonical document model, from an uploaded
file or a fetched URL — making HTML the fifth parseable format and the
first one whose source can be a URL instead of a file.

See the issue for full acceptance criteria, steps, and validation.

### Step 13 — Local embeddings and retrieval (Ollama)

Issue: [#14](https://github.com/pkuppens/context-smith/issues/14)

**Status: done.**

**Goal.** Define the `IEmbeddingService`/`IRetrievalService` contracts from
Step 8 and provide a local, credential-free implementation backed by
Ollama, so the pipeline and the demo run entirely offline. Step 8's Azure
implementation of the same interfaces remains a separate, later step.

See the issue for full acceptance criteria, steps, and validation.

### Step 14 — Angular demo: upload, parse, chat

Issue: [#15](https://github.com/pkuppens/context-smith/issues/15)

**Status: done.** See `docs/DEMO.md` to run it.

**Goal.** A running end-to-end demo: `ContextSmith.Api` (a second
Presentation-layer project alongside `ContextSmith.Mcp`) backs an Angular
app that uploads/fetches a document, shows its parsed structure, and
answers chat questions over it — grounded in retrieved chunks, using the
MCP prompts as sample prompts.

See the issue for full acceptance criteria, steps, and validation.

## Status

ContextSmith is under active development.

Steps 1-4, 6, 7, and 12-14 are done — a working local demo (`docs/DEMO.md`):
upload or fetch a document (txt/md/docx/html, or any http(s) URL), parse it
into the canonical model, chunk it, embed and index the chunks locally with
Ollama, and ask grounded questions about it through an Angular UI, with the
MCP server's own prompts offered as sample prompts. Steps 5, 8, 9, 10, 11
(Azure-backed PDF, embeddings, deployment, evaluation, and additional
Office formats) stay on the roadmap for when Azure credentials are
available.
