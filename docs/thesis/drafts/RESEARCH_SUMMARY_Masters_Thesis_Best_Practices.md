# Master's Thesis Best Practices - Research Summary

**Date:** November 12, 2025
**Research Focus:** Structure, length, and citation practices for Computer Science Master's theses

---

## 🎯 Key Research Question

**"How long should Chapter 2 (Background & Related Work) be in a Master's thesis, and how many citations is appropriate?"**

---

## 📊 Research Findings

### **Total Thesis Length (Multiple Sources)**

| Source | Pages | Notes |
|--------|-------|-------|
| **ETH Zurich** | 50-100 pages | Various CS programs |
| **FAU Computer Science** | 80-90 pages | Master's guideline |
| **University of Kassel** | 45 pages ±10% | 41-50 pages |
| **General consensus** | 60-100 pages | Text only (no appendices/refs) |
| **Most common** | **60-80 pages** | ⭐ **TYPICAL** |

**Word Count:** 25,000-50,000 words

---

### **Chapter 2 (Background/Literature Review) Length**

#### **By Percentage:**
- **20-30% of total thesis** (consistent across multiple sources)
- Some sources: up to 30% maximum
- Experimental theses: often closer to 20% (less than theoretical theses)

#### **By Pages:**

| Total Thesis | 20% | 25% | 30% |
|--------------|-----|-----|-----|
| 50 pages | 10 pages | 12.5 pages | 15 pages |
| **60 pages** | **12 pages** | **15 pages** | **18 pages** |
| 70 pages | 14 pages | 17.5 pages | 21 pages |
| 80 pages | 16 pages | 20 pages | 24 pages |

**Consensus for 60-page thesis:** **12-18 pages** for Chapter 2

**For experimental/systems theses:** Closer to 20-22% (10-13 pages)

---

### **Citation Counts**

#### **Total Thesis References:**
- **Bachelor's**: ~20 references
- **Master's**: **30-40 references** ⭐ **TYPICAL**
- PhD: 100-200+ references

#### **Chapter 2 Citations:**
- Typically **60-70% of total citations**
- For Master's (30-40 total): **20-25 citations in Chapter 2** ⭐

**NOT 50-100 citations** - that's PhD-level!

---

### **Chapter Breakdown (General)**

Based on multiple thesis writing guides:

| Chapter | % of Thesis | Pages (60-page) | Pages (80-page) |
|---------|-------------|-----------------|-----------------|
| Introduction | 10-15% | 6-9 pages | 8-12 pages |
| **Literature Review** | **20-30%** | **12-18 pages** | **16-24 pages** |
| Methodology | 15-20% | 9-12 pages | 12-16 pages |
| Results | 15-20% | 9-12 pages | 12-16 pages |
| Discussion | 20-25% | 12-15 pages | 16-20 pages |
| Conclusion | 5-10% | 3-6 pages | 4-8 pages |

---

### **Experimental CS Theses - Special Considerations**

**FAU Computer Science guideline:**
- "1/3 to 1/3 to 1/3" split
- Background / Design+Implementation / Evaluation+Discussion

**UIC Experimental Thesis Guide:**
- Background should "describe the field and how others tried to solve this problem"
- **Should NOT dominate** the thesis
- **Evaluation chapters are most important**

**Implication:** For experimental theses, background can be **shorter than 30%** (closer to 20%)

---

## 📖 Literature Review Structure (Best Practices)

### **Common Patterns:**

**Pattern 1: By Category** (Most Common)
```
2.1 Introduction
2.2 Category A (e.g., Agent Memory)
2.3 Category B (e.g., Centralized Tools)
2.4 Category C (e.g., Event Sourcing)
2.5 Gap Analysis
```

**Pattern 2: Chronological**
```
2.1 Early Work (2000-2010)
2.2 Recent Advances (2010-2020)
2.3 State-of-the-Art (2020-2024)
2.4 Gap Analysis
```

**Pattern 3: By Approach**
```
2.1 Theoretical Approaches
2.2 Practical Systems
2.3 Hybrid Methods
2.4 Gap Analysis
```

**Recommendation:** Pattern 1 (by category) for CS theses

---

### **Level of Detail:**

**Per Related Work Item:**
- **NOT:** Multi-page detailed explanations
- **YES:** 1-2 paragraph summaries
- **YES:** Critical evaluation (strengths + limitations)
- **YES:** Connection to your work

**Example Structure per item:**
```
[System Name] [Authors, Year]: [Brief description of what it does].
[Key features]. [Limitations relevant to your work].
```

**Length:** 4-8 sentences per item (1-2 paragraphs)

---

### **Comparison Tables:**

**Best Practice:** Use tables for visual comparison
- Helps readers quickly see differences
- Highlights gaps clearly
- Professional appearance

**Typical Size:** 0.5-1 page per table

---

## 🎓 Differences: Master's vs. PhD

| Aspect | Master's | PhD |
|--------|----------|-----|
| **Total Length** | 60-80 pages | 150-300 pages |
| **Lit Review** | 12-18 pages | 30-60 pages |
| **Citations** | 30-40 total | 100-200+ total |
| **Depth** | Focused, selective | Comprehensive, exhaustive |
| **Time** | 1-2 weeks | 1-2 months |

**Key Difference:** Master's is **focused and selective**, PhD is **comprehensive and exhaustive**

