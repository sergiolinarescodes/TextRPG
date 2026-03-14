# Word Dictionary — Ralph Loop Prompt

Classify NLTK dictionary words into the game's action system via SQLite database. Each iteration: get a batch of unclassified words, classify them, insert into the DB.

---

## CONTEXT MANAGEMENT

Each iteration runs in a **fresh context window** (no accumulated history). State lives in the SQLite DB and filesystem.

- **Keep batch JSON output minimal** — write JSON to `Tools/WordAction/batch_temp.json` and pipe it to `batch_insert.py`, do NOT paste large JSON blocks inline
- **Run `context.py` every iteration** — it contains valid actions, targets, tags, and distribution data

### Running the loop
```bash
bash Tools/WordAction/ralph-loop.sh        # 10 iterations (default)
bash Tools/WordAction/ralph-loop.sh 50     # 50 iterations
bash Tools/WordAction/ralph-loop.sh 0      # run until COMPLETE
```

---

## PIPELINE TOOLS

| Script | Purpose |
|--------|---------|
| `Tools/WordAction/setup.py` | One-time: install NLTK, create tracking tables |
| `Tools/WordAction/seed_db.py` | One-time: create DB with ~50 seed words |
| `Tools/WordAction/batch_next.py --count N` | Get next N unclassified words as JSON |
| `Tools/WordAction/batch_insert.py` | Read classified JSON from stdin, insert into DB |
| `Tools/WordAction/stats.py` | Show progress (total/processed/remaining) |
| `Tools/WordAction/context.py` | Generate full reference data (valid actions, targets, tags, families, DB distribution) |
| `Tools/WordAction/audit.py` | Deep audit of word action distribution |

---

## EACH ITERATION

### Step 1: Get next batch
```bash
py Tools/WordAction/batch_next.py --count 100
```
If output is `COMPLETE`, all words are done — stop.

### Step 1.5: Load context and analyze state
```bash
py Tools/WordAction/context.py
py Tools/WordAction/stats.py
py Tools/WordAction/audit.py
```
**`context.py` outputs ALL reference data you need**: valid actions with descriptions, valid targets/tags/triggers/effects, word family pre-classifications, and current DB distribution. Read its output carefully — it replaces all reference tables.

Review the audit output to understand:
- **Action distribution**: which actions are overused/underused?
- **Tag coverage**: which tags need more words?
- **Unit diversity**: are summons clustered around the same passive archetypes?
- **Item coverage**: which equipment slots are underrepresented?
- **Duplicate profiles**: which action+value+target combos already have too many words?

Use this context to guide classification decisions for the current batch.

### Step 2: Classify the words
For each word in the batch, decide:
- Is it a **game word** (has meaningful combat/RPG actions)? → assign actions, target, cost, range, area, tags
- Is it a **non-game word** (articles, prepositions, abstract nouns with no combat meaning)? → empty actions, default meta, no tags

