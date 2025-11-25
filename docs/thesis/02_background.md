# Chapter 2: Background & Related Work

**Status**: ✅ READY TO WRITE NOW
**Target completion**: November 20, 2025
**Estimated length**: 10-15 pages

---

## Writing Instructions

This chapter establishes the intellectual context for your work. Goals:
1. Show you understand the landscape (agent architectures, event sourcing, LLM techniques, governance)
2. Identify gaps that motivate your design choices
3. Provide terminology and concepts used in later chapters

**Tone**: Scholarly but not exhaustive. Cite 30-50 key papers. Focus on depth over breadth.

**Structure**: Each section should:
- Define key concepts
- Survey 5-10 representative works
- Critically analyze strengths/limitations
- Connect to your problem (how does this inform TOOL?)

---

## 2.1 Memory, Knowledge & Agent Architectures

**Target length**: 2-3 pages

### What to cover

**Agent memory taxonomies**:
- **Short-term memory**: Context window (e.g., GPT-4's 128k tokens)
- **Long-term memory**: External storage (databases, vector stores)
- **Episodic memory**: Past interactions, conversation history
- **Semantic memory**: Factual knowledge, rules, procedures

**Key frameworks**:
- **ReAct** (Yao et al., 2022): Reasoning + Acting, but no persistent memory
- **Reflexion** (Shinn et al., 2023): Self-reflection, episodic memory of past failures
- **Generative Agents** (Park et al., 2023): Memory stream with retrieval for believable NPCs
- **MemGPT** (Packer et al., 2023): Hierarchical memory management inspired by OS paging
- **Cognitive architectures** (Soar, ACT-R): Symbolic memory, procedural/declarative split

**Agent platforms**:
- **LangChain**: Tool use, chains, but limited memory governance
- **AutoGPT**: Autonomous agents, file-based memory, no audit trail
- **BabyAGI**: Task decomposition, simple memory store

**Gap analysis**:
- Most focus on episodic memory (conversation history), not **shared rule memory**
- No versioning or audit trails (can't answer "what did the agent know at time T?")
- Limited multi-agent consistency (each agent has its own memory)

**Connection to TOOL**:
- TOOL focuses on **semantic memory** (rules/instructions), not episodic
- Multi-agent shared memory with versioning
- Audit trail enables "time-travel" queries

### Papers to cite
- ReAct (Yao et al., 2022)
- Reflexion (Shinn et al., 2023)
- Generative Agents (Park et al., 2023)
- MemGPT (Packer et al., 2023)
- Cognitive Architectures survey (Laird et al., 2017)
- LangChain documentation
- Memory Augmented Neural Networks (Graves et al., 2016)

---

## 2.2 Event Sourcing, Audit Logs & Replayability

**Target length**: 2-3 pages

### What to cover

**Event sourcing principles** (Fowler, 2005; Vernon, 2013):
- Store state changes as immutable events (not just current state)
- Replay events to reconstruct state
- Audit trail "for free"
- Common in finance, e-commerce (order processing, inventory)

**CQRS (Command Query Responsibility Segregation)**:
- Separate write model (commands → events) from read model (projections)
- Enables multiple views of same event stream

**Systems using event sourcing**:
- **Event Store**: Native event sourcing DB
- **Kafka**: Distributed log, retains events
- **NATS JetStream**: Lightweight event streaming (what TOOL uses)
- **Git**: Events = commits, replay = checkout + apply patches

**Replayability in ML/LLM systems**:
- **DVC (Data Version Control)**: Versioning datasets
- **MLflow**: Experiment tracking, but not memory state
- **Weights & Biases**: Logs training runs, not inference memory
- **Few works on versioned agent memory**: Mostly ad-hoc snapshots

**Gap analysis**:
- Event sourcing widely used in traditional software, **rarely in LLM agent systems**
- Existing LLM systems use snapshots (database dumps), not event replay
- No standard for versioning agent memory or prompts

**Connection to TOOL**:
- TOOL applies event sourcing to agent instruction memory
- EVENTS → DELTAS → PROJECTIONS mirrors CQRS pattern
- NATS JetStream provides durable, replayable streams

### Papers/resources to cite
- Martin Fowler's "Event Sourcing" article (2005)
- Vaughn Vernon, *Implementing Domain-Driven Design* (2013)
- NATS JetStream documentation
- Git internals (for analogy)
- Kafka: The Definitive Guide
- Kleppmann, *Designing Data-Intensive Applications* (2017)

---

## 2.3 LLMs, Prompt Memory & Retrieval Techniques

**Target length**: 3-4 pages (this is a core section)

### What to cover

**Prompt engineering & in-context learning**:
- **Few-shot learning** (Brown et al., GPT-3 paper): Examples in prompt improve performance
- **Chain-of-thought** (Wei et al., 2022): Reasoning steps in prompt
- **Instruction tuning** (FLAN, T0): Training on instructions, but static

**Retrieval-Augmented Generation (RAG)**:
- **RAG** (Lewis et al., 2020): Retrieve docs from corpus, inject into prompt
- **Dense retrieval** (DPR, Karpukhin et al., 2020): BERT-based embeddings
- **ColBERT** (Khattab & Zaharia, 2020): Late interaction for efficiency
- **Self-RAG** (Asai et al., 2023): Agents decide when to retrieve

**Vector databases for LLMs**:
- **Pinecone, Weaviate, Qdrant**: Store embeddings, retrieve similar docs
- **FAISS** (Facebook AI): Fast approximate nearest neighbor search
- **ChromaDB, LanceDB**: Lightweight embedding stores

**Prompt memory systems**:
- **LangChain Memory**: ConversationBufferMemory, VectorStoreMemory (no versioning)
- **Prompt databases** (e.g., PromptLayer, Helicone): Log prompts, but no governance
- **Agent memory stores** (e.g., Zep.ai): Focus on conversation history, not rules

**Lexical vs. semantic search**:
- **BM25/TF-IDF**: Lexical, keyword matching (what FTS5 does)
- **Dense embeddings**: Semantic similarity via neural encoders
- **Hybrid search**: Combine lexical + semantic (ColBERTv2, SPLADE)

**Gap analysis**:
- RAG systems focus on factual retrieval (e.g., Wikipedia), **not rule governance**
- No versioning (can't retrieve "rules active on Jan 15, 2024")
- Black-box retrieval: can't trace why doc X was retrieved
- No multi-agent consistency (each agent queries independently, may get different results)

**Connection to TOOL**:
- TOOL uses **lexical search (FTS5)** for v1.0 (simple, explainable, deterministic)
- Future work (v3.0): Hybrid retrieval (FTS5 + embeddings)
- Unlike RAG, TOOL provides **versioning** and **audit** (can retrieve rules from past state)
- Retrieval is **deterministic** at same DELTA sequence number (same state → same results)

### Papers to cite
- RAG (Lewis et al., 2020)
- GPT-3 (Brown et al., 2020)
- DPR (Karpukhin et al., 2020)
- ColBERT (Khattab & Zaharia, 2020)
- Self-RAG (Asai et al., 2023)
- Chain-of-Thought (Wei et al., 2022)
- Instruction Tuning (Wei et al., FLAN-T5)
- Vector database surveys

---

## 2.4 Explainability, Rule Systems & Governance

**Target length**: 2-3 pages

### What to cover

**Explainable AI (XAI)**:
- **Attention visualizations** (e.g., BERTViz): Show which tokens the model attends to
- **Feature attribution** (LIME, SHAP): Explain model predictions
- **Critique of XAI for LLMs**: Post-hoc, unreliable, doesn't explain reasoning process

**Rule-based systems**:
- **Expert systems** (MYCIN, 1970s): If-then rules, explainable but brittle
- **Business rules engines** (Drools, CLIPS): Declarative rules, forward/backward chaining
- **Hybrid neural-symbolic systems**: Combine rules + neural nets (e.g., Logic Tensor Networks, Neural Theorem Provers)

**Governance & compliance in AI**:
- **Model cards** (Mitchell et al., 2019): Document model provenance, limitations
- **Datasheets for datasets** (Gebru et al., 2018): Document data provenance
- **GDPR "right to explanation"**: Users can request explanation for automated decisions
- **AI auditing** (Raji et al., 2020): Trace decisions to training data/rules

**Agent governance frameworks**:
- **Constitutional AI** (Anthropic, Bai et al., 2022): Hard-code values via prompts
- **Red-teaming LLMs**: Adversarial testing for harmful outputs
- **Human-in-the-loop (HITL)**: Admins approve/reject agent actions (e.g., Langsmith, AutoGPT with approval mode)

**Gap analysis**:
- Most XAI methods are post-hoc and opaque for LLMs
- Rule-based systems are rigid, don't handle ambiguity
- Governance tools focus on **model-level** (what training data), not **inference-level** (what rules did agent use?)
- No standard for **versioned, auditable rule injection**

**Connection to TOOL**:
- TOOL provides **provenance**: each response cites rule IDs + Git commits
- **Audit trail**: AUDIT stream logs all admin actions (CRUD, approve/reject)
- **Human oversight**: Admins can review/edit rules (v5.0 Admin UI)
- **Deterministic explainability**: replay to time T, see exact rules agent had

### Papers to cite
- Constitutional AI (Bai et al., 2022)
- Model Cards (Mitchell et al., 2019)
- Datasheets for Datasets (Gebru et al., 2018)
- AI Auditing (Raji et al., 2020)
- LIME (Ribeiro et al., 2016)
- SHAP (Lundberg & Lee, 2017)
- Expert systems surveys (Jackson, 1998)
- Logic Tensor Networks (Serafini & Garcez, 2016)

---

## 2.5 Gaps in Existing Approaches

**Target length**: 1-2 pages

### What to write

Synthesize the gaps identified in 2.1-2.4:

**Gap 1: No versioned, auditable memory for agents**:
- Existing systems: snapshots, logs, but not replayable events
- Can't answer: "What rules did Agent A have at time T?"
- TOOL addresses: DELTA stream with sequence numbers, deterministic replay

**Gap 2: Lack of multi-agent consistency**:
- LangChain, AutoGPT: Each agent has independent memory
- No guarantee of consistency (same input → different outputs if memory diverges)
- TOOL addresses: Shared PROJECTION DB, all agents read same state

**Gap 3: Black-box retrieval**:
- RAG systems: Retrieve docs, but can't trace why or which version
- TOOL addresses: Provenance (rule ID + commit hash), explicit retrieval log

**Gap 4: No governance workflow**:
- Most systems: Manual prompt editing, no approval process
- TOOL addresses: Promoter (gating logic), AUDIT stream, Admin UI (v5.0)

**Gap 5: Limited explainability for LLM decisions**:
- XAI methods: Post-hoc, unreliable
- TOOL addresses: Explicit rule citations in responses (e.g., `[im:api.auth@v3]`)

**Positioning TOOL**:
"This thesis proposes TOOL, an event-sourced instruction memory system that addresses these gaps by combining principles from event sourcing, rule-based systems, and prompt engineering. Unlike RAG systems (which focus on factual retrieval) or agent frameworks (which focus on orchestration), TOOL provides a **governed, auditable, replayable rulebook** for multi-agent systems."

### Writing tips
- This section is your "sales pitch" for why TOOL is needed
- Don't overclaim (acknowledge that your system also has limits)
- Set up Chapter 3 (Design) by framing the requirements

---

## Section Checklist

Before moving to Chapter 3, ensure:

- [ ] Each subsection defines key concepts clearly
- [ ] 30-50 papers cited across all sections
- [ ] Gaps are clearly identified and connected to your design
- [ ] Transition to Chapter 3 is smooth (e.g., "Given these gaps, we now present TOOL's design...")
- [ ] Tables/figures used where helpful (e.g., comparison table of memory systems)

---

## Recommended Structure for Each Subsection

```markdown
## 2.X Topic Name

[Opening paragraph: why this topic matters for your thesis]

### Key Concepts
[Define 3-5 core terms/ideas]

### Representative Works
[Survey 5-10 papers/systems, organize by theme]

**System A** (Author, Year):
- What it does
- Strengths
- Limitations

**System B** (Author, Year):
- ...

### Critical Analysis
[What's missing? What doesn't work for your problem?]

### Connection to TOOL
[How does your work build on / differ from these?]
```

---

## Resources to Help You Write This Chapter

1. **Your docs/THESIS.md**: Section on related work
2. **Google Scholar alerts**: Set up for "agent memory", "event sourcing", "RAG", "LLM governance"
3. **Survey papers**:
   - Agent architectures: Search "LLM agents survey 2024"
   - Event sourcing: Fowler's articles, DDD books
   - RAG: "Retrieval-Augmented Generation survey"
4. **Citation manager**: Use Zotero or Mendeley to organize papers

---

## Timeline

- **Day 1-2**: Read/skim 30-50 papers, take notes
- **Day 3-4**: Write sections 2.1-2.4 (draft)
- **Day 5**: Write section 2.5 (synthesis)
- **Day 6-7**: Revise, add citations, format

---

## Next Steps After Completing This Chapter

1. Send to advisor for feedback (if available)
2. Proceed to Chapter 3 (Design & Architecture)
3. Update references.bib with all cited works
