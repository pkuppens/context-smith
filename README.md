# ContextSmith

> From source documents to meaningful AI context.

ContextSmith is a C#/.NET reference implementation for document-to-context processing.

ContextSmith normalizes flat text and heavily formatted documents into meaningful, hierarchically structured documents. It prepares this structure for RAG, search, and AI agents.

The project keeps each processing step explicit. A developer can replace a managed service with a local or custom implementation. Azure services are the first managed implementations where they provide a fast and practical path for business applications.

## Project goals

ContextSmith has six main goals.

1. **Create one document model.** Convert different source formats into one canonical hierarchical model.
2. **Preserve useful structure.** Keep headings, sections, lists, tables, metadata, and source references when they add meaning.
3. **Keep the pipeline replaceable.** Use service interfaces for parsing, enrichment, chunking, embeddings, and search.
4. **Compare strategies.** Support multiple chunking strategies and embedding models, and measure their retrieval results.
5. **Expose the pipeline through MCP.** Provide clear tools, resources, and prompts for agents and MCP clients.
6. **Use managed Azure services without coupling the design to Azure.** Use Azure for quick implementation where useful, but keep local and custom alternatives possible.

The project is both a working reference implementation and an engineering testbed. It makes document-to-context decisions visible, testable, and measurable.

## Initial document types

The first document phase includes these formats.

| Format | Extension | Initial approach |
| --- | --- | --- |
| Plain text | `.txt` | Parse text and infer structure where possible. |
| Markdown | `.md` | Use Markdown structure as explicit source structure. |
| Word | `.docx` | Read source structure with Open XML and optional managed services. |
| PDF | `.pdf` | Read text and layout with a PDF parser or a managed document service. |

Later phases can add these formats.

| Format | Extension | Planned use |
| --- | --- | --- |
| Excel | `.xlsx` | Convert workbooks, sheets, tables, and ranges into semantic document structures. |
| PowerPoint | `.pptx` | Convert slides, titles, text blocks, notes, and tables into semantic document structures. |

The canonical model must not depend on a source file type.

## Non-goals

ContextSmith is not intended to be:

- a Word editor;
- an Excel editor;
- a PowerPoint editor;
- a generic OCR product;
- a SharePoint replacement;
- an Azure Document Intelligence replacement;
- an Azure AI Search replacement;
- a new vector database;
- a document management system;
- an agent framework;
- an application that hides all processing decisions behind one managed service.

Its primary responsibility is this transformation:

```text
source document
      ↓
meaningful hierarchical document
      ↓
traceable and measurable AI context
```

## Documentation

- [docs/PREREQUISITES.md](docs/PREREQUISITES.md) — the tools and minimum versions a development machine needs. Read before running a `dotnet` command; update when a required tool or version changes.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — the processing pipeline, the canonical document model, the Azure/local service split, and the MCP surface. Read before changing a pipeline stage, the document model, or a service boundary.
- [docs/PLAN.md](docs/PLAN.md) — the implementation roadmap and current status. Read to see what's next; update when a step starts or finishes.
- [docs/DEMO.md](docs/DEMO.md) — how to run the end-to-end demo (upload/URL, parse, chat) locally with Ollama.
- [docs/RESEARCH.md](docs/RESEARCH.md) — the research questions the project must answer and where it claims value over a direct managed pipeline. Read before proposing a chunking/embedding strategy comparison; update when a question is answered.
- `AGENTS.md` — shared conventions for AI coding agents (issue tracker, domain docs, writing style). `CLAUDE.md` adds Claude-specific notes on top.

## Status

ContextSmith is under active development. See [docs/PLAN.md](docs/PLAN.md) for details.
