export interface MemoryRule {
  id: string;
  title: string;
  content: string;
  provenance: {
    repo: string;
    ref: string;
    path: string;
    blob_sha: string;
  };
}

export interface CompiledMemory {
  ns: string;
  generated_at: string;
  rules: MemoryRule[];
  note?: string;
}

export interface Message {
  role: 'user' | 'assistant';
  content: string;
  memory?: CompiledMemory;
}

export interface SearchResult {
  item_id: string;
  title: string;
  content: string;
  version: number;
}

export interface StateResponse {
  ns: string;
  activeItems: number;
  imHash: string;
}

export interface DebugItem {
  ns: string;
  item_id: string;
  version: number;
  title: string;
  preview: string;
  labels_json: string;
  is_active: number;
  policy_version: string;
  occurred_at: string;
  emitted_at: string;
}

export interface ItemDetail {
  current: any;
  history: any[];
  provenance: any[];
}

export interface SourceBinding {
  repo: string;
  ref: string;
  path: string;
  blob_sha: string;
}

export interface ProvenanceResponse {
  id: string;
  title: string;
  content: string;
  version: number;
  sources: SourceBinding[];
}

export interface ReplayResult {
  ns: string;
  eventsProcessed: number;
  activeCount: number;
  imHash: string;
  replayTimeMs: number;
  startedAt: string;
  completedAt: string;
}