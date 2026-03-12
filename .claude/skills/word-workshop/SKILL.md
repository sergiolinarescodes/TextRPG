---
name: word-workshop
description: >
  Creative mechanic design workshop. Pulls random unclassified words as inspiration
  to design and implement NEW C# game mechanics — action handlers, passive triggers/effects,
  status effects, unit/item archetypes. Use when user says "word workshop", "new mechanic",
  "design mechanic", or "expand mechanics".
user-invocable: true
---

# Word Workshop — Mechanic Design Skill

Design and implement new game mechanics inspired by random dictionary words. One word at a time, interview-style.

**IMPORTANT**: Use the `AskUserQuestion` tool for ALL user decisions throughout this skill. Never ask the user to type free-form responses when a structured selection would work. This makes the workflow fast and clickable.

---

## Phase 1: Context Snapshot

Run the audit to understand the current mechanic landscape:

```bash
py Tools/WordAction/audit.py
```

Summarize findings as a brief gap analysis:
- Which actions are unused or rare (<1% usage)?
- Which passive triggers/effects are underused?
- Which status effects exist but lack variety?
- What mechanic categories are missing entirely?

Frame as: "Here are the gaps — new mechanics in these areas would have the most impact."

---

## Phase 2: Pull One Word

Pull a batch of random words from NLTK (batch_next.py goes alphabetically, so use random sampling instead):

```bash
py -c "
import random
from nltk.corpus import words as nltk_words
all_words = sorted(set(w.lower() for w in nltk_words.words() if w.isalpha() and 4 <= len(w) <= 10))
sample = random.sample(all_words, 10)
for w in sample:
    print(w)
"
```

Filter out words with no game potential (articles, prepositions, pronouns, conjunctions, obscure technical jargon).

Use `AskUserQuestion` to let the user pick which word to explore:
- Present 3-4 of the most inspiring words as options
- Each option's description should hint at the mechanic direction (e.g., "ravens — dark/scavenger, could inspire death-triggered mechanics")

---

## Phase 3: Step-by-Step Builder

Build the word's design one piece at a time using sequential `AskUserQuestion` calls. The flow depends on what type of word it is.

**DO NOT show JSON at any point.** Describe everything in plain English.

---

### Flow A: Unit Word (Summon)

When the word is best as a summon unit, walk through these steps in order:

#### Step 1 — Ability 1
Propose 3 options for the unit's first ability. Mix existing actions and NEW action ideas. For each, briefly explain what it does and how many future words could reuse it.

`AskUserQuestion`: "What should this unit's first ability be?"
- Option 1: a NEW action idea (label it "NEW: ActionName") with description of gameplay + reusability
- Option 2: a different NEW action idea
- Option 3: an existing action that fits thematically

#### Step 2 — Ability 2
Same format. Propose 3 different options for a second ability. Complement the first — if ability 1 is damage, ability 2 should be utility/debuff/etc.

`AskUserQuestion`: "What should the second ability be?"
- 3 options (mix of NEW and existing, different from step 1 choices)

#### Step 3 — Passive
**The passive is the unit's identity** — it should be the most memorable, defining trait. Propose 3-4 creative passive compositions that make this unit feel unique.

**Prioritize word-mechanic-tied passives** that interact with the player's typing:
- `on_word_length` — triggers based on word length (short words = quick/weak, long words = powerful)
- `on_word_tag` — triggers when words of a certain tag are played
- `on_word_played` — triggers on every word played
- **NEW triggers** — propose triggers that don't exist yet, like reacting to specific letters, repeated words, consonant/vowel ratios, etc.

At least 2 options should use NEW triggers or effects. At least 1 should tie into typing/word mechanics. Include "No passive" only if genuinely appropriate.

