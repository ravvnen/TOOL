# Thesis Writing Guide

This folder contains structured guidance for writing your Master's thesis on TOOL (Event-Sourced Instruction Memory for AI Agents).

---

## Quick Start

**Start writing NOW**: Chapters 1-3 are ready!

1. **TODAY (Nov 11)**: Read [00_THESIS_PLAN.md](./00_THESIS_PLAN.md) - understand what you can write NOW vs. what needs implementation
2. **This Week (Nov 11-17)**: Write [Chapter 1: Introduction](./01_introduction.md)
3. **Next Week (Nov 18-24)**: Write [Chapter 2: Background](./02_background.md) and [Chapter 3: Design](./03_design.md)
4. **Dec 1+**: Write [Chapter 4: Implementation](./04_implementation.md) after v1.0 is stable

---

## File Structure

### Planning & Overview
- **[00_THESIS_PLAN.md](./00_THESIS_PLAN.md)** ← START HERE! Prioritized writing plan, timeline, scope decisions
- **[00_TABLE_OF_CONTENTS.md](./00_TABLE_OF_CONTENTS.md)** - Full ToC with page estimates
- **[00_front_matter.md](./00_front_matter.md)** - Abstract, preface, acknowledgements (write LAST)

### Chapters (Write in Order)

| Chapter | File | Status | Target Date | Can Write Now? |
|---------|------|--------|-------------|----------------|
| 1. Introduction | [01_introduction.md](./01_introduction.md) | ✅ Ready | Nov 13 | ✅ YES |
| 2. Background | [02_background.md](./02_background.md) | ✅ Ready | Nov 20 | ✅ YES |
| 3. Design | [03_design.md](./03_design.md) | ✅ Ready | Nov 24 | ✅ YES |
| 4. Implementation | [04_implementation.md](./04_implementation.md) | ⚠️ Waiting | Dec 1 | ⚠️ After v1.0 stable |
| 5. Methodology | [05_methodology.md](./05_methodology.md) | ⚠️ Waiting | Dec 8 | ⚠️ Design now, finalize after pilot |
| 6. Results | [06_results.md](./06_results.md) | 🚧 Blocked | Jan 17 | ❌ After experiments |
| 7. Discussion | [07_discussion.md](./07_discussion.md) | 🚧 Blocked | Jan 22 | ❌ After Chapter 6 |
| 8. Conclusion | [08_conclusion.md](./08_conclusion.md) | 🚧 Blocked | Jan 24 | ❌ Write last |

---

## Writing Status

### What You Can Write NOW (50-60% of thesis) ✅

Chapters 1-3 are **ready to write immediately**. They do NOT require:
- RAG implementation (v3.0)
- Extractor implementation (v6.0)
- Completed experiments
- Full v1.0 stability

**Total**: ~30-40 pages out of 60-80 page thesis

### What Needs Implementation First

- **Chapter 4**: Requires v1.0 to be stable (target: Dec 1)
- **Chapter 5**: Can outline now, but finalize after pilot experiments (Dec 1-8)
- **Chapters 6-8**: Require completed experiments (Dec 24-Jan 10)

---

## Key Decisions to Make THIS WEEK

### Decision 1: RAG & Extractor Scope (Due: Nov 15)

**Option A: Scope them OUT** (Recommended)
- Focus on event-sourced memory, replayability, FTS5 retrieval
- RAG and extractor → Future Work (Chapter 8.3)
- ✅ Can start writing NOW, complete on time
- ✅ Still novel contribution (event-sourcing for agent memory)

**Option B: Implement RAG, skip extractor**
- Add v3.0 RAG by Dec 10
- Compare FTS5 vs. RAG in evaluation (H2 becomes more interesting)
- ⚠️ Delays writing, tighter timeline

**Option C: Implement both** (Not recommended)
- ❌ High risk of missing deadlines

**Action**: Decide by Nov 15 after reading Chapter 1 draft (will clarify scope)

---

## Usage Guide

