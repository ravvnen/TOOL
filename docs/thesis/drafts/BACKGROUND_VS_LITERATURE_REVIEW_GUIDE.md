# Background vs. Literature Review: What Goes Where?

**Purpose:** Clarify the distinction between Background (prerequisite knowledge) and Literature Review (what others have done)

**Date:** November 12, 2025

**Key Question:** "What's the difference between Background and Literature Review, and should they be in one chapter or separate?"

---

## 🎯 The Core Distinction

### **Background = "What the reader needs to know"**
**Purpose:** Provide prerequisite knowledge, concepts, definitions

**Examples:**
- "What is event sourcing?" (concept explanation)
- "What is RAG?" (technique explanation)
- "What are LLMs?" (technology background)
- "What is provenance tracking?" (foundational concept)

**Tone:** Educational, tutorial-like
**Focus:** Teach concepts the reader needs to understand your work

---

### **Literature Review / Related Work = "What others have done"**
**Purpose:** Survey existing research, systems, solutions

**Examples:**
- "What agent memory systems exist?" (Park, MemGPT, Reflexion)
- "What instruction management tools exist?" (Copilot, Claude, Cursor)
- "What RAG systems exist?" (Lewis et al., 2020)
- "What event-sourced systems exist?" (microservices examples)

**Tone:** Critical, analytical, comparative
**Focus:** Show what exists, identify gaps, position your work

---

## 📚 What the Research Says

### **Academia Stack Exchange - Top Answer:**

> **Background Chapter:**
> "A Background chapter is best used to present contextual or prerequisite information that is important or essential to understand the main body of your thesis."

> **Literature Review:**
> "Literature Review is a critical assessment of existing works relevant to a research topic. It helps to acknowledge scholars in the discipline area and also to identify existing gaps to justify the need for the present study."

**Source:** https://academia.stackexchange.com/questions/49582/

### **Key Insight:**
> "There are no widely accepted rules for what is appropriate for Background or Literature Review chapters, or whether your thesis should include either or both. Some theses may combine both into a single chapter, while others keep them separate depending on disciplinary conventions."

---

## 🖥️ Common Patterns in Computer Science

### **Option 1: Combined Chapter (Most Common in CS)** ✅ **RECOMMENDED**

**Chapter 2: Background & Related Work**

Structure:
```
2.1 Introduction
2.2 Technical Background (concepts, definitions)
    - Event sourcing (what it is)
    - RAG (what it is)
    - Provenance (what it is)
2.3 Related Work (existing systems, papers)
    - Agent memory systems
    - Instruction management tools
    - RAG systems
    - Event-sourced systems
2.4 Gap Analysis
```

**Advantages:**
- ✅ Keeps reader in one narrative flow
- ✅ Avoids repetition
- ✅ Common in CS (experimental/systems theses)
- ✅ Cleaner structure (fewer chapters)