`AskUserQuestion`: "What passive should this unit have?"
- Option 1: a NEW trigger/effect that ties into word mechanics (label "NEW: trigger_name")
- Option 2: a different NEW trigger/effect, creative and defining
- Option 3: an existing combo that's thematic but uses underused triggers
- Option 4 (optional): "No passive" only if the unit's abilities are strong enough alone

#### Step 4 — Tags (existing)
Use `AskUserQuestion` with `multiSelect: true` to pick from existing tags.

`AskUserQuestion`: "Which existing tags fit this word?" (multi-select)
- 3-4 most relevant tags from the existing 16

#### Step 4b — New Tags
**ALWAYS** offer 2-3 new tag ideas that don't exist yet but would fit this word AND benefit 10+ future words. This step is mandatory, not optional.

`AskUserQuestion`: "Create any new tags?" (multi-select)
- Option 1: "NEW: TAG_NAME" — description explains what it covers + how many future words benefit
- Option 2: "NEW: TAG_NAME" — another idea
- Option 3: "No new tags" — stick with existing only

#### Step 5 — Mana Cost
**MANDATORY step.** Query the DB for similar words (same primary action or similar action combos) to show the user what existing words cost. This gives context for appropriate pricing.

```bash
py -c "
import sqlite3
conn = sqlite3.connect('Assets/StreamingAssets/wordactions.db')
# Replace ACTION_NAME with the chosen primary action
rows = conn.execute('''
    SELECT wa.word, wm.cost, GROUP_CONCAT(wa.action_name || ':' || wa.value, ' + ')
    FROM word_actions wa
    JOIN word_meta wm ON wa.word = wm.word
    WHERE wa.word IN (SELECT word FROM word_actions WHERE action_name = 'ACTION_NAME')
    GROUP BY wa.word
    ORDER BY wm.cost
''').fetchall()
for r in rows:
    print(f'  cost {r[1]}: {r[0]:15s} → {r[2]}')
conn.close()
"
```

Present 3-4 cost options with reference comparisons. Each option description MUST include 2-3 existing words at that cost level with their action profiles.

`AskUserQuestion`: "What mana cost for the summon word (singular)?"
- Option 1: lowest reasonable cost — description lists similar-cost words (e.g., "Cost 3: raven → Summon:1, treasonist → Summon:1")
- Option 2: moderate cost — description lists words
- Option 3: higher cost — description lists premium words
- Option 4: the highest cost if word is particularly powerful

Then for plural form: cost = singular cost + 3 (standard plural summon surcharge).

For the **unit's ability words**, also set costs:
- Simple ability words (single action): cost 0-1
- Combo ability words (2 actions): cost 1-2

#### Step 6 — Confirm
Present the full design summary in plain English:
- Unit name, type (enemy/structure), stats
- Ability 1: action name (NEW or existing), what it does
- Ability 2: action name (NEW or existing), what it does
- Passive: trigger -> effect -> target (NEW or existing pieces labeled)
- Tags
- **Mana costs**: singular summon cost, plural summon cost, each ability word cost
- Full file list of everything to create/modify

`AskUserQuestion`: "Implement this design?"
- "Yes, implement"
- "Tweak something"
- "Start over with new word"

---

### Flow B: Action Word (not a unit)

When the word is a regular combat/support word (not a summon/item), walk through:

#### Step 1 — Primary Action
Propose 3 options for the word's main action. At least one should be a NEW action. For each, explain gameplay + reusability.

`AskUserQuestion`: "What should this word's primary action be?"
- Option 1: NEW action idea
- Option 2: different NEW action idea
- Option 3: existing action that fits

#### Step 2 — Secondary Action (optional)
Propose 2-3 options for a complementary secondary action, or "No secondary."

`AskUserQuestion`: "Add a secondary action?"
- Option 1: a complementary action (NEW or existing)
- Option 2: a different complementary action
- Option 3: "No secondary action"

#### Step 3 — Tags
Multi-select from relevant existing tags, then ALWAYS offer new tag ideas (same as unit flow Steps 4 + 4b).

