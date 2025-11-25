# Chapter 2: Background & Related Work - REALISTIC OUTLINE

**Based on Comprehensive Research of Master's Thesis Best Practices**

---

## 📊 Research-Based Targets

**Target Length:** 10-12 pages (20-25% of 60-page thesis)
**Target Citations:** 20-25 academic sources
**Target Sections:** 5 main sections (not 7!)
**Detail Level:** 1-2 paragraphs per related work item (NOT full pages)
**Estimated Time:** 1 week (not 2!)

---

## 🎯 Chapter Purpose

**Primary Goal:** Position TOOL in the landscape and show that NO existing system combines:
1. Shared memory for multi-agent consistency
2. Event-sourced architecture (versioning, replay)
3. Governance workflows (human oversight)

**NOT:** Exhaustive literature review of everything tangentially related
**YES:** Focused review of directly relevant systems with clear gap analysis

---

## Structure (5 Sections, 10-12 pages total)

---

## 2.1 Introduction (0.5 pages)

### **Content:**
Brief overview of what this chapter covers:
- Agent memory approaches
- Current instruction management tools (Copilot, Claude, Cursor)
- Event sourcing & provenance foundations
- Gap analysis leading to TOOL

### **Writing Notes:**
- 1-2 paragraphs max
- No citations needed here
- Sets roadmap for chapter

---

## 2.2 Agent Memory Architectures (2-2.5 pages)

### **Purpose:**
Show that existing agent memory systems are single-agent focused with no versioning or governance.

### **Structure:**

**2.2.1 Episodic Memory Systems** (1 paragraph each):
- **Generative Agents [Park et al., 2023]**: Memory stream with recency/importance/relevance scoring. Single-agent, no versioning, no governance.
- **Reflexion [Shinn et al., 2023]**: Verbal reflections from past errors. Episodic memory only, no shared institutional memory.

**2.2.2 Hierarchical Memory** (1 paragraph):
- **MemGPT [Packer et al., 2023]**: Virtual context management with paging. Single-agent, no shared state, no provenance.

**2.2.3 Agent Frameworks** (1 paragraph):
- **LangChain [Chase, 2022], AutoGPT [Richards, 2023]**: Memory as pluggable module. No versioning, no multi-agent consistency.

**Comparison Table 2.1** (0.5 pages):

| System | Memory Type | Multi-Agent | Versioning | Governance | Provenance |
|--------|-------------|-------------|------------|------------|------------|
| Generative Agents | Episodic | ❌ | ❌ | ❌ | ❌ |
| MemGPT | Hierarchical | ❌ | ❌ | ❌ | ❌ |
| Reflexion | Episodic | ❌ | ❌ | ❌ | ❌ |
| LangChain | Conversation | ❌ | ❌ | ❌ | ❌ |
| **TOOL** | **Shared** | **✅** | **✅** | **✅** | **✅** |

**Synthesis** (1 paragraph):
While these systems advance individual agent memory, they do not address multi-agent consistency, versioning, or governance—the focus of this thesis.

### **Citations:** 5-6 papers
- Park et al., 2023; Packer et al., 2023; Shinn et al., 2023; Chase, 2022; Richards, 2023; Wang et al., 2024 (survey)

### **Writing Notes:**
- Be concise: 1-2 paragraphs per system
- Focus on limitations relevant to TOOL
- Don't explain every detail of their architecture
- Table provides visual clarity

---

## 2.3 Centralized Instruction Management (3-4 pages) ⭐ **MOST IMPORTANT**

### **Purpose:**
This is your main "state-of-the-art" section. Show that current tools (Copilot, Claude, Cursor) are insufficient despite being the closest to your work.

### **Structure:**

**Introduction** (1 paragraph):
Recent AI coding assistants recognize the need for project-specific instructions...

**2.3.1 File-Based Instruction Systems** (1 paragraph each):

**GitHub Copilot Instructions [GitHub, 2024]:**
- What: `.github/copilot-instructions.md` file
- How: Copilot reads file, includes in context
- Pros: Centralized, version-controlled via Git
- Cons: No real-time propagation (manual pull), no audit trail beyond Git, no governance workflow, manual merge conflicts

**Claude Project Instructions [Anthropic, 2024]:**
- What: Project-level instructions in Claude UI
- How: Applied to all conversations in project
- Cons: No versioning (overwrite only), no audit trail, not integrated with code repository

**Cursor .cursorrules [Cursor, 2024]:**
- What: `.cursorrules` file in project root
- How: Cursor IDE reads file
- Cons: Same as Copilot (no real-time, no audit, no governance)

