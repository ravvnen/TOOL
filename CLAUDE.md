Multi-Agent Rulebook System

Version: 0.2 — with Admin CRUD & MCP

A system where multiple agents share a consistent, auditable Instruction Memory (IM) (rulebook), support real-time updates, vector retrieval (RAG), explainability, and administrative oversight. Agents may run local LLMs or use external ones, but memory, provenance, and business logic are centralized (or semi-central). MCP (Model Context Protocol) is optionally layered to enable external tool clients/IDE integrations.

Goals & Key Properties
Goal	Description
Consistency	Agents with identical inputs and memory state should produce the same output (especially at temperature = 0).
Freshness	Rule updates (via Git/webhooks) should propagate with low latency to all agents.
Explainability / Auditability	Every answer must be traceable to rule IDs and provenance (commit, repo, path).
Administrative control	Humans (admins) should be able to review, override, add, edit, or delete rules/proposals.
Scalability & modularity	The system should evolve, support multiple clients/agents, split services if needed.
RAG / Vector retrieval	Later integration of embedding-based retrieval to augment lexical rule search.
High-Level Architecture / Components
             GitHub Webhooks / Seeder
                     │
                     ▼
                  EVENTS (JetStream)
                     │
                 Promoter Service
                     │ emits
                     ▼
                DELTAS (JetStream)
                     │
            Projection + Embedding Service
                     │
        Projection DB (rules, history, provenance)
        Vector store / embeddings index
                     │
                     ▼
             Application API & MCP Server
                     │
   ┌──────────────────┼──────────────────────────┐
   ▼                  ▼                          ▼
Agent UI / Local      IDE / MCP Client           Admin UI
(LLM + chat UI)       (could be Claude, Cursor)   (rule management)

Components

Promoter

Subscribes to EVENTS (e.g. evt.seed.proposal.v1, evt.im.proposal.v1)

Runs gating/policy logic

Inserts into promoter DB (versions, seen events)

Emits DELTAS events (im.upsert.v1, im.retract.v1)

Records audit decisions (skip, defer, promote, override)

Projection / Embedding Service

Durable consumer of DELTAS

Maintains:

im_items_current

im_items_history

source_bindings (provenance)

Embedding & vector index:

On upsert: compute embedding & upsert into vector index

On retract: mark or remove vector entry

Must be idempotent (apply same event twice yields same state)

Application API & MCP Server

Exposes REST and/or JSON-RPC (via MCP) endpoints: chat, search, rule retrieval, admin CRUD

Implements tool / resource interface (if MCP)

Handles authentication, authZ (agents vs admins)

Enforces business policies for admin edits, overrides, conflicts

Logs all operations for metrics & audit

Agent UI / Local LLM

UI (React or similar) that:

Presents chat interface

Shows memory injected, allows insertion of rules, etc.

Calls API / MCP server to:

Fetch relevant rules

Compile memory JSON

Submit prompt + memory to LLM

May maintain a local cache (SQLite or in-memory) powered by subscribing to DELTAS (optional, for performance)

Admin UI / Dashboard

Web interface (within Agent UI or separate) that:

Shows pending proposals

Shows rule listing, version history, provenance

Allows admins to add, edit, delete, approve/reject proposals

Diff views, rollback, confirm operations

Calls admin endpoints in the API

Data Model / Schema Sketch
-- Promoter / gating
CREATE TABLE promoter_items (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  is_active INTEGER NOT NULL,
  policy_version TEXT NOT NULL,
  source_repo TEXT NOT NULL,
  source_ref TEXT NOT NULL,
  source_path TEXT NOT NULL,
  source_blob_sha TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  PRIMARY KEY(ns, item_id)
);

CREATE TABLE promoter_item_versions (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  input_event_id TEXT NOT NULL,
  policy_version TEXT NOT NULL,
  source_repo TEXT NOT NULL,
  source_ref TEXT NOT NULL,
  source_path TEXT NOT NULL,
  source_blob_sha TEXT NOT NULL,
  emitted_at TEXT NOT NULL,
  PRIMARY KEY (ns, item_id, version)
);