#### Step 4 — Mana Cost
**MANDATORY step.** Query the DB for words with the same primary action to show cost comparisons.

```bash
py -c "
import sqlite3
conn = sqlite3.connect('Assets/StreamingAssets/wordactions.db')
rows = conn.execute('''
    SELECT wa.word, wm.cost, wm.target, GROUP_CONCAT(wa.action_name || ':' || wa.value, ' + ')
    FROM word_actions wa
    JOIN word_meta wm ON wa.word = wm.word
    WHERE wa.word IN (SELECT word FROM word_actions WHERE action_name = 'ACTION_NAME')
    GROUP BY wa.word
    ORDER BY wm.cost
''').fetchall()
for r in rows:
    print(f'  cost {r[1]}: {r[0]:15s} ({r[2]}) → {r[3]}')
conn.close()
"
```

Present 3-4 cost options with previews showing existing words at each cost level. Each option description MUST include 2-3 existing reference words with their action combos and targets.

Use this **Mana Cost Reference Table** for calibration:

| Cost | Profile | Example |
|------|---------|---------|
| 0 | Weak single action (value 1), common word | drip → Water:1 |
| 1 | Single action (value 2-3) or weak combo | flame → Burn:2 + Damage:2 |
| 2 | Strong single action (value 3-4) or standard combo | saber → Cleave:3 |
| 3 | Multi-action combo or AoE | tornado → Damage:3 + Scramble:1 |
| 4 | Premium multi-action or powerful AoE | supernova → Cataclysm:4 (All) |
| 5 | Maximum power (3 actions or devastating AoE) | tsunami → Water:5 + Damage:5 + Push:2 + Scramble:1 |

**Cost modifiers:**
- AoE targeting (AllEnemies, All): +1 cost over single-target equivalent
- Self-only beneficial (heal, buff, shield): -1 cost (no offensive threat)
- Combo with secondary debuff (Bleed, status): +1 cost
- Summon words (singular): base ability cost + 4
- Summon words (plural/dual): singular cost + 3 (summons both ally slots)

`AskUserQuestion`: "What mana cost for this word?"
- Option descriptions MUST reference specific existing words at each cost level

For **plural forms**: typically same cost or +1 if target upgrades (e.g., SingleEnemy → TwoRandomEnemies).
For **plural summon forms**: singular cost + 3 (dual summon premium).

#### Step 5 — Confirm
Summary including **mana cost + reference justification** + file list + implement question (same as unit flow Step 6).

---

### Flow C: Item Word

When the word is best as an equipment item, walk through:

#### Step 1 — Item Type
`AskUserQuestion`: "What type of item?"
- Options: "weapon", "head", "wear", "accessory", "consumable"

#### Step 2 — Item Passive
Propose 3 passive compositions for the item (or "No passive").

`AskUserQuestion`: "What passive should this item have?"
- 3 options mixing NEW and existing triggers/effects

#### Step 3 — Tags
Multi-select from relevant existing tags, then ALWAYS offer new tag ideas (same as unit flow Steps 4 + 4b).

#### Step 4 — Mana Cost
**MANDATORY step.** Items typically cost 0 mana (they go to inventory, not combat). But some premium items may cost 1-2. Query DB for existing item words to compare.

`AskUserQuestion`: "What mana cost for this item word?"
- Option 1: "Cost 0 (standard)" — most items are free to acquire
- Option 2: "Cost 1" — if item has immediate combat effect on pickup
- Option 3: "Cost 2" — premium item with strong stats/passive

#### Step 5 — Confirm
Summary + implement question.

---

## Phase 4: Implement

After user approval, implement following the appropriate pattern below.

### New Action (Template-Based)

1. Add `public const string` to `ActionNames.cs`
2. Add `ActionTemplateDef` entry to `ActionDefinitionTable.cs`
3. Add to `VALID_ACTIONS` in `batch_insert.py`
4. Add row to AVAILABLE ACTIONS table in `ralph-prompt.md`
5. Add the inspiring word to `seed_db.py` (SEED_ACTIONS, SEED_META, SEED_TAGS)
6. Add verification checks to `CombatActionVerificationScenario`

