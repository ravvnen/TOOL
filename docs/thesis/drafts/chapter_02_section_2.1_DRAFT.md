# Chapter 2: Background & Related Work

## 2.1 Agent Memory Architectures

The integration of memory mechanisms into large language model (LLM) agents has emerged as a critical research area for enabling persistent, contextually-aware behavior. While LLMs demonstrate impressive in-context learning capabilities [Brown et al., 2020], their stateless nature—processing each query independently without retaining information across interactions—limits their effectiveness in multi-turn conversations, long-term tasks, and environments requiring accumulated knowledge. Recent research explores various memory architectures to address these limitations, ranging from episodic memory streams to hierarchical context management. This section reviews key approaches to agent memory and positions TOOL's shared, event-sourced memory architecture within this landscape.

### Generative Agents: Episodic Memory Streams

Park et al. [2023] introduced *Generative Agents*, a system where autonomous agents maintain a memory stream of observations and reflections to simulate human-like behavior in interactive environments. Each agent stores timestamped observations (e.g., "Alice walked into the coffee shop at 10:30 AM") and periodically generates *reflections*—higher-level summaries extracted from recent observations (e.g., "Alice prefers morning coffee meetings"). Retrieval combines three scoring mechanisms: *recency* (recent memories weighted higher), *importance* (manually or LLM-scored significance), and *relevance* (embedding similarity to the current query). This architecture enables agents to exhibit temporally consistent behavior and recall past interactions.

However, Generative Agents focuses on *individual agent memory* for simulation purposes. Each agent maintains a separate memory stream with no mechanism for sharing institutional knowledge across multiple agents. Memory persists only within a single agent's lifetime, and there is no versioning, governance workflow, or audit trail. While effective for behavioral simulation, this approach does not address the multi-agent consistency and explainability requirements of production systems.

### MemGPT: Hierarchical Virtual Context Management

