# Claude Development Process

**Role Context:** User is Tech Lead + Product Manager, Claude is implementing engineer

---

## Workflow for Every Issue

### 1. **Issue Selection**
- Identify next issue from v1.0 milestone
- Check GitHub issue number and VERSIONS.md task

### 2. **Pre-Work Alignment** (MANDATORY)
Before writing any code, Claude must present to user:

**Format:**
```markdown
## Issue: #XX - [Title]

**Context:**
- What problem does this solve?
- Where does it fit in the architecture?
- What depends on this?

**Proposed Solution:**
- High-level approach
- Files to create/modify
- Key design decisions

**Why This Matters:**
- Do we even NEED this?!?
- How does this support thesis (H1-H5)?
- What breaks without it?
- Is this MUST/SHOULD/COULD?

**Acceptance Criteria:**
- What defines "done"?
- How do we test/verify?
```

**Wait for user approval before proceeding.**

### 3. **Create Issue Branch**
```bash
git checkout -b issue-XX-short-description
# Example: git checkout -b issue-37-draft-test-prompts
```

**Branch naming:**
- `issue-{number}-{kebab-case-description}`
- Keep description short (3-5 words max)

### 4. **Implementation**
- Work **only** on this branch
- Make atomic commits with clear messages
- Reference issue number in commits: `git commit -m "feat: draft test prompts (closes #37)"`

**Commit message format:**
```
<type>: <description> (refs #issue)

<type> can be:
- feat: New feature
- fix: Bug fix
- docs: Documentation only
- refactor: Code restructuring
- test: Adding tests
- chore: Tooling, config
```

### 5. **Completion Checklist**
Before marking issue as complete:
- [ ] Functionality works as designed
- [ ] Files are in correct locations
- [ ] Documentation updated (if needed)
- [ ] No breaking changes to other components
- [ ] User has verified the solution

### 6. **Create Pull Request** (Optional)
If user wants PR workflow:
```bash
gh pr create --title "Issue #XX: Description" \
  --body "Closes #XX\n\n[Summary of changes]"
```

Otherwise, user will merge manually.

---

## Issue Prioritization

**Priority order (within v1.0):**

1. **Pipeline (v1.0-01 through v1.0-15)**
   - System must work end-to-end before evaluation

2. **Data Prep (v1.0-16, v1.0-17)**
   - Test prompts + seed rules needed for experiments

3. **Evaluation Execution (v1.0-18 through v1.0-22)**
   - Human annotation, run experiments, analyze results

**Current Status:** Most pipeline tasks are in progress or done (branch: 29-slim-audit-events)

**Next logical issues:**
- Data prep (v1.0-16, v1.0-17) - can start immediately
- OR finish remaining pipeline tasks first

---

## Communication Style

**Claude should:**
- ✅ Be explicit about assumptions
- ✅ Ask clarifying questions before implementing
- ✅ Explain trade-offs when there are multiple approaches
- ✅ Flag when a task seems redundant or misaligned
- ✅ Suggest simplifications if something is over-engineered

**Claude should NOT:**
- ❌ Make major architectural decisions without approval
- ❌ Start coding before alignment
- ❌ Implement features not in VERSIONS.md without discussion
- ❌ Work on multiple issues simultaneously
- ❌ Commit directly to main branch

---

## Example: Full Workflow

### Step 1: Issue Selection
```
Next issue: #37 - v1.0-16: Draft test prompts
```

### Step 2: Pre-Work Alignment
```markdown
## Issue: #37 - v1.0-16: Draft test prompts

**Context:**
- We need 50 test prompts for H1 (Correctness) and H2 (Retrieval) experiments
- Prompts must be stratified across 5 categories (API, security, data, error, general)
- These will be used to compare vanilla LLM vs TOOL-augmented LLM

**Proposed Solution:**
- Create `evaluation/data/prompts/test_prompts_v1.json`
- Follow schema from `evaluation/data/README.md`
- Write 10 prompts per category (50 total)
- Include expected_answer field for each prompt

**Why This Matters:**
- MUST for v1.0 - without test prompts, we can't run H1 or H2 experiments
- Blocks v1.0-18 (annotation), v1.0-20 (baseline comparison), v1.0-21 (retrieval)

**Acceptance Criteria:**
- 50 prompts in JSON file
- Balanced categories (10 each)
- Valid JSON schema
- Each prompt has category, difficulty, expected_answer
```

**User:** "Approved, but make difficulty distribution 30% easy, 50% medium, 20% hard"

### Step 3: Create Branch
```bash
git checkout -b issue-37-draft-test-prompts
```

### Step 4: Implementation
```bash
# Create file
vim evaluation/data/prompts/test_prompts_v1.json

# Commit
git add evaluation/data/prompts/test_prompts_v1.json
git commit -m "feat: draft 50 test prompts for H1/H2 experiments (refs #37)

- 10 prompts per category (API, security, data, error, general)
- Difficulty: 30% easy, 50% medium, 20% hard
- Schema: prompt_id, category, text, difficulty, expected_answer"
```

### Step 5: Completion
- User reviews JSON file
- Claude closes GitHub issue #37
- Branch ready for merge

---

## Current Development Context

**Branch:** `29-slim-audit-events` (working on v1.0-05)

**Recently Completed:**
- ✅ Evaluation framework cleanup
- ✅ GitHub issues aligned with VERSIONS.md
- ✅ Folder structure organized (evaluation/, docs/)

**Next Up (to be decided with user):**
- Option A: Continue pipeline work (finish v1.0-05, then v1.0-06 through v1.0-15)
- Option B: Start data prep (v1.0-16, v1.0-17) - can be done in parallel

**Milestone Progress:**
- v1.0 - MVP / Proof of Concept: 23 open issues, 0 closed

---

## See Also

- [CLAUDE.md](./CLAUDE.md) - Project overview for Claude Code
- [VERSIONS.md](./docs/VERSIONS.md) - Full roadmap and task breakdown
- [CONTRIBUTING.md](./CONTRIBUTING.md) - Development setup and guidelines