---

## 🔬 Experimental Thesis Considerations

### **Chapter Balance for Experimental Work:**

**Less Important:**
- Extensive literature review
- Theoretical background
- Philosophy/motivation

**More Important:**
- System design (clear diagrams, architecture)
- Implementation details (tech stack, challenges)
- Evaluation methodology (rigorous experiments)
- Results (data, analysis, interpretation)

**Typical Balance:**
```
Chapter 1 (Introduction): 8-10%
Chapter 2 (Background): 15-20% ← SHORTER
Chapter 3 (Design): 15-18% ← IMPORTANT
Chapter 4 (Implementation): 12-15% ← IMPORTANT
Chapter 5 (Methodology): 12-15% ← IMPORTANT
Chapter 6 (Results): 15-18% ← MOST IMPORTANT
Chapter 7 (Discussion): 10-12%
Chapter 8 (Conclusion): 5-8%
```

---

## 📚 Sources Consulted

### **University Guidelines:**
1. **ETH Zurich** - Multiple CS program guidelines
2. **FAU Computer Science 7** - Master thesis writing guidelines
3. **University of Kassel** - Faculty 07 thesis guidelines
4. **University of Toronto** - CS Master's thesis examples

### **Thesis Writing Guides:**
5. **MW Editing** - Thesis chapter breakdown with percentages
6. **Academia Insider** - "How Long is a Master's Thesis?"
7. **Paperpile** - Thesis structure guide
8. **UIC EVL** - "How to do a Computer Science Thesis"

### **Academic Resources:**
9. **Academia Stack Exchange** - Multiple threads on thesis length
10. **Research Gate** - Discussions on Master's thesis standards

---

## ✅ Recommendations for YOUR Thesis (TOOL)

### **Your Context:**
- Computer Science Master's
- Experimental/systems thesis (implementation + evaluation)
- Target: 60-70 pages total

### **Recommended Structure:**

| Chapter | Pages | % | Rationale |
|---------|-------|---|-----------|
| 1. Introduction | 5-7 | 8-10% | Motivation, RQ, scope, contributions |
| **2. Background** | **10-12** | **17-20%** | **Focused review, state-of-the-art** |
| 3. Design | 10-12 | 15-18% | TOOL architecture |
| 4. Implementation | 8-10 | 12-15% | Tech stack, challenges |
| 5. Methodology | 8-10 | 12-15% | Experiment design (H1-H5) |
| 6. Results | 10-12 | 15-18% | **MOST IMPORTANT** |
| 7. Discussion | 6-8 | 10-12% | Interpretation, limitations |
| 8. Conclusion | 3-5 | 5-8% | Summary, future work |
| **TOTAL** | **60-70** | **100%** | |

### **Chapter 2 Specifically:**

**Length:** 10-12 pages (not 15-20!)
**Sections:** 5 sections (not 7!)
**Citations:** 20-25 papers (not 50-100!)
**Detail:** 1-2 paragraphs per item (not full pages!)
**Tables:** 2-3 comparison tables ✅
**Time:** 1 week (not 2 weeks!)

---

## 🎯 Key Takeaways

### **1. Master's ≠ PhD**
- Master's is focused, selective, concise
- PhD is comprehensive, exhaustive, detailed
- Don't aim for PhD-level lit review in Master's!

### **2. 20-30% Rule**
- Background/Lit Review = 20-30% of thesis
- For 60-page thesis: 12-18 pages
- For experimental thesis: closer to 20% (10-13 pages)

### **3. Citation Counts**
- Total: 30-40 references (Master's standard)
- Chapter 2: 20-25 citations (60-70% of total)
- **NOT 50-100** (that's PhD!)

### **4. Experimental Thesis Balance**
- Background important but should NOT dominate
- Focus on: Design, Implementation, Evaluation
- Results chapter = most important!

### **5. Level of Detail**
- 1-2 paragraphs per related work item
- Comparison tables for visual clarity
- Critical evaluation, not just description

---

## 📊 Your Original vs. Research-Based Plan

| Aspect | Your Original Plan | Research-Based |
|--------|-------------------|----------------|
| Ch 2 Length | 10-15 pages | 10-12 pages ✅ (close!) |
| Sections | 7 | 5 ⬇️ |
| Citations | 50-100 | 20-25 ⬇️⬇️ |
| Detail | Multi-paragraph | 1-2 paragraphs ⬇️ |

**Good News:** Your page count instinct (10-15) was **correct**!
**Adjustment Needed:** Fewer citations, fewer sections, less detail per item

---

## 🚀 Implementation

**Files Created:**
1. `chapter_02_background_OUTLINE_REALISTIC.md` - Revised outline
2. `CHAPTER2_COMPARISON_Original_vs_Realistic.md` - Side-by-side comparison
3. `RESEARCH_SUMMARY_Masters_Thesis_Best_Practices.md` - This document

**Next Steps:**
1. ✅ Review realistic outline
2. Write Section 2.3 (Centralized Tools) - 3-4 pages
3. Revise Section 2.1 (trim to 2-2.5 pages)
4. Write remaining sections (2.4, 2.5, 2.6)
5. Assemble and verify length (10-12 pages)

---

**Status:** Research complete, guidelines clear, ready to write with confidence! 🎓