Packer et al. [2023] propose *MemGPT*, a system that extends LLM context capacity through hierarchical memory management inspired by virtual memory in operating systems. MemGPT organizes memory into three tiers: *main context* (active working memory within the LLM's context window), *recursive summary* (compressed summaries of past interactions), and *archival storage* (long-term storage indexed for retrieval). A paging mechanism dynamically swaps memory segments in and out of main context based on relevance, enabling agents to operate over extended conversations and large knowledge bases that exceed token limits.

MemGPT's architecture is sophisticated, but it remains *single-agent focused*. There is no shared memory layer across multiple agent instances, and memory updates are local to each agent. Additionally, MemGPT does not provide versioning (memory segments can be overwritten), provenance tracking (no audit trail of who changed what), or governance workflows (no human oversight of memory edits). While MemGPT addresses context window constraints, it does not solve the *drift problem* when multiple agents need to share consistent, governed institutional memory.

### Reflexion: Learning from Verbal Feedback

Shinn et al. [2023] introduce *Reflexion*, a framework where agents improve task performance by reflecting on past errors and storing verbal reflections in memory. After completing a task (e.g., coding, question answering), the agent generates a self-critique: "I failed because I assumed the API returned JSON, but it actually returned XML." These reflections are stored in episodic memory and retrieved in future trials, enabling the agent to avoid repeating mistakes. Reflexion demonstrates that explicit verbal reasoning and memory of past failures can improve correctness over time.

Like Generative Agents and MemGPT, Reflexion focuses on *episodic memory*—memories of specific interactions and tasks—rather than *institutional memory* (rules, policies, conventions that apply across all tasks). Reflexion's memory is also single-agent: each agent learns independently without sharing lessons across a team. There is no mechanism to version reflections, audit changes, or govern what goes into memory. While Reflexion advances agent learning, it does not address the shared, versioned, governed memory requirements of multi-agent systems in regulated domains.

### Agent Frameworks: LangChain and AutoGPT

Popular agent frameworks such as *LangChain* [Chase, 2022] and *AutoGPT* [Richards, 2023] provide modular architectures for building LLM-based agents with tool use, planning, and memory capabilities. LangChain offers memory modules (e.g., `ConversationBufferMemory`, `VectorStoreMemory`) that persist conversation history or store documents in vector databases. AutoGPT maintains a short-term memory buffer and can optionally integrate external storage backends.

However, these frameworks treat memory as an *afterthought*—a pluggable component rather than a foundational architecture. Memory typically consists of raw conversation logs or document embeddings with no versioning, provenance, or governance mechanisms. When multiple agents are deployed, each maintains its own memory state with no guarantee of consistency. Wang et al. [2024] and Sumers et al. [2023] survey the current state of LLM agent architectures and note that memory remains an underexplored design dimension, particularly in multi-agent settings where coordination and consistency are critical.

### Cognitive Architectures and Symbolic Memory

Beyond LLM-specific systems, cognitive architectures from AI research—such as *Soar* [Laird, 2012], *ACT-R* [Anderson et al., 2004], and *CLARION* [Sun, 2006]—provide structured approaches to agent memory. These systems distinguish between *declarative memory* (facts, rules) and *procedural memory* (skills, strategies), and use production rules to match working memory against long-term memory. While these architectures offer rigorous symbolic reasoning and explicit memory structures, they predate modern LLMs and do not address the challenges of integrating natural language generation with versioned, governed institutional memory in distributed systems.

### Positioning TOOL's Memory Architecture

TOOL addresses gaps left by prior work through a *shared, event-sourced memory architecture* designed specifically for multi-agent consistency, explainability, and governance. Table 2.1 summarizes key differences:

**Table 2.1: Comparison of Agent Memory Architectures**

| System | Memory Type | Multi-Agent | Versioning | Governance | Provenance | Real-Time |
|--------|-------------|-------------|------------|------------|------------|-----------|
| Generative Agents [Park et al., 2023] | Episodic stream | ❌ | ❌ | ❌ | ❌ | N/A |
| MemGPT [Packer et al., 2023] | Hierarchical | ❌ | ❌ | ❌ | ❌ | N/A |
| Reflexion [Shinn et al., 2023] | Episodic reflections | ❌ | ❌ | ❌ | ❌ | N/A |
| LangChain [Chase, 2022] | Conversation buffer | ❌ | ❌ | ❌ | ❌ | ⚠️ |
| AutoGPT [Richards, 2023] | Short-term buffer | ❌ | ❌ | ❌ | ❌ | ⚠️ |
| **TOOL** | **Shared, project-specific** | **✅** | **✅ (DELTAS)** | **✅ (Promoter)** | **✅ (Git+Event)** | **✅ (NATS)** |

**Key distinctions:**

1. **Multi-agent consistency:** Existing systems focus on single-agent memory. TOOL provides a *shared projection* where all agents see the same memory state, preventing drift.

2. **Versioning:** Existing systems overwrite memory or append without structure. TOOL's event-sourced architecture (EVENTS → DELTAS → Projection) enables time-travel queries and deterministic replay.

3. **Governance:** Existing systems lack human oversight. TOOL's Promoter service and Admin UI provide approval workflows and audit trails for memory changes.

4. **Provenance:** Existing systems do not track *who* changed memory, *when*, or *why*. TOOL binds every memory item to Git commits and event metadata, enabling full traceability (see Section 2.5).

5. **Real-time propagation:** Existing systems rely on periodic polling or manual synchronization. TOOL uses NATS JetStream for low-latency, push-based memory updates (H4).

While prior work advances episodic and hierarchical memory for individual agents, TOOL addresses a distinct problem: *how to maintain consistent, versioned, governed institutional memory across multiple agents in production environments*. The next section examines prompt engineering and in-context learning—the foundation for memory injection's impact on agent correctness (H1).

---

**[End of Section 2.1]**

**Next:** Section 2.2 (Prompt Engineering & In-Context Learning)

---

## References for Section 2.1

*Note: These references should be integrated into the main Chapter 2 bibliography.*

- Anderson, J. R., Bothell, D., Byrne, M. D., Douglass, S., Lebiere, C., & Qin, Y. (2004). An integrated theory of the mind. *Psychological Review*, 111(4), 1036.
- Brown, T. B., Mann, B., Ryder, N., et al. (2020). Language models are few-shot learners. *Advances in Neural Information Processing Systems (NeurIPS)*, 33, 1877-1901.
- Chase, H. (2022). LangChain. GitHub repository: https://github.com/langchain-ai/langchain
- Laird, J. E. (2012). *The Soar Cognitive Architecture*. MIT Press.
- Packer, C., Wooders, S., Lin, K., Fang, V., Patil, S. G., Stoica, I., & Gonzalez, J. E. (2023). MemGPT: Towards LLMs as operating systems. *arXiv preprint arXiv:2310.08560*.
- Park, J. S., O'Brien, J. C., Cai, C. J., Morris, M. R., Liang, P., & Bernstein, M. S. (2023). Generative agents: Interactive simulacra of human behavior. *arXiv preprint arXiv:2304.03442*. (Also published in UIST 2023)
- Richards, T. B. (2023). AutoGPT. GitHub repository: https://github.com/Significant-Gravitas/AutoGPT
- Shinn, N., Cassano, F., Gopinath, A., Narasimhan, K., & Yao, S. (2023). Reflexion: Language agents with verbal reinforcement learning. *Advances in Neural Information Processing Systems (NeurIPS)*.
- Sumers, T. R., Yao, S., Narasimhan, K., & Griffiths, T. L. (2023). Cognitive architectures for language agents. *arXiv preprint arXiv:2309.02427*.
- Sun, R. (2006). The CLARION cognitive architecture: Extending cognitive modeling to social simulation. In R. Sun (Ed.), *Cognition and Multi-Agent Interaction* (pp. 79-99). Cambridge University Press.
- Wang, L., Ma, C., Feng, X., et al. (2024). A survey on large language model based autonomous agents. *arXiv preprint arXiv:2308.11432*.

