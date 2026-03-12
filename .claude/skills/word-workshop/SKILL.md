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

#### Step 5 — Confirm
Present the full design summary in plain English:
- Unit name, type (enemy/structure), stats
- Ability 1: action name (NEW or existing), what it does
- Ability 2: action name (NEW or existing), what it does
- Passive: trigger -> effect -> target (NEW or existing pieces labeled)
- Tags
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

#### Step 4 — Confirm
Summary + file list + implement question (same as unit flow Step 5).

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

#### Step 4 — Confirm
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

### New Unit/Item Archetype

1. Design unit stats + passive compositions using existing (or newly created) triggers/effects
2. Add to `seed_db.py` (SEED_UNITS, SEED_UNIT_PASSIVES, SEED_UNIT_ABILITIES, SEED_UNIT_TAGS)
3. Add showcase word with `Summon`/`Item` action to SEED_ACTIONS
4. For items: add to SEED_ITEMS + SEED_ITEM_PASSIVES

### Save Word Relationships for Ralph-Loop

**MANDATORY** for every new action/tag created:

1. **Add related words to `preregister_families.py`** (NOT seed_db.py): When brainstorming word families (e.g., "Purify is reusable by: cleanse, cure, remedy..."), add 5-10 related words to the `FAMILIES` list in `Tools/WordAction/preregister_families.py` with preliminary classifications. These are inserted into the DB with `status='draft'` — immediately playable but flagged for refinement. When ralph-loop encounters a draft word, its `INSERT OR REPLACE` automatically overwrites the draft entry with `status=NULL` (refined).

2. **Update WORD FAMILIES table in `ralph-prompt.md`**: Add a row listing 10-20 words that map to the new action/tag. This helps ralph-loop classify words it encounters that weren't pre-registered.

3. **Balance mana costs across the family**: Single-action words cost less (1-2), combo words cost more (3-4), premium 3-action words cost the most (4-5). Summons add +3 on top.

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
8. **Singular + plural** — When adding words to the DB, ALWAYS add both singular and plural forms unless they mean completely different things. For Summon words specifically: singular = Summon:1 (one ally, lower cost), plural = Summon:2 (both ally slots, higher cost). Max ally slots is 2.

---

## Existing Mechanics Catalog

### Actions (43 total)

**Scaled Damage (4):** Damage (Str/PDef), MagicDamage (Mgc/MDef), WeaponDamage (Dex/PDef), Smash (Str/PDef)

**Support (3):** Heal (heal template), Shield (shield template), Thinking (mana_self)

**Utility (3):** Pay (noop), Push (push), Fire (fire)

**Status -> Target (6):** Burn->Burning, Water->Wet, Fear->Fear, Stun->Stun, Poison->Poisoned, Bleed->Bleeding

**Status -> Self (8):** Grow->Growing, Thorns->Thorns, Reflect->Reflecting, Hardening->Hardening, Drunk->Drunk, Freeze->Frostbitten, Energize->Energetic, Sleep->Sleep

**Tag-Driven (1):** Relax (noop, RELAX tag removes Anxiety)

**Stat Buffs (7):** BuffStrength, BuffMagicPower, BuffPhysicalDefense, BuffMagicDefense, BuffLuck, BuffMaxMana, BuffManaRegen

**Stat Debuffs (7):** DebuffStrength, DebuffMagicPower, DebuffPhysicalDefense, DebuffMagicDefense, DebuffLuck, DebuffMaxMana, DebuffManaRegen

**Special (1):** Melt (PhysicalDefense debuff, stat_modifier template)

**Custom Handlers (8):** Shock (lightning+wet bonus), Concentrate (mana+Concentrated), Summon (spawn unit), RestHeal (conditional heal), Scramble (swap slot positions), Item (equip to inventory), Siphon (steal random stat from target, buff self), Deceive (Fear + Concussion)

**Interaction (11):** Enter, Talk, Steal, Search, Pray, Rest, Open, Trade, Recruit, Leave, Charm

### Status Effects (24 total)

Burning, Wet, Poisoned, Frozen, Slowed, Cursed, Buffed, Shielded, ExtraTurn, Stun, Concussion, Fear, Bleeding, Concentrated, Growing, Thorns, Reflecting, Hardening, Drunk, Frostbitten, Energetic, Tired, Sleep, Anxiety

### Passive Triggers (10 + taunt marker)

on_ally_hit, on_self_hit, on_round_end, on_round_start, on_turn_start, on_turn_end, on_word_played, on_word_length, on_word_tag, on_kill, taunt

### Passive Effects (6)

heal, damage, shield, mana, apply_status, steal_stat

### Passive Targets (5)

Self, AllAllies, AllEnemies, Injured, Attacker

### Tags (20)

NATURE, ELEMENTAL, OFFENSIVE, RESTORATION, SHADOW, PHYSICAL, DEFENSIVE, ARCANE, HOLY, SUPPORT, PSYCHIC, SPELL, MELEE, SOCIAL, THOUGHTS, RELAX, DWELLING, BEAST, FLYING, LIGHT, CLEANSING, DEBUFF, STEALTH

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
