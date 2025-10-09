TOOL Project Roadmap & Milestones

Current State: baseline / MVP exists (v1.0 in progress).
Goal: through versioned steps, add replayability, RAG, MCP, admin CRUD, rule extraction.
Version Sequence:

v1.0 — Basic MVP & End-to-End Pipeline

v2.0 — Replayability & Retractions

v3.0 — RAG / Hybrid Retrieval

v4.0 — MCP / Tooling / External Client Layer

v5.0 — Admin CRUD / Governance UI & Edits

v6.0 — Rule Extraction & Flexible Input Ingestion

v7.0 — Deferred / Future Work (Snapshots, Rollback, Performance Optimizations)

Version v1.0 — MVP / Proof of Concept

Objective: Establish the core pipeline so that rule proposals propagate and agent can respond using memory + provenance, with evaluation infrastructure.

Key Features & Tasks (Issues)
Issue ID	Title	Description / Acceptance Criteria
v1.0-01	Receiver API & Webhooks	Build HTTP endpoints to accept seeded proposals or GitHub webhooks, validate input, forward to internal pipeline
v1.0-02	EVENTS ingestion / stream	Hook webhook receiver to publish messages into the EVENTS stream (e.g. NATS / JetStream)
v1.0-03	Promoter logic: canonicalization & dedup	Promoter processes EVENTS, canonicalizes item content, deduplicates repeats
v1.0-04	DELTA emission (im.upsert)	Promoter emits DELTA messages (im.upsert.v1) for new or updated rules
v1.0-05	Audit / decision logging	Promoter logs decisions (skip, promote, override) into audit table for traceability
v1.0-06	Projection / DeltaConsumer	Component that consumes DELTA stream and builds in-memory or persistent projection (current + history)
v1.0-07	Agent container: subscription / sync	Agent container subscribes (or periodically syncs) DELTAS → stores local state (SQLite or similar)
v1.0-08	/state & /why APIs	Agent exposes endpoints: /state returns namespace/hash, /why returns provenance for a rule
v1.0-09	MemoryCompiler & prompt injection	Given a prompt + namespace, build memory JSON of relevant rules, inject into system/user prompt structure
v1.0-10	Local LLM integration & chat	Inside agent container, call local LLM using injected memory, produce chat responses citing rule IDs
v1.0-11	Minimal UI / chat frontend	Simple UI (web or command) for sending prompts and viewing responses + memory citations
v1.0-12	E2E propagation test	Test scenario: propose rule → DELTA → projection → agent sees it → memory used in chat
v1.0-13	Logging & instrumentation	Log message timestamps, pipeline latencies, agent state hashes, memory snapshots
v1.0-14	Baseline experiment runner	Infrastructure to run "vanilla vs TOOL-augmented" experiments, with logging of results and metadata
v1.0-15	Preliminary metric scaffolding	Setup skeleton for consistency, correctness, retrieval metrics (placeholders)
v1.0-16	Draft test prompts	Create 50 test prompts stratified across 5 categories (API design, security, data modeling, error handling, general)
v1.0-17	Seed baseline rules	Seed 20+ rules into IM covering all prompt categories for evaluation
v1.0-18	Recruit & train annotators	Recruit 2-3 annotators, conduct calibration session, measure inter-rater reliability (Cohen's κ > 0.7)
v1.0-19	Label ground truth	Pilot study (20% overlap) → measure κ → full dataset annotation with graded relevance
v1.0-20	Run baseline comparison	Execute Condition A (vanilla) vs Condition B (TOOL) experiment, n=50, paired t-test
v1.0-21	Run retrieval quality experiment	Measure P@5, Recall@10, MRR, NDCG@10 for FTS5 search against ground truth
v1.0-22	Analysis notebooks	Statistical analysis, plots, 95% CI, hypothesis testing (H1-H3), results documentation

Deliverable / Definition of Done:

The pipeline works end-to-end: proposals → DELTA → agent memory → chat response

Basic UI + agent response with provenance

Experiment framework in place

Logging / metrics instrumentation ready

v1.0 milestone closed when these are stable and tested

Version v2.0 — Replayability & Retractions

Objective: Add capabilities to retract rules, replay from history, and verify determinism through event sourcing.

Key Features & Tasks (Issues)
Issue ID	Title	Description / Acceptance Criteria
v2.0-01	Support im.retract.v1 events	Promoter can issue and handle retraction events, marking rules inactive or removal
v2.0-02	DELTA schema extension	Extend DELTA message format to support deletion / retraction events
v2.0-03	Replay engine implementation	Module to replay DELTA logs from beginning to reconstruct memory state
v2.0-04	Full replay correctness test	Drop projection state, replay all DELTAS, compare resulting state hash with live state
v2.0-05	Idempotency & duplicate handling	Ensure reapplying same DELTA doesn't corrupt state; handle duplicate events safely
v2.0-06	Recovery for missing deltas	If agent misses DELTAS, allow catch-up via replay / sync logic
v2.0-07	Logging / metrics: replay times	Instrument replay times, memory usage, event counts
v2.0-08	Extend experiment: replayability metrics	Run multiple replay tests (10+ trials), log successes/failures, compile stats for H3

Deliverable / Definition of Done:

Retraction events function correctly and propagate

Full replay works and verifies correct state (SRA = 1.00)

Agents can catch up on missed deltas via replay

Replay experiments produce deterministic results (prove H3)

Logging / instrumentation for replay operations

v2.0 milestone closed when these features are validated and stable

Version v3.0 — RAG / Hybrid Retrieval

Objective: Augment your rule retrieval mechanism with embedding-based / semantic retrieval (RAG / hybrid) to improve relevance beyond lexical text search.

Key Features & Tasks (Issues)
Issue ID	Title	Description / Acceptance Criteria
v3.0-01	Embedder integration	Integrate embedding model (e.g. sentence-transformer) for rule content
v3.0-02	Vector DB / index	Set up vector store (e.g. pgvector, Qdrant) and embed rule entries
v3.0-03	Embedding insertion on upsert / retraction	When rules change, update vector DB accordingly
v3.0-04	Hybrid retrieval algorithm	Combine lexical (FTS5) + vector similarity scores to rank rules
v3.0-05	Fallback / threshold logic	If embedding score low / missing, fallback to lexical-only retrieval
v3.0-06	MemoryCompiler integration	Plugin hybrid retrieval into MemoryCompiler prompt building
v3.0-07	Retrieval experiment runner	Run systematic retrieval experiments comparing lexical vs hybrid
v3.0-08	Analysis notebook: RAG vs lexical	Produce P@K, MRR, NDCG, and comparison plots
v3.0-09	Correctness impact experiment	Test whether hybrid retrieval improves or changes correctness / agent responses
v3.0-10	Monitor embedding latency & overhead	Measure vector lookup times, memory, CPU costs
v3.0-11	Edge case / fallback testing	Missing embeddings, rule deletions, vector DB faults, fallback behavior

Deliverable / Definition of Done:

Hybrid retrieval works, ranking improves vs lexical baseline (if possible)

MemoryCompiler uses hybrid ranking transparently

Experiments show retrieval improvements or trade-offs

System remains stable with embedding integration

Logging of embedding latency / costs

v3.0 milestone concluded after validation and analysis

Version v4.0 — MCP / External Protocol / Tooling

Objective: Expose your memory / rulebook system as a tool service using MCP (Model Context Protocol), enabling external clients / IDEs to interact.

Key Features & Tasks (Issues)
Issue ID	Title	Description / Acceptance Criteria
v4.0-01	Define MCP schema (tools & resources)	Tools: search_rules, compile_memory, why, admin*; Resources: im://rules/current etc.
v4.0-02	Implement MCP server layer	Bridge MCP RPCs / JSON-RPC to your core API / backend logic
v4.0-03	Client adaptation to MCP	Modify Agent UI / test tool to call via MCP rather than REST
v4.0-04	Streaming via MCP	If protocol supports, allow Delta push / subscription via MCP
v4.0-05	Demo tool / plugin	Build a small integration (e.g. VSCode plugin, CLI tool) using MCP interface
v4.0-06	Consistency test: MCP vs internal path	Compare outputs from MCP path vs agent UI path under same memory & prompt
v4.0-07	Error handling & fallback	If MCP call fails or times out, fallback to REST or graceful error
v4.0-08	Versioning & backward compatibility	Support older clients or evolving schema gracefully
v4.0-09	Logging & metrics for MCP calls	Track MCP latency, error rates, usage stats
v4.0-10	Integrate MCP into evaluation path	Use MCP path as one of tool modes in experiments

Deliverable / Definition of Done:

External clients / tools can query memory via MCP

MCP responses match internal API / agent results

Demo tooling (plugin or CLI) demonstrates usage

Consistency verified between MCP vs internal path

Fallback / error handling in place

Logging of MCP usage

v4.0 milestone closed upon stable integration and demonstration

Version v5.0 — Admin CRUD / Governance UI & Editing

Objective: Add full administrative control over rules: create, edit, delete, audit trails, diff UI, and ensure propagation of admin edits to agents.

Key Features & Tasks (Issues)
Issue ID	Title	Description / Acceptance Criteria
v5.0-01	API endpoints: CRUD rules	POST, PUT, DELETE endpoints under /admin/rules or similar, with namespace parameter
v5.0-02	Version / conflict validation logic	On edit / delete, verify version, detect conflicting edits, enforce policies
v5.0-03	Audit trail for admin actions	Log admin user, diff, timestamp, original version, new version
v5.0-04	Admin UI for rule list / version history	Web UI to show all rules, their versions, contents, provenance
v5.0-05	Admin UI edit / delete / diff view	In UI, allow editing of rule content & labels; show diff from prior version
v5.0-06	Admin UI create new rule	Form for admins to manually add new rules (title, content, labels)
v5.0-07	Propagate admin edits via DELTA / push / replay	Admin changes should emit DELTA events or upstream equivalents
v5.0-08	Tests: agent consistency after edits	Agents must pick up admin edits, memory hash / content updated
v5.0-09	UI notifications for agent when rules edited	Agents' UI shows updates triggered by admin edits (pop-up or badge)
v5.0-10	Metrics: admin overhead / intervention rate	Log times to review proposals, frequency of edits, failure rates

Deliverable / Definition of Done:

Admins can create, edit, delete rules via API + UI

All admin operations are auditable and propagate correctly

Agents reflect admin edits in memory and responses

UI shows notifications to agents for admin rule changes

Logging / metrics on admin usage

v5.0 milestone complete when admin control is fully functional and tested



TOOL Roadmap: Versions & Issues

Versions / Milestones:

v1.0 — MVP / Proof of Concept

v2.0 — Replay & Retractions

v3.0 — RAG / Hybrid Retrieval

v4.0 — MCP / Tooling / External Protocol

v5.0 — Admin CRUD / Governance UI & Edits

v6.0 — Rule Extraction & Flexible Input Ingestion

v7.0 — Deferred / Future Work

Below is a detailed breakdown for each version, with issues / tasks.

v1.0 — MVP / Proof of Concept

Objective: Implement the minimal functional pipeline, agent, memory injection, and baseline evaluation scaffolding.

Issues / Tasks:

v1.0-01 — Build Receiver API & Webhooks endpoint

v1.0-02 — Bridge webhook → EVENTS stream

v1.0-03 — Implement Promoter logic: canonicalization / dedup

v1.0-04 — Emit DELTA events (im.upsert)

v1.0-05 — Audit log of promoter decisions

v1.0-06 — DeltaConsumer / Projection building (current + history)

v1.0-07 — Agent container: subscribe or sync DELTAS

v1.0-08 — Agent endpoints /state and /why

v1.0-09 — MemoryCompiler: select relevant rules, build JSON context

v1.0-10 — Integrate local LLM and chat interface with provenance

v1.0-11 — Minimal UI / chat frontend

v1.0-12 — E2E test: change → propagate → agent sees it

v1.0-13 — Logging / instrumentation of latencies / hashes

v1.0-14 — Baseline experiment runner (vanilla vs memory-assisted)

v1.0-15 — Metric scaffolding: consistency, correctness, retrieval placeholders

v1.0-16 — Draft test prompts: create 50 prompts (10 per category: API, security, data, error handling, general)

v1.0-17 — Seed baseline rules: seed 20+ rules into IM covering all prompt categories

v1.0-18 — Recruit & train annotators: 2-3 raters, calibration session, measure Cohen's κ > 0.7

v1.0-19 — Label ground truth: pilot 20% overlap → measure κ → full annotation with graded relevance (0/1/2)

v1.0-20 — Run baseline comparison: Condition A (vanilla LLM) vs Condition B (TOOL), n=50, paired t-test

v1.0-21 — Run retrieval quality experiment: measure P@5, Recall@10, MRR, NDCG@10 for FTS5 search

v1.0-22 — Analysis notebooks: statistical tests, plots, 95% CI, hypothesis validation (H1-H3), results documentation

Exit Criteria:

Pipeline works end to end

Agent can respond using memory

Baseline experiment path exists

Logging in place

v2.0 — Replay & Retractions

Objective: Add support for retraction events, full replay from DELTAS, and verify determinism through event sourcing.

Issues / Tasks:

v2.0-01 — Support im.retract.v1 in Promoter

v2.0-02 — Extend DELTA schema for deletions / retractions

v2.0-03 — Replay engine: full reconstruction from DELTAS

v2.0-04 — Full replay correctness tests (SRA = 1.00 target)

v2.0-05 — Idempotency & duplicate event safeguards

v2.0-06 — Recovery logic for missed deltas

v2.0-07 — Instrument logs: replay durations, event counts

v2.0-08 — Extended replay experiments (10+ trials for H3)

Exit Criteria:

Retraction events work correctly

Full replay works reliably (proves H3)

Tests confirm deterministic memory reconstruction

Agents can catch up on missed deltas

Logging and metrics for replay operations

v3.0 — RAG / Hybrid Retrieval

Objective: Integrate embedding / semantic retrieval and blend with lexical search to improve rule relevance.

Issues / Tasks:

v3.0-01 — Integrate embedding model (e.g. sentence-transformer)

v3.0-02 — Set up vector DB / index for rules

v3.0-03 — Embed rules on upsert / removal

v3.0-04 — Hybrid retrieval algorithm (lexical + vector)

v3.0-05 — Fallback logic when embedding is absent / low confidence

v3.0-06 — MemoryCompiler integration of hybrid ranking

v3.0-07 — Retrieval experiment runner (lexical vs hybrid)

v3.0-08 — Notebook: analyze retrieval performance (P@K, MRR, NDCG)

v3.0-09 — Experiment: impact on correctness via hybrid vs lexical

v3.0-10 — Measure embedding lookup latency / resource cost

v3.0-11 — Edge case fallback / embedding errors tests

Exit Criteria:

Hybrid retrieval usable and stable

Experiments compare lexical vs hybrid performance

System falls back gracefully on missing embeddings

MemoryCompiler uses hybrid ranking

v4.0 — MCP / External Protocol Tooling

Objective: Expose memory, search, and provenance as a tool service via MCP, enabling external clients / IDEs integration.

Issues / Tasks:

v4.0-01 — Define MCP tools & resources: compile_memory, search_rules, why, etc.

v4.0-02 — MCP server implementation bridging core logic

v4.0-03 — Adapt clients to use MCP instead of REST

v4.0-04 — Support streaming / push via MCP (if protocol supports)

v4.0-05 — Demo plugin / external client integration

v4.0-06 — Validate consistency between MCP client and native UI path

v4.0-07 — Fallback / error handling in MCP calls

v4.0-08 — Logging / metrics for MCP calls (latency, error)

v4.0-09 — Integrate MCP mode into experiments

Exit Criteria:

External tools can query memory via MCP

Results consistent with UI path

Error / fallback handling works

Demo client exists

MCP path included in evaluation

v5.0 — Admin CRUD / Governance UI & Edits

Objective: Provide full admin control for rule management: create, edit, delete rules, audit, diff UI, propagate edits.

Issues / Tasks:

v5.0-01 — API endpoints for Create / Update / Delete rules

v5.0-02 — Version validation / conflict checks on edits

v5.0-03 — Audit log for admin actions (user, diff, timestamp)

v5.0-04 — Admin UI: rule list with history / version view

v5.0-05 — Admin UI: edit / diff view / delete

v5.0-06 — Admin UI: create new rules

v5.0-07 — Propagate admin edits via DELTA & push / replay

v5.0-08 — Tests: agent memory reflects admin edits

v5.0-09 — Agent UI notifications for admin edits

v5.0-10 — Metrics: admin usage, latency, error rates

Exit Criteria:

Admins can manage rules via UI + API

Edits propagate correctly to agent memory

Audit trail present

Agents notified of admin changes

Tests confirming memory consistency after admin edits

v6.0 — Rule Extraction & Flexible Input Ingestion

Objective: Support ingestion / extraction of rules from unstructured or semi-structured sources (e.g. Markdown, docs), review UI, integrate into memory pipeline with provenance.

Issues / Tasks:

v6.0-01 — Build extractor for Markdown / text → rule candidates

v6.0-02 — Normalize extracted content into rule schema

v6.0-03 — Conflict / noise detection of extracted vs existing rules

v6.0-04 — Admin review UI for extracted proposals

v6.0-05 — Diff mapping / alignment from document changes → rule updates

v6.0-06 — Integrate extraction pipeline into promoter path

v6.0-07 — Evaluation pipeline: precision, recall, error breakdown

v6.0-08 — Tests: extraction → rule promotion → consistency in memory

v6.0-09 — Provenance retention: link extracted rule to original doc / version

v6.0-10 — UI display of rule lineage / extraction origin

Exit Criteria:

Extraction from unstructured sources proposal works

Admin review UI approves / rejects proposals

Promoter can ingest approved extracted rules

Experiments evaluating extraction quality

Provenance lineage preserved

v7.0 — Deferred / Future Work

Objective: Performance optimizations and advanced features deferred from core thesis scope.

Issues / Tasks:

v7.0-01 — Snapshot / checkpoint mechanism (deferred from v2.0)

v7.0-02 — Snapshot metadata tracking

v7.0-03 — Partial replay from snapshot + tail

v7.0-04 — Rollback API / CLI (time-travel to past state)

v7.0-05 — UI for rollback / history browsing

v7.0-06 — Performance optimizations for large rulesets (>10K rules)

v7.0-07 — Horizontal scaling / multi-node deployment

Exit Criteria:

These features are NOT required for thesis completion. They represent future work and production-readiness improvements.
