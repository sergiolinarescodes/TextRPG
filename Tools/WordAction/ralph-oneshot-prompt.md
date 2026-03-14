You are running a SINGLE iteration of the word classification pipeline. Do exactly ONE batch, then exit.

## Step 1: Read the full classification guide
Read `ralph-prompt.md` from the project root. It contains all rules, actions, targets, tags, design guidelines, and JSON format.

## Step 2: Get context and batch
Run these commands:
```
py Tools/WordAction/batch_next.py --count 100
py Tools/WordAction/context.py
py Tools/WordAction/stats.py
py Tools/WordAction/audit.py
```
If batch_next.py outputs `COMPLETE`, print "RALPH_COMPLETE" and exit.

## Step 3: Classify (follow ralph-prompt.md Phase A/B/C)
- Phase A: Triage all 100 words into game-word candidates vs non-game
- Phase B: Deep investigation of each candidate (action combos, uniqueness, synergies)
- Phase C: Cross-reference batch for diversity

## Step 4: Insert
Write the full JSON array (all 100 words) to `Tools/WordAction/batch_temp.json`, then run:
```
py Tools/WordAction/batch_insert.py --file Tools/WordAction/batch_temp.json
```

## Step 5: Verify and exit
Run `py Tools/WordAction/stats.py` to confirm insertion. Print a short summary table of game words classified, then exit.
