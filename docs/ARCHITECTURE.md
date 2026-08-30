# Architecture

Read this file to understand the processing pipeline, the canonical document model, and the service boundaries. Update it when a pipeline stage, the document model, the MCP surface, or the Azure/local split changes.

## Why structure matters

Documents contain more information than plain text.

Meaning can come from these source features:

- headings;
- section hierarchy;
- paragraph relationships;
- lists and numbering;
- tables;
- styles and formatting;
- page layout;
- nearby text;
- document metadata.

A pipeline can lose this information when it converts the source to flat text too early.

For example, a source can contain this hierarchy:

```text
Employee Handbook
  └── Leave Policy
       └── Parental Leave
            └── Eligibility
                 └── paragraph
```

A flat chunk can contain only this text:

```text
Employees qualify after twelve months of employment...
```

A context-aware representation can keep the hierarchy:

```text
Document: Employee Handbook
Section: Leave Policy > Parental Leave > Eligibility

Employees qualify after twelve months of employment...
```

ContextSmith keeps this structure available for later processing.

## Processing pipeline

The pipeline uses named stages. Each stage has one clear responsibility.

```text
TXT / MD / DOCX / PDF
          ↓
        Parse
          ↓
      Normalize
          ↓
Hierarchical Document Model
          ↓
        Enrich
          ↓
        Chunk
          ↓
        Embed
          ↓
        Index
          ↓
 RAG / Search / Agents
```

### Parse

**Parse** reads a source document and converts source elements into document blocks. It does not decide how retrieval should work.

### Normalize

**Normalize** maps equivalent source features to one canonical representation. It can also remove presentation details that do not add meaning.

### Hierarchical Document Model

The **Hierarchical Document Model** stores document elements and their parent-child relationships. Downstream code uses this model instead of source-specific objects.

### Enrich

**Enrich** adds useful context that the parser cannot provide with enough confidence. Enrichment can use deterministic rules, an LLM, or another service.

### Chunk

**Chunk** creates retrieval units from the hierarchical document. A chunk keeps source references and useful parent context.

### Embed

**Embed** converts selected chunk content into a vector with a selected embedding model. The pipeline can compare more than one embedding model.

### Index

**Index** stores chunks, vectors, metadata, and source references in a selected retrieval service.

## Canonical document model

Each parser produces the same semantic model.

A simplified model looks like this:

```text
Document
 ├── Metadata
 ├── Section
 │    ├── Heading
 │    ├── Paragraph
 │    ├── List
 │    └── Table
 └── Section
      ├── Heading
      └── Paragraph
```

Each important element can also contain provenance data.

Provenance tells the system where content came from. It can include a file identifier, page number, source element identifier, or source location.

After parsing, downstream components should not need to know if the source was TXT, Markdown, DOCX, PDF, or another supported format.

## Chunking and embedding strategies

ContextSmith does not define one chunking strategy as the correct strategy.

The project can support strategies such as:

- fixed-size chunking;
- paragraph chunking;
- section-aware chunking;
- structure-aware chunking;
- context-enriched chunking;
- model-assisted semantic chunking.

Each strategy implements the same application contract.

```text
                IChunkingStrategy
                       │
       ┌───────────────┼────────────────┐
       │               │                │
  FixedSize       Paragraph      StructureAware
                                        │
                                 ContextEnriched
```

The same rule applies to embeddings and to retrieval.

```text
               IEmbeddingService
                       │
       ┌───────────────┼────────────────┐
       │               │                │
 Azure model      Local model      Custom provider

                IRetrievalService
                       │
       ┌───────────────┼────────────────┐
       │               │                │
Azure AI Search   Local (file)     Custom provider
```

`Embedding:Provider` and `Retrieval:Provider` select the active implementation
of each interface from configuration. Adding a provider means adding one more
case to the switch that builds the service, not a code change at any call
site. `Ollama` and `AzureOpenAI` are the implemented `Embedding:Provider`
values (`ContextSmith.Retrieval.Local` and `ContextSmith.Retrieval.Azure`,
respectively). `Retrieval:Provider` is a separate axis from `Storage:Provider`
because Azure AI Search is a vector index, not a general document store, so
it cannot also serve as an `IDocumentStore`; when `Retrieval:Provider` is
unset it defaults to following `Storage:Provider`, preserving the original
`InMemory`/`File` behavior. `InMemory`, `File`, and `AzureSearch` are the
implemented `Retrieval:Provider` values.

A test can compare combinations of chunking strategies and embedding models.

Possible retrieval metrics include:

- Recall@K;
- Precision@K;
- Mean Reciprocal Rank (MRR);
- normalized Discounted Cumulative Gain (nDCG).

The project should measure results before it claims that one strategy is better.

## Deterministic processing and LLM processing

ContextSmith uses deterministic processing when the source already contains reliable information.

Examples include:

- read DOCX styles and numbering;
- read Markdown headings;
- extract paragraphs and tables;
- detect explicit section hierarchy;
- preserve source locations;
- count tokens;
- apply structural chunking rules.

ContextSmith can use an LLM when semantic interpretation adds useful information.

Examples include:

- identify a heading that only uses visual formatting;
- resolve an ambiguous section boundary;
- create a short parent-context description;
- review the quality of a generated chunk.

An LLM should not recreate information that deterministic code can read reliably.

## Persistence

`IDocumentStore` and `IRetrievalService` store data behind a narrow contract. Each
implementation lives in one file. `ContextSmith.Application` defines both
interfaces. `ContextSmith.Retrieval.Local` and `ContextSmith.Persistence.Local`
hold the in-memory and file-backed implementations.

A future SQLite or PostgreSQL implementation can use the same two
interfaces. It does not need interface changes. A likely schema:

