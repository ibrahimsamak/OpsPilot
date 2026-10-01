import { Injectable } from '@angular/core';
import { ChatEvent, ChatMessageDto, MeResponse } from './models';

export const API_BASE = 'http://localhost:5102';

@Injectable({ providedIn: 'root' })
export class ChatService {
  async me(token: string): Promise<MeResponse> {
    const res = await fetch(`${API_BASE}/api/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      throw new Error(`GET /api/me returned ${res.status}. Is the token valid and does the user have a role?`);
    }
    return (await res.json()) as MeResponse;
  }

  async *stream(messages: ChatMessageDto[], token: string, signal: AbortSignal): AsyncGenerator<ChatEvent> {
    const res = await fetch(`${API_BASE}/api/chat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'text/event-stream',
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ messages }),
      signal,
    });

    if (!res.ok || !res.body) {
      const detail = await res.text().catch(() => '');
      throw new Error(`POST /api/chat returned ${res.status} ${detail}`);
    }

    const reader = res.body.pipeThrough(new TextDecoderStream()).getReader();
    let buffer = '';

    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      buffer += value.replace(/\r\n/g, '\n');

      let boundary: number;
      while ((boundary = buffer.indexOf('\n\n')) >= 0) {
        const block = buffer.slice(0, boundary);
        buffer = buffer.slice(boundary + 2);
        const evt = parseSseBlock(block);
        if (evt) yield evt;
      }
    }
  }
}

function parseSseBlock(block: string): ChatEvent | null {
  let type = 'message';
  const data: string[] = [];
  for (const line of block.split('\n')) {
    if (line.startsWith('event:')) type = line.slice(6).trim();
    else if (line.startsWith('data:')) data.push(line.slice(5).trimStart());
  }
  if (data.length === 0) return null;
  return { type, data: JSON.parse(data.join('\n')) } as ChatEvent;
}
