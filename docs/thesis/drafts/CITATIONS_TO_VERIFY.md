# Citations That Need Verification

**Priority: HIGH** - Verify these before submitting thesis

These citations are marked with ⚠️ in `chapter_01_bibliography.bib` and need verification for accurate bibliographic details.

---

## 🔴 Priority 1: Likely Incorrect or Uncertain

### 1. **Weisz et al., 2021** - "Perfection Not Required?"
- **Current entry:** IUI 2021
- **Issue:** Title and venue need verification
- **How to verify:** Search Google Scholar for "Weisz Human-AI Code Translation 2021"
- **Expected:** May be a CHI paper or workshop paper

### 2. **Ross et al., 2023** - "Programmer-AI Collaboration"
- **Current entry:** CHI Extended Abstracts 2023
- **Issue:** May be workshop paper, check exact title
- **How to verify:** Search Google Scholar for "Ross Martinez Houde programmer AI 2023"

### 3. **Chiang & Lee, 2023** - "Can LLMs Be Alternative to Human Evaluations?"
- **Current entry:** arXiv 2305.01937
- **Issue:** May have been published at ACL 2023
- **How to verify:** Check if arXiv paper was accepted to ACL 2023 or EMNLP 2023

### 4. **Miller, 2023** - WordNet
- **Current entry:** Citing 1995 classic WordNet paper
- **Issue:** Chapter 1 says "Miller, 2023" but likely meant the 1995 paper
- **How to verify:** Check if you meant the classic WordNet paper (Miller 1995) or a 2023 update
- **Fix:** Change citation to `[Miller, 1995]` in Chapter 1 text

### 5. **Munson & Vogel, 2018** - "Research Prototypes"
- **Current entry:** Uncertain - may be a design methods paper
- **Issue:** Can't find this exact reference
- **How to verify:** Search for "Munson Vogel Research Prototypes 2018"
- **Alternative:** May need to replace with a different research methods citation

### 6. **Storey et al., 2020** - Twitter paper?
- **Current entry:** "Evolution of Software Developers" (ICSE 2020)
- **Issue:** Chapter 1 cites this for "How Software Developers Use Twitter" but the paper is about developer evolution
- **How to verify:** Check if you meant a different Storey paper (she has several on Twitter use)
- **Possible fix:** May need to cite "Storey et al., 2014" or a different paper

---

## 🟡 Priority 2: Need Conference Version

These are currently cited as arXiv papers but may have been published at conferences. Check if conference version exists and cite that instead.

### 7. **Park et al., 2023** - Generative Agents
- **Status:** arXiv 2304.03442
- **Check:** Published in UIST 2023 (note added in .bib file)
- **Action:** Verify and update to UIST 2023 proceedings format

### 8. **Liu et al., 2023** - G-Eval
- **Status:** arXiv 2303.16634
- **Check:** Published in EMNLP 2023 (note added in .bib file)
- **Action:** Verify and update to EMNLP 2023 proceedings format

### 9. **Jacovi et al., 2021** - Formalizing Trust
- **Status:** arXiv 2010.07487
- **Check:** Published in FAccT 2021 (note added in .bib file)
- **Action:** Verify and update to FAccT 2021 proceedings format

### 10. **Dettmers et al., 2023** - QLoRA
- **Status:** arXiv 2305.14314
- **Check:** Published in NeurIPS 2023 (note added in .bib file)
- **Action:** Verify and update to NeurIPS 2023 proceedings format

---

## 🟢 Priority 3: Double-Check URLs

These are online resources (documentation, tools) - verify URLs are still active and update access dates.

### 11. GitHub Copilot Documentation (2024)
- **URL:** https://docs.github.com/copilot
- **Action:** Verify URL works and update access date to today

### 12. Anthropic Claude Documentation (2024)
- **URL:** https://docs.anthropic.com
- **Action:** Find exact URL for "project instructions" documentation

### 13. Cursor Documentation (2024)
- **URL:** https://cursor.sh/docs
- **Action:** Verify URL for `.cursorrules` documentation

### 14. All other @misc entries
- **Action:** Check all URLs are still active
- **Action:** Update all "Accessed: 2025-01-10" to your actual access date

---

## How to Verify Citations

### Method 1: Google Scholar
1. Go to https://scholar.google.com
2. Search for: `author_name title keywords year`
3. Click "Cite" → "BibTeX" to get accurate entry
4. Compare with your .bib file and update

### Method 2: DBLP (Computer Science Bibliography)
1. Go to https://dblp.org
2. Search for author or title
3. Click "export record" → "BibTeX"
4. Copy accurate entry

### Method 3: ACM Digital Library / IEEE Xplore
1. Go to https://dl.acm.org or https://ieeexplore.ieee.org
2. Search for paper
3. Click "Export Citation" → "BibTeX"
4. Copy accurate entry

### Method 4: arXiv to Conference
1. Go to arXiv paper (e.g., https://arxiv.org/abs/2304.03442)
2. Scroll down to "Submission history"
3. Check if it says "Published in [Conference]"
4. If yes, search for conference version and cite that

---

## Verification Checklist

Use this checklist to track progress:

- [ ] Weisz et al., 2021 ✅ Verified
- [ ] Ross et al., 2023 ✅ Verified
- [ ] Chiang & Lee, 2023 ✅ Verified / Changed to correct year
- [ ] Miller, 2023 → 1995 ✅ Changed in Chapter 1 text
- [ ] Munson & Vogel, 2018 ✅ Verified / Replaced
- [ ] Storey et al., 2020 ✅ Verified / Replaced with correct paper
- [ ] Park et al., 2023 ✅ Updated to UIST 2023
- [ ] Liu et al., 2023 ✅ Updated to EMNLP 2023
- [ ] Jacovi et al., 2021 ✅ Updated to FAccT 2021
- [ ] Dettmers et al., 2023 ✅ Updated to NeurIPS 2023
- [ ] All URLs checked ✅ All active, access dates updated

---

## Estimated Time

- **Priority 1 (6 citations):** ~1-2 hours
- **Priority 2 (4 citations):** ~30 minutes
- **Priority 3 (URLs):** ~15 minutes

**Total:** ~2-3 hours

---

## After Verification

Once all citations are verified:

1. ✅ Update `chapter_01_bibliography.bib`
2. ✅ Remove all `⚠️ VERIFY` notes
3. ✅ Update any citation years in `chapter_01_introduction_FINAL.md` if they changed
4. ✅ Generate PDF to check formatting
5. ✅ Send to advisor for review

---

**Status:** All citations marked for verification
**Next Action:** Start with Priority 1 citations (highest risk)
