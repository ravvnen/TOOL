# Quick Citation Verification Checklist

## Top 20 Most Critical Citations (Verify These First)

These are the most important citations that need verification. I've included Google Scholar links and expected citation counts to help you verify they're real.

### 🔴 PRIORITY 1: Core LLM Papers (MUST VERIFY)

| Citation | Paper Title | Venue | Year | Expected Citations |
|----------|-------------|-------|------|-------------------|
| Brown et al., 2020 | Language Models are Few-Shot Learners (GPT-3) | NeurIPS | 2020 | ~15,000+ |
| Ouyang et al., 2022 | Training language models to follow instructions (InstructGPT) | NeurIPS | 2022 | ~5,000+ |
| Wei et al., 2022 | Chain-of-Thought Prompting | NeurIPS | 2022 | ~3,000+ |
| Lewis et al., 2020 | Retrieval-Augmented Generation (RAG) | NeurIPS | 2020 | ~4,000+ |

**How to verify**: Google Scholar → search title → confirm author, venue, year

---

### 🟡 PRIORITY 2: Agent Memory Papers (NEED TO VERIFY)

| Citation | Paper Title | Status | Notes |
|----------|-------------|--------|-------|
| Park et al., 2023 | Generative Agents: Interactive Simulacta | ✅ Real | Stanford, published at UIST 2023 |
| Packer et al., 2023 | MemGPT: Towards LLMs as Operating Systems | ✅ Real | arXiv preprint (check if published) |
| Shinn et al., 2023 | Reflexion: Language Agents with Verbal RL | ✅ Real | NeurIPS 2023 |
| Wang et al., 2024 | Survey on LLM Autonomous Agents | ⚠️ Check | May be 2023, verify exists |
| Sumers et al., 2023 | Cognitive Architectures for Language Agents | ⚠️ Check | Verify this paper exists |

**Action**: Search Google Scholar for "MemGPT" and "Reflexion" to verify.

---

### 🟢 PRIORITY 3: Explainability Papers (STANDARD REFS)

| Citation | Paper Title | Status | Notes |
|----------|-------------|--------|-------|
| Lipton, 2018 | The Mythos of Model Interpretability | ✅ Real | CACM, highly cited |
| Rudin, 2019 | Stop explaining black box models | ✅ Real | Nature Machine Intelligence |
| Doshi-Velez & Kim, 2017 | Towards Rigorous Science of Interpretable ML | ✅ Real | arXiv, highly cited |
| Jacovi et al., 2021 | Formalizing Trust in AI | ⚠️ Check | Verify exact title/venue |

---

### 🔵 PRIORITY 4: Event Sourcing & Distributed Systems

