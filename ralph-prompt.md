# Word Dictionary — Ralph Loop Prompt

Classify NLTK dictionary words into the game's action system via SQLite database. Each iteration: get a batch of unclassified words, classify them, insert into the DB.

---

## PIPELINE TOOLS

| Script | Purpose |
|--------|---------|
| `Tools/WordAction/setup.py` | One-time: install NLTK, create tracking tables |
| `Tools/WordAction/seed_db.py` | One-time: create DB with ~50 seed words |
| `Tools/WordAction/batch_next.py --count N` | Get next N unclassified words as JSON |
| `Tools/WordAction/batch_insert.py` | Read classified JSON from stdin, insert into DB |
| `Tools/WordAction/stats.py` | Show progress (total/processed/remaining) |

---

## EACH ITERATION

### Step 1: Get next batch
```bash
python Tools/WordAction/batch_next.py --count 150
```
If output is `COMPLETE`, all words are done — stop.

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

**Per-action targeting**: Actions can optionally include `"target"`, `"range"`, and `"area"` fields to override the word-level defaults. Use this when a word's actions target different things (e.g. "absorb" damages an enemy but heals self). When omitted, the action inherits from the word-level `target`/`range`/`area`.

### Step 3: Insert into DB
```bash
echo '<JSON>' | python Tools/WordAction/batch_insert.py
```

### Step 4: Check progress
```bash
python Tools/WordAction/stats.py
```

---

## AVAILABLE ACTIONS

| ActionId | Effect |
|----------|--------|
| `Damage` | Deal direct damage (Value = amount) |
| `Heal` | Restore health (Value = amount) |
| `Burn` | Apply Burning DoT (Value = duration) |
| `Water` | Apply Wet status (Value = duration) |
| `Fire` | Fire elemental (Value = intensity) |
| `Push` | Push targets away (Value = tiles) |
| `Shock` | Lightning damage + bonus to Wet targets |
| `Fear` | Apply Fear debuff (Value = duration) |
| `Stun` | Apply Stun (Value = duration) |
| `Freeze` | Apply Frozen — immune but can't act (Value = duration) |
| `Concussion` | Apply Concussion stacking debuff (Value = stacks) |
| `Concentrate` | Restore mana + apply Concentrated buff (Value = mana amount) |
| `Bleed` | Apply Bleeding DoT — grows if untreated, heals reduce (Value = ignored, uses 999 duration) |
| `Summon` | Summon a unit (Value = creature level). If the word matches a unit in the DB, uses that unit's stats and passives. Structure-type units (high HP, no/few attacks, defensive passives) use words like "fortress", "wall", "totem", "barricade". Offensive structures like "turret" have attack abilities + offensive passives (e.g. retaliate damage when allies are hit). |
| `Slow` | Slow target (Value = duration) |
| `BuffStrength` | Buff Strength stat (Value = amount) |
| `BuffMagicPower` | Buff Magic Power stat (Value = amount) |
| `BuffPhysicalDefense` | Buff Physical Defense stat (Value = amount) |
| `BuffMagicDefense` | Buff Magic Defense stat (Value = amount) |
| `BuffLuck` | Buff Luck stat (Value = amount) |
| `DebuffStrength` | Debuff Strength stat (Value = amount) |
| `DebuffMagicPower` | Debuff Magic Power stat (Value = amount) |
| `DebuffPhysicalDefense` | Debuff Physical Defense stat (Value = amount) |
| `DebuffMagicDefense` | Debuff Magic Defense stat (Value = amount) |
| `DebuffLuck` | Debuff Luck stat (Value = amount) |
| `Heavy` | Heavy impact (Value = intensity) |
| `Wind` | Wind elemental (Value = intensity) |
| `Earth` | Earth elemental (Value = intensity) |
| `Dark` | Dark magic (Value = intensity) |
| `Light` | Light magic (Value = intensity) |
| `Curse` | Apply curse debuff (Value = duration) |
| `Poison` | Apply poison DoT (Value = duration) |
| `Grow` | Apply Growing regen — heals per tick, bonus when Wet (Value = duration) |
| `Thorns` | Apply Thorns — retaliates damage back to attackers (Value = duration) |
| `Reflect` | Apply Reflecting — redirects single-target abilities back to caster (Value = stacks) |
| `Hardening` | Apply Hardening — flat damage reduction that decays each turn (Value = stacks) |
| `Shield` | Apply shield (Value = amount) |
| `Time` | Time manipulation (Value = intensity) |

---

## TARGETING

