import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChatResponse,
  DocumentStructureSummary,
  PromptDefinition,
} from './models';

const API_BASE_URL = 'http://localhost:5010';

@Injectable({ providedIn: 'root' })
export class ContextSmithApi {
  constructor(private readonly http: HttpClient) {}

  uploadFile(file: File): Observable<DocumentStructureSummary> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<DocumentStructureSummary>(
      `${API_BASE_URL}/api/documents`,
      form,
    );
  }

  uploadUrl(url: string): Observable<DocumentStructureSummary> {
    return this.http.post<DocumentStructureSummary>(
      `${API_BASE_URL}/api/documents`,
      { url },
    );
  }

  chat(documentId: string, message: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${API_BASE_URL}/api/chat`, {
      documentId,
      message,
    });
  }

  prompts(): Observable<PromptDefinition[]> {
    return this.http.get<PromptDefinition[]>(`${API_BASE_URL}/api/prompts`);
  }
}
