# Table of Contents: Narrative Flow Analysis ("Røde Tråd")

**Purpose:** Critical examination of thesis structure with focus on narrative flow and WHY each section exists

**Date:** November 12, 2025

**Principle:** Every section must have a clear purpose and contribute to the overall story arc

---

## 🎯 The Core Story Arc

**Your thesis tells this story:**

1. **Problem:** Multi-agent LLM systems drift without shared, versioned, governed memory
2. **Gap:** Existing tools (Copilot/Claude/Cursor) and agent frameworks don't solve this
3. **Solution:** Event-sourced shared memory with replay, provenance, and governance
4. **Implementation:** TOOL system (v1.0 + v2.0 replay engine)
5. **Validation:** Experiments prove correctness, retrieval, replayability, freshness, consistency
6. **Reflection:** What worked, what didn't, when to use this architecture

**Every section should advance this story. If it doesn't → cut it or merge it.**

---

## 📖 Chapter-by-Chapter Narrative Flow

---

## **Chapter 1: Introduction** (5-7 pages)

### **Purpose:** Set up the problem and promise a solution

### **Narrative Arc:**
"Multi-agent LLM systems exist → they drift without shared memory → existing tools insufficient → we propose event-sourced shared memory → here's what we'll show"

---

### **1.1 Motivation & Problem Statement** (2 pages)

**What it covers:**
- Agents deployed in production need consistent behavior
- Current practice: ad-hoc prompts, file-based configs (Copilot, Claude, Cursor)
- Problem: Drift, inconsistency, no audit trails, no governance

**WHY it's needed:**
- Establishes that this is a REAL problem (not academic toy)
- Shows existing tools are insufficient (sets up Chapter 2)

**Red thread connection:**
- **→ 1.2:** "We've shown the problem exists. Now let's scope what we'll actually solve."

**Questions to ask yourself:**
- [ ] Does this make the reader care? (Is the problem important?)
- [ ] Does it mention multi-agent consistency specifically?
- [ ] Does it acknowledge Copilot/Claude/Cursor exist but are insufficient?

---

### **1.2 Scope & Research Questions** (1-1.5 pages)

**What it covers:**
- Focus: Multi-agent consistency via event-sourced shared memory
- Research questions (H1-H5)
- What's IN scope: v1.0 + v2.0 (replay engine)
- What's OUT of scope: v3.0 RAG, v5.0 Admin UI, v6.0 Extractor (future work)

