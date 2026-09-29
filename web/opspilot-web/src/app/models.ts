export type Role = 'user' | 'assistant';

export interface ChatMessageDto {
  role: Role;
  content: string;
}

export interface Citation {
  id: string;
  source: string;
  heading: string;
  snippet: string;
}

export interface ToolCall {
  name: string;
  arguments: Record<string, unknown> | null;
}

export interface Usage {
  inputTokens?: number | null;
  outputTokens?: number | null;
}

export interface MeResponse {
  name: string | null;
  roles: string[];
  tools: string[];
}

export interface ChatTurn {
  question: string;
  answer: string;
  tools: ToolCall[];
  citations: Citation[];
  unknownCitations: string[];
  status: 'streaming' | 'done' | 'error';
  error?: string;
  usage?: Usage;
}

export type ChatEvent =
  | { type: 'delta'; data: { text: string } }
  | { type: 'tool'; data: ToolCall }
  | { type: 'grounding'; data: { citations: Citation[]; unknownCitations: string[] } }
  | { type: 'done'; data: Usage }
  | { type: 'error'; data: { message: string } };
