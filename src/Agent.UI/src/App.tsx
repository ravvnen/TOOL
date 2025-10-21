import { useState, useEffect, useRef } from 'react';
import { useApi } from './hooks/useApi';
import { useOllama } from './hooks/useOllama';
import type { DebugItem, StateResponse, SearchResult, CompiledMemory, Message, ReplayResult, ProvenanceResponse } from './types';

function App() {
  const [tab, setTab] = useState<'chat' | 'state' | 'items' | 'search' | 'compile' | 'replay'>('chat');
  const [state, setState] = useState<StateResponse | null>(null);
  const [items, setItems] = useState<DebugItem[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
  const [compilePrompt, setCompilePrompt] = useState('');
  const [compiledMemory, setCompiledMemory] = useState<CompiledMemory | null>(null);
  const [replayResult, setReplayResult] = useState<ReplayResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Provenance modal state
  const [showProvenance, setShowProvenance] = useState(false);
  const [provenance, setProvenance] = useState<ProvenanceResponse | null>(null);
  const [provenanceLoading, setProvenanceLoading] = useState(false);

  // Chat state
  const [messages, setMessages] = useState<Message[]>([]);
  const [prompt, setPrompt] = useState('');
  const chatEndRef = useRef<HTMLDivElement>(null);

  const api = useApi();
  const ollama = useOllama();

  const loadState = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await api.getState();
      setState(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const loadItems = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await api.getDebugItems();
      setItems(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async () => {
    if (!searchQuery.trim()) return;
    try {
      setLoading(true);
      setError(null);
      const data = await api.search(searchQuery, 10);
      setSearchResults(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const handleCompile = async () => {
    if (!compilePrompt.trim()) return;
    try {
      setLoading(true);
      setError(null);
      const data = await api.compileMemory(compilePrompt, 6);
      setCompiledMemory(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const handleReplay = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await api.triggerReplay();
      setReplayResult(result);

      // Also refresh state to compare hashes
      const currentState = await api.getState();
      setState(currentState);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  const handleSendMessage = async () => {
    if (!prompt.trim() || loading) return;

    const userMessage: Message = { role: 'user', content: prompt };
    setMessages((m) => [...m, userMessage]);
    setPrompt('');
    setLoading(true);
    setError(null);

    try {
      // 1. Compile memory from TOOL
      const memory = await api.compileMemory(prompt, 6);

      // 2. Call local Ollama with compiled memory
      const response = await ollama.chat(prompt, memory, 'llama3', 0.2);

      const botMessage: Message = {
        role: 'assistant',
        content: response,
        memory,
      };
      setMessages((m) => [...m, botMessage]);
    } catch (e) {
      const errorMsg = e instanceof Error ? e.message : 'Unknown error';
      setError(errorMsg);
      const botMessage: Message = {
        role: 'assistant',
        content: `❌ Error: ${errorMsg}`,
      };
      setMessages((m) => [...m, botMessage]);
    } finally {
      setLoading(false);
    }
  };

  const handleShowProvenance = async (id: string) => {
    try {
      setProvenanceLoading(true);
      setShowProvenance(true);
      const data = await api.getWhy(id);
      setProvenance(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unknown error');
      setShowProvenance(false);
    } finally {
      setProvenanceLoading(false);
    }
  };

  useEffect(() => {
    if (tab === 'state') loadState();
    if (tab === 'items') loadItems();
  }, [tab]);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  return (
    <div className="container">
      <h1 style={{ marginBottom: '24px', fontSize: '2em' }}>Agent.UI Debug Interface</h1>

      <div className="tabs">
        <div className={`tab ${tab === 'chat' ? 'active' : ''}`} onClick={() => setTab('chat')}>
          Chat
        </div>
        <div className={`tab ${tab === 'state' ? 'active' : ''}`} onClick={() => setTab('state')}>
          State
        </div>
        <div className={`tab ${tab === 'items' ? 'active' : ''}`} onClick={() => setTab('items')}>
          Debug Items
        </div>
        <div className={`tab ${tab === 'search' ? 'active' : ''}`} onClick={() => setTab('search')}>
          Search
        </div>
        <div className={`tab ${tab === 'compile' ? 'active' : ''}`} onClick={() => setTab('compile')}>
          Compile Memory
        </div>
        <div className={`tab ${tab === 'replay' ? 'active' : ''}`} onClick={() => setTab('replay')}>
          Replay (Admin)
        </div>
      </div>

      {error && (
        <div className="card" style={{ background: '#7f1d1d', marginBottom: '16px' }}>
          <strong>Error:</strong> {error}
        </div>
      )}

      {/* CHAT TAB */}
      {tab === 'chat' && (
        <div className="card" style={{ height: 'calc(100vh - 200px)', display: 'flex', flexDirection: 'column' }}>
          <h2 style={{ marginBottom: '16px' }}>Chat with Local LLM + Memory</h2>

          <div style={{ flex: 1, overflowY: 'auto', marginBottom: '16px', padding: '8px' }}>
            {messages.map((msg, i) => (
              <div key={i} style={{ marginBottom: '16px' }}>
                <div className={msg.role === 'user' ? 'bubble user' : 'bubble bot'}>
                  <strong>{msg.role === 'user' ? 'You' : 'Assistant'}:</strong>
                  <div style={{ marginTop: '4px', whiteSpace: 'pre-wrap' }}>{msg.content}</div>
                </div>
                {msg.memory && (
                  <details style={{ marginTop: '8px', marginLeft: msg.role === 'user' ? '0' : '20px' }}>
                    <summary style={{ cursor: 'pointer', color: '#22d3ee', fontSize: '0.9em' }}>
                      Memory Context ({msg.memory.rules.length} rules)
                    </summary>
                    <div style={{ marginTop: '8px', background: '#1e293b', padding: '12px', borderRadius: '8px' }}>
                      {msg.memory.rules.map((rule, j) => (
                        <div key={j} style={{ marginBottom: '12px' }}>
                          <div style={{ fontWeight: 700, color: '#22d3ee' }}>
                            <span
                              style={{ cursor: 'pointer', textDecoration: 'underline' }}
                              onClick={() => handleShowProvenance(rule.id)}
                            >
                              {rule.title}
                            </span>
                            <span className="tag" style={{ marginLeft: '8px' }}>{rule.id}</span>
                          </div>
                          <pre className="rule" style={{ fontSize: '11px', marginTop: '4px' }}>
                            {rule.content}
                          </pre>
                        </div>
                      ))}
                    </div>
                  </details>
                )}
              </div>
            ))}
            {messages.length === 0 && (
              <div className="empty" style={{ marginTop: '40px' }}>
                Send a message to start chatting with the LLM + compiled memory
              </div>
            )}
            <div ref={chatEndRef} />
          </div>

          <div className="row" style={{ marginTop: 'auto' }}>
            <input
              className="input"
              placeholder="Type your message..."
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && !loading && handleSendMessage()}
              disabled={loading}
            />
            <button className="btn" onClick={handleSendMessage} disabled={loading || !prompt.trim()}>
              {loading ? 'Sending...' : 'Send'}
            </button>
          </div>
        </div>
      )}

      {/* STATE TAB */}
      {tab === 'state' && (
        <div className="card">
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: '16px' }}>
            <h2>Database State</h2>
            <button className="btn" onClick={loadState} disabled={loading}>
              {loading ? 'Loading...' : 'Refresh'}
            </button>
          </div>
          {state && (
            <div>
              <div className="row" style={{ gap: '24px', marginTop: '16px' }}>
                <div>
                  <div className="muted">Namespace</div>
                  <div style={{ fontSize: '1.2em', fontWeight: 700 }}>{state.ns}</div>
                </div>
                <div>
                  <div className="muted">Active Items</div>
                  <div style={{ fontSize: '1.2em', fontWeight: 700 }}>{state.activeItems}</div>
                </div>
                <div>
                  <div className="muted">IM Hash</div>
                  <code className="small">{state.imHash}</code>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ITEMS TAB */}
      {tab === 'items' && (
        <div className="card">
          <div className="row" style={{ justifyContent: 'space-between', marginBottom: '16px' }}>
            <h2>All Items in Database</h2>
            <button className="btn" onClick={loadItems} disabled={loading}>
              {loading ? 'Loading...' : 'Refresh'}
            </button>
          </div>
          <div style={{ overflowX: 'auto' }}>
            <table>
              <thead>
                <tr>
                  <th>Item ID</th>
                  <th>Version</th>
                  <th>Title</th>
                  <th>Active</th>
                  <th>Preview</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item, i) => (
                  <tr key={i}>
                    <td>
                      <code className="small">{item.item_id}</code>
                    </td>
                    <td>{item.version}</td>
                    <td>{item.title}</td>
                    <td>{item.is_active ? '✅' : '❌'}</td>
                    <td style={{ maxWidth: '400px' }}>
                      <code className="small">{item.preview}</code>
                    </td>
                    <td>
                      <button
                        className="btn"
                        style={{ fontSize: '0.85em', padding: '4px 8px' }}
                        onClick={() => handleShowProvenance(item.item_id)}
                      >
                        Why?
                      </button>
                    </td>
                  </tr>
                ))}
                {items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="empty">
                      No items found
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* SEARCH TAB */}
      {tab === 'search' && (
        <div className="card">
          <h2 style={{ marginBottom: '16px' }}>Search Rules (FTS)</h2>
          <div className="row" style={{ marginBottom: '16px' }}>
            <input
              className="input"
              placeholder="Enter search query..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            />
            <button className="btn" onClick={handleSearch} disabled={loading || !searchQuery.trim()}>
              {loading ? 'Searching...' : 'Search'}
            </button>
          </div>
          <div className="grid">
            {searchResults.map((result, i) => (
              <div key={i} className="card">
                <div style={{ fontWeight: 700, marginBottom: '8px' }}>
                  {result.title}
                  <span className="tag">
                    {result.item_id}@v{result.version}
                  </span>
                </div>
                <pre className="rule" style={{ fontSize: '12px' }}>
                  {result.content}
                </pre>
              </div>
            ))}
            {searchResults.length === 0 && searchQuery && !loading && (
              <div className="empty">No results found for "{searchQuery}"</div>
            )}
          </div>
        </div>
      )}

      {/* COMPILE MEMORY TAB */}
      {tab === 'compile' && (
        <div className="card">
          <h2 style={{ marginBottom: '16px' }}>Compile Memory (Test MemoryCompiler)</h2>
          <div className="row" style={{ marginBottom: '16px' }}>
            <input
              className="input"
              placeholder="Enter prompt to compile memory..."
              value={compilePrompt}
              onChange={(e) => setCompilePrompt(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleCompile()}
            />
            <button className="btn" onClick={handleCompile} disabled={loading || !compilePrompt.trim()}>
              {loading ? 'Compiling...' : 'Compile'}
            </button>
          </div>
          {compiledMemory && (
            <div>
              <div style={{ marginBottom: '12px' }}>
                <strong>Namespace:</strong> {compiledMemory.ns} &nbsp;|&nbsp;
                <strong>Generated:</strong> {new Date(compiledMemory.generated_at).toLocaleString()} &nbsp;|&nbsp;
                <strong>Rules Found:</strong> {compiledMemory.rules.length}
              </div>
              {compiledMemory.note && (
                <div className="card" style={{ background: '#7f1d1d', marginBottom: '12px' }}>
                  {compiledMemory.note}
                </div>
              )}
              <pre className="rule" style={{ maxHeight: '500px', overflow: 'auto' }}>
                {JSON.stringify(compiledMemory, null, 2)}
              </pre>
            </div>
          )}
        </div>
      )}

      {/* REPLAY TAB (ADMIN) */}
      {tab === 'replay' && (
        <div className="card">
          <h2 style={{ marginBottom: '16px' }}>Replay Engine (Admin / v2.0 H3 Validation)</h2>
          <p style={{ marginBottom: '16px', color: '#94a3b8' }}>
            Trigger a full replay of DELTAS stream to validate State Reconstruction Accuracy (SRA = 1.00).
            This operation creates a fresh database and replays all events from the beginning.
          </p>

          <button className="btn" onClick={handleReplay} disabled={loading}>
            {loading ? 'Replaying...' : 'Trigger Replay'}
          </button>

          {replayResult && state && (
            <div style={{ marginTop: '24px' }}>
              <h3 style={{ marginBottom: '16px' }}>Replay Result</h3>

              <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))' }}>
                <div className="card">
                  <div className="muted">Events Processed</div>
                  <div style={{ fontSize: '1.5em', fontWeight: 700 }}>{replayResult.eventsProcessed}</div>
                </div>

                <div className="card">
                  <div className="muted">Active Rules</div>
                  <div style={{ fontSize: '1.5em', fontWeight: 700 }}>{replayResult.activeCount}</div>
                </div>

                <div className="card">
                  <div className="muted">Replay Time</div>
                  <div style={{ fontSize: '1.5em', fontWeight: 700 }}>{replayResult.replayTimeMs}ms</div>
                </div>
              </div>

              <div style={{ marginTop: '24px' }}>
                <h4 style={{ marginBottom: '12px' }}>Hash Comparison (SRA Validation)</h4>

                <div className="card">
                  <div style={{ marginBottom: '8px' }}>
                    <strong>Live State Hash:</strong>
                    <br />
                    <code className="small">{state.imHash}</code>
                  </div>
                  <div style={{ marginBottom: '8px' }}>
                    <strong>Replayed State Hash:</strong>
                    <br />
                    <code className="small">{replayResult.imHash}</code>
                  </div>
                  <div style={{ marginTop: '16px', padding: '12px', borderRadius: '8px', background: state.imHash === replayResult.imHash ? '#14532d' : '#7f1d1d' }}>
                    <strong style={{ fontSize: '1.2em' }}>
                      {state.imHash === replayResult.imHash ? '✅ SRA = 1.00 (Perfect Match)' : '❌ SRA = 0.00 (Mismatch)'}
                    </strong>
                    <br />
                    {state.imHash === replayResult.imHash ? (
                      <span style={{ color: '#86efac' }}>
                        Replay successfully reconstructed identical state. Event sourcing determinism validated!
                      </span>
                    ) : (
                      <span style={{ color: '#fca5a5' }}>
                        Warning: Replay produced different state. Investigate non-determinism in projection logic.
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <details style={{ marginTop: '24px' }}>
                <summary style={{ cursor: 'pointer', color: '#22d3ee', fontSize: '1em', marginBottom: '8px' }}>
                  Full Replay Result (JSON)
                </summary>
                <pre className="rule" style={{ maxHeight: '400px', overflow: 'auto' }}>
                  {JSON.stringify(replayResult, null, 2)}
                </pre>
              </details>
            </div>
          )}

          {!replayResult && !loading && (
            <div className="empty" style={{ marginTop: '24px' }}>
              Click "Trigger Replay" to validate event sourcing determinism (H3).
            </div>
          )}
        </div>
      )}

      {/* PROVENANCE MODAL */}
      {showProvenance && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            background: 'rgba(0, 0, 0, 0.7)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
          }}
          onClick={() => setShowProvenance(false)}
        >
          <div
            className="card"
            style={{ maxWidth: '800px', maxHeight: '80vh', overflow: 'auto', margin: '20px' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="row" style={{ justifyContent: 'space-between', marginBottom: '16px' }}>
              <h2>Rule Provenance</h2>
              <button className="btn" onClick={() => setShowProvenance(false)}>
                Close
              </button>
            </div>

            {provenanceLoading && <div className="empty">Loading provenance...</div>}

            {!provenanceLoading && provenance && (
              <div>
                <div style={{ marginBottom: '16px' }}>
                  <div className="muted">Rule ID</div>
                  <code className="small">{provenance.id}</code>
                  <span className="tag" style={{ marginLeft: '8px' }}>
                    v{provenance.version}
                  </span>
                </div>

                <div style={{ marginBottom: '16px' }}>
                  <div className="muted">Title</div>
                  <div style={{ fontSize: '1.2em', fontWeight: 700 }}>{provenance.title}</div>
                </div>

                <div style={{ marginBottom: '16px' }}>
                  <div className="muted">Content</div>
                  <pre className="rule" style={{ maxHeight: '300px', overflow: 'auto' }}>
                    {provenance.content}
                  </pre>
                </div>

                <div style={{ marginBottom: '16px' }}>
                  <div className="muted">Git Sources ({provenance.sources.length})</div>
                  {provenance.sources.length === 0 ? (
                    <div className="empty">No source bindings found</div>
                  ) : (
                    <table style={{ marginTop: '8px' }}>
                      <thead>
                        <tr>
                          <th>Repository</th>
                          <th>Ref</th>
                          <th>Path</th>
                          <th>Blob SHA</th>
                        </tr>
                      </thead>
                      <tbody>
                        {provenance.sources.map((src, i) => (
                          <tr key={i}>
                            <td>
                              <code className="small">{src.repo}</code>
                            </td>
                            <td>
                              <code className="small">{src.ref}</code>
                            </td>
                            <td>
                              <code className="small">{src.path}</code>
                            </td>
                            <td>
                              <code className="small">{src.blob_sha.substring(0, 8)}...</code>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