### New Action (Custom Handler)

All of the above, plus:
1. Create `internal sealed class` in `Handlers/` implementing `IActionHandler`
2. Register in `ActionHandlerFactory.CreateDefault()` under "Complex handlers"
3. Add `.meta` file (copy structure from existing handler meta)

### New Passive Trigger

1. Create `internal sealed class` with `[AutoScan]` in `Passive/Triggers/`
2. Add display mapping in `PassiveDefinitions.cs` (trigger name + description)
3. Add to `VALID_TRIGGERS` in `batch_insert.py`
4. Add to Available triggers table in `ralph-prompt.md`
5. Design a showcase unit/item that uses it, add to `seed_db.py`
6. Add verification checks in `PassiveVerificationScenario`

### New Passive Effect

1. Create `internal sealed class` with `[AutoScan]` in `Passive/Effects/`
2. Add display mapping in `PassiveDefinitions.cs` (effect name + color)
3. Add to `VALID_EFFECTS` in `batch_insert.py`
4. Add to Available effects table in `ralph-prompt.md`
5. Design a showcase unit/item that uses it, add to `seed_db.py`
6. Add verification checks in `PassiveVerificationScenario`

### New Status Effect

1. Add to `StatusEffectType` enum
2. Create handler in `StatusEffect/Handlers/` implementing `IStatusEffectHandler`
3. Create the action that applies it (template `apply_status` or custom handler)
4. Add constant to `ActionNames.cs` + template def to `ActionDefinitionTable.cs`
5. Register handler in `StatusEffectHandlerFactory`
6. Add to `VALID_STATUS_EFFECTS` in `batch_insert.py`
7. Update Available statuses in `ralph-prompt.md`
8. Add to `StatusEffectColors` if custom color needed
9. Add the inspiring word to `seed_db.py`

### Letter Challenge Passives (on_letter_in_word)

The **Letter Challenge System** (`ILetterChallengeService`) enables passives that interact with specific letters in typed words. Use this for items/units that reward typing precision.

**How it works**: Each turn, the trigger selects a random letter (based on mode). If the player's word contains that letter, the passive effect fires. A visual in the bottom-right corner shows the active letter with a dancing animation.

**Available modes** (trigger_param DSL):
| Mode | Selection | Match |
|------|-----------|-------|
| `vowel` | Random vowel (a,e,i,o,u) | Contains (default) |
| `consonant` | Random consonant | Contains |
| `any` | Random letter (a-z) | Contains |
| `fixed:e` | Always letter 'e' | Contains |
| `vowel:starts_with` | Random vowel | Word starts with it |
| `consonant:ends_with` | Random consonant | Word ends with it |
| `fixed:e:position:3` | Always 'e' | At position 3 in word |
| `multi:aei:all` | Letters a,e,i | Word has ALL of them |

**When designing items, always consider letter challenge passives** as an option alongside standard triggers. They create a unique typing-interactive experience.

**Example item passives:**
- Telescope: `on_letter_in_word(vowel) → buff_stat(Luck:1) → Self` — match vowels for +1 Luck
- Sniper scope: `on_letter_in_word(consonant) → damage(2) → RandomEnemy` — match consonants for damage
- Oracle eye: `on_letter_in_word(vowel:starts_with) → shield(2) → Self` — word starts with vowel for shield

### New Unit/Item Archetype