Output JSON array:
```json
[
    {
        "word": "tsunami",
        "target": "AllEnemies",
        "cost": 6,
        "range": 0,
        "area": "Single",
        "tags": ["ELEMENTAL", "NATURE", "OFFENSIVE"],
        "actions": [
            {"action": "Water", "value": 5},
            {"action": "Damage", "value": 5},
            {"action": "Push", "value": 2}
        ]
    },
    {
        "word": "absorb",
        "target": "SingleEnemy",
        "cost": 0,
        "range": 0,
        "area": "Single",
        "tags": ["SHADOW", "RESTORATION"],
        "actions": [
            {"action": "Damage", "value": 1, "target": "SingleEnemy"},
            {"action": "Heal", "value": 1, "target": "Self"}
        ]
    },
    {
        "word": "sacrifice",
        "target": "SingleEnemy",
        "cost": 2,
        "range": 0,
        "area": "Single",
        "tags": ["SHADOW", "OFFENSIVE"],
        "actions": [
            {"action": "Damage", "value": 5, "target": "SingleEnemy"},
            {"action": "Damage", "value": 2, "target": "Self"}
        ]
    },
    {
        "word": "rampage",
        "target": "AllEnemies",
        "cost": 4,
        "range": 0,
        "area": "Single",
        "tags": ["OFFENSIVE", "MELEE", "PHYSICAL"],
        "actions": [
            {"action": "Damage", "value": 3, "target": "AllEnemies"},
            {"action": "BuffStrength", "value": 2, "target": "Self"}
        ]
    },
    {
        "word": "library",
        "target": "Self",
        "cost": 3,
        "range": 0,
        "area": "Single",
        "tags": ["SUPPORT", "ARCANE"],
        "actions": [{"action": "Summon", "value": 3}],
        "unit": {
            "display_name": "LIBRARY",
            "unit_type": "structure",
            "max_health": 20,
            "strength": 0, "magic_power": 5,
            "phys_defense": 3, "magic_defense": 5,
            "luck": 0, "starting_shield": 0,
            "color": [0.6, 0.5, 0.3],
            "actions": [],
            "passives": [
                {"trigger": "on_word_length", "trigger_param": "6", "effect": "heal", "value": 2, "target": "AllAllies"},
                {"trigger": "on_word_length", "trigger_param": "8", "effect": "shield", "value": 1, "target": "AllAllies"}
            ]
        }
    },
    {
        "word": "the",
        "target": "SingleEnemy",
        "cost": 0,
        "range": 0,
        "area": "Single",
        "tags": [],
        "actions": []
    }
]
```

### Per-Action Targeting (Multi-Target Words)

**Every action supports its own target.** Any action — Damage, Heal, Burn, Drunk, Freeze, Poison, Shield, Buff, Debuff, Concentrate, Thorns, Reflect, Hardening, Grow, Energize, Stun, Fear, Sleep, Mana, etc. — can target anything: Self, SingleEnemy, AllEnemies, AllAllies, RandomEnemy, positional slots, stat-based picks, or any other valid target.

This is the key to creative word design. Don't think of actions as having "default" targets. Think about what THIS word would actually do, and target each action independently.

**Rules:**
- All actions share the same target → use word-level `"target"` only (no per-action fields needed)
- Actions target different things → each action MUST have an explicit `"target"` field
- Word-level `"target"` = the primary target (used for UI/display). Set it to whichever action is the "main" one

**Design patterns:**

| Pattern | Actions | Example words |
|---------|---------|---------------|
| Vampiric | Damage(Enemy) + Heal(Self) | absorb, drain, leech |
| Sacrifice | Damage(Enemy,high) + Damage/Poison(Self) | sacrifice, immolate, martyr |
| Berserker | Damage(AllEnemies) + Buff(Self) | rampage, frenzy, berserk |
| Rally | Buff(AllAlliesAndSelf) + Fear(AllEnemies) | warcry, rally, inspire |
| Guardian | Shield(AllAllies) + Hardening(Self) | protect, fortify, bulwark |
| Cursed Gift | Heal(Self) + Drunk/Poison(Self) | moonshine, toxicant, hemlock |
| Intimidation | Fear(AllEnemies) + BuffStrength(Self) | intimidate, menace, terrorize |
| Sabotage | DebuffStrength(Enemy) + Poison(Enemy) + Shield(Self) | sabotage, undermine |
| Friendly Fire | Damage(AllEnemies) + Damage(AllAllies) | earthquake, shockwave, cataclysm |
| Contagion | Poison(AllEnemies) + Burn(Self) | plague, outbreak, pandemic |
| Overexertion | BuffStrength(Self,high) + Bleed(Self) | overexert, strain, push |
| Tactical | Stun(FrontEnemy) + Damage(BackEnemy) | flank, pincer, outmaneuver |

**Think creatively.** A word like "gamble" could BuffLuck(Self) + Damage(Self). "Bribe" could DebuffStrength(SingleEnemy) + Mana(Self). "Martyr" could Heal(AllAllies) + Damage(Self). Any combination is possible — let the word's meaning drive the targeting.

