# Chapter 1: Introduction

## 1.1 Motivation & Problem Statement

Two developers, Alice and Bob, work in the same repository with AI coding assistants. Alice asks "What logging format should we use?" and receives advice to use structured JSON logs. Ten minutes later, Bob asks the same question and is told to use printf-style logs. Both answers are technically correct, but the codebase becomes inconsistent. Without shared, consistent memory, AI agents drift apart in their recommendations.

This inconsistency is manageable when agents suggest code style. But teams increasingly deploy AI agents for high-stakes decisions: code review that blocks deployments, security analysis that flags vulnerabilities, customer support that denies refunds [Weisz et al., 2021; Ross et al., 2023]. When agents make consequential decisions, inconsistency becomes a critical problem.

The root cause is **knowledge drift**. Current LLM systems lack robust mechanisms for shared long-term memory [Packer et al., 2023; Park et al., 2023]. Each agent starts from a generic system prompt, forcing developers to manually synchronize prompts or accept divergent behavior. As institutional knowledge evolves—teams adopt new security policies, refactor APIs, or standardize practices—agents lag behind, producing outputs that reflect outdated information.

In regulated industries such as finance, healthcare, and legal services, this creates **governance and explainability challenges** [GDPR Article 22; FDA, 2021; Jacovi et al., 2021]. When an AI agent rejects a pull request due to a security concern, stakeholders must answer: "Why did the agent make that decision?" Existing LLM systems operate as black boxes with no clear trace from recommendation back to source rule or policy [Lipton, 2018; Rudin, 2019; Doshi-Velez & Kim, 2017]. Even when agents cite rules, there is often no mechanism to verify which version was active at decision time, who authored it, or how it evolved [Lwakatare et al., 2020; Wachter et al., 2017].

Recent tools attempt to address these problems. **Centralized instruction files**—such as GitHub Copilot's `.github/copilot-instructions.md` [GitHub, 2024], Claude's project instructions [Anthropic, 2024], and Cursor's `.cursorrules` [Cursor, 2024]—allow teams to specify project-specific context in a shared repository. These represent a significant step forward: developers can document rules once, and agents (in theory) consult them. However, these approaches remain **static files** with critical limitations. When a developer updates `.github/copilot-instructions.md`, other developers' agents may not automatically receive the update, depending on their local sync. More importantly, there is no versioning: teams cannot answer "which instructions were active on January 15?" There is no audit trail of who changed what or when, and no governance workflow for approving or rejecting changes [Dakhel et al., 2023]. If two developers simultaneously edit the file, conflicts must be resolved manually with no record of the decision process.

Other tools fall short in different ways. **Retrieval-Augmented Generation (RAG)** systems [Lewis et al., 2020; Gao et al., 2023] retrieve relevant documents at query time but treat retrieval as a black box with no versioning or governance workflow. **Prompt databases** [PromptLayer, 2023; Helicone, 2023] explicitly inject context but require manual synchronization across agents and lack event history. **Fine-tuning** [Ouyang et al., 2022; Zhang et al., 2023] embeds knowledge in model weights but is expensive, opaque, and impossible to update incrementally. **Agent frameworks** like LangChain [Chase, 2022] and AutoGPT [Richards, 2023] focus on orchestration but treat memory as an afterthought, storing conversation history without versioning, provenance, or multi-agent consistency guarantees [Wang et al., 2024; Sumers et al., 2023].

**How can we design a shared memory system for LLM agents that provides multi-agent consistency, explainability, replayability, and governance, while supporting real-time updates and multi-agent deployments?**

We evaluate this through a proof-of-concept implementation (TOOL) and controlled experiments with 50 annotated prompts, measuring correctness (human evaluation), retrieval quality (Precision@5, MRR), replayability (state reconstruction accuracy), freshness (latency), and consistency (cited rule agreement).

## 1.2 Scope & Research Questions

To address this research question, we focus on specific boundaries and formulate testable hypotheses.