1. Design unit stats + passive compositions using existing (or newly created) triggers/effects
2. Add to `seed_db.py` (SEED_UNITS, SEED_UNIT_PASSIVES, SEED_UNIT_ABILITIES, SEED_UNIT_TAGS)
3. Add showcase Summon words to SEED_ACTIONS following this **MANDATORY** pattern:
   - **Singular** (e.g., "mercenary"): 1 row — `("mercenary", "Summon", 1, "Self", None, None, "", 0)`
   - **Plural** (e.g., "mercenaries"): 2 rows with `assoc_word` pointing to the unit_id:
     ```python
     ("mercenaries", "Summon", 1, "Self", None, None, "mercenary", 0),
     ("mercenaries", "Summon", 1, "Self", None, None, "mercenary", 1),
     ```
   - Each Summon row = 1 handler Execute() call = 1 entity spawned. Plural needs 2 rows to fill both ally slots.
   - `assoc_word` is how `SummonActionHandler` resolves the unit_id for stats/passives/abilities/display name. Without it, plural words won't find the unit definition.
4. For items: add to SEED_ITEMS + SEED_ITEM_PASSIVES

### Save Word Relationships for Ralph-Loop

**MANDATORY** for every new action/tag created:

1. **Add related words to `preregister_families.py`** (NOT seed_db.py): When brainstorming word families (e.g., "Purify is reusable by: cleanse, cure, remedy..."), add 5-10 related words to the `FAMILIES` list in `Tools/WordAction/preregister_families.py` with preliminary classifications. These are inserted into the DB with `status='draft'` — immediately playable but flagged for refinement. When ralph-loop encounters a draft word, its `INSERT OR REPLACE` automatically overwrites the draft entry with `status=NULL` (refined).

2. **Update WORD FAMILIES table in `ralph-prompt.md`**: Add a row listing 10-20 words that map to the new action/tag. This helps ralph-loop classify words it encounters that weren't pre-registered.

3. **Balance mana costs across the family using the chosen word as anchor**: The mana cost selected during design becomes the **reference cost** for the entire word family. When pre-registering related words in `preregister_families.py`, calibrate each word's cost relative to the anchor:
   - Same action profile as anchor → same cost
   - Fewer/weaker actions → -1 cost
   - More/stronger actions or combo additions → +1 cost
   - AoE targeting upgrade → +1 cost
   - Summons: base ability cost + 4 for singular, singular cost + 3 for plural (dual summon)

   This creates a consistent cost curve that ralph-loop can reference. When ralph-loop encounters a word that maps to an existing action, it should check the DB for other words with that action and match costs proportionally.

### After All Code Changes

```bash
dotnet build TextRPG.sln
```
Must compile with 0 errors.

```bash
py Tools/WordAction/seed_db.py
```
Regenerate DB with seeds + draft word families. This runs `preregister_families.py` automatically at the end, inserting all draft words into the DB.

Verify the DB was updated:
```bash
py -c "import sqlite3; conn=sqlite3.connect('Assets/StreamingAssets/wordactions.db'); print(f'Words: {conn.execute(\"SELECT COUNT(DISTINCT word) FROM word_actions\").fetchone()[0]}, Drafts: {conn.execute(\"SELECT COUNT(*) FROM word_meta WHERE status=\\\"draft\\\"\").fetchone()[0]}'); conn.close()"
```

---

## Phase 5: Next Word

Use `AskUserQuestion`:
- "Continue?" → "Yes, another word", "No, show session summary"

If yes → loop back to Phase 2.
If no → show summary of all mechanics created this session:
  - New mechanic name + type
  - Files created/modified
  - Showcase words added

---

## Key Design Principles

