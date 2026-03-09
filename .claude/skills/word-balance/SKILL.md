---
name: word-balance
description: >
  Audits word action distribution in the SQLite DB and suggests rebalancing.
  Use when user asks to "audit words", "check balance", "rebalance actions",
  or "word distribution".
---

# Word Balance Audit

## Instructions

### Step 1: Run the audit script

```bash
/c/Users/kelns/AppData/Local/Programs/Python/Python312/python.exe Tools/WordAction/audit.py
```

This produces a full report covering:
- Action usage distribution (unused, under/over-represented)
- Value distribution per action (are values too clustered?)
- Target type distribution (is targeting too homogeneous?)
- Cost distribution
- Tag coverage (missing or under-represented tags)
- Tags-per-word distribution
- Duplicate action profiles (words with identical action+value combos)
- Unit/passive analysis (trigger/effect/target diversity, summon coverage)
- Concrete recommendations

### Step 2: Analyze the report

Read the audit output and identify the top issues:
1. **Unused actions** — actions that exist in code but no word uses them
2. **Under-represented actions** (<1% of words) — need more words assigned
3. **Over-represented actions** (>25% of words) — too many similar words
4. **Dominant target types** (>50%) — diversify targeting
5. **Under-represented tags** (<3%) — assign to more words
6. **Duplicate profiles** — words with identical action combos should be differentiated
7. **Summon words without unit definitions** — need unit data added
8. **Unused passive triggers/effects** — units should demonstrate more variety

### Step 3: Propose changes

For each issue found, propose specific word-level changes. Format as a table:

| Word | Current | Proposed Change | Reason |
|------|---------|-----------------|--------|
| word | Damage:3 | Damage:3 + Bleed:2 | Bleed is under-represented |

Changes can include:
- **Add secondary actions** to existing words (e.g. add Bleed to a physical word)
- **Adjust values** to fix clustering (e.g. too many Damage:3 words)
- **Change targets** to diversify targeting distribution
- **Add tags** to under-tagged words
- **Add unit definitions** to summon words that lack them
- **Reassign** a word's primary action if the action is over-represented

### Step 4: Apply changes

After user approval, generate a JSON array with the corrected words and pipe to batch_insert:

```bash
echo '<JSON>' | /c/Users/kelns/AppData/Local/Programs/Python/Python312/python.exe Tools/WordAction/batch_insert.py
```

`batch_insert.py` uses `INSERT OR REPLACE`, so existing words will be updated.

### Step 5: Verify

Run the audit again to confirm improvements:

```bash
/c/Users/kelns/AppData/Local/Programs/Python/Python312/python.exe Tools/WordAction/audit.py
```

## Important Rules

- Never modify C# files — all changes go through the Python pipeline
- Only propose changes that make thematic sense (word meaning must match actions)
- Don't over-correct — some imbalance is natural (Damage will always be common)
- Preserve existing word identity — don't completely change a word's role
- Summon words MUST have cost >= 1
- Values must be 1-10, costs 0-10
- When adding unit definitions to summon words, include the full `unit` JSON object
- Run audit before AND after changes to measure improvement