We focus on **shared memory** for project-specific context—including explicit guidelines ("use async/await for asynchronous operations"), conventions ("our API uses JWT tokens"), architectural facts ("we use NATS JetStream for event streaming"), and domain-specific knowledge—rather than general world knowledge or common sense facts [Dettmers et al., 2023]. By **shared memory system**, we mean a persistent storage and retrieval mechanism for project-specific context that governs agent behavior in a particular codebase or team environment. This is distinct from **conversational memory** (short-term chat history within a single session), **episodic memory** (long-term interaction logs), or **general knowledge bases** (encyclopedic facts). A key category of memory content is **instructions**—rules, guidelines, and conventions that direct agent behavior—but the system also stores facts, architectural decisions, and domain knowledge.

We propose an **event-sourced architecture** [Fowler, 2005; Vernon, 2013]—a design pattern where all state changes are captured as immutable events in an append-only log, enabling versioning, audit trails, and deterministic replay. Our proof-of-concept system, TOOL (The Organized Operating Language), targets teams of 1-10 agents managing 10s to 100s of memory items, using lexical retrieval (FTS5 full-text search) [SQLite FTS5, 2023]. **Multi-agent consistency** means that all agents with access to the same memory state produce the same memory citations and recommendations for identical inputs (at temperature=0).

Due to time and resource constraints inherent to a Master's thesis research prototype, the following are **out of scope:**
- Large-scale deployments (1000s of agents or memory items) — focus on small teams enables rigorous controlled experiments
- Vector-based retrieval (RAG with embeddings) [Karpukhin et al., 2020; Khattab & Zaharia, 2020] — deferred to future work (v3.0), baseline uses lexical search
- Automatic memory extraction from documentation [Rajpurkar et al., 2018; Chen et al., 2021] — memory items are manually seeded to isolate memory architecture evaluation
- Production deployment — this is a research prototype demonstrating feasibility, not a production-ready system

**Research Questions:**
- **RQ1:** Can event-sourced memory provide consistent retrieval for multi-agent systems? [Kleppmann, 2017]
- **RQ2:** Does memory injection improve agent correctness compared to baseline (no shared memory)? [Wei et al., 2022; Kojima et al., 2022]
- **RQ3:** Can the system deterministically replay event history to reconstruct memory state? [Fowler, 2005]
- **RQ4:** What is the latency of memory updates propagating from source to agent prompts?
- **RQ5:** How does retrieval quality (lexical search) affect agent performance? [Robertson & Zaragoza, 2009]

We evaluate whether this shared memory architecture improves agent correctness (H1), along with testing retrieval quality (H2), replayability (H3), freshness (H4), and deterministic behavior (H5).

**Hypotheses:**
- **H1 (Correctness):** Agents with injected memory produce responses rated higher in correctness than baseline (human evaluation, 50 prompts, paired t-test, α=0.05) [Chiang & Lee, 2023; Liu et al., 2023].
- **H2 (Retrieval Quality):** MemoryCompiler retrieves relevant memory items with Precision@5 ≥ 0.70, MRR ≥ 0.75 [Järvelin & Kekäläinen, 2002; Voorhees & Harman, 2005].
- **H3 (Replayability):** DELTA replay achieves State Reconstruction Accuracy (SRA) = 1.0 [Bainomugisha et al., 2013].
- **H4 (Freshness):** Median latency from memory change to agent UI display < 5 seconds [Kreps et al., 2011; Narkhede et al., 2017].
- **H5 (Consistency):** Repeated queries (temperature=0) produce identical cited memory IDs with ≥ 95% agreement [Brown et al., 2020; Ouyang et al., 2022].

## 1.3 Contributions & Claims

Having defined the scope and research questions, we now outline the contributions of this work and the claims we make based on the architecture, implementation, and evaluation.

**Contributions:**

**1. Architecture:** An event-sourced, shared memory system for LLM agents [Fowler, 2005; Richardson, 2018] with three streams—EVENTS (raw inputs), DELTAS (approved changes), and AUDIT (governance log)—enabling deterministic replay, full provenance tracking [Buneman et al., 2001; Cheney et al., 2009], and human oversight.