1. **Code-first, not data-first** — The goal is expanding the C# mechanic set, not filling the DB
2. **One word at a time** — Deep creative exploration, not batch processing
3. **Always check existing mechanics first** — Only propose new code when it adds genuine reusability
4. **Reusability threshold** — New mechanic should benefit 5+ future words to justify code complexity
5. **Showcase entries** — Every new mechanic gets at least one word in seed_db.py
6. **Update the pipeline** — Every new mechanic must update batch_insert.py + ralph-prompt.md so ralph-loop can classify words into it
7. **Use AskUserQuestion for ALL decisions** — Never ask users to type when they can click
8. **Singular + plural summon pattern** — ALWAYS add both singular and plural forms. For Summon words, the DB pattern is CRITICAL:
   - **Singular** (e.g., "mercenary"): 1 Summon row, value=1, assoc_word="" (word itself IS the unit_id), seq=0
   - **Plural** (e.g., "mercenaries"): 2 Summon rows, each value=1, assoc_word=unit_id (e.g., "mercenary"), seq=0 and seq=1. This creates 2 separate Execute() calls so both ally slots fill.
   - **NEVER** use value=2 on a single Summon row to mean "summon 2" — each row = 1 summon execution.
   - **assoc_word is mandatory** when the word differs from the unit_id (all plurals, plus words like "crewmate" → "pirate").
   - Max ally slots is 2. Max enemy slots is 3.
9. **Mana cost is a design decision** — ALWAYS ask the user to choose mana cost with DB-sourced comparisons of similar words. The chosen cost becomes the reference anchor for all related words in preregister_families.py and ralph-prompt.md WORD FAMILIES. This is critical for ralph-loop: when it encounters a new word that maps to an existing action, it calibrates cost by looking at what similar words cost in the DB.

---

## Phase 1.5: Full Mechanic Scan (Mandatory)

After running audit.py, run this comprehensive scan before ANY design work:

```bash
py -c "
import sqlite3, os, glob
conn = sqlite3.connect('Assets/StreamingAssets/wordactions.db')

print('=== ALL ACTIONS ===')
for r in conn.execute('SELECT DISTINCT action_name FROM word_actions ORDER BY action_name'):
    print(f'  {r[0]}')

print('\n=== ALL WORD TAGS ===')
for r in conn.execute('SELECT DISTINCT tag FROM word_tags ORDER BY tag'):
    print(f'  {r[0]}')

print('\n=== ALL UNIT TAGS ===')
for r in conn.execute('SELECT DISTINCT tag FROM unit_tags ORDER BY tag'):
    print(f'  {r[0]}')

print('\n=== ALL UNITS ===')
for r in conn.execute('SELECT unit_id, unit_type FROM units ORDER BY unit_id'):
    print(f'  {r[0]:20s} {r[1]}')

print('\n=== ALL ITEMS ===')
for r in conn.execute('SELECT item_id, item_type FROM items ORDER BY item_id'):
    print(f'  {r[0]:20s} {r[1]}')

print('\n=== ALL STATUS EFFECTS (from handlers) ===')
for f in sorted(glob.glob('Assets/Scripts/Core/StatusEffect/Handlers/*Handler.cs')):
    print(f'  {os.path.basename(f)}')

print('\n=== ALL TAG DEFINITIONS (from code) ===')
for f in sorted(glob.glob('Assets/Scripts/Core/EventEncounter/Reactions/Tags/Definitions/*TagDefinition.cs')):
    print(f'  {os.path.basename(f)}')

print('\n=== ALL PASSIVE TRIGGERS (from code) ===')
for f in sorted(glob.glob('Assets/Scripts/Core/Passive/Triggers/*Trigger.cs')):
    print(f'  {os.path.basename(f)}')

print('\n=== ALL PASSIVE EFFECTS (from code) ===')
for f in sorted(glob.glob('Assets/Scripts/Core/Passive/Effects/*Effect.cs')):
    print(f'  {os.path.basename(f)}')

print('\n=== ALL STATS (from code) ===')
import re
with open('Assets/Scripts/Core/EntityStats/StatType.cs') as f:
    body = f.read().split('{', 2)[2].split('}')[0]
    for m in re.findall(r'(\w+)', body):
        if m[0].isupper(): print(f'  {m}')

conn.close()
"
```

This auto-discovers everything. No hardcoded lists to maintain.

**RULE: Before labeling anything "NEW", grep for it first:**
```bash
rg -i "mechanic_name" Assets/Scripts/ --type cs
```