| Citation | Status | Notes |
|----------|--------|-------|
| Fowler, 2005 | ✅ Real | Martin Fowler's blog post (authoritative) |
| Vernon, 2013 | ✅ Real | "Implementing Domain-Driven Design" book (Addison-Wesley) |
| Kleppmann, 2017 | ✅ Real | "Designing Data-Intensive Applications" (O'Reilly) |
| Kreps et al., 2011 | ✅ Real | Kafka paper, NetDB workshop |

**No action needed**: These are authoritative industry references.

---

## Citations That MIGHT NOT EXIST (Need Replacement)

### ⚠️ Uncertain - Verify or Replace

| Citation | Claim | Status | Replacement If Needed |
|----------|-------|--------|----------------------|
| Weisz et al., 2021 | "Teams rely on AI agents for code review" | ⚠️ Check | Replace with GitHub Copilot study if doesn't exist |
| Ross et al., 2023 | "AI agents for API design" | ⚠️ Check | May be a different year or author |
| Dakhel et al., 2023 | GitHub Copilot study | ⚠️ Check | Verify this paper exists |
| Chiang & Lee, 2023 | LLMs as human evaluators | ⚠️ Check | Verify this paper exists |
| Liu et al., 2023 | G-Eval paper | ⚠️ Check | Verify this paper exists |

**Action**: If these don't exist, I can suggest real alternative citations.

---

## How to Verify (Step-by-Step)

### For Academic Papers

1. **Google Scholar**: `scholar.google.com`
2. **Search**: Paper title in quotes, e.g., `"Language Models are Few-Shot Learners"`
3. **Verify**:
   - Authors match (Brown, Ouyang, etc.)
   - Year matches (2020, 2022, etc.)
   - Venue matches (NeurIPS, ACL, etc.)
   - Citation count reasonable (not 0 for claimed major paper)

### For Books

1. **Amazon/Google Books**: Search title
2. **Verify**:
   - Author matches (Fowler, Kleppmann, Vernon)
   - Publisher reputable (O'Reilly, Addison-Wesley)
   - Publication year matches

### For Tools (GitHub)

1. **GitHub**: `github.com/search`
2. **Search**: Repository name (LangChain, AutoGPT)
3. **Verify**:
   - Stars > 10k (indicates established tool)
   - Active maintenance (recent commits)
   - Official repository (not fork)

---

## Quick Verification Commands

```bash
# Search Google Scholar via command line (if you have scholar.py installed)
scholar.py "Language Models are Few-Shot Learners" | head -n 5

# Or use curl + grep
curl -s "https://scholar.google.com/scholar?q=Language+Models+are+Few-Shot+Learners" | grep -o "Cited by [0-9]*"
```

---

## What to Do If Citation Doesn't Exist

### Option 1: Find Similar Paper
- Search Google Scholar for the topic (e.g., "AI agents code review")
- Pick a highly-cited recent paper (>100 citations, last 3 years)
- Replace citation with real one

### Option 2: Remove Claim
- If no good citation exists, soften the claim or remove it
- Example: "Teams increasingly rely on..." → "Teams are beginning to adopt..."

### Option 3: Ask Me for Alternatives
- Tell me which citation doesn't exist
- I'll suggest 2-3 real alternatives from Google Scholar

---

## Regex to Find All Citations in Document

```bash
# Extract all citations from chapter
grep -o '\[.*[0-9]\{4\}.*\]' chapter_01_introduction_CITED.md | sort -u
```

This will list all unique citations for manual verification.

---

## After Verification: Create BibTeX File

Once you've verified all citations exist:

1. **Use Zotero** (recommended):
   - Install Zotero + browser extension
   - Visit each paper's Google Scholar page
   - Click Zotero icon → auto-imports citation
   - Export all as BibTeX

2. **Manual BibTeX** (if no Zotero):
   - Google Scholar → Click "Cite" → BibTeX
   - Copy into `references.bib`
   - Repeat for all 82 citations

3. **Auto-generate** (if using LaTeX):
   - `\bibliographystyle{ieeetr}` (or ACM, APA)
   - `\bibliography{references.bib}`
   - LaTeX will auto-format

---

## Verification Progress Tracker

Use this checklist as you verify:

- [ ] Verified top 5 LLM papers (Brown, Ouyang, Wei, Lewis, etc.)
- [ ] Verified agent memory papers (Park, Packer, Shinn)
- [ ] Verified explainability papers (Lipton, Rudin)
- [ ] Verified event sourcing sources (Fowler, Vernon, Kleppmann)
- [ ] Verified evaluation metrics (Järvelin, Voorhees)
- [ ] Checked uncertain citations (Weisz, Ross, Dakhel)
- [ ] Replaced non-existent citations with real alternatives
- [ ] Created BibTeX file with all verified citations
- [ ] Formatted bibliography in university-required style (IEEE/ACM/APA)

---

## Expected Timeline

- **Verification**: 2-3 hours (for 82 citations)
- **BibTeX creation**: 1-2 hours (if using Zotero, faster)
- **Formatting**: 30 min (LaTeX auto-formats)
- **Total**: Half a day to get bibliography ready

---

## When in Doubt

If you're unsure about a citation:
1. Ask me for verification
2. I can Google Scholar search it for you
3. I can suggest alternative citations if original doesn't exist

**Key principle**: Better to have 60 verified citations than 82 uncertain ones.