**2. Implementation:** A proof-of-concept system (TOOL) built with .NET 9 [Microsoft, 2024], NATS JetStream [NATS.io, 2024], SQLite + FTS5 [Hipp, 2023], and React [Meta, 2024], demonstrating event-sourced memory in multi-agent deployment.

**3. Evaluation Framework:** A dataset of 50 annotated prompts with ground-truth memory labels [Bowman et al., 2015; Rajpurkar et al., 2016], plus protocols for measuring correctness (H1), retrieval quality (H2) [Voorhees, 2001], replayability (H3), freshness (H4), and consistency (H5).

**4. Empirical Evaluation:** A systematic evaluation of whether memory injection improves agent correctness (H1), whether lexical retrieval achieves acceptable quality for small memory sets (H2), and whether event replay enables perfect state reconstruction (H3) [Wei et al., 2022; Chiang & Lee, 2023; Robertson & Zaragoza, 2009; Fowler, 2005; Kleppmann, 2017].

**5. Design Insights:** Guidance on when to use event-sourced memory vs. RAG [Lewis et al., 2020; Gao et al., 2023], how to balance simplicity (FTS5) vs. scalability (vector search) [Karpukhin et al., 2020; Johnson et al., 2019], and how to design governance workflows for human-in-the-loop oversight [Amershi et al., 2019; Cai et al., 2019].

**Claims** (to be validated through evaluation):
- Event-sourced, shared memory improves multi-agent consistency compared to static prompts or no shared memory [Kleppmann, 2017; Richardson, 2018].
- Memory injection (providing relevant context to agents) improves correctness on domain-specific tasks [Wei et al., 2022; Chiang & Lee, 2023].
- DELTA replay enables deterministic state reconstruction (SRA = 1.0) [Fowler, 2005].
- The system achieves low-latency memory propagation (< 5 seconds) [Kreps et al., 2011].
- Lexical retrieval (FTS5) is sufficient for small memory sets but may require RAG at scale [Robertson & Zaragoza, 2009; Lewis et al., 2020].

**Non-contributions:**
- Not a general-purpose knowledge graph [Miller, 2023; Ji et al., 2021] or large-scale RAG system [Lewis et al., 2020].
- Not a production deployment [Munson & Vogel, 2018].
- Does not solve automatic rule induction [Muggleton & De Raedt, 1994] or learning from agent interactions [Ouyang et al., 2022].

## 1.4 Thesis Outline

The remainder of this thesis is structured as follows.

**Chapter 2 (Background & Related Work)** reviews agent memory architectures [Park et al., 2023; Packer et al., 2023; Shinn et al., 2023], event sourcing [Fowler, 2005; Vernon, 2013], LLM prompt memory techniques [Brown et al., 2020; Wei et al., 2022], and explainability systems [Lipton, 2018; Rudin, 2019].

**Chapter 3 (Design & Architecture)** presents the TOOL architecture: EVENTS/DELTAS/AUDIT streams, promoter logic, projection database [Kleppmann, 2017], MemoryCompiler, and replay mechanism [Fowler, 2005].

**Chapter 4 (Implementation)** describes the .NET/NATS/SQLite implementation, including the DeltaConsumer refactor, Agent.UI, and experiment framework [Storey et al., 2020].

**Chapter 5 (Evaluation Methodology)** defines the evaluation protocol: dataset construction [Bowman et al., 2015], baselines, metrics (H1-H5) [Voorhees, 2001; Järvelin & Kekäläinen, 2002], and statistical methods [Cohen, 1988; Wilcoxon, 1945].

**Chapter 6 (Results)** reports experimental findings across correctness, retrieval quality, replayability, freshness, and consistency [Field, 2013; Cumming, 2014].

**Chapter 7 (Discussion)** reflects on design insights [Stol & Fitzgerald, 2018], limitations [Cook & Campbell, 1979], and trade-offs.