CREATE TABLE promoter_seen_events (
  ns TEXT NOT NULL,
  event_id TEXT NOT NULL,
  PRIMARY KEY (ns, event_id)
);

CREATE TABLE promoter_audit (
  ns TEXT NOT NULL,
  decision_id TEXT PRIMARY KEY,
  item_id TEXT,
  action TEXT,
  reason_code TEXT,
  reason_detail TEXT,
  policy_version TEXT,
  input_subject TEXT,
  input_hash TEXT,
  prior_version INTEGER,
  prior_hash TEXT,
  new_version INTEGER,
  is_same_hash INTEGER,
  delta_type TEXT,
  delta_subject TEXT,
  delta_msg_id TEXT,
  deltas_stream TEXT,
  deltas_seq INTEGER,
  received_at TEXT,
  decided_at TEXT,
  published_at TEXT,
  latency_ms INTEGER,
  emitted_at TEXT
);

-- Projection / rulebook
CREATE TABLE im_items_current (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  is_active INTEGER NOT NULL,
  policy_version TEXT NOT NULL,
  occurred_at TEXT NOT NULL,
  emitted_at TEXT NOT NULL,
  PRIMARY KEY(ns, item_id)
);

CREATE TABLE im_items_history (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  is_active INTEGER NOT NULL,
  policy_version TEXT NOT NULL,
  occurred_at TEXT NOT NULL,
  emitted_at TEXT NOT NULL,
  PRIMARY KEY(ns, item_id, version)
);

CREATE TABLE source_bindings (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  repo TEXT NOT NULL,
  ref TEXT NOT NULL,
  path TEXT NOT NULL,
  blob_sha TEXT NOT NULL,
  PRIMARY KEY(ns, item_id, version)
);

-- Vector / embeddings (example Postgres + pgvector schema)
CREATE TABLE im_vectors (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  embedding VECTOR,      -- pgvector or equivalent
  doc JSONB,             -- { title, content, labels }
  PRIMARY KEY(ns, item_id, version)
);


You may also add a full-text index or tsvector column for lexical search.

API & MCP Schema
HTTP / REST Endpoints
HTTP Verb	Path	Role / Purpose	Input	Output
POST	/api/v1/webhooks/seed	Receive seeded proposals	proposal JSON	200 OK / error
POST	/api/v1/webhooks/github-event	Receive GitHub events	GitHub payload	200 OK / error
POST	/api/v1/chat	Chat endpoint for UI clients	{ prompt, topK?, temperature?, model? }	{ compiled_memory, llm_response }
GET	/api/v1/state?ns=...	Status: count & hash	—	{ ns, active_items, im_hash }
GET	/api/v1/search?ns& q & k	Keyword / vector query	query params	list of rule snippets
GET	/api/v1/why?ns&id=...	Provenance for a rule	query params	provenance + history
GET	/api/v1/admin/proposals?ns=...	List proposals for admin review	—	list of proposals
GET	/api/v1/admin/proposals/{id}	Detail a proposal	path params	full proposal object
POST	/api/v1/admin/proposals/{id}/approve?ns=...	Admin approve	—	success/failure
POST	/api/v1/admin/proposals/{id}/reject?ns=...	Admin reject	—	success/failure
GET	/api/v1/admin/rules?ns=...	List current rules	—	list of rules
GET	/api/v1/admin/rules/{itemId}?ns=...	Detail rule	path params	full rule + history
POST	/api/v1/admin/rules?ns=...	Create new rule	{ title, content, labels }	created rule
PUT	/api/v1/admin/rules/{itemId}?ns=...	Edit existing rule	{ title, content, labels }	success / error
DELETE	/api/v1/admin/rules/{itemId}?ns=...	Delete or retract rule	—	success / error
GET	/api/v1/debug/items?ns=...	Debug listing	—	list of items
GET	/api/v1/debug/item/{itemId}?ns=...	Debug detail	—	current + history + provenance
MCP Tool / Resource Schema (Sketch)

tools:

