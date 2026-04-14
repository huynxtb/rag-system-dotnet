import { FormEvent, KeyboardEvent, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { api, ChatSession, ChatSessionSummary } from '../api';

export default function ChatPage() {
  const { t } = useTranslation();
  const [sessions, setSessions] = useState<ChatSessionSummary[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [active, setActive] = useState<ChatSession | null>(null);
  const [input, setInput] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const loadSessions = async () => {
    const { data } = await api.get<ChatSessionSummary[]>('/chat/sessions');
    setSessions(data);
    return data;
  };

  const loadSession = async (id: string) => {
    const { data } = await api.get<ChatSession>(`/chat/sessions/${id}`);
    setActive(data);
    setActiveId(id);
  };

  useEffect(() => {
    loadSessions().then((list) => {
      if (list.length > 0 && !activeId) loadSession(list[0].id);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Scroll to newest message.
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [active?.messages.length, busy]);

  // Auto-resize the composer textarea.
  useEffect(() => {
    const ta = textareaRef.current;
    if (!ta) return;
    ta.style.height = 'auto';
    ta.style.height = `${Math.min(ta.scrollHeight, 180)}px`;
  }, [input]);

  const newChat = async () => {
    setError(null);
    const { data } = await api.post<ChatSession>('/chat/sessions');
    setActive(data);
    setActiveId(data.id);
    await loadSessions();
    textareaRef.current?.focus();
  };

  const deleteSession = async (id: string) => {
    if (!confirm(t('chat.deleteConfirm'))) return;
    await api.delete(`/chat/sessions/${id}`);
    const next = await loadSessions();
    if (activeId === id) {
      if (next.length > 0) loadSession(next[0].id);
      else { setActive(null); setActiveId(null); }
    }
  };

  const send = async (e?: FormEvent) => {
    e?.preventDefault();
    if (!input.trim() || busy) return;
    setError(null);

    // Ensure a session exists.
    let sessionId = activeId;
    if (!sessionId) {
      const { data } = await api.post<ChatSession>('/chat/sessions');
      sessionId = data.id;
      setActive(data);
      setActiveId(sessionId);
    }

    const question = input.trim();
    setInput('');

    // Optimistic user message so the UI feels instant.
    setActive((prev) => prev ? {
      ...prev,
      messages: [...prev.messages, {
        role: 'User', content: question, createdAt: new Date().toISOString(), sources: []
      }]
    } : prev);

    setBusy(true);
    try {
      const { data } = await api.post<ChatSession>(
        `/chat/sessions/${sessionId}/messages`,
        { question, topK: 5 }
      );
      setActive(data);
      await loadSessions();
    } catch (err: any) {
      setError(err.response?.data?.error || t('errors.generic'));
      // Roll back the optimistic user message on failure.
      setActive((prev) => prev ? { ...prev, messages: prev.messages.slice(0, -1) } : prev);
    } finally {
      setBusy(false);
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  };

  const headerTitle = useMemo(() => active?.title ?? t('chat.title'), [active, t]);

  return (
    <div className="chat-layout">
      {/* Sidebar */}
      <aside className="chat-sidebar">
        <div className="chat-sidebar-header">
          <span>{t('nav.chat')}</span>
          <button className="btn" onClick={newChat} style={{ padding: '4px 10px', fontSize: 12 }}>
            + {t('chat.newChat')}
          </button>
        </div>
        <div className="chat-sidebar-list">
          {sessions.length === 0 && (
            <div style={{ padding: '12px 4px', fontSize: 12, color: '#9ca3af' }}>
              {t('chat.noSessions')}
            </div>
          )}
          {sessions.map((s) => (
            <div
              key={s.id}
              className={`session-item ${activeId === s.id ? 'active' : ''}`}
              onClick={() => loadSession(s.id)}
            >
              <span className="title">{s.title}</span>
              <button
                className="delete-btn"
                title={t('chat.delete')}
                onClick={(e) => { e.stopPropagation(); deleteSession(s.id); }}
              >
                ×
              </button>
            </div>
          ))}
        </div>
      </aside>

      {/* Main conversation */}
      <section className="chat-main">
        <div className="chat-header">{headerTitle}</div>

        <div className="chat-messages">
          {(!active || active.messages.length === 0) && !busy && (
            <div className="chat-empty">{t('chat.emptyState')}</div>
          )}

          {active?.messages.map((m, i) => (
            <div key={i} className={`msg-row ${m.role === 'User' ? 'user' : 'assistant'}`}>
              <div>
                <div className="msg-bubble">{m.content}</div>
                {m.role === 'Assistant' && m.sources.length > 0 && (
                  <div className="msg-sources">
                    {m.sources.map((s, j) => (
                      <span
                        key={j}
                        className="msg-source-chip"
                        title={s.snippet}
                      >
                        [{j + 1}] {s.documentType} v{s.version} · {s.score.toFixed(2)}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ))}

          {busy && (
            <div className="msg-row assistant">
              <div className="msg-bubble">
                <span className="dot-typing"><span/><span/><span/></span>
              </div>
            </div>
          )}

          <div ref={bottomRef} />
        </div>

        {error && <div className="error" style={{ margin: '0 20px' }}>{error}</div>}

        <form className="chat-composer" onSubmit={send}>
          <textarea
            ref={textareaRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={t('chat.placeholder')}
            rows={1}
            disabled={busy}
          />
          <button className="btn" type="submit" disabled={busy || !input.trim()}>
            {busy ? t('chat.thinking') : t('chat.send')}
          </button>
        </form>
      </section>
    </div>
  );
}
