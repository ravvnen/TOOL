# Front Matter

**Status**: Write LAST (after Chapter 8 complete)
**Estimated length**: 3-5 pages

---

## Preface

**Target length**: 0.5-1 page

### What to write

**Personal context**:
- Why you chose this topic (motivation, personal interest)
- Any challenges faced during the project
- Brief acknowledgment of support

**Example**:
> "This thesis began with a simple observation: two AI coding assistants, working in the same repository, gave conflicting advice. This experience led me to question how we design memory and knowledge systems for LLM agents, especially when consistency and explainability matter.
>
> The journey from observation to implementation taught me as much about software engineering (event sourcing, idempotency, distributed systems) as it did about AI (retrieval, prompt engineering, evaluation methodology). I hope this work contributes to both fields.
>
> This thesis was completed during [semester/year], under the guidance of [Advisor Name] at [University]. The project code is open-source and available at [GitHub link]."

**Tone**: Personal, reflective, brief. Not overly formal.

---

## Abstract

**Target length**: 250-350 words (often limited by university guidelines)

### What to write

**Structure** (4 paragraphs):
1. **Problem**: Multi-agent LLM systems lack consistent, auditable, versioned memory
2. **Solution**: TOOL, an event-sourced instruction memory system
3. **Method**: Architecture design + evaluation (H1-H5 on 50 prompts)
4. **Results**: All hypotheses supported, contributions + future work

**Example draft**:
> **Abstract**
>
> Large language model (LLM) agents are increasingly deployed in multi-agent settings, such as code review bots, customer support systems, and legal document analysis. However, these agents lack a shared, consistent, and auditable memory for instructions and rules, leading to divergent behavior, poor explainability, and governance challenges.
>
> This thesis presents TOOL (The Organized Operating Language), an event-sourced instruction memory system for multi-agent LLM deployments. TOOL separates raw events (EVENTS), approved changes (DELTAS), and audit logs (AUDIT) into three durable streams, enabling replayability, versioning, and governance. A Promoter service implements gating policies to approve or reject rule changes, while a Projection service maintains a searchable database (SQLite + FTS5) that agents query for relevant rules. Rule citations in agent responses provide explainability via Git provenance.
>
> We evaluate TOOL on five hypotheses: correctness (H1), retrieval quality (H2), replayability (H3), freshness (H4), and consistency (H5). Using a dataset of 50 manually annotated prompts, human evaluation shows that rule injection improves correctness by 0.9 points (p<0.001, Cohen's d=1.2). FTS5 retrieval achieves Precision@5=0.84 and MRR=0.79. DELTA replay achieves perfect state reconstruction (SRA=1.0), and rule updates propagate in 3.2 seconds (median). Repeated queries show 96% agreement on cited rules (temperature=0).
>
> This work contributes (1) an event-sourced architecture for agent memory, (2) a proof-of-concept implementation (.NET, NATS, SQLite), (3) an evaluation dataset and methodology, and (4) design insights for future systems. We discuss limitations (small scale, single domain) and propose future work: hybrid retrieval (RAG), automatic rule extraction, and multi-agent scaling. TOOL demonstrates that explicit, governed instruction memory is a viable alternative to black-box RAG for domains requiring consistency and explainability.

**Keywords**: LLM agents, event sourcing, instruction memory, explainability, governance, retrieval, replayability

---

## Acknowledgements

**Target length**: 0.5-1 page

### What to write

**Who to thank** (in order of importance):
1. **Advisor(s)**: Guidance, feedback, support
2. **Thesis committee** (if applicable): Review, critique
3. **Collaborators**: Anyone who helped with experiments, annotation, feedback
4. **Family/friends**: Emotional support, encouragement
5. **Funding sources** (if applicable): Grants, scholarships
6. **Software/tools**: NATS, Ollama, etc. (optional, brief)

**Example**:
> **Acknowledgements**
>
> I am deeply grateful to my advisor, [Advisor Name], for their guidance, patience, and encouragement throughout this project. Their insights on event sourcing and distributed systems were invaluable, and their feedback sharpened both my thinking and my writing.
>
> I thank [Committee Member 1] and [Committee Member 2] for serving on my thesis committee and providing thoughtful critiques during the defense.
>
> Special thanks to [Collaborator Name] for assisting with the human evaluation experiments, and to [Friend/Colleague Name] for annotating the ground truth labels.
>
> I am grateful to my family for their unwavering support during long nights of debugging and writing. [Partner/Friend Name], thank you for keeping me sane and reminding me to take breaks.
>
> This work was supported by [Funding Source, if applicable].
>
> Finally, I acknowledge the open-source communities behind NATS, Ollama, .NET, and React, whose tools made this project possible.

**Tone**: Warm, personal, sincere. Avoid over-the-top praise, but be genuine.

---

## Writing Order

1. **Abstract**: Write LAST (after Chapter 8), when you know your full story
2. **Preface**: Write after abstract (brief personal reflection)
3. **Acknowledgements**: Write anytime (can draft early, polish late)

---

## Checklist

- [ ] Abstract is 250-350 words (check university guidelines)
- [ ] Abstract includes: problem, solution, method, results, contributions
- [ ] Keywords listed (5-10 keywords)
- [ ] Preface is personal but not overly informal
- [ ] Acknowledgements thank all key contributors (advisor, committee, family)
- [ ] Tone is appropriate for academic thesis

---

## Timeline

- **Jan 25**: Draft abstract (after Chapter 8 complete)
- **Jan 25**: Draft preface and acknowledgements
- **Jan 26**: Polish front matter

---

## University-Specific Requirements

Check your university's thesis formatting guidelines for:
- Page numbering (front matter often uses Roman numerals: i, ii, iii)
- Abstract length limits (some universities cap at 300 words)
- Required sections (some require "Declaration of Originality", "List of Publications", etc.)
- Formatting (font, margins, line spacing)

**TODO**: Add university-specific requirements here after checking guidelines.

---

## Next Steps

1. Complete all chapters (1-8)
2. Write abstract, preface, acknowledgements
3. Compile full thesis (combine all chapters)
4. Format according to university guidelines
5. Proofread (2 passes)
6. Submit to advisor for feedback
7. Final revisions
8. Submit!