search_rules({ ns, q, k }) → list of { item_id, version, title, snippet, labels }

compile_memory({ ns, prompt, k }) → memory JSON { ns, generated_at, rules: [...] }

get_rule({ ns, item_id }) → full rule with provenance

why({ ns, item_id }) → provenance + history

admin_list_proposals({ ns })

admin_approve_proposal({ ns, proposal_id })

admin_reject_proposal({ ns, proposal_id })

admin_create_rule({ ns, title, content, labels })

admin_update_rule({ ns, item_id, title, content, labels })

admin_delete_rule({ ns, item_id })

resources:

im://rules/current → snapshot of all active rules

im://rules/{itemId} → full JSON of a rule + history

mcp://changes/sse → SSE or streaming of recent DELTAS (optional)

prompts:

apply_rulebook_prompt → the system instruction template you inject, which includes memory JSON, demands citations, etc.

Clients discover via tools/list, prompts/list, then invoke tools/call.

Memory / Prompt Composition

Agent (or UI) sends user prompt to API/MCP: { prompt, topK, ns }

API calls compile_memory(ns, prompt, topK)

Internally, service fetches relevant rules (via search/embedding)

Builds JSON:

{
  "ns": "...",
  "generated_at": "...",
  "rules": [
    { "item_id": "foo", "version": 3, "title":"…", "content":"…", "labels":[…] },
    ...
  ]
}


API prepares messages:

System: your system template

System: memory JSON serialized to string

User: prompt

Forward to LLM (local or remote), get response

Return to UI/client, optionally include compiled_memory so UI can show it

Admin / Proposal Workflow

Proposals originate from webhooks (seed, GitHub, etc.).

Promoter’s policy might skip, defer, promote, or audit a proposal.

Deferred or skipped proposals appear in the admin proposal pool.

Admin UI lists them; admin inspects title/content/diff + provenance.

Admin approves or rejects:

On approve: the system re-runs promotion or forcibly emits a DELTAS event

On reject: the system records audit and discards or archives

Once rules are in im_items_current, they’re visible to agents and included in memory.

Admins also should be able to directly create/edit/retract rules, bypassing webhook → proposal path (with audit). All admin actions create audit records.

Metrics & Evaluation Plan (with admin path included)
Consistency

Use the same methodology: (prompt, ns, model, im_hash) → K runs → compare outputs

Now, agents and API/MCP clients can be compared (e.g. “If someone uses the MCP tool vs agent UI, are results the same?”)

Log for each run: memory JSON snapshot, prompt, model version, answer text

Freshness / Latency

Timestamps to measure:

t_merge (Git / seed)

t_emit (Promoter)

t_apply_projection (Projection service)

t_compile (when compile_memory is invoked by agent or MCP)

t_admin_action (when admin approves / edits)

Compute intervals: Δme, Δea, Δma, Δadmin (how long does it take for an admin action to reflect to agents)

Metrics: median, p90, p95, count of misses

Explainability / Audit

Memory JSON includes rule IDs and versions

Answers must cite [im:item@vN]

Validate:

Provenance correctness (PCR): cited IDs exist in memory and match versions

Replay fidelity (RF): memory JSON hash matches stored hash

Admin edits should be linked to audit logs (user, timestamp, diff)

Record all tool calls, admin operations, agent prompt → answer mappings in logs/traces

Deployment & Scaling Strategies

Start monolithic / unified API, modularized. If a domain (e.g. webhooks, chat, admin) becomes heavy, split it later behind a gateway.

Use middleware layers (Auth, Rate Limit, Logging) so cross-cutting concerns are in one place.

Concurrent loads: agent chat traffic vs admin UI vs webhook bursts; monitor and scale accordingly.

PostgreSQL + pgvector / Qdrant as production backing stores for projection DB and vector index (instead of SQLite).

Version your APIs (v1, v2) to allow future evolution without breaking clients.

For admin UI, use real-time updates (SSE / WebSocket) for new proposals.

Backup, migration, audit, replay:

You can always replay DELTAS from start to rebuild projection.

