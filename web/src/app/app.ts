import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContextSmithApi } from './context-smith-api';
import { DocumentOutline } from './document-outline';
import {
  ChatMessageVm,
  DocumentStructureSummary,
  PromptDefinition,
} from './models';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DocumentOutline],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  protected readonly document = signal<DocumentStructureSummary | null>(null);
  protected readonly fileName = signal<string | null>(null);
  protected readonly urlInput = signal('');
  protected readonly uploading = signal(false);
  protected readonly uploadError = signal<string | null>(null);

  protected readonly prompts = signal<PromptDefinition[]>([]);
  protected readonly messages = signal<ChatMessageVm[]>([]);
  protected readonly chatInput = signal('');
  protected readonly sending = signal(false);

  constructor(private readonly api: ContextSmithApi) {}

  ngOnInit(): void {
    this.api.prompts().subscribe({
      next: (prompts) => this.prompts.set(prompts),
      error: () => this.prompts.set([]),
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.api.uploadFile(file).subscribe({
      next: (summary) => this.onDocumentReady(file.name, summary),
      error: (err) => this.onUploadError(err),
    });
  }

  submitUrl(): void {
    const url = this.urlInput().trim();
    if (!url) {
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.api.uploadUrl(url).subscribe({
      next: (summary) => this.onDocumentReady(url, summary),
      error: (err) => this.onUploadError(err),
    });
  }

  usePrompt(prompt: PromptDefinition): void {
    const name = this.fileName() ?? '<file name>';
    const id = this.document()?.documentId ?? '<document id>';
    this.chatInput.set(
      prompt.template.replace('{fileName}', name).replace(/{documentId}/g, id),
    );
  }

  sendMessage(): void {
    const message = this.chatInput().trim();
    const doc = this.document();
    if (!message || !doc || this.sending()) {
      return;
    }

    this.messages.update((current) => [...current, { role: 'user', text: message }]);
    this.chatInput.set('');
    this.sending.set(true);

    this.api.chat(doc.documentId, message).subscribe({
      next: (response) => {
        this.messages.update((current) => [
          ...current,
          { role: 'assistant', text: response.answer, sources: response.sources },
        ]);
        this.sending.set(false);
      },
      error: (err) => {
        this.messages.update((current) => [
          ...current,
          { role: 'assistant', text: `Error: ${err.message ?? 'the request failed'}` },
        ]);
        this.sending.set(false);
      },
    });
  }

  private onDocumentReady(name: string, summary: DocumentStructureSummary): void {
    this.fileName.set(name);
    this.document.set(summary);
    this.messages.set([]);
    this.uploading.set(false);
  }

  private onUploadError(err: unknown): void {
    const message = err instanceof Error ? err.message : 'Upload failed.';
    this.uploadError.set(message);
    this.uploading.set(false);
  }
}
