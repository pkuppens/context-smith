# Plan

Read this file for the implementation roadmap and current status. Update it when a step starts, finishes, or the step order changes.

## Initial implementation strategy

The project starts with a small working path and extends it in controlled steps.

### Step 1 — Repository and application foundation

Create the GitHub repository, .NET solution, build workflow, tests, coding rules, and architecture documentation.

### Step 2 — Canonical document model

Define the source-independent hierarchy, metadata, and provenance model.

### Step 3 — Text and Markdown

Implement TXT and Markdown parsers first. Use them to test the canonical model with simple inputs.

### Step 4 — DOCX

Implement a DOCX parser with Open XML. Preserve explicit Word structure where it adds meaning.

### Step 5 — PDF

Implement a PDF adapter. Azure Document Intelligence can be the first managed implementation.

### Step 6 — Chunk strategies

Implement at least a fixed-size strategy and a structure-aware strategy. Add a common evaluation contract.

### Step 7 — MCP

Expose selected application use cases as MCP tools. Add resources and prompts when their purpose is clear.

### Step 8 — Embeddings and retrieval

Add more than one embedding option where practical. Add Azure AI Search as the first managed retrieval implementation.

### Step 9 — Azure deployment

Deploy the MCP server and required services on Azure. Keep the same application contracts used during local development.

### Step 10 — Evaluation

Compare document parsing, chunking, embedding, and retrieval choices with repeatable test data.

### Step 11 — Additional Office formats

Add Excel and PowerPoint after the canonical model and evaluation approach are stable.

## Status

ContextSmith is under active development.

The first goal is to create a small and testable document-processing core. Later stages add managed Azure implementations, MCP access, retrieval, deployment, and comparative evaluation.