Store audit tables so manual edits + promoter decisions are always traceable.

README + Getting Started (Sketch)

Your root README.md would include:

Architecture overview (diagram + description)

How to run locally: dependencies (NATS, database), seeding, starting promoter, projection, API, UI

Example calls: /api/v1/chat, /api/v1/admin/proposals, /api/v1/search, MCP tool invocation

How to seed rules (via seeder or GitHub webhooks)

Where to inspect audit logs / debug endpoints

How to run metrics / consistency experiments

How to extend: adding new tool endpoints, adding new admin workflows, adding vector search, or splitting services later

You can also include a CHANGELOG section, versioning notes, and examples of JSON payloads.






Thesis Narrative: Integrating Rulebook Memory with Agent Systems
1. Introduction & Motivation

Modern large language models (LLMs) are powerful at generating fluent text, reasoning, and synthesizing knowledge, but they have critical limitations in long-term consistency, traceability, and governance. In practice, teams often need to enforce evolving rules, policies, and compliance constraints (e.g. security rules, business logic, regulatory guidelines). If an LLM “forgets” a rule, or its outputs drift, this can lead to unpredictable or undesirable behavior.

The motivation of your thesis is to bridge the gap between black-box generative LLMs and a more structured, auditable, rule-driven “instruction memory” that agents consult and reason over. You address questions such as:

How can multiple agent instances maintain consistent, fresh, explainable memory over changing rules?

How can we support human oversight / admin editing of rules while preserving auditability and minimal divergence?

How do we measure whether agents remain consistent, responsive, and justifiable under rule changes?

Your system implements a concrete architecture to explore these questions.

2. Problem & Research Gaps

Here are the key problems your thesis seeks to address, and where prior literature is weak or silent.

2.1 Memory Drift & Consistency in Agent Systems
In real-world systems, multiple agents (or even the same agent at different times) may need to reason using the same institutional memory (rules, policies). Without a shared, consistent memory, their responses may diverge. Current LLM systems often embed memory ad hoc (prompts, caches) which is not robust under updates or scaling.

2.2 Lack of Explainability & Traceability in LLM Answers
LLMs are “black boxes.” If an AI system recommends or enforces something, teams need to justify: “why did you decide that?” To support that, system decisions should tie back to explicit rules, provenance (commit, author, version). This is central in fields like Explainable AI (XAI) where transparency and accountability are crucial. 
ScienceDirect
+2
SpringerLink
+2

2.3 Administrative Oversight & Governance
Often, rules evolve, exceptions arise, or humans need to edit or override memory. Systems rarely support clean admin CRUD over policies with audit trails. Integrating human-in-the-loop edits while preserving consistency and audit is a nontrivial design challenge.

2.4 Scalability & Freshness of Rule Propagation
As the rule space grows, or multiple agents spin up, we need a mechanism to propagate rule changes quickly, cheaply, and reliably without reloading entire models or recomputing from scratch. Replay of full logs every query is too expensive.

2.5 Evaluation Methods in Agent-Rule Systems
There is little standard methodology for evaluating multi-agent systems with rulebooks: e.g. how do you measure “consistency across agents,” “freshness of updates,” or “explainability correctness”?

Thus your work fills a gap at the intersection of agent memory architectures, explainable AI, and human governance of AI behavior.

3. Your Contributions

Your system embodies several novel contributions:

Event-Sourced Instruction Memory Architecture

Use of EVENTS → Promoter → DELTAS → Projection pipeline

Idempotent, stateful representation (current + history)

Support for replay and audit

Rule-Injected Agent Containers with Provenance Support

Each agent/container can subscribe to DELTAS (or local DB) and maintain memory

MemoryCompiler injects structured JSON into prompts

LLM outputs cite rule IDs; system can answer /why by looking up provenance

Admin CRUD & Oversight Integration

Admin UI / API endpoints to review, approve, override, add, edit, delete rules

Audit logs record every admin and promoter decision, with rationale and diff

Ensures humans remain in control of institutional memory

Metrics & Experimental Framework for Consistency, Freshness, Explainability