**Disadvantages:**
- ⚠️ Chapter can get long (but 10-15 pages is fine for Master's)

---

### **Option 2: Separate Chapters** (Less Common in CS)

**Chapter 2: Background**
```
2.1 Introduction
2.2 Event Sourcing
2.3 RAG & Retrieval
2.4 Provenance Tracking
2.5 LLM Agent Architectures (concepts only)
```

**Chapter 3: Related Work**
```
3.1 Introduction
3.2 Agent Memory Systems (Park, MemGPT, etc.)
3.3 Instruction Management Tools (Copilot, Claude, Cursor)
3.4 RAG Systems (Lewis et al.)
3.5 Event-Sourced Systems (microservices)
3.6 Gap Analysis
```

**Chapter 4: Design & Architecture** (your solution)

**Advantages:**
- ✅ Clear separation of concepts vs. systems
- ✅ Easier to write (one focus per chapter)

**Disadvantages:**
- ❌ Can feel repetitive (explain event sourcing in Ch 2, then discuss event-sourced systems in Ch 3)
- ❌ Less common in CS experimental theses
- ❌ Your thesis now has 9 chapters instead of 8 (may exceed page limit)

---

## 🎓 Best Practices from Multiple Sources

### **WPI (Worcester Polytechnic Institute):**

> "The Background/Literature Review chapter should summarize relevant literature to provide context for the reader and justify your study by demonstrating gaps in the knowledge base."

**Source:** https://libguides.wpi.edu/c.php?g=889594&p=6395829

### **SAGE Publishing - Literature Review Guide:**

> "A literature review has four main objectives: 1) It surveys the literature in your chosen area of study, 2) It synthesizes the information to identify what is known and not known, 3) It identifies areas of controversy, 4) It formulates questions that need further research."

**Source:** SAGE Chapter 2 Review of Literature guide

### **Key Takeaway:**
Your Chapter 2 should do BOTH:
- Educate the reader (Background)
- Survey existing work (Literature Review)
- Show the gap your work fills

---

## 🔍 Applied to YOUR Thesis (TOOL)

### **Current Structure: "Chapter 2: Background & Related Work"** ✅ **GOOD CHOICE**

Your current approach combines both, which is **standard for CS experimental theses**.

Let's map what you have:

---

### **What Goes Where in YOUR Chapter 2?**

#### **Background (Prerequisite Knowledge):**

These sections teach concepts:

**2.4 Event Sourcing & Provenance** (2-3 pages)
- **Purpose:** Teach reader what event sourcing IS
- **Content:**
  - Event sourcing pattern (Fowler, Kleppmann)
  - Immutable events, deterministic replay, idempotency
  - Provenance tracking (why/where provenance)
  - Mapping to TOOL (EVENTS → DELTAS → Projection)
- **Why it's background:** Reader needs to understand event sourcing to understand your architecture

**2.3 Retrieval & Context Injection** (2 pages) - **Partial Background**
- **Purpose:** Explain RAG and prompt engineering concepts
- **Content:**
  - What is RAG? (Lewis et al., 2020)
  - What is prompt engineering? (few-shot, chain-of-thought)
  - How does in-context learning work? (Brown et al., 2020)
- **Why it's background:** Reader needs to understand these concepts to understand H1 (correctness) and H2 (retrieval)

---

#### **Literature Review / Related Work (What Others Have Done):**

These sections survey existing systems:

**2.1 Agent Memory Architectures** (2-2.5 pages)
- **Purpose:** Show what agent memory systems exist
- **Content:**
  - Generative Agents (Park et al., 2023)
  - MemGPT (Packer et al., 2023)
  - Reflexion (Shinn et al., 2023)
  - LangChain, AutoGPT
  - Comparison table
- **Why it's related work:** Shows existing solutions and their limitations

**2.2 Centralized Instruction Management** (3-4 pages) ⭐ **MOST IMPORTANT**
- **Purpose:** Show state-of-the-art instruction tools
- **Content:**
  - GitHub Copilot Instructions
  - Claude Project Instructions
  - Cursor Rules
  - PromptLayer, Helicone
  - Detailed comparison table
- **Why it's related work:** Shows closest competitors to TOOL

**2.3 Retrieval & Context Injection** (2 pages) - **Partial Related Work**
- **Purpose:** Show RAG systems that exist (in addition to explaining concept)
- **Content:**
  - RAG systems (Lewis et al., 2020; surveys)
  - Hybrid retrieval approaches
  - Connection to TOOL's FTS5 baseline
- **Why it's related work:** Shows existing retrieval approaches

---

#### **Gap Analysis (Bridge to Your Work):**

**2.5 Summary & Gap Analysis** (1 page)
- **Purpose:** Synthesize all previous sections, state research gap
- **Content:**
  - Gap #1: Agent memory (single-agent, no multi-agent consistency)
  - Gap #2: Instruction tools (no versioning, no governance)
  - Gap #3: RAG (no governance)
  - Gap #4: Event sourcing (not applied to LLM memory)
  - **The gap TOOL fills:** No system provides ALL of these
- **Why it's needed:** Justifies your research, leads to Chapter 3

---

## ✅ Recommended Structure for YOUR Chapter 2

### **"Chapter 2: Background & Related Work"** (10-12 pages)

**Mix background and related work naturally:**

```
2.1 Introduction (0.5 pages)
    - Preview of chapter
    - What reader will learn (background)
    - What systems exist (related work)
    - Research gap this thesis addresses

2.2 Agent Memory Architectures (2-2.5 pages) [RELATED WORK]
    - Survey existing agent memory systems
    - Show limitations (no multi-agent, no versioning)

2.3 Centralized Instruction Management (3-4 pages) [RELATED WORK] ⭐
    - Survey state-of-the-art tools (Copilot/Claude/Cursor)
    - Detailed comparison showing limitations

2.4 Retrieval & Context Injection (2 pages) [BACKGROUND + RELATED WORK]
    - Background: Explain RAG, prompt engineering concepts
    - Related work: Survey RAG systems
    - Show limitation: no governance

2.5 Event Sourcing & Provenance (2-3 pages) [BACKGROUND]
    - Background: Explain event sourcing pattern
    - Background: Explain provenance tracking
    - Show it's used in microservices (related work)
    - Show gap: not applied to LLM agent memory

2.6 Summary & Gap Analysis (1 page) [SYNTHESIS]
    - Synthesize all gaps
    - State: No system provides ALL features
    - Lead to Chapter 3: TOOL's solution
```

**Total: 10-12 pages**

**Key characteristics:**
- ✅ Combines background (concepts) and related work (systems)
- ✅ Flows naturally (no artificial separation)
- ✅ Each section has clear purpose
- ✅ Builds toward gap analysis
- ✅ Standard for CS experimental theses

---

## 📊 How to Write Each Type of Content

### **When Writing BACKGROUND (Concepts):**

**Purpose:** Educate the reader

**Tone:** Tutorial-like, clear definitions

**Structure:**
1. Define the concept
2. Explain how it works
3. Give examples
4. Explain why it matters to your work

**Example - Event Sourcing:**
```
Event sourcing is a pattern where all changes to application state are stored as a
sequence of immutable events [Fowler, 2005]. Rather than storing only the current state,
event-sourced systems maintain an append-only log of all state transitions. The current
state can be reconstructed by replaying all events from the beginning.

For example, in a banking system, instead of storing "balance = $100", an event-sourced
system stores:
- Event 1: Deposited $100
- Event 2: Withdrew $20
- Event 3: Deposited $20
Current balance ($100) is derived by replaying these events.

This pattern provides three key benefits [Kleppmann, 2017]:
1. Auditability: Full history preserved
2. Replayability: State reconstructable at any point in time
3. Debugging: Can replay events to reproduce bugs

In TOOL, event sourcing enables deterministic state reconstruction (H3), full audit trails
(R3: explainability), and time-travel queries (query memory state at any past time).
```

**Key features:**
- ✅ Clear definition
- ✅ Concrete example
- ✅ Explains benefits
- ✅ Connects to your work (TOOL)

---

### **When Writing RELATED WORK (Existing Systems):**

**Purpose:** Show what exists, identify gaps

**Tone:** Critical, analytical, comparative

**Structure:**
1. Brief introduction to system/paper
2. Key features
3. Limitations relevant to your work
4. Connection to your work (how it differs)

**Example - Generative Agents:**
```
Generative Agents [Park et al., 2023] maintains a memory stream of observations and
reflections, using recency/importance/relevance scoring for retrieval. Each agent stores
timestamped observations (e.g., "Alice walked into coffee shop at 10:30 AM") and
periodically generates reflections—higher-level summaries (e.g., "Alice prefers morning
coffee meetings"). This architecture enables agents to exhibit temporally consistent
behavior in interactive simulations.

However, Generative Agents focuses on individual agent memory with no mechanism for
sharing institutional knowledge across multiple agents. Memory persists only within a
single agent's lifetime, with no versioning (rules cannot be rolled back to previous
states), no governance workflow (no human oversight of memory changes), and no
provenance tracking (cannot trace why a memory exists or who created it).

In contrast, TOOL provides shared memory across multiple agents via a centralized
projection database, event-sourced versioning enabling rollback to any previous state,
governance workflows with human oversight (Promoter + Admin UI), and full provenance
tracking via source_bindings (Git commit, author, timestamp).
```

**Key features:**
- ✅ Brief system description
- ✅ Critical evaluation (what it lacks)
- ✅ Direct comparison to your work
- ✅ Shows the gap

---

## 🎯 Common Mistakes to Avoid

### **❌ Mistake 1: Writing Background Without Connection to Your Work**

**Bad:**
```
Event sourcing is a pattern where state changes are stored as events. Martin Fowler
defined it in 2005. It has three benefits: auditability, replayability, and debugging.
Many companies use event sourcing, including Netflix and Amazon.
```

**Why it's bad:** Reads like Wikipedia, no connection to TOOL

**Good:**
```
Event sourcing [Fowler, 2005] stores state changes as immutable events, enabling
deterministic state reconstruction—a property TOOL leverages to prove replayability
(H3). By replaying all DELTAS from sequence 1, TOOL can reconstruct the exact projection
state with SRA = 1.00, ensuring consistency across agents even after system failures.
```

**Why it's good:** Directly connects concept to your contribution

---

### **❌ Mistake 2: Writing Related Work Without Critical Evaluation**

**Bad:**
```
Generative Agents [Park et al., 2023] is a system where agents have memory.
MemGPT [Packer et al., 2023] is another system with hierarchical memory.
Reflexion [Shinn et al., 2023] uses verbal reflections.
```

**Why it's bad:** Just lists systems, no analysis, no gap

**Good:**
```
While Generative Agents [Park et al., 2023], MemGPT [Packer et al., 2023], and
Reflexion [Shinn et al., 2023] advance individual agent memory, they do not address
multi-agent consistency. Each agent maintains separate memory with no shared state,
leading to drift when multiple agents operate on the same project. TOOL addresses this
gap through a centralized projection database that all agents query, ensuring identical
memory states (H5: consistency).
```

**Why it's good:** Shows gap, positions your work

---

### **❌ Mistake 3: Putting Too Much Background, Too Little Related Work**

**Imbalanced Chapter 2:**
```
2.1 Introduction (0.5 pages)
2.2 Event Sourcing (5 pages) ← TOO MUCH BACKGROUND
2.3 RAG (4 pages) ← TOO MUCH BACKGROUND
2.4 Provenance (3 pages) ← TOO MUCH BACKGROUND
2.5 Related Work (2 pages) ← TOO LITTLE
2.6 Gap Analysis (0.5 pages)
Total: 15 pages (but 12 pages is background!)
```

**Why it's bad:** Reads like a textbook, not positioning your work

**Balanced Chapter 2:**
```
2.1 Introduction (0.5 pages)
2.2 Agent Memory Systems (2.5 pages) ← RELATED WORK (brief background embedded)
2.3 Centralized Tools (3.5 pages) ← RELATED WORK ⭐
2.4 RAG & Prompt Eng. (2 pages) ← BACKGROUND + RELATED WORK (mixed)
2.5 Event Sourcing (2.5 pages) ← BACKGROUND (with related work examples)
2.6 Gap Analysis (1 page) ← SYNTHESIS
Total: 12 pages (balanced)
```

**Why it's good:** Focuses on gap, embeds background naturally

---

## 🚀 Recommended Approach for TOOL Thesis

### **Use Combined "Background & Related Work" Chapter** ✅

**Rationale:**
1. ✅ Standard in CS experimental theses
2. ✅ Keeps reader in one narrative flow
3. ✅ Avoids repetition
4. ✅ Your TOC already follows this pattern!

### **Balance:**
- ~40% Background (concepts: event sourcing, RAG, provenance)
- ~50% Related Work (systems: agent memory, Copilot/Claude/Cursor)
- ~10% Gap Analysis (synthesis)

### **Writing Strategy:**

**For each section, ask:**
1. **Does the reader need to learn this concept?** → Background
2. **Do I need to show what others have done?** → Related Work
3. **Can I do both in one section?** → Yes! (e.g., Section 2.4: explain RAG concept, then survey RAG systems)

**Example - Section 2.4 (Retrieval & Context Injection):**

```
2.4 Retrieval & Context Injection

[BACKGROUND PART - 1 paragraph]
Retrieval-Augmented Generation (RAG) [Lewis et al., 2020] combines parametric memory
(LLM weights) with non-parametric memory (retrieved documents). Given a query, RAG first
retrieves relevant documents from a corpus using semantic similarity (typically via
embeddings), then conditions the LLM generation on both the query and retrieved context.
This approach improves factual accuracy for knowledge-intensive tasks.

[RELATED WORK PART - 1 paragraph]
Recent RAG systems [Gao et al., 2023] employ hybrid retrieval (combining lexical and
semantic search), query rewriting, and reranking. However, these systems treat the
corpus as a static black box with no versioning—if the corpus changes, there is no
audit trail of what was retrieved at what time. Similarly, there is no governance
workflow to review or approve corpus changes.

[CONNECTION TO TOOL - 1 paragraph]
TOOL uses FTS5 (lexical search) as a baseline retrieval mechanism (H2: retrieval quality).
While FTS5 lacks the semantic understanding of RAG, it provides deterministic, reproducible
results essential for our evaluation. Future work (v3.0) will integrate RAG while maintaining
event-sourced governance—ensuring every corpus change is versioned, auditable, and traceable.
```

**See how it flows?** Background → Related Work → Your Work, all in one section!

---

## ✅ Final Recommendation

**Your current "Chapter 2: Background & Related Work" is the RIGHT approach!**

**Just ensure:**
1. ✅ Each section balances background (concepts) and related work (systems)
2. ✅ You critically evaluate limitations (not just describe)
3. ✅ You connect everything back to TOOL's gap
4. ✅ Section 2.3 (Centralized Tools) is your MAIN related work comparison

---

**Status:** Background vs. Literature Review distinction clarified! ✅