**WHY it's needed:**
- Tells reader exactly what to expect (manages expectations)
- Prevents PhD-scope creep (focuses on Master's-level contribution)

**Red thread connection:**
- **← 1.1:** "We identified the problem. Here's exactly what we'll tackle."
- **→ 1.3:** "Now let me tell you what our specific contributions are."

**Questions to ask yourself:**
- [ ] Are H1-H5 clearly stated?
- [ ] Is it clear that RAG/Admin UI/Extractor are OUT of scope?
- [ ] Does it connect to the problem in 1.1?

---

### **1.3 Contributions & Claims** (1-1.5 pages)

**What it covers:**
- C1: Event-sourced architecture (EVENTS → DELTAS → Projection)
- C2: Replay engine & determinism (v2.0 COMPLETE)
- C3: Provenance tracking (source_bindings)
- C4: Evaluation framework (H1-H5)
- C5: Working prototype (v1.0 + v2.0)

**WHY it's needed:**
- Tells reader what's NEW (not just "we built a system")
- Sets up evaluation criteria (claims must be validated)

**Red thread connection:**
- **← 1.2:** "We defined the scope. Here's what we claim to contribute."
- **→ 1.4:** "Here's how the thesis is organized to validate these claims."

**Questions to ask yourself:**
- [ ] Is replay engine (v2.0) emphasized as major contribution?
- [ ] Do contributions map to research questions (H1-H5)?
- [ ] Are contributions realistic (not overpromising unimplemented features)?

---

### **1.4 Thesis Outline** (0.5 pages)

**What it covers:**
- Brief roadmap of Chapters 2-8

**WHY it's needed:**
- Helps reader navigate the thesis structure
- Shows logical flow from background → design → implementation → evaluation → discussion

**Red thread connection:**
- **← 1.3:** "We've stated our contributions. Here's how the rest of the thesis validates them."
- **→ Chapter 2:** "Now let's examine what others have done and where the gaps are."

**Questions to ask yourself:**
- [ ] Does the outline show a clear story arc?
- [ ] Does it explain WHY each chapter is needed?

---

## **Chapter 2: Background & Related Work** (10-12 pages)

### **Purpose:** Show that existing work has gaps that TOOL fills

### **Narrative Arc:**
"Others tried to solve parts of this problem → Agent memory (single-agent) → Instruction tools (no versioning) → RAG (no governance) → Event sourcing (not applied to LLMs) → **Gap:** No one combines all of these"

---

### **2.1 Agent Memory Architectures** (2-2.5 pages)

**What it covers:**
- Generative Agents (Park et al., 2023): Episodic memory, single-agent
- MemGPT (Packer et al., 2023): Hierarchical memory, single-agent
- Reflexion (Shinn et al., 2023): Verbal reflections, single-agent
- LangChain, AutoGPT: Memory as pluggable module, no versioning
- Comparison table

**WHY it's needed:**
- Shows that agent memory research exists BUT is single-agent focused
- No multi-agent consistency, no versioning, no governance
- Establishes gap #1: "Single-agent memory ≠ shared institutional memory"

**Red thread connection:**
- **← Chapter 1:** "We said multi-agent consistency is the problem. Here's what others did for single agents."
- **→ 2.2:** "Agent memory research focused on episodic/individual memory. But what about institutional rules? Let's look at instruction management tools."

**Questions to ask yourself:**
- [ ] Does each paragraph clearly state the limitation relevant to TOOL?
- [ ] Does the comparison table show TOOL fills the gaps?
- [ ] Is it concise (1-2 paragraphs per system, not multi-page)?

---

### **2.2 Centralized Instruction Management** (3-4 pages) ⭐ **MOST IMPORTANT**

**What it covers:**
- GitHub Copilot Instructions (`.github/copilot-instructions.md`)
- Claude Project Instructions (project-level)
- Cursor Rules (`.cursorrules`)
- PromptLayer, Helicone (cloud prompt management)
- Detailed comparison table: versioning, real-time, audit, governance
- Limitations: No real-time propagation, no audit trails, no governance workflows

**WHY it's needed:**
- **This is your main "state-of-the-art" comparison**
- Shows that Copilot/Claude/Cursor are closest to TOOL but insufficient
- Establishes gap #2: "Centralized instruction files exist BUT lack versioning, real-time, audit, governance"
- Chapter 1 mentions these tools → Chapter 2 examines them in depth

**Red thread connection:**
- **← 2.1:** "Agent memory research is single-agent. But what about tools that manage instructions centrally?"
- **→ 2.3:** "Instruction tools exist but lack versioning/governance. What about retrieval techniques? Let's look at RAG and prompt engineering."

**Questions to ask yourself:**
- [ ] Does this section clearly show Copilot/Claude/Cursor limitations?
- [ ] Does the comparison table highlight TOOL's advantages?
- [ ] Is it detailed enough (3-4 pages) since it's the main comparison?
- [ ] Does it explain WHY versioning/audit/governance matter?

**🚨 CRITICAL:** This section is MISSING from current TOC! Must be added.

---

### **2.3 Retrieval & Context Injection** (2 pages)

**What it covers:**
- Prompt engineering: Few-shot learning (Brown et al., 2020), chain-of-thought (Wei et al., 2022)
- RAG: Retrieval-Augmented Generation (Lewis et al., 2020), surveys (Gao et al., 2023)
- Connection to TOOL: Memory injection ≈ in-context learning
- TOOL uses FTS5 (lexical) baseline, NOT semantic RAG (v3.0 future work)
- Limitation: RAG lacks governance, versioning

**WHY it's needed:**
- Explains why H1 (correctness via memory injection) is plausible (in-context learning works!)
- Explains why TOOL uses FTS5 baseline (practical, proven)
- Establishes gap #3: "RAG excels at retrieval BUT lacks governance/versioning"

**Red thread connection:**
- **← 2.2:** "Instruction tools exist but lack features. What about retrieval? How do you select relevant rules?"
- **→ 2.4:** "RAG retrieves information but lacks governance. How do you ensure auditability? Let's look at event sourcing."

**Questions to ask yourself:**
- [ ] Does it explain prompt engineering briefly (foundation for H1)?
- [ ] Does it clearly state TOOL uses FTS5, NOT RAG (v3.0 future)?
- [ ] Does it explain why RAG alone isn't enough (no governance)?

---

### **2.4 Event Sourcing & Provenance** (2-3 pages)

**What it covers:**
- Event sourcing pattern: Immutable events, deterministic replay, idempotency (Fowler, Kleppmann)
- Application to LLM agent memory: Why it's rare but valuable
- Provenance tracking: Why/where provenance (Buneman, Cheney)
- TOOL's architecture mapping: EVENTS → DELTAS → Projection → Replay

**WHY it's needed:**
- **Event sourcing is TOOL's core architectural contribution**
- Explains v2.0 replay engine (validates H3: determinism)
- Explains provenance (enables explainability)
- Establishes gap #4: "Event sourcing used in microservices BUT not applied to LLM agent memory"

**Red thread connection:**
- **← 2.3:** "RAG retrieves rules but lacks auditability. How do you ensure every change is traceable?"
- **→ 2.5:** "We've shown gaps in agent memory, instruction tools, RAG, and lack of event sourcing. Let's synthesize."

**Questions to ask yourself:**
- [ ] Does it explain event sourcing clearly (not everyone knows it)?
- [ ] Does it show concrete mapping to TOOL (EVENTS/DELTAS/Projection)?
- [ ] Does it emphasize v2.0 replay engine as major contribution?

---

### **2.5 Summary & Gap Analysis** (1 page)

**What it covers:**
- Gap #1: Agent memory (single-agent, no multi-agent consistency)
- Gap #2: Instruction tools (no versioning, no real-time, no audit, no governance)
- Gap #3: RAG (no governance/versioning)
- Gap #4: Event sourcing (not applied to LLM agent memory)
- **The gap TOOL addresses:** Combines all of these → event-sourced shared memory with versioning, governance, provenance, real-time updates

**WHY it's needed:**
- Synthesizes all previous sections into one clear research gap
- Sets up Chapter 3 (your solution)

**Red thread connection:**
- **← 2.4:** "We've examined agent memory, instruction tools, RAG, event sourcing. Each has gaps."
- **→ Chapter 3:** "No existing system provides all of this. Here's how TOOL does it."

**Questions to ask yourself:**
- [ ] Does it clearly state NO existing system provides all features?
- [ ] Does it lead naturally to Chapter 3 (design)?
- [ ] Is it concise (1 page synthesis, not new content)?

---

## **Chapter 3: Design & Architecture** (12-15 pages)

### **Purpose:** Show HOW TOOL solves the problems identified in Chapters 1-2

### **Narrative Arc:**
"We've shown the gaps. Here's our solution → Requirements → System overview → Event flow → Each component (Promoter, Projection, MemoryCompiler, Replay) → Design decisions"

---

### **3.1 Requirements & Design Goals** (1.5-2 pages)

**What it covers:**
- R1: Multi-agent consistency (shared projection)
- R2: Freshness (real-time propagation via NATS + SSE)
- R3: Explainability (provenance tracking)
- R4: Replayability (deterministic state reconstruction)
- R5: Governance (audit trails, human oversight)
- Derived from problems in Chapter 1-2

**WHY it's needed:**
- Translates problems into concrete design requirements
- Sets criteria for success (will be validated in Chapter 6)

**Red thread connection:**
- **← Chapter 2:** "We identified gaps. Here are the requirements our solution must satisfy."
- **→ 3.2:** "Given these requirements, here's the high-level system architecture."

**Questions to ask yourself:**
- [ ] Do requirements map directly to problems in Chapter 1?
- [ ] Do requirements map to H1-H5 experiments?
- [ ] Are requirements testable/measurable?

---

### **3.2 System Overview & Data Flow** (2-3 pages)

**What it covers:**
- High-level architecture diagram: EVENTS → Promoter → DELTAS → Projection → Agent UI
- Event flow from webhook/seed → agent response
- Component responsibilities (brief, detailed in later sections)

**WHY it's needed:**
- Gives reader mental model of the system before diving into components
- Shows how event sourcing works at high level

**Red thread connection:**
- **← 3.1:** "We have requirements. Here's the system overview that satisfies them."
- **→ 3.3:** "Now let's examine the event streams in detail."

**Questions to ask yourself:**
- [ ] Can reader understand data flow from diagram?
- [ ] Does it show event sourcing pattern clearly?
- [ ] Does it defer details to later sections (not overwhelming)?

---

### **3.3 EVENTS, DELTAS, AUDIT Streams** (2 pages)

**What it covers:**
- EVENTS stream: Proposals from webhooks/seeds
- DELTAS stream: Approved changes (im.upsert.v1, im.retract.v1)
- AUDIT stream: Promoter decisions (logged for governance)
- Why separate streams? (separation of concerns, idempotency)
- Event schemas (brief, details in SCHEMAS.md appendix)

**WHY it's needed:**
- Explains core event sourcing implementation
- Shows how versioning/audit trails work

**Red thread connection:**
- **← 3.2:** "We showed the system overview. Here's how the event streams work in detail."
- **→ 3.4:** "Events flow through streams. Now let's see how Promoter processes them."

**Questions to ask yourself:**
- [ ] Is the distinction between EVENTS/DELTAS/AUDIT clear?
- [ ] Does it explain WHY separate streams (not just WHAT)?
- [ ] Does it connect to requirements (R3: explainability, R4: replayability)?

---

### **3.4 Promoter: Policy & Decision Logic** (2-3 pages)

**What it covers:**
- Subscribes to EVENTS stream
- Canonicalization (normalize content)
- Deduplication (content hash matching)
- Version management (increment versions)
- Emits DELTAS (im.upsert.v1, im.retract.v1)
- Records audit decisions (promoter_audit table)
- Idempotency (promoter_seen_events)

**WHY it's needed:**
- Promoter is the "gatekeeper" - ensures quality and governance
- Implements R5 (governance) via audit trails
- Explains how versioning works

**Red thread connection:**
- **← 3.3:** "Events flow through streams. Promoter is the gatekeeper."
- **→ 3.5:** "Promoter emits DELTAS. Now let's see how Projection consumes them."

**Questions to ask yourself:**
- [ ] Does it explain WHY Promoter exists (not just WHAT it does)?
- [ ] Does it connect to R5 (governance)?
- [ ] Does it mention v5.0 Admin UI as future work (Promoter has audit logic, but no UI)?

---

### **3.5 Projection / DeltaConsumer / Database** (2-3 pages)

**What it covers:**
- Subscribes to DELTAS stream (durable consumer)
- Maintains projection DB:
  - im_items_current (active rules)
  - im_items_history (full version history)
  - source_bindings (Git provenance)
- Idempotency (deltas_seen_events)
- Handles both upserts and retractions
- Enables time-travel queries

**WHY it's needed:**
- Projection is the "materialized view" that agents query
- Implements R1 (multi-agent consistency) via shared DB
- Implements R3 (explainability) via provenance
- Implements R4 (replayability) via history

**Red thread connection:**
- **← 3.4:** "Promoter emits DELTAS. Projection consumes them and builds the materialized view."
- **→ 3.6:** "Projection DB stores rules. MemoryCompiler retrieves relevant rules for agent prompts."

**Questions to ask yourself:**
- [ ] Does it explain WHY projection pattern is used?
- [ ] Does it connect to requirements (R1, R3, R4)?
- [ ] Does it explain idempotency (important for determinism)?

---

### **3.6 MemoryCompiler & Prompt Injection** (2 pages)

**What it covers:**
- FTS5 full-text search (lexical, NOT semantic)
- Query projection DB for relevant rules
- Build memory JSON structure
- Inject into system/user prompt
- Agent cites rule IDs in responses (e.g., `[im:api.idempotency@v3]`)

**WHY it's needed:**
- MemoryCompiler is how agents USE the shared memory
- Implements R1 (consistency) - all agents see same rules
- Enables H1 (correctness) - memory injection improves responses
- Enables H2 (retrieval quality) - measures FTS5 performance

**Red thread connection:**
- **← 3.5:** "Projection stores rules. MemoryCompiler retrieves and injects them."
- **→ 3.7:** "MemoryCompiler serves agents. Now let's discuss governance (Promoter audit + future Admin UI)."

**Questions to ask yourself:**
- [ ] Does it explain FTS5 choice (practical baseline, not RAG)?
- [ ] Does it connect to H1 (correctness) and H2 (retrieval)?
- [ ] Does it explain rule citation mechanism (explainability)?

---

### **3.7 Audit & Governance Design** (1-2 pages)

**What it covers:**
- Promoter audit trail (promoter_audit table)
- Records every decision: promote, skip, defer, override
- Tracks: who, what, when, why
- **v5.0 Admin UI (future work):** Design intent for CRUD interface, approval workflows
- Note: Audit logic implemented, UI is future work

**WHY it's needed:**
- Implements R5 (governance) - human oversight
- Shows how system supports explainability and accountability
- Honest about v5.0 Admin UI being future work

**Red thread connection:**
- **← 3.6:** "MemoryCompiler serves agents. But how do humans oversee the system?"
- **→ 3.8:** "We've covered operational components. Now let's discuss replay (v2.0 COMPLETE)."

**Questions to ask yourself:**
- [ ] Does it clearly state Admin UI is v5.0 future work?
- [ ] Does it explain what audit trail captures?
- [ ] Does it connect to R5 (governance)?

**🚨 CRITICAL:** Rename from "Admin / CRUD / Oversight Paths" to "Audit & Governance Design" (more honest about what's implemented)

---

### **3.8 Replay Engine & Idempotency** (2-3 pages)

**What it covers:**
- v2.0 Replay Engine (COMPLETE!)
- Drop projection DB → replay all DELTAS → reconstruct identical state
- Idempotency: applying same DELTA twice = same result
- SRA (State Reconstruction Accuracy) metric
- Use cases: disaster recovery, audit, validation (H3)
- **Snapshots (v7.0 future work):** Brief mention as optimization

**WHY it's needed:**
- **v2.0 replay is MAJOR contribution** - proves determinism (H3)
- Implements R4 (replayability)
- Shows concrete benefit of event sourcing

**Red thread connection:**
- **← 3.7:** "We've covered operational components. Replay is the 'superpower' of event sourcing."
- **→ 3.9:** "We've designed the system. Now let's discuss design decisions and trade-offs."

**Questions to ask yourself:**
- [ ] Does it emphasize v2.0 replay as major contribution?
- [ ] Does it explain WHY replay matters (not just HOW)?
- [ ] Does it connect to H3 (determinism validation)?
- [ ] Does it clearly state snapshots are v7.0 future optimization?

**🚨 CRITICAL:** Rename from "Replay, Snapshotting & Idempotency" to "Replay Engine & Idempotency" (snapshots not implemented)

---

### **3.9 Trade-offs & Design Decisions** (1.5-2 pages)

**What it covers:**
- Why FTS5 (not RAG)? Practical baseline, RAG is v3.0 future
- Why NATS (not Kafka)? Lighter weight, simpler for thesis
- Why SQLite (not PostgreSQL)? Sufficient for 1K-10K rules
- Why separate Promoter/Projection? Separation of concerns, testability
- What's deferred? Admin UI (v5.0), RAG (v3.0), Extractor (v6.0), Snapshots (v7.0)

**WHY it's needed:**
- Shows you made conscious design choices (not random)
- Explains scope decisions (Master's thesis, not production system)
- Acknowledges limitations and future work

**Red thread connection:**
- **← 3.8:** "We've designed the system. Here's why we made these choices."
- **→ Chapter 4:** "Design is complete. Now let's discuss implementation details."

**Questions to ask yourself:**
- [ ] Does it justify FTS5 baseline (not RAG)?
- [ ] Does it explain scope constraints (Master's thesis)?
- [ ] Does it acknowledge future work (v3.0-v7.0)?

---

## **Chapter 4: Implementation** (10-12 pages)

### **Purpose:** Show that you actually BUILT the system (not just designed it)

### **Narrative Arc:**
"We designed the system. Here's how we implemented it → Tech stack → Each component (Promoter, Projection, MemoryCompiler, Replay) → Experiments framework → Challenges"

---

### **4.1 Technology Stack & Dependencies** (1-1.5 pages)

**What it covers:**
- .NET 9.0 (ASP.NET Core Web API)
- NATS JetStream (event streaming)
- SQLite + FTS5 (database + full-text search)
- React 19 + Vite (Agent UI)
- Ollama (local LLM for testing)
- Why these choices? (lightweight, proven, suitable for thesis)

**WHY it's needed:**
- Grounds design in concrete technologies
- Explains practical constraints

**Red thread connection:**
- **← Chapter 3:** "We designed the system. Here's the tech stack we used to build it."
- **→ 4.2:** "Given this stack, let's start with Promoter implementation."

---

### **4.2 Promoter Module Implementation** (2 pages)

**What it covers:**
- Code structure: `PromoterService.cs`
- NATS subscription setup (durable consumer)
- Canonicalization algorithm
- Deduplication (content hash, seen_events)
- Version increment logic
- DELTA emission (Nats-Msg-Id for idempotency)
- Audit logging
- Implementation challenges

**WHY it's needed:**
- Shows Promoter design (3.4) is actually implemented
- Provides concrete details for reproducibility

**Red thread connection:**
- **→ 4.3:** "Promoter emits DELTAS. Now let's see Projection implementation."

---

### **4.3 DeltaConsumer / Projection Implementation** (2 pages)

**What it covers:**
- Code structure: `DeltaStreamConsumerService.cs`, `DeltaProjector.cs`, `DeltaParser.cs`
- NATS subscription (durable consumer)
- Event parsing (upserts and retractions)
- DB updates (im_items_current, im_items_history, source_bindings)
- Idempotency (deltas_seen_events)
- Implementation challenges (refactoring history)

**WHY it's needed:**
- Shows Projection design (3.5) is actually implemented

**Red thread connection:**
- **→ 4.4:** "Projection stores rules. Now let's see MemoryCompiler implementation."

---

### **4.4 MemoryCompiler & FTS5 Search** (1.5-2 pages)

**What it covers:**
- Code structure: `MemoryCompilerService.cs`
- FTS5 query construction
- Relevance ranking (basic)
- Memory JSON structure
- Prompt injection format
- Implementation challenges

**WHY it's needed:**
- Shows MemoryCompiler design (3.6) is actually implemented
- Explains FTS5 baseline for H2 (retrieval quality)

**Red thread connection:**
- **→ 4.5:** "MemoryCompiler retrieves rules. Now let's see Replay Engine implementation (v2.0 COMPLETE!)."

---

### **4.5 Replay Engine Implementation** (2-3 pages) ⭐ **MAJOR CONTRIBUTION**

**What it covers:**
- Code structure: `ReplayEngine.cs`, `ReplayController.cs`
- Replay algorithm: Drop projection → replay DELTAS from sequence 1
- Handles both upserts and retractions
- SRA computation (hash comparison)
- API endpoint: `POST /api/v1/admin/replay`
- Integration tests: `ReplayCorrectnessTests.cs`
- Experiment harness: `scripts/run_replay_experiments.sh`
- Implementation challenges

**WHY it's needed:**
- **v2.0 replay is MAJOR contribution** - deserves its own section!
- Shows determinism is actually implemented and tested
- Sets up H3 validation in Chapter 6

**Red thread connection:**
- **← 4.4:** "We've covered operational components. Replay is the key innovation."
- **→ 4.6:** "Replay is implemented. Now let's discuss Agent UI and real-time updates."

**🚨 CRITICAL:** This section is MISSING from current TOC! Must be added.

---

### **4.6 Agent UI & Real-Time Updates (SSE)** (1.5-2 pages)

**What it covers:**
- React 19 + Vite setup
- Chat interface, search, compile memory tabs
- SSE (Server-Sent Events) for push notifications
- DELTA stream subscription
- UI notification display
- Implementation challenges

**WHY it's needed:**
- Shows real-time propagation (R2: freshness) is implemented
- Sets up H4 validation (freshness/latency)

**Red thread connection:**
- **→ 4.7:** "System components implemented. Now let's discuss experiment framework."

---

### **4.7 Experiment Metrics Framework Integration** (1.5-2 pages)

**What it covers:**
- Experiment infrastructure: `evaluation/` directory structure
- H1-H5 experiment scaffolding
- Dataset design (prompts, ground truth labels)
- Logging and instrumentation
- Statistical analysis tools
- Note: Experiments not yet run (Chapter 6 will report results)

**WHY it's needed:**
- Shows evaluation framework is ready (even if experiments pending)
- Sets up Chapter 5 (methodology) and Chapter 6 (results)

**Red thread connection:**
- **→ 4.8:** "Framework is ready. Now let's discuss implementation challenges."

---

### **4.8 Logging, Instrumentation & Debug Tooling** (1 page)

**What it covers:**
- Structured logging
- Metrics collection (latencies, event counts)
- Debug UI features
- Implementation challenges

**WHY it's needed:**
- Shows system is observable and debuggable

**Red thread connection:**
- **→ 4.9:** "We've covered implementation details. Let's reflect on challenges."

---

### **4.9 Engineering Challenges & Lessons** (1.5-2 pages)

**What it covers:**
- Challenge: DeltaConsumer refactoring (split Parser/Projector/Consumer)
- Challenge: Idempotency edge cases
- Challenge: Real-time propagation (SSE setup)
- Challenge: Replay engine testing
- Lessons learned

**WHY it's needed:**
- Shows thesis is honest about difficulties
- Provides insights for future implementers

**Red thread connection:**
- **← 4.1-4.8:** "We implemented the system. Here's what was hard."
- **→ Chapter 5:** "Implementation complete. Now let's discuss how we'll evaluate it."

---

## **Chapter 5: Evaluation Methodology** (8-10 pages)

### **Purpose:** Explain HOW we'll validate our claims (before showing results)

### **Narrative Arc:**
"We built the system. Now let's design rigorous experiments to test H1-H5 → Dataset → Baselines → Protocols → Metrics → Statistics"

---

### **5.1 Evaluation Goals Overview** (1 page)

**What it covers:**
- Map H1-H5 to design requirements (R1-R5)
- Experimental vs. analytical validation
- Overall evaluation strategy

**WHY it's needed:**
- Connects evaluation back to research questions (Chapter 1) and requirements (Chapter 3)

**Red thread connection:**
- **← Chapter 4:** "System is implemented. Here's how we'll validate it meets requirements."
- **→ 5.2:** "Let's start with dataset design."

---

### **5.2 Dataset Design: Prompts & Rule Ground Truth** (2-3 pages)

**What it covers:**
- 50 prompts (stratified across 5 categories: API design, security, data modeling, error handling, general)
- 20-40 rules seeded into IM
- Ground truth annotation: Which rules are relevant for each prompt (graded 0/1/2)
- Annotator recruitment, training, calibration
- Inter-rater reliability (Cohen's κ > 0.7)

**WHY it's needed:**
- Dataset is foundation for H1 (correctness) and H2 (retrieval quality)
- Shows rigorous methodology (not cherry-picked examples)

**Red thread connection:**
- **→ 5.3:** "We have a dataset. Now let's define baselines for comparison."

---

### **5.3 Baselines & Comparative Systems** (1.5-2 pages)

**What it covers:**
- Condition A: Vanilla LLM (no memory) - baseline for H1
- Condition B: TOOL (FTS5 retrieval + memory injection) - experimental condition
- Random retrieval baseline for H2
- **NOT comparing to RAG** (v3.0 future work)
- **NOT comparing to Admin UI** (v5.0 future work)

**WHY it's needed:**
- Defines what we're comparing against (must be fair!)
- Clarifies that RAG comparison is out of scope

**Red thread connection:**
- **→ 5.4:** "We have baselines. Now let's design experiment protocols."

---

### **5.4 Experiment Protocols & Controls** (2-3 pages)

**What it covers:**
- H1 protocol: Paired comparison (same prompt, Condition A vs B), human rating (Likert 1-5)
- H2 protocol: Retrieval evaluation (P@5, Recall@10, MRR, NDCG@10)
- H3 protocol: Replay trials (10+ runs), SRA computation
- H4 protocol: Latency measurement (timestamps at each stage)
- H5 protocol: Repeated runs (temperature=0), rule citation agreement
- Controls: Same LLM, same temperature, same prompt, etc.

**WHY it's needed:**
- Shows experiments are rigorous and reproducible

**Red thread connection:**
- **→ 5.5:** "We have protocols. Now let's define metrics precisely."

---

### **5.5 Metrics Definitions & Computation** (1.5-2 pages)

**What it covers:**
- H1: Mean rating, Cohen's d (effect size)
- H2: P@5, Recall@10, MRR, NDCG@10 formulas
- H3: SRA = (hash_original == hash_replayed) ? 1.0 : 0.0
- H4: Δ_total = t_ui_display - t_merge (latency)
- H5: Agreement = |rules_run1 ∩ ... ∩ rules_run10| / |rules_run1 ∪ ... ∪ rules_run10|

**WHY it's needed:**
- Precise metric definitions ensure reproducibility

**Red thread connection:**
- **→ 5.6:** "We have metrics. Now let's discuss statistical analysis."

---

### **5.6 Statistical Methods & Power Analysis** (1.5-2 pages)

**What it covers:**
- H1: Paired t-test, α = 0.017 (Bonferroni correction), power analysis (n=50)
- H2: Bootstrap confidence intervals (95% CI)
- H3: Deterministic (all trials must succeed for SRA = 1.00)
- H4: Descriptive statistics (median, p90, p95)
- H5: Mean agreement across prompts

**WHY it's needed:**
- Shows statistical rigor (not just "eyeballing" results)

**Red thread connection:**
- **→ 5.7:** "We have statistical methods. Now let's discuss threats to validity."

---

### **5.7 Threats to Validity & Mitigations** (1-1.5 pages)

**What it covers:**
- Internal validity: Annotator bias (mitigate: multiple raters, κ > 0.7)
- External validity: Limited to consulting domain (mitigate: stratified prompts)
- Construct validity: Human ratings subjective (mitigate: clear rubric)
- Conclusion validity: Small sample (mitigate: effect size reporting)

**WHY it's needed:**
- Shows you're aware of limitations (academic honesty)

**Red thread connection:**
- **← 5.1-5.6:** "We designed rigorous experiments. Here are remaining threats."
- **→ Chapter 6:** "Methodology is defined. Now let's present results."

---

## **Chapter 6: Results & Analysis** (10-12 pages)

### **Purpose:** Report experimental findings (validate H1-H5)

### **Narrative Arc:**
"We ran experiments. Here are the results → H1 (correctness) → H2 (retrieval) → H3 (replayability) → H4 (freshness) → H5 (consistency) → Explainability (qualitative)"

**Note:** Present in order of importance, not H1-H5 order!

---

### **6.1 Correctness Results (H1)** (2-3 pages) ⭐ **MOST IMPORTANT**

**What it covers:**
- Condition A (vanilla) vs. Condition B (TOOL) ratings
- Mean ratings, paired t-test results
- Effect size (Cohen's d)
- 95% confidence intervals
- Per-category breakdown (API, security, etc.)
- Example responses showing improvement
- **Result:** TOOL significantly outperforms vanilla (p < 0.017, d > 0.5) - or not!

**WHY it's needed:**
- H1 is PRIMARY hypothesis (does memory injection actually help?)
- Validates main contribution

**Red thread connection:**
- **← Chapter 5:** "We designed H1 experiment. Here are the results."
- **→ 6.2:** "Memory injection works. But is retrieval quality good? Let's check H2."

---

### **6.2 Retrieval Quality Results (H2)** (2 pages)

**What it covers:**
- FTS5 performance: P@5, Recall@10, MRR, NDCG@10
- Comparison to random baseline
- Per-category breakdown
- Examples: Queries with high/low precision
- **Result:** P@5 > 0.70, MRR > 0.75 (or not!)

**WHY it's needed:**
- Validates MemoryCompiler FTS5 baseline (necessary for H1)

**Red thread connection:**
- **→ 6.3:** "Retrieval quality is acceptable. But does replay work? Let's check H3."

---

### **6.3 Replayability & Determinism (H3)** (2-3 pages) ⭐ **MAJOR CONTRIBUTION**

**What it covers:**
- 10+ replay trials
- SRA results (all trials must yield SRA = 1.00)
- Replay duration (median, p90, p95)
- Hash verification
- **Result:** SRA = 1.00 (100% success rate) - proves determinism!

**WHY it's needed:**
- **v2.0 replay is MAJOR contribution** - H3 validates it works!
- Shows event sourcing benefits

**Red thread connection:**
- **→ 6.4:** "Replay works perfectly. How fast do updates propagate? Let's check H4."

---

### **6.4 Freshness & Latency (H4)** (1.5-2 pages)

**What it covers:**
- Latency breakdown: Δ_me, Δ_ea, Δ_ma, Δ_total
- Push (SSE) vs. pull (polling) comparison
- Median, p90, p95 latencies
- SLO violations (if any)
- **Result:** Push median < 100ms (or not!)

**WHY it's needed:**
- Validates real-time propagation (R2: freshness)

**Red thread connection:**
- **→ 6.5:** "Updates propagate quickly. Are agents consistent? Let's check H5."

---

### **6.5 Consistency Results (H5)** (1.5 pages)

**What it covers:**
- 20 prompts × 10 runs = 200 queries
- Rule citation agreement per prompt
- Mean agreement across prompts
- **Result:** Mean agreement > 0.95 (or not!)

**WHY it's needed:**
- Validates multi-agent consistency (R1)

**Red thread connection:**
- **→ 6.6:** "Agents are consistent. Can we explain their answers? Let's check provenance."

---

### **6.6 Provenance & Explainability** (1-1.5 pages)

**What it covers:**
- Qualitative analysis: Can users trace answers to rules?
- Example: Query `/why` for a rule → returns Git commit, author, file, blob SHA
- User study (optional): Can humans understand provenance?

**WHY it's needed:**
- Validates explainability (R3)

**Red thread connection:**
- **→ 6.7:** "We've validated H1-H5 and provenance. Let's analyze errors and edge cases."

---

### **6.7 Ablation, Sensitivity & Error Analysis** (1.5-2 pages)

**What it covers:**
- Ablation: What if we remove memory injection? (validates H1 is due to memory, not placebo)
- Sensitivity: How does retrieval threshold affect H1? (P@5 vs P@10)
- Error analysis: When does TOOL fail? (false positives, false negatives)
- Lessons learned

**WHY it's needed:**
- Shows deeper understanding of system behavior
- Honest about failure cases

**Red thread connection:**
- **← 6.1-6.6:** "We reported main results. Here's deeper analysis."
- **→ Chapter 7:** "Results are in. Now let's interpret them."

---

## **Chapter 7: Discussion** (6-8 pages)

### **Purpose:** Interpret results, reflect on design, discuss limitations

### **Narrative Arc:**
"We showed results. What do they mean? → Claims validated/invalidated → Design insights → Limitations → Guidelines for future systems"

---

### **7.1 Reflection on Claims & Hypotheses** (1.5-2 pages)

**What it covers:**
- H1 (correctness): Supported/not supported? Effect size interpretation
- H2 (retrieval): Supported/not supported? FTS5 sufficient?
- H3 (replayability): Supported? (should be 100% success!)
- H4 (freshness): Supported? Latency acceptable?
- H5 (consistency): Supported? Agreement high enough?
- Overall: Did we achieve what we set out to do?

**WHY it's needed:**
- Connects results (Chapter 6) back to claims (Chapter 1)

**Red thread connection:**
- **← Chapter 6:** "Results are in. Here's what they mean for our claims."
- **→ 7.2:** "Claims are validated. What design insights did we gain?"

---

### **7.2 Design Insights & Patterns** (1.5-2 pages)

**What it covers:**
- Event sourcing for LLM agent memory: When is it worth the complexity?
- FTS5 vs. RAG: FTS5 sufficient for structured rules (RAG for unstructured docs?)
- Promoter pattern: Separation of concerns valuable
- Real-time propagation: SSE vs. polling trade-offs
- Lessons for future systems

**WHY it's needed:**
- Extracts generalizable insights (not just "TOOL works")

**Red thread connection:**
- **→ 7.3:** "We've identified insights. Now let's be honest about limitations."

---

### **7.3 Limitations, Pitfalls & Validity Threats** (2-3 pages)

**What it covers:**
- **Implementation limitations:**
  - FTS5 baseline (no RAG) - v3.0 future work
  - No Admin UI (v5.0 future work)
  - No Extractor (v6.0 future work)
  - Scalability: SQLite suitable for 1K-10K rules (not 100K+)
- **Evaluation limitations:**
  - Small dataset (50 prompts)
  - Single domain (consulting)
  - Human ratings subjective
- **Design limitations:**
  - Event sourcing complexity (learning curve)
  - NATS operational overhead (not serverless)
- Honest reflection on what didn't work

**WHY it's needed:**
- Academic honesty (every thesis has limitations!)
- Sets up future work (Chapter 8)

**Red thread connection:**
- **→ 7.4:** "We've acknowledged limitations. Here are guidelines for practitioners."

---

### **7.4 Guidelines for Future Systems** (1.5-2 pages)

**What it covers:**
- When to use event-sourced memory: Multi-agent, regulated domains, audit requirements
- When NOT to use: Single-agent, low-consistency requirements
- FTS5 vs. RAG: Structured rules → FTS5; Unstructured docs → RAG
- Promoter pattern: Reusable for other governance workflows
- Real-time propagation: SSE for low latency, polling for simplicity

**WHY it's needed:**
- Provides actionable advice for practitioners

**Red thread connection:**
- **→ 7.5:** "We've given general guidelines. Now let's be specific about when this architecture fits."

---

### **7.5 When This Architecture Is Suitable** (1 page)

**What it covers:**
- **Good fit:**
  - Multi-agent systems
  - Regulated domains (finance, healthcare, legal)
  - Audit/compliance requirements
  - Versioning/time-travel queries needed
- **Poor fit:**
  - Single-agent systems
  - No governance requirements
  - High-volume, low-latency (event sourcing overhead)

**WHY it's needed:**
- Helps reader decide if TOOL pattern applies to their problem

**Red thread connection:**
- **← 7.1-7.4:** "We've reflected on results, insights, limitations, guidelines."
- **→ Chapter 8:** "Now let's conclude and discuss future work."

---

## **Chapter 8: Conclusion & Future Work** (3-5 pages)

### **Purpose:** Wrap up thesis, summarize contributions, point to future research

### **Narrative Arc:**
"We set out to solve X → We built Y → We validated Z → Future work: A, B, C"

---

### **8.1 Summary of Contributions & Findings** (1.5-2 pages)

**What it covers:**
- Restate problem (Chapter 1)
- Summarize contributions (C1-C5)
- Summarize findings (H1-H5 results)
- Final takeaway: Event-sourced shared memory works for multi-agent consistency

**WHY it's needed:**
- Reminds reader of the story arc
- Emphasizes what's NEW

**Red thread connection:**
- **← Chapter 1:** "We promised to solve these problems. Here's what we delivered."
- **→ 8.2:** "We've summarized contributions. What are the broader implications?"

---

### **8.2 Implications: Theory & Practice** (1 page)

**What it covers:**
- **Theory:** Event sourcing + LLM agent memory (novel combination)
- **Practice:** Governance patterns for production AI systems
- **Industry:** Model for regulated domains (finance, healthcare)

**WHY it's needed:**
- Shows broader impact beyond thesis

**Red thread connection:**
- **→ 8.3:** "We've discussed implications. Now let's outline future research."

---

### **8.3 Future Extensions (RAG, MCP, Admin UI, Extractor)** (1-1.5 pages)

**What it covers:**
- **v3.0 RAG:** Semantic/hybrid retrieval (embeddings, vector DB)
- **v4.0 MCP:** External protocol integration (IDE plugins)
- **v5.0 Admin UI:** Governance workflows, CRUD interface
- **v6.0 Extractor:** Markdown → rule parsing
- **v7.0+:** Snapshots, rollback, performance optimizations
- Other directions: Agent-proposed rules, multi-model systems

**WHY it's needed:**
- Shows thesis is not "done" - opens research directions

**Red thread connection:**
- **→ 8.4:** "We've outlined future work. Final reflections."

---

### **8.4 Final Reflections** (0.5-1 page)

**What it covers:**
- Personal reflections on thesis journey
- What you learned
- Why this work matters

**WHY it's needed:**
- Humanizes the thesis (optional but nice)

**Red thread connection:**
- **← Entire thesis:** "This is what we built, validated, and learned."

---

## 🎯 Summary: The Red Thread

### **Act 1: Problem (Chapters 1-2)**
1. Multi-agent systems drift without shared memory (Chapter 1)
2. Existing work has gaps (Chapter 2)

### **Act 2: Solution (Chapters 3-4)**
3. TOOL's event-sourced architecture (Chapter 3)
4. We actually built it (Chapter 4)

### **Act 3: Validation (Chapters 5-6)**
5. Rigorous experiments (Chapter 5)
6. Results validate claims (Chapter 6)

### **Act 4: Reflection (Chapters 7-8)**
7. Interpret results, discuss limitations (Chapter 7)
8. Conclude, point to future work (Chapter 8)

---

## ✅ Critical Questions for Each Section

Before writing any section, ask:

1. **WHY does this section exist?** (What gap does it fill in the story?)
2. **WHAT specific content does it cover?** (Be precise!)
3. **HOW does it connect to previous section?** (Red thread backwards)
4. **WHERE does it lead next?** (Red thread forwards)
5. **WHO is the audience?** (What do they need to know?)

---

**Status:** Full TOC narrative flow analysis complete! Every section mapped to story arc. 🚀