“Task runs” metric comparing K agents (or repeated runs) under identical inputs

Defined metrics: exact-match, normalized edit distance, Jaccard, embedding cosine

Latency metrics: merge → emit → apply → compile

Provenance correctness (PCR) and replay fidelity

Separable / Evolvable System Design

You start with a unified API + modular core, but the architecture supports splitting services later

MCP (Model-Context Protocol) integration allows IDE / external client access

Room for vector/RAG integration (embedding retrieval) as next phase

Together, these provide a working testbed for exploring how structured memory + LLMs + human oversight can co-exist in a controlled, measurable system.

4. How the System Implements the Thesis Architecture

Here is how your components map to your research goals:

Goal / Research Question	Architectural Feature	Explanation / Justification
Consistency across agents	Shared DELTAS + projection DB + memory injection	All agents see the same rule deltas in order; memory injection ensures consistent context
Freshness / low latency	Durable NATS consumers, streaming updates, incremental projection	Instead of replaying full logs per query, state is updated incrementally
Explainability / traceability	Audit logs (promoter_audit), source_bindings, memory IDs in responses	Every answer can be traced to specific rule versions and sources
Human oversight & control	Admin CRUD, proposals, overrides integrated	Admins can intervene, fix rule gaps, approve/reject proposals
Evaluation & measurement	Metric definitions + instrumentation in pipeline	You collect data on divergence, latency, explanation accuracy
Scalability & extensibility	Modular monolith → service split, vector integration later	You separate business logic from API, making it easier to refactor or extend
5. Relation to Broader AI / Memory Theory

To situate your work in existing theory:

Memory architectures in AI agents: Recent work explores combining episodic, semantic, and procedural memory in agents. Your system creates a structured, symbolic long-term memory (rulebook) that agents consult. 
ResearchGate

Memory + attention in deep learning: The idea that neural systems benefit from external memory stores (e.g. memory-augmented neural networks) is well studied. Your system externalizes memory into a rule database, which acts as a separate memory module. 
arXiv

Explainable AI (XAI): You operationalize explainability by forcing rule citations, provenance, auditability. This aligns with research that calls for transparent AI systems in safety-critical domains. 
SpringerLink
+2
European Data Protection Supervisor
+2

Cognitive / symbolic architectures: Your rulebook + memory injection echoes symbolic architectures (e.g. production systems, Soar) where rules are matched against working memory. (See Soar, ACT-R) 
Wikipedia

Human-in-the-loop systems: Many AI systems require human governance; your admin CRUD, audit, override capabilities provide a concrete instantiation of this principle.

Thus your system bridges symbolic architectures, LLM-driven reasoning, and human oversight in a unified, measurable framework.

6. Experimental & Validation Plan

In your thesis, you’ll conduct experiments to validate your claims. Some outline:

Consistency experiment

Prepare a set of prompts over a namespace, lock temperature=0.

Run K agents (or repeated runs) and record outputs.

Compute exact-match rate, normalized edit distance, Jaccard overlap, embedding cosine.

Vary prompt complexity, number of rules, rule overlap.

Freshness / latency measurement

For each rule update (via seeder / Git webhook), record times: t_merge, t_emit, t_apply_projection, t_compile_agent

Compute Δme, Δea, Δma distributions.

Evaluate SLO (e.g. < 100 ms, < 500 ms) and count missed/delayed updates.

Explainability / provenance validation

For a set of sample prompts, ask agents to respond, then evaluate whether their cited rule IDs exist in the memory snapshot, and whether the provenance matches the canonical source.

Compute PCR (Provenance Correctness Rate) and replay fidelity.

Admin override / edit experiments

Insert a “buggy rule,” let system run, then force admin override.

Confirm all agents adopt updated rule, and responses shift accordingly (consistency preserved).

Measure latency from admin action to agent effect.

Ablation studies

Turn off audit, or remove memory injection, or simulate divergence, to show how your system degrades.

Compare with a baseline “vanilla LLM prompt” approach (no rule system).