**2.3.2 Prompt Management Services** (1 paragraph):

**PromptLayer [2023], Helicone [2023]:**
- What: Cloud services for managing prompt templates
- Cons: No event history (updates overwrite), manual synchronization, no governance workflow

**Comparison Table 2.2** (1 page):

| Feature | Copilot | Claude | Cursor | PromptLayer | **TOOL** |
|---------|---------|--------|--------|-------------|----------|
| **Storage** | Git file | Cloud | Git file | Cloud API | Event DB |
| **Versioning** | Git only | ❌ | Git only | ❌ | ✅ DELTAS |
| **Real-time** | ⚠️ Manual | ⚠️ Eventual | ⚠️ Manual | ⚠️ Poll | ✅ Push |
| **Audit trail** | Git log | ❌ | Git log | ❌ | ✅ Full |
| **Governance** | ❌ PR | ❌ | ❌ PR | ❌ | ✅ Promoter |
| **Multi-agent** | ❌ | ❌ | ❌ | ❌ | ✅ Shared |
| **Time-travel** | Git | ❌ | Git | ❌ | ✅ Replay |
| **Provenance** | Commit | ❌ | Commit | ❌ | ✅ Full |

**2.3.3 Limitations & Gaps** (1-2 paragraphs):
While these tools represent significant progress, they lack:
1. Real-time propagation (depends on local sync)
2. Audit trails beyond basic Git logs
3. Governance workflows (approval/rejection)
4. Multi-agent consistency guarantees
5. Time-travel queries ("what was active on date X?")

### **Citations:** 4-5 sources
- GitHub, 2024; Anthropic, 2024; Cursor, 2024; PromptLayer, 2023; Helicone, 2023

### **Writing Notes:**
- This section can be longer (3-4 pages) because it's your main comparison
- Be specific about limitations
- Table is critical—make it detailed
- Be respectful: "While X provides valuable capabilities, it does not address..."

---

## 2.4 Retrieval & Context Injection (2 pages)

### **Purpose:**
Show that RAG and prompt engineering alone don't solve the governance problem.

### **Structure:**

**2.4.1 Retrieval-Augmented Generation** (1 paragraph):
- **RAG [Lewis et al., 2020]**: Retrieve documents, inject into context
- **Recent surveys [Gao et al., 2023]**: Overview of techniques
- **Limitation**: Treats corpus as black box, no versioning, no governance

**2.4.2 Prompt Engineering & In-Context Learning** (1 paragraph):
- **Few-shot learning [Brown et al., 2020]**: LLMs learn from examples
- **Chain-of-Thought [Wei et al., 2022]**: Step-by-step reasoning
- **Connection to TOOL**: Memory injection similar to in-context learning, but with versioning + governance

**Comparison to TOOL** (1 paragraph):

| Aspect | RAG Systems | TOOL |
|--------|-------------|------|
| Retrieval | Semantic (embeddings) | Lexical (FTS5) baseline |
| Versioning | ❌ | ✅ Event-sourced |
| Governance | ❌ | ✅ Promoter + Admin |
| Provenance | Document metadata | Full Git + event |

**Synthesis** (1 paragraph):
RAG excels at retrieval but lacks governance. TOOL can integrate RAG techniques (v3.0) while maintaining event-sourced guarantees.

### **Citations:** 5-6 papers
- Lewis et al., 2020; Gao et al., 2023; Brown et al., 2020; Wei et al., 2022; Kojima et al., 2022; Ouyang et al., 2022