### Step 3: Insert into DB
Write the JSON array to `Tools/WordAction/batch_temp.json` using the Write tool, then run:
```bash
py Tools/WordAction/batch_insert.py --file Tools/WordAction/batch_temp.json
```
**IMPORTANT**: Do NOT use heredocs (`<<EOF`), `echo`, or `cat` to pipe JSON through stdin — this breaks on Windows bash due to quote escaping. Always write to a file first, then use `--file`.

### Step 4: Check progress
```bash
py Tools/WordAction/stats.py
```

---

## UNIT PASSIVES (for Summon words)

Units summoned via `Summon` action can have composable passives. Include a `"unit"` field with stats, abilities, and passives. The `unit_id` is the word itself (lowercase). See `context.py` output for valid triggers, effects, targets, and statuses.

### Unit JSON format

```json
"unit": {
    "display_name": "LIBRARY",
    "unit_type": "structure",
    "max_health": 20,
    "strength": 0, "magic_power": 5,
    "phys_defense": 3, "magic_defense": 5,
    "luck": 0, "starting_shield": 0,
    "color": [0.6, 0.5, 0.3],
    "actions": ["scratch", "charge"],
    "passives": [
        {"trigger": "on_word_length", "trigger_param": "6", "effect": "heal", "value": 2, "target": "AllAllies"},
        {"trigger": "taunt"}
    ]
}
```

- `unit_type`: `"enemy"` (has abilities, fights) or `"structure"` (few/no abilities, passive effects)
- `actions`: array of word strings the unit can use in combat — must be words that exist in the DB
- `passives`: array of passive objects (trigger + effect + target)
- `color`: `[r, g, b]` floats 0.0-1.0 (display color)
- Stats: `max_health` 5-100, others 0-15, `starting_shield` 0-20
- `taunt` passive: `{"trigger": "taunt"}` — marker only, forces enemy targeting

### Passive design archetypes

| Word Theme | unit_type | Trigger | Effect | Target | Examples |
|---|---|---|---|---|---|
| Walls/Forts | structure | on_ally_hit | heal | Injured | fortress, bastion, rampart |
| Healing totems | structure | on_round_end | heal | AllAllies | totem, shrine, obelisk |
| Attack structures | structure | on_ally_hit | damage | Attacker | turret, cannon, sentry |
| Knowledge | structure | on_word_length 6+ | heal/shield | AllAllies | library, academy, archive |
| Nature synergy | structure | on_word_tag NATURE | heal | AllAllies | grove, garden, glade |
| Aura emitters | structure | on_round_start | apply_status | AllEnemies | pyre, beacon, brazier |
| Mana sources | structure | on_word_played | mana | Self | fountain, nexus, leyline |
| Death release | structure | on_death (siphon) | heal | AllAllies | twinflower |
| Shield givers | structure | on_ally_hit | shield | Injured | sentinel, aegis, ward |
| Taunt tanks | structure | taunt | — | — | sentinel, guardian, decoy |
| Kill-reward | enemy | on_kill | heal | Self | predator, hunter, reaper |

### Enemy mana balance (for units with abilities)

Enemy units have a fixed mana pool: **MaxMana=10, StartingMana=5, ManaRegen=2/turn**.

| Cost | Turn 1 (5 mana) | Turn 2 (+2 regen) | Turn 3 (+2 regen) | Frequency |
|------|-----------------|-------------------|-------------------|-----------|
| **5** | Use (5→0) | scratch (2) | scratch (4) | Every 3 turns |
| **4** | Use (5→1) | scratch (3) | Use (5→1) | Every 2 turns |
| **3** | Use (5→2) | Use (4→1) | Use (3→0) | ~2 of 3 turns |
| **2** | Use (5→3) | Use (5→3) | Use (5→3) | Every turn |

**Design rules:**
- **Signature ability** (cost 4-5): Defining move. Strong effect. Used turn 1, then every 2-3 turns.
- **Mid-tier filler** (cost 2-3): Secondary ability between signature cooldowns.
- **Free fallback** (cost 0): Basic attacks — better than scratch but not special.
- **Scratch** (cost 0, 1 dmg): Universal last resort. AI strongly avoids it.
- **Never cost 1**: Cost 1 with 2 regen means every turn with no tradeoff.

