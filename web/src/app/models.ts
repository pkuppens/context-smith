export interface DocumentOutlineNode {
  title: string | null;
  level: number;
  children: DocumentOutlineNode[];
}

export interface DocumentStructureSummary {
  documentId: string;
  sectionCount: number;
  headingCount: number;
  paragraphCount: number;
  outline: DocumentOutlineNode;
}

export interface PromptDefinition {
  name: string;
  goal: string;
  template: string;
}

export interface ChatSource {
  headingPath: string[];
  text: string;
}

export interface ChatResponse {
  answer: string;
  sources: ChatSource[];
}

export interface ChatMessageVm {
  role: 'user' | 'assistant';
  text: string;
  sources?: ChatSource[];
}