```text
documents(id TEXT PRIMARY KEY, content TEXT, updated_at TIMESTAMP)
chunks(id TEXT PRIMARY KEY, document_id TEXT, text TEXT, heading_path TEXT, source_id TEXT, location TEXT)
embeddings(chunk_id TEXT PRIMARY KEY REFERENCES chunks(id), vector BLOB, dimension INTEGER)
```

SQLite has no native vector column. A first implementation can store the
vector as a packed `BLOB` and score matches in application code, the same
way `InMemoryRetrievalService` and `FileRetrievalService` do today.
PostgreSQL can use the `pgvector` extension for a native vector column and
index instead.

Configuration selects the active backend at startup through `Storage:Provider`
(`InMemory` or `File` today). Adding a database backend means adding one more
case to this switch, not changing the interfaces or their callers.

## Relationship with Azure

ContextSmith does not replace Azure Document Intelligence, Microsoft Foundry, or Azure AI Search.

These services already provide important production capabilities. For example, Azure Document Intelligence can extract text, layout, paragraph roles, and tables. Azure AI Search can perform chunking, vectorization, indexing, and retrieval.

ContextSmith works at a different boundary. It defines the document model, processing contracts, strategy selection, provenance, and evaluation above individual service implementations.

Azure can be the first implementation for a service when it gives a fast and reliable delivery path. This is useful for business applications that need managed identity, managed scaling, security controls, and operational support.

The application design remains service-agnostic. A developer must be able to replace an Azure implementation with a local or custom implementation when this is useful.

For example:

| Capability | First managed implementation | Possible alternative |
| --- | --- | --- |
| PDF layout extraction | Azure Document Intelligence | Local PDF parser or custom service |
| DOCX parsing | Open XML SDK | Azure Document Intelligence or custom parser |
| Semantic enrichment | Azure OpenAI / Foundry model | Local model or another LLM provider |
| Embeddings | Azure OpenAI embedding model | Local embedding model or custom provider |
| Search and vector retrieval | Azure AI Search | Elasticsearch, OpenSearch, or local vector store |
| MCP hosting | Azure Container Apps | Local ASP.NET Core host or another container platform |

This design allows two useful modes.

### Managed mode

Managed mode uses Azure services for fast implementation and deployment.

```text
Source Document
      ↓
ContextSmith
      ↓
Azure managed services
      ↓
Prepared AI context
```

### Controlled mode

Controlled mode replaces one or more managed services with local or custom implementations.

```text
Source Document
      ↓
ContextSmith contracts
      ↓
Local / custom services
      ↓
Prepared AI context
```

The same domain model and application use cases should work in both modes.

## Model Context Protocol

ContextSmith exposes selected application capabilities through the Model Context Protocol (MCP).

MCP is an interface to the application. MCP does not contain the document-processing business logic.

```text
MCP Client / AI Agent
         │
         ▼
   ContextSmith MCP
         │
         ▼
Application Services
         │
         ▼
 ContextSmith Domain
```

### Tools

A tool performs an operation and returns an operation result.

| Tool | Goal |
| --- | --- |
| `analyze_document` | Inspect a document and report its structure, metadata, and processing warnings. |
| `prepare_document` | Convert a source document into the normalized model that later stages can use. |
| `create_chunks` | Create retrieval chunks with the selected chunking strategy. |
| `evaluate_chunks` | Measure chunk quality and compare chunking strategies. |
| `create_embeddings` | Create vector embeddings with the selected embedding model. |
| `index_document` | Store chunks, metadata, and vectors in the selected search service. |
| `search_knowledge` | Retrieve relevant chunks from the selected search service. |

The first implementation can expose only the tools that are needed for the first vertical slice.

### Resources

A resource gives an MCP client read-only access to reusable application data.

| Resource | Goal |
| --- | --- |
| `contextsmith://documents/{documentId}` | Return the metadata and processing state for one document. |
| `contextsmith://documents/{documentId}/structure` | Return the canonical hierarchy for one document. |
| `contextsmith://documents/{documentId}/analysis` | Return analysis results and warnings for one document. |
| `contextsmith://documents/{documentId}/chunks` | Return the generated chunks for one document. |
| `contextsmith://schemas/document` | Return the current canonical document schema. |
| `contextsmith://profiles/chunking` | Return the available chunking profiles and their settings. |

Resource parameters:

- `{documentId}` is the unique ContextSmith identifier for a processed document.

Later resources can add parameters such as `{strategyId}` or `{modelId}`. Each parameter must have one documented meaning.

### Prompts

A prompt gives the user or MCP client a reusable instruction for a common workflow.

| Prompt | Goal |
| --- | --- |
| `analyze-document-structure` | Guide an agent to inspect document structure before it changes or indexes content. |
| `prepare-document-for-rag` | Guide an agent through document preparation, chunk creation, and quality checks. |
| `review-chunk-quality` | Guide an agent to inspect generated chunks and report likely retrieval problems. |

Application rules must not exist only inside prompts. The application must enforce required rules in code.

## Service-agnostic architecture

The core domain does not depend on Azure, MCP, SharePoint, or a specific model provider.

```text
Presentation
  ├── MCP
  └── API
        │
        ▼
Application
        │
        ▼
Domain
        ▲
        │
Infrastructure
  ├── TXT / Markdown parser
  ├── DOCX / Open XML
  ├── PDF parser / Document Intelligence
  ├── LLM services
  ├── embedding services
  ├── search services
  ├── storage services
  └── document sources
```

The intended dependency direction is:

```text
Presentation ──> Application ──> Domain
Infrastructure ────────────────> Application
Infrastructure ────────────────> Domain
```

The Domain project must not reference Azure SDKs, the MCP SDK, Open XML, or SharePoint APIs.
