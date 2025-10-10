import type { CompiledMemory, SearchResult, StateResponse, DebugItem, ItemDetail, ProvenanceResponse } from '../types';

export function useApi() {
  const API_BASE = '/api/v1';
  const NS = 'ravvnen.consulting';

  const get = async <T>(url: string): Promise<T> => {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${await response.text()}`);
    }
    return response.json();
  };

  const post = async <T>(url: string, data: any): Promise<T> => {
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${await response.text()}`);
    }
    return response.json();
  };

  return {
    // Compile memory from TOOL API
    compileMemory: async (prompt: string, topK: number = 6): Promise<CompiledMemory> => {
      return post(`${API_BASE}/compile-memory`, { prompt, topK, ns: NS });
    },

    // Get state
    getState: async (): Promise<StateResponse> => {
      return get(`${API_BASE}/state?ns=${NS}`);
    },

    // Get provenance (why)
    getWhy: async (id: string): Promise<ProvenanceResponse> => {
      return get(`${API_BASE}/why?id=${encodeURIComponent(id)}&ns=${NS}`);
    },

    // Search rules
    search: async (query: string, k: number = 12): Promise<SearchResult[]> => {
      return get(`${API_BASE}/search?q=${encodeURIComponent(query)}&k=${k}&ns=${NS}`);
    },

    // Debug endpoints
    getDebugItems: async (): Promise<DebugItem[]> => {
      return get(`${API_BASE}/debug/items?ns=${NS}`);
    },

    getDebugItem: async (id: string): Promise<ItemDetail> => {
      return get(`${API_BASE}/debug/item/${encodeURIComponent(id)}?ns=${NS}`);
    },
  };
}