**Enemy design patterns:**

| Pattern | Ability 1 | Ability 2 | Example |
|---------|-----------|-----------|---------|
| Signature + scratch | cost 4-5 | scratch (0) | bat, goblin, skeleton |
| Signature + free attack | cost 4-5 | basic attack (0) | orc, golem, predator |
| Signature + filler | cost 5 | cost 2 | shaman, wraith |
| All-in | cost 5 | scratch (0) | sellsword |

---

## EQUIPPABLE ITEMS (for Item words)

Words with the `Item` action become equippable items. Include an `"item"` field with type, stats, and optional passives. The `item_id` is the word itself (lowercase). See `context.py` output for valid item types.

### Item JSON format

```json
{
    "word": "crown",
    "target": "Self",
    "cost": 0,
    "range": 0,
    "area": "Single",
    "tags": ["ARCANE", "SUPPORT"],
    "actions": [{"action": "Item", "value": 0}],
    "item": {
        "display_name": "CROWN",
        "item_type": "head",
        "durability": 0,
        "strength": 0, "magic_power": 2,
        "phys_defense": 0, "magic_defense": 0,
        "luck": 1, "max_health": 0, "max_mana": 0,
        "color": [1.0, 0.85, 0.2],
        "passives": [],
        "tags": []
    }
}
```

**Item tags**: Optional `"tags"` array on the `"item"` field. Words with SILVER/VALUABLE word_tags require a matching tagged item in inventory when used with "give".

Weapon-type items use `assoc_word` to link ammo words:
```json
{
    "word": "gun",
    "target": "Self",
    "cost": 0,
    "range": 0,
    "area": "Single",
    "tags": ["PHYSICAL", "OFFENSIVE"],
    "actions": [
        {"action": "Item", "value": 5, "assoc_word": "9mm"},
        {"action": "Item", "value": 5, "assoc_word": "buckshot"}
    ],
    "item": {
        "display_name": "GUN",
        "item_type": "weapon",
        "durability": 5,
        "strength": 0, "magic_power": 0,
        "phys_defense": 0, "magic_defense": 0,
        "luck": 0, "max_health": 0, "max_mana": 0,
        "color": [0.5, 0.5, 0.5],
        "passives": []
    }
}
```

**Consumable example** (beer with healing + drunk ammo):
```json
{
    "word": "beer",
    "target": "Self",
    "cost": 0,
    "range": 0,
    "area": "Single",
    "tags": ["SUPPORT"],
    "actions": [{"action": "Item", "value": 3, "assoc_word": "sip"}],
    "item": {
        "display_name": "BEER",
        "item_type": "consumable",
        "durability": 3,
        "strength": 0, "magic_power": 0,
        "phys_defense": 0, "magic_defense": 0,
        "luck": 0, "max_health": 0, "max_mana": 0,
        "color": [1.0, 0.85, 0.2],
        "passives": []
    }
}
```

### Item types and equipment slots

| item_type | Slot | Behavior |
|-----------|------|----------|
| `head` | HEAD | Passive stats only |
| `wear` | WEAR | Passive stats only |
| `accessory` | ACCESSORY | Stats + optional passives |
| `consumable` | CONSUMABLE | Auto-equips. Durability = uses. Ammo via `assoc_word`. Destroyed when empty. |
| `weapon` | WEAPON | Stats + ammo via `assoc_word`, durability |

### Item design archetypes

| Word Theme | item_type | Stats Focus | Passives | Examples |
|---|---|---|---|---|
| Crowns/Helms | head | magic_power, luck | on_self_hit: shield | crown, helm, tiara |
| Armor/Cloaks | wear | phys_defense, magic_defense | — | cloak, robe, armor |
| Rings/Bands | accessory | strength, luck | — | ring, band, bracelet |
| Amulets | accessory | magic_power | on_word_played: mana | amulet, pendant, talisman |
| Potions/Drinks | consumable | — | — | beer, potion, elixir |
| Food/Herbs | consumable | — | — | bread, apple, mushroom |
| Swords/Axes | weapon | strength | — | sword, axe, spear |
| Staves/Wands | weapon | magic_power | on_word_tag: damage | staff, wand, scepter |

