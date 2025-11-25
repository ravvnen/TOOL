# Terminology Update: "Instruction Memory" → "Shared Memory System"

**Date:** November 12, 2025
**File Updated:** `chapter_01_introduction_FINAL.md`

---

## 🎯 What Changed

Based on your feedback, we updated the terminology throughout Chapter 1 to more accurately reflect what TOOL actually does.

### Before: "Instruction Memory System"
- ❌ Too narrow—implied only prescriptive rules
- ❌ Didn't capture facts, conventions, architecture

### After: "Shared Memory System" (or "Event-Sourced, Shared Memory System")
- ✅ Generic enough to cover rules, facts, conventions, domain knowledge
- ✅ Emphasizes **multi-agent consistency** (core contribution)
- ✅ "Event-sourced" modifier highlights architecture contribution

---

## 📝 Key Updates

### 1. **Research Question** (Section 1.1)

**Before:**
```
How can we design an instruction memory system for LLM agents that provides
multi-agent consistency, explainability, replayability, and governance...?
```

**After:**
```
How can we design a shared memory system for LLM agents that provides
multi-agent consistency, explainability, replayability, and governance...?
```

---

### 2. **Definition** (Section 1.2)

**Before:**
```
By instruction memory system, we mean a persistent storage and retrieval
mechanism specifically for rules and guidelines...
```

**After:**
```
By shared memory system, we mean a persistent storage and retrieval mechanism
for project-specific context that governs agent behavior in a particular
codebase or team environment. This is distinct from:
- Conversational memory (short-term chat history)
- Episodic memory (long-term interaction logs)
- General knowledge bases (encyclopedic facts)

A key category of memory content is instructions—rules, guidelines, and
conventions that direct agent behavior—but the system also stores facts,
architectural decisions, and domain knowledge.
```

**Why this is better:**
- ✅ Clearly differentiates from other memory types
- ✅ Explains that "instructions" are ONE type of content, not ALL content
- ✅ Preserves the term "instruction" for when discussing rules specifically

---

### 3. **Contribution #1** (Section 1.3)

**Before:**
```
1. Architecture: An event-sourced instruction memory system [...]
```

**After:**
```
1. Architecture: An event-sourced, shared memory system for LLM agents [...]
```

---

### 4. **Research Questions** (Section 1.2)

**Before:**
```
RQ1: Can event-sourced memory provide consistent rule retrieval...?
RQ2: Does rule injection improve agent correctness...?
RQ4: What is the latency of rule updates propagating...?
```

**After:**
```
RQ1: Can event-sourced memory provide consistent retrieval...?
RQ2: Does memory injection improve agent correctness...?
RQ4: What is the latency of memory updates propagating...?
```

---

### 5. **Hypotheses** (Section 1.2)

**Before:**
```
H1: Agents with injected rules produce responses rated higher...
H2: MemoryCompiler retrieves relevant rules with Precision@5 ≥ 0.70...
H4: Median latency from rule change to agent UI display < 5 seconds
H5: Repeated queries produce identical cited rule IDs with ≥ 95% agreement
```

**After:**
```
H1: Agents with injected memory produce responses rated higher...
H2: MemoryCompiler retrieves relevant memory items with Precision@5 ≥ 0.70...
H4: Median latency from memory change to agent UI display < 5 seconds
H5: Repeated queries produce identical cited memory IDs with ≥ 95% agreement
```

---

### 6. **Claims** (Section 1.3)

**Before:**
```
- Event-sourced memory improves multi-agent consistency compared to static prompts...
- Rule injection improves correctness on domain-specific tasks
- The system achieves low-latency rule propagation (< 5 seconds)
- Lexical retrieval (FTS5) is sufficient for small rule sets...
```

**After:**
```
- Event-sourced, shared memory improves multi-agent consistency compared to
  static prompts or no shared memory
- Memory injection (providing relevant context to agents) improves correctness...
- The system achieves low-latency memory propagation (< 5 seconds)
- Lexical retrieval (FTS5) is sufficient for small memory sets...
```

---

### 7. **Scope** (Section 1.2)

**Before:**
```
Out of scope:
- Large-scale deployments (1000s of agents or rules)
- Automatic rule extraction from documentation — rules are manually seeded...
```

**After:**
```
Out of scope:
- Large-scale deployments (1000s of agents or memory items)
- Automatic memory extraction from documentation — memory items are manually seeded...
```

---

## 🔑 Key Terminology Decisions

| Term | When to Use | Example |
|------|-------------|---------|
| **Shared memory system** | General reference to TOOL | "TOOL is a shared memory system for LLM agents" |
| **Event-sourced, shared memory system** | When emphasizing architecture | "We propose an event-sourced, shared memory system..." |
| **Memory items** | Generic reference to stored content | "Teams manage 10s to 100s of memory items" |
| **Instructions** | When specifically referring to rules/guidelines | "A key category of memory content is instructions" |
| **Memory injection** | Providing context to agents | "Memory injection improves agent correctness (H1)" |
| **Memory citations** | References agents provide | "Agents produce the same memory citations (H5)" |
| **Project-specific context** | Type of memory content | "Shared memory for project-specific context" |