**Basic:** `Self`, `SingleEnemy`, `AllEnemies` (alias: `AreaEnemies`), `All` (alias: `AreaAll`), `AllAllies`, `AllAlliesAndSelf`
**Positional:** `FrontEnemy` (slot 0, or nearest), `MiddleEnemy` (slot 1, or nearest), `BackEnemy` (slot 2, or nearest), `Melee` (alias for FrontEnemy), `Area` (alias for AllEnemies)
**Random:** `RandomEnemy`, `RandomAlly`, `RandomAny`
**Stat-based:** `LowestHealthEnemy`, `HighestHealthEnemy`, `LowestDefenseEnemy`, `HighestDefenseEnemy`, `LowestStrengthEnemy`, `HighestStrengthEnemy`, `LowestMagicEnemy`, `HighestMagicEnemy`
**Random+stat:** `RandomLowestHealthEnemy`, `RandomHighestHealthEnemy`
**Status (any):** `RandomEnemyWithStatus`, `RandomEnemyWithoutStatus`, `AllEnemiesWithStatus`, `AllEnemiesWithoutStatus`
**Subset:** `HalfEnemiesRandom`, `TwoRandomEnemies`, `ThreeRandomEnemies`

### Composite status targeting (preferred)

Use `BaseType+StatusEffect` format to target enemies with a specific status. Any base target type can be combined with any status effect:

- `AllEnemies+Burning` — all burning enemies
- `RandomEnemy+Wet` — random wet enemy
- `LowestHealthEnemy+Poisoned` — lowest-HP poisoned enemy
- `SingleEnemy+Bleeding` — single bleeding enemy

Available status effects: `Burning`, `Wet`, `Poisoned`, `Frozen`, `Slowed`, `Cursed`, `Buffed`, `Shielded`, `Stun`, `Concussion`, `Fear`, `Bleeding`, `Concentrated`, `Growing`, `Thorns`, `Reflecting`, `Hardening`

---

## AREA SHAPES

**Note:** The combat system uses a slot-based layout (no 2D grid), so area shapes are ignored at runtime. Always use `"area": "Single"` (default). Targeting is fully determined by the `target` field.

---

## TAGS

`NATURE`, `ELEMENTAL`, `OFFENSIVE`, `RESTORATION`, `SHADOW`, `PHYSICAL`, `DEFENSIVE`, `ARCANE`, `HOLY`, `SUPPORT`, `PSYCHIC`

---

## CLASSIFICATION GUIDELINES

- **Word meaning drives effects**: "avalanche" → Damage + Push + Water; "whisper" → Fear; "fortress" → Heal + Shield (Self)
- **Longer/rarer words = stronger**: 3-4 letter words are weak (1-2 value), 7+ letter words are powerful (4-6 value)
- **Cost scales with power**: powerful words cost more energy
- **Range is ignored** (slot system — everything is always in range). Use `0` for all words.
- **Area is ignored** (slot system — no area expansion). Use `"Single"` for all words. Targeting is handled by the `target` field (e.g. `AllEnemies` for AoE).
- **Most NLTK words are NOT game words** — articles, prepositions, obscure terms → empty actions array, they still get marked as processed
- **~15-20% of words should have actions** — be selective, only words with clear combat/RPG meaning
- **Mix targeting types**: don't make every word SingleEnemy
- **Use specific stat buffs/debuffs**: use `BuffStrength`, `DebuffPhysicalDefense`, etc. instead of generic `Buff`. Match the stat to the word's meaning (e.g. "fortify" → `BuffPhysicalDefense`, "weaken" → `DebuffStrength`)
- **Per-action targeting**: if a word's actions target different things, add `target`/`range`/`area` to individual actions (e.g. "absorb" damages enemy + heals self)
- **Duplicate actions**: a word CAN have the same action multiple times (e.g. "barrage" → Damage×2). Each instance executes separately. Example:
  ```json
  {"word": "barrage", "actions": [{"action": "Damage", "value": 2}, {"action": "Damage", "value": 2}]}
  ```
- **Structure words**: Defensive/building words ("fortress", "barricade", "wall", "totem", "bunker") → `Summon` action, target `Self`, tags `DEFENSIVE`/`SUPPORT`. Offensive structure words ("turret", "cannon", "sentry") → `Summon` action, target `Self`, tags `OFFENSIVE`/`PHYSICAL`. These are units with high HP, passives, and few/no active attacks — the passive system handles their effects automatically.
- **Balance tags**: ensure good coverage across all categories

---

## RULES

- Use the Python pipeline (batch_next → classify → batch_insert) — do NOT edit C# files
- Every game word MUST have at least one action mapping and at least one tag
- Non-game words get empty actions and tags — they're just marked processed
- Values must be 1-10 (enforced by DB constraint)
- Cost must be 0-10
- Range 0 = unlimited
- Each iteration processes one batch (~150 words)