Each chapter file contains:
1. **Status** (ready/waiting/blocked)
2. **Target completion date**
3. **Estimated length** (pages)
4. **Writing instructions** (what to cover, tone, structure)
5. **Detailed section guidance** (subsection-by-subsection breakdown)
6. **Writing tips** (dos and don'ts)
7. **Checklist** (before moving to next chapter)
8. **Resources** (which existing docs to reference)
9. **Timeline** (day-by-day writing schedule)

### How to Use These Files

1. **Read the planning doc first**: [00_THESIS_PLAN.md](./00_THESIS_PLAN.md)
2. **Open the chapter file**: e.g., [01_introduction.md](./01_introduction.md)
3. **Read the entire file** (don't skip the guidance!)
4. **Draft the chapter** (in a separate .docx, .tex, or .md file for actual writing)
5. **Check the checklist** before moving on
6. **Iterate**: Get feedback, revise, polish

---

## Immediate Action Items (This Week)

### Nov 11-13 (3 days)
- [ ] Read [00_THESIS_PLAN.md](./00_THESIS_PLAN.md) in full
- [ ] Read [01_introduction.md](./01_introduction.md) guidance
- [ ] **Write Chapter 1 draft** (4-6 pages)
- [ ] Get feedback from advisor (if available)

### Nov 14-15 (2 days)
- [ ] Revise Chapter 1 based on feedback
- [ ] **DECIDE**: RAG scope (Option A, B, or C)
- [ ] Start literature review for Chapter 2 (skim 30-50 papers)

### Nov 16-17 (Weekend)
- [ ] Start Chapter 2 draft (write sections 2.1-2.3)
- [ ] Organize references in Zotero/Mendeley

---

## Integration with Existing Docs

These thesis chapter files **reference** your existing documentation:

| Thesis Chapter | References |
|----------------|------------|
| Chapter 1 | CLAUDE.md, METRICS.md, docs/THESIS.md |
| Chapter 2 | docs/THESIS.md (related work section) |
| Chapter 3 | docs/ARCHITECTURE.md, docs/SCHEMAS.md |
| Chapter 4 | src/TOOL/ codebase, recent PRs (#63, #60, etc.) |
| Chapter 5 | METRICS.md, docs/annotation_protocol.md |
| Chapter 6 | experiments/ logs, analysis/ notebooks |
| Chapter 7 | Reflection on Chapters 3-6 |
| Chapter 8 | VERSIONS.md (future work), entire thesis |

**Don't duplicate content**: Reference existing docs, extract/adapt key points.

---

## Tips for Success

### 1. Write daily (even if just 30 minutes)
- Consistency > marathon sessions
- Set daily target (e.g., 1 page/day = 7 pages/week)

### 2. Start with the easiest parts
- Chapter 1.1 (motivation) is easier than 1.3 (contributions)
- Chapter 2 (literature review) is mostly reading + summarizing

### 3. Don't aim for perfection in first draft
- Get ideas down, refine later
- "You can't edit a blank page"

### 4. Use the guidance files as templates
- Copy structure from chapter files
- Fill in with your content

### 5. Get feedback early and often
- Share drafts with advisor after each chapter
- Iterate quickly (don't wait until thesis is "done")

### 6. Parallel work: writing + implementation
- Mornings: Thesis writing (fresh mind)
- Afternoons: Implementation (v1.0, experiments)
- Evenings: Literature review, reading

---

## Milestones & Deadlines

### Nov 2025
- **Nov 13**: Chapter 1 draft done ✅
- **Nov 15**: RAG scope decision made ✅
- **Nov 20**: Chapter 2 draft done ✅
- **Nov 24**: Chapter 3 draft done ✅

### Dec 2025
- **Dec 1**: v1.0 stable, Chapter 4 draft done ✅
- **Dec 8**: Pilot experiments done, Chapter 5 finalized ✅
- **Dec 24**: Full experiments begin 🧪

### Jan 2025
- **Jan 10**: Experiments complete 🧪
- **Jan 17**: Chapter 6 draft done ✅
- **Jan 22**: Chapter 7 draft done ✅
- **Jan 24**: Chapter 8 + front matter done ✅
- **Jan 31**: First full thesis draft complete 📄

### Feb 2025
- **Feb 1-14**: Revisions (2 passes) 📝
- **Feb 15**: Submit to advisor for final feedback 📧
- **Feb 28**: Final revisions ✅

### Mar 2025
- **Mar 1**: Thesis submission 🎓

---

## Questions?

If guidance is unclear or you're stuck:
1. Re-read the planning doc: [00_THESIS_PLAN.md](./00_THESIS_PLAN.md)
2. Check existing project docs: CLAUDE.md, ARCHITECTURE.md, METRICS.md
3. Ask advisor for clarification
4. Remember: **Perfect is the enemy of done**. Ship drafts, iterate!

---

## Motivation

Writing a thesis is hard, but you've done the hard part (building the system!). Now you get to:
- **Tell your story** (why this matters)
- **Share your insights** (what you learned)
- **Contribute to the field** (help others build better systems)

You've got this! 💪

**Start with Chapter 1. Write for 30 minutes today. That's all it takes to begin.**