**Chapter 8 (Conclusion)** summarizes contributions and proposes future work including RAG integration [Gao et al., 2023], multi-agent scaling [Wu et al., 2023], and rule learning [Ouyang et al., 2022].

---

## References

*Note: Citations currently use [Author, Year] format for drafting clarity. For final submission, convert to IEEE/ACM numbered style [1], [2], etc. Full references to be formatted in BibTeX. Below are the 82+ citations needed:*

### AI/LLM Agent Systems
- **Brown et al., 2020**: "Language Models are Few-Shot Learners" (GPT-3 paper)
- **Weisz et al., 2021**: "Perfection Not Required? Human-AI Partnerships in Code Translation"
- **Ross et al., 2023**: "Programmer-Ai Collaboration in a Multi-Developer Environment"
- **Park et al., 2023**: "Generative Agents: Interactive Simulacra of Human Behavior"
- **Packer et al., 2023**: "MemGPT: Towards LLMs as Operating Systems"
- **Shinn et al., 2023**: "Reflexion: Language Agents with Verbal Reinforcement Learning"
- **Wang et al., 2024**: "A Survey on Large Language Model based Autonomous Agents"
- **Sumers et al., 2023**: "Cognitive Architectures for Language Agents"
- **Wu et al., 2023**: "AutoGen: Enabling Next-Gen LLM Applications via Multi-Agent Conversation"

### Prompt Engineering & In-Context Learning
- **Wei et al., 2022**: "Chain-of-Thought Prompting Elicits Reasoning in LLMs"
- **Kojima et al., 2022**: "Large Language Models are Zero-Shot Reasoners"
- **Ouyang et al., 2022**: "Training language models to follow instructions with human feedback" (InstructGPT)
- **Zhang et al., 2023**: "Instruction Tuning for Large Language Models: A Survey"

### RAG & Retrieval
- **Lewis et al., 2020**: "Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks"
- **Gao et al., 2023**: "Retrieval-Augmented Generation for Large Language Models: A Survey"
- **Karpukhin et al., 2020**: "Dense Passage Retrieval for Open-Domain Question Answering"
- **Khattab & Zaharia, 2020**: "ColBERT: Efficient and Effective Passage Search via Contextualized Late Interaction over BERT"
- **Johnson et al., 2019**: "Billion-scale similarity search with GPUs" (FAISS)
- **Robertson & Zaragoza, 2009**: "The Probabilistic Relevance Framework: BM25 and Beyond"