### Item rules
- Item stats: 0-5 per stat, total budget ~2-5
- Item cost is always 0 (equipping is free)
- Weapon durability: 3-10 (0 = infinite, avoid infinite for weapons)
- Non-weapon durability: 0 (equipment doesn't break)
- Item passives use the same trigger/effect/target system as unit passives
- Items should be ~2-3% of game words
- Item word target is always `Self`

---

## DESIGN AWARENESS

Every game word is a **design decision** — it permanently shapes the player's vocabulary. Treat each classification as designing a card for a deckbuilder: clear identity, reason to exist, role in the ecosystem.

Consider the FULL ecosystem:
- **Actions**: What makes THIS word's combo different from existing words?
- **Summons**: What unique battlefield role does this unit fill?
- **Items**: Which slot needs this item? What build does it enable?
- **Tags**: Which existing passives does this tag activate?

**Design principle: fewer well-designed words > many generic ones.** If a word doesn't offer something distinct, classify it as non-game.

### IMPORTANT: Existing DB words use legacy targeting

Most words already in the DB use old-style word-level targeting where all actions share one target. **Do NOT copy their targeting patterns.** Use existing words only for **cost calibration** (how much mana for this power level). For targeting, always design from the word's meaning using per-action targeting patterns — see "Per-Action Targeting" section above.

### MANA COST CALIBRATION (0–20 range)

The full mana range is **0–20**. Use the ENTIRE range to create meaningful cost curves. Cheap words are common and weak. Expensive words are rare, devastating, and worth saving mana for.

| Cost | Tier | Profile | Reference Examples |
|------|------|---------|-------------------|
| 0 | Free | Single weak action (value 1). Common short words. | scratch → Damage:1, drip → Water:1 |
| 1–2 | Cheap | Single action (value 2–3) or weak 2-action combo | flame → Burn:2, sting → Damage:2 + Poison:1 |
| 3–4 | Standard | Solid single action (value 4–5) or good 2-action combo | slash → Cleave:3 + Bleed:1, heal → Heal:4 |
| 5–7 | Strong | Powerful combo (2–3 actions), strong AoE, or impactful debuff | tornado → Damage:4 + Push:2 + Scramble:1 |
| 8–10 | Premium | Devastating multi-action, wide AoE, or elite summon | earthquake → Damage:6 + Stun:2 (AllEnemies), fortress → Summon:5 |
| 11–14 | Epic | Massive power with 3–4 high-value actions or game-changing effects | armageddon → Cataclysm:8 (All), tsunami → Water:7 + Damage:7 + Push:3 |
| 15–20 | Legendary | Ultimate abilities. World-ending damage, full-party effects, or supreme summons. Reserved for long, rare, powerful words. | annihilation → Cataclysm:10 (All) + Fear:3, resurrection → Heal:10 + Purify:5 (AllAlliesAndSelf) |

**Cost modifiers:**
- AoE targeting: +2 cost over single-target
- Self-only beneficial: -1 cost
- Combo with secondary debuff: +1 cost
- Summon words: base ability cost + 3 (singular). Plural = singular cost x2 (double)
- Items: cost 0
- Action values above 5: +1 cost per point above 5

**Target distribution by cost tier:**
- Cost 0–2: mostly SingleEnemy
- Cost 3–7: mix of SingleEnemy, Self, AllEnemies
- Cost 8–14: more AllEnemies, All, LowestHealthEnemy, stat-based targets
- Cost 15–20: All, AllEnemies, AllAlliesAndSelf

**IMPORTANT**: Before assigning a cost, check the DB for words with the same primary action and calibrate proportionally. The goal is a smooth bell curve — most words cost 1–5, some cost 6–10, a few cost 11–15, and legendary words 16–20.

---

## QUALITY INVESTIGATION PROTOCOL

Every batch must go through three phases. Do NOT skip or combine them.

### Phase A — Triage (scan all 100 words)

Quick-scan the entire batch into two buckets:
- **Game word candidates**: any word a player might plausibly type. This includes common English words (even abstract ones like "achieve", "acid", "able"), verbs, adjectives with emotional/physical connotations, and any word with even a loose RPG interpretation. **Be inclusive** — weak words get weak effects (value 1, cost 0), but they still get classified.
- **Non-game words**: ONLY truly unusable words — unrecognizable scientific compounds (acetylphenylhydrazine), taxonomic Latin (acanthocephalan), chemical formulas, obscure proper nouns, and pure grammatical suffixes (-ly, -ness, -tion forms of already-classified words).

**Bias toward inclusion.** If a player could conceivably type a word in combat, it should have actions — even if simple. A batch of 100 words should typically yield 10-30 game words, not 1-5. Short common words (3-4 letters) get 1 weak action (cost 0-1). Medium words (5-7) get 1-2 actions (cost 2-5). Long powerful words (8+) get 2-4 actions (cost 5-20).

Do NOT assign any actions during triage. Just categorize.

### Phase B — Deep Investigation (each game word candidate)

For each candidate:
1. **Word identity**: What RPG archetype does this word evoke?
2. **Uniqueness check**: Does an existing word already fill this role? If yes, vary the combo (different values, different secondary action, different target). Do NOT skip — synonyms and similar words should still be game words with slightly different profiles.
3. **Action combo design**: Choose actions that create interesting gameplay, not just "Damage N". Consider multi-action combos, status setups, varied targeting.
4. **If Summon**: Design a unique battlefield role. Vary passive triggers. No existing summon should have the same trigger+effect combo.
5. **If Item**: Run slot gap analysis. Design stats for a specific build archetype. No existing item should have the same type+stat profile.
6. **Tag synergies**: Choose tags that activate existing `on_word_tag` passives. Multiple tags = richer gameplay.

### Phase C — Batch Cross-Reference (before inserting)

Review ALL game words in the batch together:
- **Diversity**: No two game words with identical action+value+target profiles
- **Tag spread**: At least 3 different tags across game words
- **Targeting variety**: Not all SingleEnemy — mix in Self, AllEnemies, positional, random, stat-based
- **Summon/item variety**: No two summons share the same passive archetype; no two items share the same slot+stats
- **Synergy potential**: At least one word creates a new combo with existing words

Revise entries with identical profiles. But do NOT downgrade borderline words to non-game — give them simple, weak actions instead. A player typing "acerbic" should get SOMETHING, even if it's just Poison:1.

---

## CLASSIFICATION GUIDELINES

### Action Count Per Word

**Words can have 1 to 4+ actions — whatever fits the word.** Not every word needs multiple actions. A simple word with one clear effect is perfectly fine.

| Actions | When to use | Examples |
|---------|------------|---------|
| **1 action** | Simple, focused words. Most cheap words. | scratch → Damage:1, heal → Heal:3, burn → Burn:2 |
| **2 actions** | Standard combos. Most mid-cost words. | slash → Cleave:3 + Bleed:1, shock → Shock:2 + Stun:1 |
| **3 actions** | Strong words with multiple real-world effects. | tornado → Damage:4 + Push:2 + Scramble:1 |
| **4+ actions** | Epic/legendary words. Rare, expensive, devastating. | tsunami → Water:7 + Damage:7 + Push:3 + Scramble:2 |

**Guideline**: Ask "What would this ACTUALLY do?" Each real consequence = an action. But don't force extra actions on simple words — "punch" is just Damage, and that's fine.

### General rules
- **Word meaning drives effects**: "avalanche" → Damage + Push + Water; "whisper" → Fear
- **Longer/rarer words = stronger**: 3-4 letter = weak (value 1-2, cost 0-1), 5-7 letter = moderate (value 2-5, cost 2-7), 8+ letter = powerful (value 4-10, cost 5-20)
- **Cost scales with power** across the full 0-20 range. Use the whole range.
- **Range**: always `0` (slot system, everything in range)
- **Area**: always `"Single"` (targeting via `target` field)
- **Scientific/taxonomic words are NOT game words** — chemistry compounds, Latin taxonomy, medical jargon with no common usage get empty actions
- **Common words ARE game words** — if a player might type it, give it actions. Short/weak words get value 1, cost 0. Quality matters for STRONG words; weak words just need a reasonable effect
- **Synonyms are welcome** — similar words can share the same primary action with different secondary effects, values, or targets. The player vocabulary should be RICH, not sparse
- **Summon rationale**: every summon needs a 1-sentence explanation of its unique role
- **Item ecosystem fit**: check which slots are underrepresented via audit.py
- **Mix targeting types**: don't make every word SingleEnemy
- **Use specific stat buffs/debuffs**: match stat to word meaning (e.g. "fortify" → BuffPhysicalDefense)
- **Per-action targeting is encouraged**: Every action can target independently — Burn(Self), Drunk(Enemy), Shield(AllAllies) + Damage(AllEnemies), Heal(AllAllies) + Damage(Self), anything goes. When a word's meaning suggests different targets for different effects, USE per-action targeting. This creates the most interesting words. See the "Per-Action Targeting" section for patterns and examples. The word-level `target` is just for UI display
- **Duplicate actions**: a word CAN have the same action multiple times (e.g. "barrage" → Damage×2)
- **Structure words**: Defensive → Summon, Self, DEFENSIVE/SUPPORT. Offensive → Summon, Self, OFFENSIVE
- **Item words**: Wearable/holdable → Item action, Self, cost 0. Include `"item"` field.

### Interaction actions
These are for **event encounters** (non-combat: inns, shrines, chests). They do NOT deal damage. Value always 1, target usually SingleEnemy. See `context.py` output for the full list (Enter, Talk, Steal, Search, Pray, Rest, Open, Trade, Recruit, Leave).

**When to classify as interaction**: Words whose primary meaning is social/exploration (not combat). Do NOT give interaction actions to combat words.

---

## DIVERSITY RULES

Before each batch: run `context.py`, `stats.py`, `audit.py` — review distribution data.

- Don't assign the same action combination to more than ~5% of game words
- Mix targeting types — if >60% SingleEnemy, use more AllEnemies, Random, etc.
- **Use per-action targeting often** — at least 20% of multi-action words should have actions with different targets. Check `context.py` "Words with per-action targeting" stat and grow it
- Vary tag combinations — don't always pair OFFENSIVE+PHYSICAL
- Summon words: ~2-3% of game words. Vary passive triggers. At least 20% use parameterized triggers.
- Structures: 0-1 unit actions, 1-3 passives. Enemies: 1-2 unit actions, 0-1 passive.
- Words can (and should) have multiple tags for richer synergies.
- After 5000+ words: check for actions <1%, tags <3%, and boost them.
- Synonyms should vary their secondary actions, values, or targets — but having the same PRIMARY action is fine. Many words should deal Damage, many should Heal, etc.

### Quality checkpoints (before inserting)
- **Filler check**: Only remove words that are truly unplayable (scientific jargon). Weak-but-real words (ache, able, acid) keep their simple actions
- **Summon diversity**: No two summons in the batch with same passive trigger+effect
- **Item diversity**: No two items in the batch with same type+stat profile
- **Profile uniqueness**: Compare each game word against recent batches
- **Synergy audit**: At least 1 game word per batch creates a NEW synergy

---

## RULES

- Use the Python pipeline (batch_next → classify → batch_insert) — do NOT edit C# files
- Every game word MUST have at least one action and at least one tag
- Non-game words get empty actions and tags — just marked processed
- Values: 1-10, Cost: 0-20, Range: always 0
- Each iteration processes one batch (~100 words)
- Summon words MUST have cost >= 1
- Item words have cost 0
- Weapon-type items use `assoc_word` to link ammo
- **Run `context.py` at the start of each iteration** to get valid actions, targets, tags, triggers, effects, word families, and current DB distribution
