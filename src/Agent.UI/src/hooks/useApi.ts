import type { CompiledMemory, SearchResult, StateResponse, DebugItem, ItemDetail, ReplayResult, ProvenanceResponse, AdminActionResponse } from '../types';

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

  const put = async <T>(url: string, data: any): Promise<T> => {
    const response = await fetch(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${await response.text()}`);
    }
    return response.json();
  };

  const del = async <T>(url: string, data: any): Promise<T> => {
    const response = await fetch(url, {
      method: 'DELETE',
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

    // Admin: Trigger replay (v2.0)
    triggerReplay: async (maxSequence?: number): Promise<ReplayResult> => {
      return post(`${API_BASE}/admin/replay`, { ns: NS, maxSequence });
    },

    // Admin CRUD (v5.0)
    createRule: async (itemId: string, title: string, content: string, labels: string[], adminUserId: string, reason?: string): Promise<AdminActionResponse> => {
      return post(`${API_BASE}/admin/rules`, {
        ns: NS,
        item_id: itemId,
        title,
        content,
        labels,
        admin_user_id: adminUserId,
        reason,
      });
    },

    updateRule: async (itemId: string, title: string, content: string, labels: string[], expectedVersion: number | null, adminUserId: string, reason?: string): Promise<AdminActionResponse> => {
      return put(`${API_BASE}/admin/rules/${encodeURIComponent(itemId)}?ns=${NS}`, {
        title,
        content,
        labels,
        expected_version: expectedVersion,
        admin_user_id: adminUserId,
        reason,
      });
    },

    deleteRule: async (itemId: string, adminUserId: string, reason?: string): Promise<AdminActionResponse> => {
      return del(`${API_BASE}/admin/rules/${encodeURIComponent(itemId)}?ns=${NS}`, {
        admin_user_id: adminUserId,
        reason,
      });
    },
  };
}