### Event Sourcing & Distributed Systems
- **Fowler, 2005**: "Event Sourcing" (Martin Fowler's article)
- **Vernon, 2013**: "Implementing Domain-Driven Design"
- **Kleppmann, 2017**: "Designing Data-Intensive Applications"
- **Richardson, 2018**: "Microservices Patterns: With examples in Java"
- **Kreps et al., 2011**: "Kafka: a Distributed Messaging System for Log Processing"
- **Narkhede et al., 2017**: "Kafka: The Definitive Guide"
- **Bainomugisha et al., 2013**: "A Survey on Reactive Programming"

### Explainability & Provenance
- **Lipton, 2018**: "The Mythos of Model Interpretability"
- **Rudin, 2019**: "Stop explaining black box machine learning models for high stakes decisions and use interpretable models instead"
- **Doshi-Velez & Kim, 2017**: "Towards A Rigorous Science of Interpretable Machine Learning"
- **Jacovi et al., 2021**: "Formalizing Trust in Artificial Intelligence: Prerequisites, Causes and Goals of Human Trust in AI"
- **Buneman et al., 2001**: "Why and Where: A Characterization of Data Provenance"
- **Cheney et al., 2009**: "Provenance in Databases: Why, How, and Where"
- **Wachter et al., 2017**: "Why a Right to Explanation of Automated Decision-Making Does Not Exist in the General Data Protection Regulation"

### Human-AI Interaction & Governance
- **Amershi et al., 2019**: "Guidelines for Human-AI Interaction" (CHI 2019)
- **Cai et al., 2019**: "The Effects of Example-Based Explanations in a Machine Learning Interface"
- **Dakhel et al., 2023**: "GitHub Copilot AI pair programmer: Asset or Liability?"
- **Lwakatare et al., 2020**: "DevOps for AI – Challenges in Model Development and Lifecycle Management"

### Evaluation & Methodology
- **Bowman et al., 2015**: "A large annotated corpus for learning natural language inference" (SNLI)
- **Rajpurkar et al., 2016**: "SQuAD: 100,000+ Questions for Machine Comprehension of Text"
- **Rajpurkar et al., 2018**: "Know What You Don't Know: Unanswerable Questions for SQuAD"
- **Chen et al., 2021**: "Evaluating Large Language Models Trained on Code" (Codex evaluation)
- **Voorhees, 2001**: "The TREC Question Answering Track"
- **Voorhees & Harman, 2005**: "TREC: Experiment and Evaluation in Information Retrieval"
- **Järvelin & Kekäläinen, 2002**: "Cumulated gain-based evaluation of IR techniques" (NDCG)
- **Cohen, 1988**: "Statistical Power Analysis for the Behavioral Sciences"
- **Wilcoxon, 1945**: "Individual comparisons by ranking methods"
- **Field, 2013**: "Discovering Statistics Using IBM SPSS Statistics"
- **Cumming, 2014**: "The New Statistics: Why and How"

### Human Evaluation
- **Chiang & Lee, 2023**: "Can Large Language Models Be an Alternative to Human Evaluations?"
- **Liu et al., 2023**: "G-Eval: NLG Evaluation using GPT-4 with Better Human Alignment"

### Knowledge Representation
- **Miller, 2023**: "WordNet: A Lexical Database for English"
- **Ji et al., 2021**: "A Survey on Knowledge Graphs: Representation, Acquisition, and Applications"
- **Muggleton & De Raedt, 1994**: "Inductive Logic Programming: Theory and Methods"
- **Dettmers et al., 2023**: "QLoRA: Efficient Finetuning of Quantized LLMs"

### Regulation & Compliance
- **GDPR Article 22**: "Automated individual decision-making, including profiling" (EU Regulation 2016/679)
- **FDA, 2021**: "Artificial Intelligence/Machine Learning (AI/ML)-Based Software as a Medical Device (SaMD) Action Plan"

### Software/Tools & Centralized Instructions
- **GitHub, 2024**: GitHub Copilot documentation (https://docs.github.com/copilot)
- **Anthropic, 2024**: Claude project instructions documentation (https://docs.anthropic.com)
- **Cursor, 2024**: Cursor documentation (https://cursor.sh/docs)
- **Chase, 2022**: LangChain (GitHub repository, https://github.com/langchain-ai/langchain)
- **Richards, 2023**: AutoGPT (GitHub repository, https://github.com/Significant-Gravitas/AutoGPT)
- **PromptLayer, 2023**: PromptLayer documentation (https://promptlayer.com)
- **Helicone, 2023**: Helicone documentation (https://helicone.ai)
- **SQLite FTS5, 2023**: SQLite FTS5 Extension (https://www.sqlite.org/fts5.html)
- **NATS.io, 2024**: NATS JetStream documentation (https://nats.io)
- **Microsoft, 2024**: .NET 9 documentation (https://dotnet.microsoft.com)
- **Meta, 2024**: React documentation (https://react.dev)
- **Hipp, 2023**: SQLite documentation (https://www.sqlite.org)

### Research Methodology
- **Cook & Campbell, 1979**: "Quasi-Experimentation: Design and Analysis Issues for Field Settings"
- **Stol & Fitzgerald, 2018**: "The ABC of Software Engineering Research"
- **Munson & Vogel, 2018**: "Research Prototypes: A Practical Approach to Design"
- **Storey et al., 2020**: "How Software Developers Use Twitter"