### Action Templates

| Template | Params | Description |
|----------|--------|-------------|
| `scaled_damage` | OffensiveStat, DefensiveStat | `Max(1, base + off/3 - def/3)` |
| `apply_status` | StatusEffectType, Duration mode | Apply status (ApplySelf: true for self-buffs) |
| `stat_modifier` | StatType, "buff"/"debuff" | Temporary stat change |
| `heal` | -- | `base + MagicPower/3` |
| `shield` | -- | `base + PhysicalDefense/3` |
| `mana_self` | -- | Restore mana to caster |
| `noop` | -- | Does nothing |
| `push` | -- | Push target |
| `fire` | -- | Fire element combo |

### Key File Paths

| Component | Path |
|-----------|------|
| Action names | `Assets/Scripts/Core/ActionExecution/ActionNames.cs` |
| Action templates | `Assets/Scripts/Core/ActionExecution/ActionDefinitionTable.cs` |
| Handler factory | `Assets/Scripts/Core/ActionExecution/ActionHandlerFactory.cs` |
| Handler interface | `Assets/Scripts/Core/ActionExecution/IActionHandler.cs` |
| Action context | `Assets/Scripts/Core/ActionExecution/ActionContext.cs` |
| Handler context | `Assets/Scripts/Core/ActionExecution/IActionHandlerContext.cs` |
| Stat scaling | `Assets/Scripts/Core/ActionExecution/StatScaling.cs` |
| Custom handlers | `Assets/Scripts/Core/ActionExecution/Handlers/` |
| Action verification | `Assets/Scripts/Core/ActionExecution/Scenarios/CombatActionVerificationScenario.cs` |
| Status effect types | `Assets/Scripts/Core/StatusEffect/StatusEffectType.cs` |
| Status handlers | `Assets/Scripts/Core/StatusEffect/Handlers/` |
| Status handler factory | `Assets/Scripts/Core/StatusEffect/StatusEffectHandlerFactory.cs` |
| Status colors | `Assets/Scripts/Core/StatusEffect/StatusEffectColors.cs` |
| Passive triggers | `Assets/Scripts/Core/Passive/Triggers/` |
| Passive effects | `Assets/Scripts/Core/Passive/Effects/` |
| Passive definitions | `Assets/Scripts/Core/Passive/PassiveDefinitions.cs` |
| Passive target resolver | `Assets/Scripts/Core/Passive/PassiveTargetResolver.cs` |
| Passive verification | `Assets/Scripts/Core/Passive/Scenarios/PassiveVerificationScenario.cs` |
| Seed database | `Tools/WordAction/seed_db.py` |
| Batch insert | `Tools/WordAction/batch_insert.py` |
| Draft families | `Tools/WordAction/preregister_families.py` |
| Ralph prompt | `ralph-prompt.md` |
| Audit script | `Tools/WordAction/audit.py` |

### batch_insert.py Validation Sets

When adding new mechanics, update these sets in `batch_insert.py`:
- `VALID_ACTIONS` -- all action names
- `VALID_TRIGGERS` -- all passive trigger IDs
- `VALID_EFFECTS` -- all passive effect IDs
- `VALID_STATUS_EFFECTS` -- all status effects usable in apply_status
- `VALID_TAGS` -- all word tags
- `VALID_PASSIVE_TARGETS` -- Self, AllAllies, AllEnemies, Injured, Attacker

### Draft Word Registration

Use `batch_insert.py --draft` to insert pre-registered word families with `status='draft'` in `word_meta`. Draft words are playable immediately but flagged for ralph-loop refinement. When ralph-loop re-classifies a draft word, its normal `INSERT OR REPLACE` overwrites the draft entry with `status=NULL`.

Add new word families to `preregister_families.py` FAMILIES list (NOT to seed_db.py). The script runs automatically after `seed_db.py`.