---

## ✅ What This Achieves

### 1. **Accurate Scope**
- ✅ TOOL stores more than just rules—facts, conventions, architecture, domain knowledge
- ✅ No longer implies "prescriptive instructions only"

### 2. **Emphasizes Core Contribution**
- ✅ **"Shared"** → highlights multi-agent consistency (drift problem)
- ✅ **"Event-sourced"** → highlights architecture (versioning, replay, governance)

### 3. **Clear Differentiation**
- ✅ Distinct from conversational memory (chat history)
- ✅ Distinct from episodic memory (long-term logs)
- ✅ Distinct from general knowledge bases (Wikipedia-style)

### 4. **Preserves "Instruction" Term**
- ✅ Still use "instruction" when referring to rules/guidelines specifically
- ✅ Acknowledges that instructions are one important category of memory content
- ✅ Example: "A key category of memory content is instructions—rules, guidelines, and conventions that direct agent behavior"

---

## 📊 Terminology Hierarchy

```
Shared Memory System (TOOL)
├── Project-Specific Context
│   ├── Instructions (rules, guidelines, conventions)
│   ├── Facts (architectural, domain-specific)
│   └── Decisions (design choices, rationale)
└── Architecture: Event-Sourced
    ├── EVENTS stream (raw inputs)
    ├── DELTAS stream (approved changes)
    └── AUDIT stream (governance log)
```

**Not included:**
- ❌ Conversational memory (chat history)
- ❌ Episodic memory (interaction logs)
- ❌ General world knowledge (encyclopedic facts)

---

## 🎓 Academic Framing

Your thesis title could now be:

**Option 1 (Emphasizes "shared"):**
> "A Shared Memory System for Multi-Agent LLM Deployments: An Event-Sourced Architecture for Consistency, Explainability, and Governance"

**Option 2 (Emphasizes "event-sourced"):**
> "An Event-Sourced, Shared Memory System for LLM Agents: Enabling Multi-Agent Consistency and Governance"

**Option 3 (Simple):**
> "TOOL: A Shared Memory System for LLM Agents"

Subtitle:
> "An event-sourced architecture enabling multi-agent consistency, explainability, replayability, and governance in project-specific instruction memory"

---

## 📚 When to Use Each Term

### In Introduction (Chapter 1):
- ✅ "Shared memory system" (main term)
- ✅ "Event-sourced, shared memory system" (when emphasizing architecture)
- ✅ "Project-specific context" (when describing content)
- ✅ "Instructions" (when specifically discussing rules)

### In Background (Chapter 2):
- ✅ "Instruction memory" can appear when reviewing related work that uses this term
- ✅ But always clarify: "...which we implement as a shared memory system"

### In Design (Chapter 3):
- ✅ "Shared memory architecture"
- ✅ "Memory items" (generic)
- ✅ "Instructions" when referring to specific item types

### In Implementation (Chapter 4):
- ✅ "Memory system"
- ✅ "MemoryCompiler" (component name)
- ✅ "Memory injection" (process)

### In Evaluation (Chapter 5-6):
- ✅ "Memory injection" (experimental condition)
- ✅ "Memory items" (what's retrieved)
- ✅ "Memory citations" (what agents produce)

---

## 🔄 Consistency Check

All instances updated in `chapter_01_introduction_FINAL.md`:

- [x] Research question (1.1)
- [x] Definition paragraph (1.2)
- [x] Research questions RQ1-RQ5 (1.2)
- [x] Hypotheses H1-H5 (1.2)
- [x] Out of scope (1.2)
- [x] Contribution #1 (1.3)
- [x] Contribution #3 (1.3)
- [x] Contribution #4 (1.3)
- [x] Claims (1.3)
- [x] Multi-agent consistency definition (1.2)

---

## 📝 Action Items

### ✅ Done:
- [x] Updated all terminology in Chapter 1
- [x] Added clear definition distinguishing from other memory types
- [x] Preserved "instruction" term for specific use cases
- [x] Ensured consistency across RQs, hypotheses, contributions, claims

### 🔜 Next:
- [ ] Review updated Chapter 1 for readability
- [ ] Ensure terminology consistency in other thesis documents:
  - [ ] THESIS.md
  - [ ] METRICS.md
  - [ ] ARCHITECTURE.md
  - [ ] API.md
- [ ] Update thesis title/subtitle (if needed)
- [ ] Verify citations still make sense with new terminology

---

## ✨ Summary

**Old framing:**
- "Instruction memory system" = too narrow, implied only rules

**New framing:**
- "Shared memory system" = accurate, generic, emphasizes core contribution
- "Instructions" still used when referring to rules/guidelines specifically
- Clear differentiation from other memory types

**Impact:**
- ✅ More accurate representation of TOOL's capabilities
- ✅ Better aligns with actual implementation (MemoryCompiler, FTS5, etc.)
- ✅ Emphasizes multi-agent consistency (core contribution)
- ✅ Still acknowledges that instructions are important memory content

---

**Status:** Terminology updated and consistent throughout Chapter 1 ✅