Your instrumentation (logging, metrics, traces) within the pipeline supports these measurements.

7. Limitations & Risks

You should explicitly acknowledge limitations in your thesis:

Scalability ceiling: SQLite or even a monolithic DB may hit throughput limits; vector scale, sharding, distributed DBs might be needed.

Context window / prompt size constraints: If the rulebook is large, injecting many rules may exceed LLM context capacity. You’ll need selection or summarization heuristics.

Policy conflict / rule collisions: Two rules might conflict; system must detect and resolve or require human resolution.

Latency bottlenecks: Embedding computation, DB hot spots, network latency may slow memory compilation under load.

Drift & divergence under fallback: If an agent fails to receive some DELTAS (network partition), divergence may occur; you’ll need catch-up logic or snapshotting.

Explainability trade-off: Mandating rule citations constrains the flexibility of LLM generation; may degrade fluency or performance if rules are dense.

Security / authorization complexity: Admin endpoints need robust auth; exposing too much could be dangerous.

Dependency on rule correctness: If the rulebook is incorrect or incomplete, the system’s recommendations will suffer — the system does not “learn” rules automatically (unless you build that extension).

8. Implications, Broader Impact & Future Work

In your thesis discussion you should reflect on:

Governed AI systems: Your architecture is a model for combining human-authored rule systems with LLM flexibility in domains that require auditability (compliance, legal, finance, healthcare).

Multi-agent coordination: The pattern scales: multiple agents, even across different services, can share a consistent institutional memory.

MCP / tool integration: By exposing memory and reasoning via MCP tool interfaces, external tools, IDEs, or even other agents can interact with your system as a “memory / knowledge service.”

Learning & adaptation: A future extension: allow agents to propose new rules, or validate/verify rule suggestions through aggregated usage data.

Hybrid rule + neural architecture: Your system sits between symbolic (rule-based) and neural (LLM) systems. Exploring integration with neural embeddings (RAG) or neural rule induction would be natural next steps.

Explainable AI research contribution: You provide a working system that operationalizes explainability in LLM + memory systems, which may inform XAI research, especially for generative systems.

Thesis contributions to academia: You supply an architecture, a metric framework, a working prototype, and empirical evaluation. This can be referenced by future research on memory, multi-agent systems, or explainable AI.

If you like, I can turn this into a polished “Thesis Context & Motivation” chapter draft (with sections, transitions, and references) you can slot into your thesis. Do you want me to prepare that?

You said:
didnt you forget replayability , like the idea with event sourcing is everything is an audit trail, we can replay etc etc?
ChatGPT said:

Yes — you’re absolutely right. Replayability / time travel / full audit trail is one of the core promises of an event-sourced system, and I should have emphasized it more explicitly. Let me expand that now — conceptually, in your architecture, how we support it, and how it ties into your thesis.

What is Replayability / Time Travel in Event Sourcing

The idea is that the event log (e.g. your EVENTS and DELTAS) forms the immutable source of truth. From that you can replay the sequence of events to reconstruct the entire state of your system at any point in time.

This means that the current projection (in im_items_current) is just a materialized view built by applying the events in order. If needed, you can drop the projection and rebuild it from the event log by replaying all events.

You can also replay only up to a given timestamp (or event index), thus reconstructing past states (i.e. state at “time T”).

Because every event is preserved, you have a full audit record of what happened, when, and why (because events should carry metadata: who, source, reason). This is stronger than just “audit logs” or “version history” tables.

From Microsoft’s architecture documentation:

“The append-only storage of events provides an audit trail … It can regenerate the current state as materialized views or projections by replaying the events at any time.” 
Microsoft Learn

And in “Event sourcing beginner’s guides”:

“By storing events rather than just the current state, developers can replay the sequence of events to rebuild the application’s state at any point in time.” 
alinabo.com

So replayability is not optional — it is in many respects the defining advantage of event sourcing over classic CRUD + version tables.

How Replayability Fits into Your Architecture

Here’s how you can (and should) support replayability in your system, and how it complements your audit, projection, and admin features.