### **Writing Notes:**
- Keep RAG discussion brief (it's future work for you)
- Focus on limitations relevant to TOOL
- Mention you can integrate RAG later (v3.0)

---

## 2.5 Event Sourcing & Provenance (2-3 pages)

### **Purpose:**
Explain event sourcing pattern and why it's critical for TOOL's architecture.

### **Structure:**

**2.5.1 Event Sourcing Pattern** (2 paragraphs):
- **Fowler [2005]**: All state changes as immutable events
- **Kleppmann [2017]**: Event logs, deterministic replay, idempotency
- **Benefits**: Replayability, auditability, determinism
- Current state = replay all events from beginning

**2.5.2 Application to LLM Agent Memory** (1 paragraph):
Event sourcing uncommon in agent systems but provides guarantees needed for TOOL:
- Deterministic state reconstruction (H3)
- Full audit trail (who changed what when)
- Time-travel queries (memory state at any past time)

**2.5.3 Provenance Tracking** (1 paragraph):
- **Buneman et al. [2001], Cheney et al. [2009]**: Why/where provenance
- TOOL: Every memory item → Git commit + event metadata
- Enables explainability: "Why did agent say that?" → trace to rule + author

**TOOL's Architecture Mapping** (table/diagram):

```
Event Sourcing Concept → TOOL Implementation
──────────────────────────────────────────────
Event Log               → EVENTS stream
Approved Changes        → DELTAS stream
Projection              → SQLite DB (current + history)
Replay                  → Drop DB, replay DELTAS → reconstruct
Audit Trail             → AUDIT stream + promoter_audit table
Provenance              → source_bindings (Git commit, file, blob)
```

### **Citations:** 6-7 papers
- Fowler, 2005; Vernon, 2013; Kleppmann, 2017; Richardson, 2018; Buneman et al., 2001; Cheney et al., 2009; Kreps et al., 2011 (Kafka)

### **Writing Notes:**
- This can be 2-3 pages because it's foundational to your work
- Explain event sourcing clearly (not all readers will know it)
- Show concrete mapping to TOOL

---

## 2.6 Summary & Gap Analysis (1 page)

### **Purpose:**
Synthesize all previous sections and clearly state the research gap.

### **Structure:**

**Paragraph 1: Agent Memory Gap**
Generative Agents, MemGPT, Reflexion advance single-agent memory but don't provide shared institutional memory for multi-agent teams.

**Paragraph 2: Current Tools Gap**
Copilot, Claude, Cursor represent state-of-the-art but are static files with no real-time propagation, audit trails, or governance workflows.

**Paragraph 3: RAG Gap**
RAG systems excel at retrieval but treat the corpus as a black box with no versioning or governance.

**Paragraph 4: Event Sourcing Gap**
Event sourcing used in microservices but not applied to LLM agent memory systems.

**Paragraph 5: The Gap TOOL Addresses**
"This thesis addresses this gap by combining event-sourced architecture with shared memory and governance workflows, enabling multi-agent consistency, explainability, and auditability in LLM systems. No existing system provides all of: (1) shared memory for multi-agent consistency, (2) event-sourced versioning and replay, (3) governance workflows with human oversight, (4) full provenance tracking, and (5) real-time propagation."

### **Citations:** None (synthesis of previous sections)

### **Writing Notes:**
- This is your "research gap" statement
- Be clear and direct
- Sets up Chapter 3 (your solution)

---

## 📊 Summary Statistics

**Estimated Breakdown:**
- 2.1 Agent Memory: 2-2.5 pages (5-6 citations)
- 2.2 (merged into 2.1 above)
- 2.3 Centralized Tools: 3-4 pages (4-5 citations) ⭐ **LONGEST**
- 2.4 RAG & Prompt Engineering: 2 pages (5-6 citations)
- 2.5 Event Sourcing: 2-3 pages (6-7 citations)
- 2.6 Summary & Gaps: 1 page (0 new citations)

**Total: 10-12 pages**
**Total Citations: 20-25** (NOT 50-100!)

---

## 🎯 Key Differences from Original Outline

### **Original (Too Ambitious):**
- ❌ 15 pages, 7 sections, 50-100 citations
- ❌ Section 2.2 (Prompt Engineering) as separate 2 pages
- ❌ Section 2.6 (Explainability) as separate 2 pages
- ❌ Detailed multi-paragraph explanations of each system
- ❌ PhD-level exhaustiveness

### **Revised (Realistic for Master's):**
- ✅ 10-12 pages, 5 sections, 20-25 citations
- ✅ Prompt engineering merged into 2.4 (brief mention)
- ✅ Explainability merged into 2.5 (provenance)
- ✅ 1-2 paragraph summaries per system
- ✅ Master's-level focused review

---

## 🚀 Next Steps

1. **Review this outline** - does it make sense?
2. **Write Section 2.3 first** (Centralized Tools) - most important
3. **Then revise Section 2.1** (make concise version)
4. **Then write 2.4, 2.5, 2.6** in order
5. **Assemble full Chapter 2** and check length

---

## 📝 Writing Tips

### **DO:**
- ✅ Keep paragraphs focused (1 idea per paragraph)
- ✅ Use comparison tables (visual clarity)
- ✅ Be critical: "While X is valuable, it does not address..."
- ✅ Connect back to TOOL's requirements
- ✅ End each section with synthesis

### **DON'T:**
- ❌ Write multi-page explanations of each system
- ❌ Include every tangentially related paper
- ❌ Just list papers without critique
- ❌ Be vague about limitations
- ❌ Forget to show the gap

---

**Status:** Ready to start writing! Begin with Section 2.3 (Centralized Tools) - it's the most important and where you move content from Chapter 1.
