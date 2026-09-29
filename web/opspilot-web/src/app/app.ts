import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { ChatService } from './chat.service';
import { DEV_USERS, DevUser } from './dev-tokens';
import { ChatEvent, ChatMessageDto, ChatTurn, MeResponse } from './models';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly chat = inject(ChatService);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private abort?: AbortController;

  readonly users = DEV_USERS;
  readonly user = signal<DevUser>(DEV_USERS[0]);
  readonly me = signal<MeResponse | null>(null);
  readonly turns = signal<ChatTurn[]>([]);
  readonly draft = signal('');
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly suggestions = [
    'Why did order 123 fail?',
    "Summarize today's payment failures.",
    'What should we do about gateway_timeout failures?',
    'How many DK-900 docking stations are available?',
    'Reserve 2 units of DK-900 for order 123 while the payment is retried.',
    'Retry the payment for order 123.',
  ];

  constructor() {
    void this.loadMe();
  }

  async selectUser(name: string): Promise<void> {
    const next = this.users.find((u) => u.name === name);
    if (!next) return;
    this.stop();
    this.user.set(next);
    this.turns.set([]);
    await this.loadMe();
  }

  async loadMe(): Promise<void> {
    try {
      this.me.set(await this.chat.me(this.user().token));
      this.error.set(null);
    } catch (e) {
      this.me.set(null);
      this.error.set(e instanceof Error ? e.message : String(e));
    }
  }

  onEnter(event: Event): void {
    const e = event as KeyboardEvent;
    if (e.shiftKey) return;
    e.preventDefault();
    void this.send();
  }

  async send(text?: string): Promise<void> {
    const question = (text ?? this.draft()).trim();
    if (!question || this.busy()) return;

    const history = this.turns()
      .filter((t) => t.status === 'done' && t.answer)
      .flatMap((t): ChatMessageDto[] => [
        { role: 'user', content: t.question },
        { role: 'assistant', content: t.answer },
      ]);

    this.draft.set('');
    this.turns.update((ts) => [
      ...ts,
      { question, answer: '', tools: [], citations: [], unknownCitations: [], status: 'streaming' },
    ]);
    this.busy.set(true);
    this.abort = new AbortController();

    try {
      const messages: ChatMessageDto[] = [...history, { role: 'user', content: question }];
      for await (const evt of this.chat.stream(messages, this.user().token, this.abort.signal)) {
        this.apply(evt);
      }
      this.patchLast((t) => (t.status === 'streaming' ? { ...t, status: 'done' } : t));
    } catch (e) {
      const aborted = e instanceof DOMException && e.name === 'AbortError';
      this.patchLast((t) => ({
        ...t,
        status: aborted ? 'done' : 'error',
        error: aborted ? undefined : e instanceof Error ? e.message : String(e),
      }));
    } finally {
      this.busy.set(false);
      this.abort = undefined;
    }
  }

  stop(): void {
    this.abort?.abort();
  }

  json(value: unknown): string {
    return JSON.stringify(value ?? {}, null, 1);
  }

  private apply(evt: ChatEvent): void {
    switch (evt.type) {
      case 'delta':
        this.patchLast((t) => ({ ...t, answer: t.answer + evt.data.text }));
        break;
      case 'tool':
        this.patchLast((t) => ({ ...t, tools: [...t.tools, evt.data] }));
        break;
      case 'grounding':
        this.patchLast((t) => ({
          ...t,
          citations: evt.data.citations,
          unknownCitations: evt.data.unknownCitations,
        }));
        break;
      case 'done':
        this.patchLast((t) => ({ ...t, status: 'done', usage: evt.data }));
        break;
      case 'error':
        this.patchLast((t) => ({ ...t, status: 'error', error: evt.data.message }));
        break;
    }
  }

  private patchLast(fn: (t: ChatTurn) => ChatTurn): void {
    this.turns.update((ts) => (ts.length ? [...ts.slice(0, -1), fn(ts[ts.length - 1])] : ts));
    requestAnimationFrame(() => {
      const el = this.scroller()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    });
  }
}