Where events are stored

EVENTS stream: initial proposals, seeding, GitHub events, CI changes.

DELTAS stream: promoter emits im.upsert.v1, im.retract.v1 events.
These two event streams form your immutable event logs. (You may also archive them to durable storage, e.g. S3, filesystem, or a proper event store.)

Projection is replay over events

Your projection service (or DeltaConsumer) effectively replays DELTAS to build/maintain:

im_items_current

im_items_history

source_bindings

If you ever suspect your projection is corrupted (bug, crash, data drift), you can:

Drop those tables (or reset the DB).

Replay all DELTAS from the start (or from a checkpoint).

Rebuild the projection to correct state.

You can also replay only up to a certain event sequence / timestamp to get a past version of the rulebook. This will be useful for:

Inspecting “what was the memory at time T”

Debugging past answers from agents

Comparing agent outputs from a past snapshot

Running experiments with “frozen memory” of a day ago

Admin / Audit + Replay

Because you also record audit decisions in promoter_audit (who approved, reason, etc.), you can reconstruct both the internal promoter decisions and the rule state at each step. That gives you a causal, narrated replay:

See “Proposal P arrived at t1”,

Check promoter decision (skip/defer/publish) in audit log,

If published, see which DELTA event was emitted,

Then see projection state update.

This is better than just version tables: you get the why and when in context, not just “this rule changed from v2 → v3”.

Snapshots & performance

In a system with many events, replaying all events from the beginning may become expensive. Common patterns to manage this:

Periodic snapshots: At intervals, save the full projection state (or partial) so that replay can start from the latest snapshot + only events after.

Incremental checkpointing: Record the last applied event sequence or offset in the projection DB, so on restart you resume from where you left off.

Event compaction / retention: For very old events, you might compress them or archive them separately (if full fidelity is not always needed). But be cautious if you need full audit.

Because you already maintain im_items_history and source_bindings, your projection is effectively a live snapshot of state with version lineage built in. Replay ensures that this snapshot is always consistent with the event stream.

Implications for your system / thesis

Replayability is a proof of integrity. If someone questions, “Did the projection DB drift?”, you can rebuild to verify.

Replay supports experiments: you can freeze or rewind memory to test agent behavior under different historical memories.

Replay strengthens explainability: you can show “state at time T” and see which rules existed then, which an agent may have cited.

Replay helps forensics / debugging: if a bad answer arises after a rule change, you can replay up to that moment to see what rules were active, what promoter decisions occurred, and trace back anomalies.

Thus, replayability is both a guardrail (you can always rebuild) and a research tool (you can simulate past states, measure divergence over time).

Revision of the Thesis Section to Include Replayability

In your thesis narrative (where you outline your contributions, gaps, architecture, evaluation), include a dedicated section on Replayability and Event-Driven Integrity. Here’s a sketch you should include or adapt:

Replayability & Immutability: Why Event Sourcing Matters

One of the key benefits of adopting an event-sourced architecture is the ability to time travel — that is, to replay the event log and reconstruct the entire system state at any point in time. This property is central to robustness, auditability, and experimental reproducibility.

The system treats EVENTS and subsequently DELTAS as immutable append-only logs, with no destructive updates.

The projection layer is a derived materialized view; if ever corrupted or suspected to drift, it can be purged and replayed from the start (or from a snapshot) to reestablish consistency.

Admin edits, promoter decisions, proposals, and rule manipulations are all captured in the same event history, enabling a causal replay narrative that shows not just what changed but why and who triggered it.

Replay is crucial for experimental control: one can fix the memory to a past snapshot, issue prompts, and compare agent outputs under that frozen memory. This helps in measuring consistency, divergence, or effect of memory updates over time.

Replayability ensures transparency: auditors or reviewers can inspect the system’s state trajectory, verify that no illegal or spurious changes occurred, and cross-validate against logs or external events.

In short, replayability is not merely an audit feature: it is a foundational guarantee that your system’s state is always derivable from a canonical, immutable event history — making your memory architecture scientifically reproducible, robust, and verifiable.