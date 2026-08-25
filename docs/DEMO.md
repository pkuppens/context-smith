# Demo

Read this file to run the end-to-end demo: upload a document or paste a
URL, watch it get parsed, then ask questions about it in a chat window,
grounded in the document's own text and answered by a local Ollama model.

See `docs/PREREQUISITES.md` for the tools this needs (.NET SDK, Node.js,
Ollama) before starting.

## What the demo shows

- `ContextSmith.Api` parses an uploaded file or a fetched URL into the
  canonical document model (issues #2-#4, #13), chunks it
  (`StructureAwareChunker`, issue #6), embeds each chunk with Ollama, and
  indexes it in memory (issue #14).
- The Angular app in `web/` uploads the document, shows its heading
  outline, and offers a chat box. The three MCP prompts from
  `ContextSmith.Mcp` (issue #7) appear as clickable sample-prompt buttons —
  they are the same `PromptCatalog` entries the MCP server itself exposes,
  not a separate copy.
- Asking a question embeds it, retrieves the most similar chunks for that
  document, and sends them as context to a local Ollama chat model. The
  answer is shown together with the source chunks it was grounded in.

## Steps

1. Start Ollama, if it is not already running (see `docs/PREREQUISITES.md`
   for pulling the two models this demo uses).

2. Start `ContextSmith.Api` from the repository root:

   ```bash
   dotnet run --project src/ContextSmith.Api
   ```

   It listens on `http://localhost:5010` by default (the `http` launch
   profile in `src/ContextSmith.Api/Properties/launchSettings.json`).

3. Start the Angular dev server, in a second terminal. Use a Node.js
   version the Angular CLI accepts (see `docs/PREREQUISITES.md`):

   ```bash
   fnm use 24.19.0   # or any Node.js 22.22.3+/24.15.0+/26+
   cd web
   npm install       # first time only
   npm start
   ```

   It listens on `http://localhost:4200` and is already configured
   (`app.config.ts` / CORS in `Program.cs`) to talk to the API at
   `http://localhost:5010`.

4. Open `http://localhost:4200` in a browser.

5. Upload a document — either:
   - **Choose file**, and pick one of `samples/documents/sample.txt`,
     `sample.md`, `sample.docx`, or `sample.html`; or
   - paste a URL (any public `http(s)` page) into the URL field and click
     **Fetch**.

   The document's heading outline and section/heading/paragraph counts
   appear once parsing finishes.

6. Ask a question in the chat box, or click one of the **Sample prompts**
   buttons to fill the box with an MCP prompt template (with the current
   file name / document id already substituted in). Press Enter or click
   **Send**.

   The answer appears together with the chunks it cited — each one shows
   its heading path (e.g. `Employee Handbook > Leave Policy > Parental
   Leave`) so the answer's provenance is visible, not just its text.

## What it looks like

![Screenshot of the demo: uploaded document outline on the left, chat
answer with source chunks on the right](images/demo-screenshot.png)

## Troubleshooting

- **Chat requests hang or time out.** Ollama is not running, or the
  configured chat model is not pulled. Check `ollama list` and
  `docs/PREREQUISITES.md`.
- **Upload succeeds but the outline is empty.** The fetched/uploaded
  content had no headings or paragraphs the parser could map — try a
  different fixture or URL.
- **Browser console shows a CORS error.** The Angular dev server is not
  running on `http://localhost:4200`, or the API is not running on
  `http://localhost:5010` — `Program.cs` only allows that one origin.
- **`ng serve` refuses to start ("requires a minimum Node.js version").**
  The active Node.js is too old for this Angular CLI version — switch with
  `fnm use 24.19.0` (or install a newer version) rather than lowering the
  Angular CLI version.
