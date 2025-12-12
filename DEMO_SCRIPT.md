# Demo Script for Supervisor Meeting

**Duration:** 15-20 minutes
**Goal:** Show the research contribution, not just the code

---

## Setup Before Meeting

```bash
# Terminal 1: Start NATS
nats-server -js -m 8222 -sd ./nats_store

# Terminal 2: Start TOOL API
cd /home/user/TOOL
dotnet run --project ./src/TOOL/

# Terminal 3: Start Agent UI
cd /home/user/TOOL/src/Agent.UI
npm run dev
```

Open browser to: http://localhost:3000

---

## Demo Flow

### Part 1: The Problem (2 min)

**SAY:** "Imagine a company with 10 AI agents answering customer questions. They all need to follow the same rules - refund policies, security guidelines, etc. Current LLM systems have no good way to ensure consistency or trace why an agent gave a specific answer."

**SHOW:** Nothing yet - just explain the problem verbally.

---

### Part 2: The Solution Architecture (3 min)

**SAY:** "TOOL solves this with event sourcing. Every rule change becomes an immutable event. Let me show you the flow."

**SHOW:** Draw or show this diagram:
```
Rule Change → EVENT → Promoter → DELTA → Projection DB → Agent Query
                                              ↓
                                        LLM with Rules
```

**SAY:** "The key insight is that we can replay all events and get the exact same state. This makes the system auditable and deterministic."

---

### Part 3: Live Demo - Agent Query (5 min)

**SHOW:** Open Agent UI at http://localhost:3000

**SAY:** "Let me ask the agent a question that requires consulting the rulebook."

**DO:** Type a question like:
- "What is the API rate limit policy?"
- "How should I handle authentication errors?"

**POINT OUT:**
1. The rules that were retrieved (shown in UI)
2. The citation in the answer (e.g., `[im:api.rate_limit@v2]`)
3. The provenance info (which file, which commit)

**SAY:** "Notice how the answer cites exactly which rules it used. This is explainability - we can trace every answer back to its source."

---

### Part 4: Live Demo - Rule Update (5 min)

**SAY:** "Now watch what happens when we update a rule."

**DO:**
1. Show the current rule in the UI
2. Seed a new version of a rule (via API or seeder)
3. Show the event appearing in NATS
4. Show the projection updating
5. Ask the same question again

**POINT OUT:**
- The new rule version is now used
- The citation shows the new version number
- The update propagated without restarting anything

**SAY:** "This is freshness - rule updates propagate in real-time to all agents."

---

### Part 5: Replayability Demo (3 min)

**SAY:** "The most important research property is replayability. Let me prove the system is deterministic."

**DO:**
1. Show current state in database
2. Delete the projection database
3. Run replay from event log
4. Show state is identical

**SAY:** "We reconstructed the exact same state from just the event log. This proves the system is deterministic and auditable."

---

### Part 6: Research Questions (2 min)

**SAY:** "So my thesis evaluates 5 hypotheses..."

**SHOW:** The hypotheses table from the one-pager

**SAY:** "I have the implementation done. The remaining work is running experiments to test these hypotheses and writing the thesis."

---

## Key Phrases to Use

When professor asks about complexity:
> "The novelty isn't in any single component, but in combining event sourcing with LLM instruction memory in a way that's measurable and auditable."

When professor asks about contribution:
> "No one has applied event sourcing to LLM rule memory before. I'm also contributing a formal evaluation framework for this class of systems."

When professor asks about scope:
> "The core system is complete. I'm now adding RAG for comparison experiments and running the formal evaluation."

---

## Questions to ASK Your Professor

After the demo, ask these directly:

1. **"Based on what you've seen, does this feel like appropriate scope for a master's thesis?"**

2. **"Are there aspects of the evaluation you'd want to see strengthened?"**

3. **"Do you have concerns about the academic framing or contribution statement?"**

4. **"What would make this thesis excellent in your view?"**

5. **"Are there any red flags I should address before I start writing?"**

---

## If Things Go Wrong

**NATS won't start:**
```bash
rm -rf ./nats_store && nats-server -js -m 8222 -sd ./nats_store
```

**API crashes:**
Show the architecture diagram and explain conceptually.

**No rules in database:**
```bash
curl -X POST http://localhost:5293/api/admin/seed
```

---

## End With

**SAY:** "I'd really value your feedback on whether this is on the right track. I want to make sure I'm building something that meets your expectations for a master's thesis."

Then: **STOP TALKING AND LISTEN**

