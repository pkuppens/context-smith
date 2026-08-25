# Research

Read this file for the questions the project must answer and the value it claims over a direct managed pipeline. Update it when a research question is added, answered, or reframed.

## Main research questions

ContextSmith uses implementation and tests to answer practical questions.

- How much document hierarchy can deterministic code preserve or reconstruct?
- When does an LLM add useful semantic information?
- Does an explicit canonical hierarchy improve chunk construction and retrieval?
- Which chunking strategy works best for a given document collection?
- Which embedding model works best for a given retrieval task?
- When is a managed Azure service the best implementation?
- When does a local or custom implementation give more control or better results?
- Which MCP capabilities give an agent enough control without exposing internal implementation details?

## Where ContextSmith can add value above managed document services

ContextSmith does not assume that custom processing is better than Azure processing.

Instead, the project makes the differences measurable.

Important areas include:

1. **Canonical hierarchy.** ContextSmith keeps one explicit hierarchy across all source formats and parsing implementations.
2. **Source-aware parsing.** A DOCX parser can use styles, numbering, and relationships that are specific to Word documents.
3. **Hierarchy-aware chunks.** A chunk can use section paths, parent text, and nearby blocks as explicit context.
4. **Provider comparison.** The same source can be processed by a managed parser and a custom parser.
5. **Strategy comparison.** Different chunking strategies and embedding models can use the same evaluation set.
6. **Provenance.** Source references remain part of the core domain model and not only index metadata.
7. **LLM control.** The application decides which tasks need an LLM and which tasks must remain deterministic.
8. **MCP access.** Agents can use document preparation as a set of clear capabilities instead of one opaque ingestion job.

One central experiment is therefore:

> Does an explicit canonical document hierarchy improve chunk construction, provenance, and retrieval when compared with a direct managed ingestion pipeline?

The answer must come from tests and evaluation data.
