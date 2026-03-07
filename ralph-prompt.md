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
        "target": "AreaAll",
        "cost": 6,
        "range": 0,
        "area": "Diamond2",
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
        "range": 3,
        "area": "Single",
        "tags": ["SHADOW", "RESTORATION"],
        "actions": [
            {"action": "Damage", "value": 1, "target": "SingleEnemy", "range": 3, "area": "Single"},
            {"action": "Heal", "value": 1, "target": "Self", "range": 0, "area": "Single"}
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
| `Summon` | Summon a creature (Value = creature HP) |
| `Move` | Move source toward target (Value = steps) |
| `Slow` | Slow target (Value = duration) |
| `BuffStrength` | Buff Strength stat (Value = amount) |
| `BuffMagicPower` | Buff Magic Power stat (Value = amount) |
| `BuffPhysicalDefense` | Buff Physical Defense stat (Value = amount) |
| `BuffMagicDefense` | Buff Magic Defense stat (Value = amount) |
| `BuffLuck` | Buff Luck stat (Value = amount) |
| `BuffMovement` | Buff Movement Points stat (Value = amount) |
| `DebuffStrength` | Debuff Strength stat (Value = amount) |
| `DebuffMagicPower` | Debuff Magic Power stat (Value = amount) |
| `DebuffPhysicalDefense` | Debuff Physical Defense stat (Value = amount) |
| `DebuffMagicDefense` | Debuff Magic Defense stat (Value = amount) |
| `DebuffLuck` | Debuff Luck stat (Value = amount) |
| `DebuffMovement` | Debuff Movement Points stat (Value = amount) |
| `Heavy` | Heavy impact (Value = intensity) |
| `Wind` | Wind elemental (Value = intensity) |
| `Earth` | Earth elemental (Value = intensity) |
| `Dark` | Dark magic (Value = intensity) |
| `Light` | Light magic (Value = intensity) |
| `Curse` | Apply curse debuff (Value = duration) |
| `Poison` | Apply poison DoT (Value = duration) |
| `Shield` | Apply shield (Value = amount) |
| `Time` | Time manipulation (Value = intensity) |

---

## TARGETING

**Basic:** `Self`, `SingleEnemy`, `AreaEnemies`, `AreaAll`, `AllAllies`, `AllAlliesAndSelf`
**Positional:** `Melee` (adjacent only), `Area` (AoE around caster)
**Random:** `RandomEnemy`, `RandomAlly`, `RandomAny`
**Stat-based:** `LowestHealthEnemy`, `HighestHealthEnemy`, `LowestDefenseEnemy`, `HighestDefenseEnemy`, `LowestStrengthEnemy`, `HighestStrengthEnemy`, `LowestMagicEnemy`, `HighestMagicEnemy`
**Random+stat:** `RandomLowestHealthEnemy`, `RandomHighestHealthEnemy`
**Status:** `RandomEnemyWithStatus`, `RandomEnemyWithoutStatus`, `AllEnemiesWithStatus`, `AllEnemiesWithoutStatus`
**Specific status:** `AllBurningEnemies`, `AllWetEnemies`, `AllPoisonedEnemies`, `AllFrozenEnemies`, `AllStunnedEnemies`, `AllCursedEnemies`, `AllFearfulEnemies`
**Status+random:** `RandomBurningEnemy`, `RandomWetEnemy`, `RandomPoisonedEnemy`, `RandomFrozenEnemy`, `RandomStunnedEnemy`
**Status+stat:** `LowestHealthBurningEnemy`, `LowestHealthPoisonedEnemy`, `LowestHealthWetEnemy`
**Subset:** `HalfEnemiesRandom`, `TwoRandomEnemies`, `ThreeRandomEnemies`

---

## AREA SHAPES

| Shape | Tiles |
|-------|-------|
| `Single` | 1 tile — no expansion (default) |
| `Cross` | + shape — center + 4 cardinal neighbors |
| `Square3x3` | 3x3 grid centered on target |
| `Diamond2` | All tiles within Manhattan distance 2 |
| `Line3` | 3 tiles in line from caster through target |
| `VerticalLine` | Full vertical line in front of caster through target |

Area splash is **indiscriminate** — hits everyone in the shape regardless of faction.

---

## TAGS

`NATURE`, `ELEMENTAL`, `OFFENSIVE`, `RESTORATION`, `SHADOW`, `PHYSICAL`, `DEFENSIVE`, `ARCANE`, `HOLY`, `SUPPORT`, `PSYCHIC`

---

## CLASSIFICATION GUIDELINES

- **Word meaning drives effects**: "avalanche" → Damage + Push + Water; "whisper" → Fear; "fortress" → Heal + Shield (Self)
- **Longer/rarer words = stronger**: 3-4 letter words are weak (1-2 value), 7+ letter words are powerful (4-6 value)
- **Cost scales with power**: powerful words cost more energy
- **Range makes sense thematically**: melee words (slash, punch) Range 1-2; ranged words (arrow, bolt) Range 3-5; magic words Range 0 (unlimited)
- **Area matches word meaning**: "earthquake" → Diamond2; "beam" → Line3; "blast" → Cross; "rain" → VerticalLine
- **Most NLTK words are NOT game words** — articles, prepositions, obscure terms → empty actions array, they still get marked as processed
- **~15-20% of words should have actions** — be selective, only words with clear combat/RPG meaning
- **Mix targeting types**: don't make every word SingleEnemy
- **Use specific stat buffs/debuffs**: use `BuffStrength`, `DebuffPhysicalDefense`, etc. instead of generic `Buff`. Match the stat to the word's meaning (e.g. "fortify" → `BuffPhysicalDefense`, "weaken" → `DebuffStrength`)
- **Per-action targeting**: if a word's actions target different things, add `target`/`range`/`area` to individual actions (e.g. "absorb" damages enemy + heals self)
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
