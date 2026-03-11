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
| `Damage` | Deal physical direct damage (Value = amount). Use for physical attacks (punch, slash, crush). Do NOT use for magical/elemental attacks — use `MagicDamage` instead. |
| `MagicDamage` | Deal magic damage scaled by MagicPower vs MagicDefense (Value = amount). Use for ALL magical/elemental damage: fire spells, water spells, shadow magic, holy attacks, etc. Any word that deals damage through magical means should use `MagicDamage` instead of `Damage`. Always pair with `SPELL` tag. |
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
| `Drunk` | Apply Drunk status — scrambles keyboard letters. Value = stacks (5 letters per stack). Applied to SELF. Stacks decrement each turn. |
| `Shield` | Apply shield (Value = amount) |
| `Item` | Equippable item — goes to player inventory. Value = durability (0 = infinite). Requires an `"item"` field with type, stats, and optional passives. Weapon-type items use `assoc_word` to link ammo words. Consumable-type items auto-equip and have durability = uses. |
| `Melt` | Melts or softens materials through intense heat — differs from Burn (combustion of flammable materials). Melt works on metal, ice, wax. In combat, debuffs physical defense (Value = amount). |
| `Smash` | Powerful physical impact that deals heavy damage and can break objects (Value = damage amount). Works on breakable targets in events. |
| `Pay` | Spend currency/valuables — used for bribery, trading, recruitment. Target is Self (payment comes from the payer). Value = gold amount paid. Used with "give" prefix to target NPCs (e.g. "give money" pays an NPC). |
| `Time` | Time manipulation (Value = intensity) |

### INTERACTION ACTIONS

These actions are used for **event encounters** (non-combat: inns, shrines, chests, NPCs). They do NOT deal damage or apply status effects. The game's reaction system handles outcomes (heal, reward, transition, etc.) based on what the player targets. Value is always 1 for interaction actions. Target is usually `SingleEnemy` (the interactable entity).

| ActionId | Usage | Example words |
|----------|-------|---------------|
| `Enter` | Enter/go to a place | enter, go, visit, arrive, venture |
| `Talk` | Speak to an NPC | talk, speak, greet, converse, chat |
| `Steal` | Attempt to steal from target | steal, swipe, pilfer, pickpocket, filch |
| `Search` | Search/examine something | search, examine, inspect, investigate, scrutinize |
| `Pray` | Pray at a shrine or altar | pray, worship, beseech, kneel, invoke |
| `Rest` | Rest or sleep (target Self) | rest, sleep, nap, snooze, doze |
| `Open` | Open a container/door | open, unlock, unseal, unbar, crack |
| `Trade` | Trade with a merchant | trade, barter, haggle, deal, exchange |
| `Recruit` | Recruit/hire an ally | recruit, hire, enlist, summon, rally |
| `Leave` | Leave/exit current area | leave, exit, depart, withdraw, flee |

**When to classify as interaction action:**
- Words whose primary meaning is a social/exploration action (not combat)
- Words that imply interacting with objects or people non-violently
- Synonyms of the above action types
- **Do NOT give interaction actions to combat words** — "attack", "slash", "fireball" remain combat actions

**Interaction action format:**
```json
{"action": "Enter", "value": 1}
```

---

## UNIT PASSIVES (for Summon words)

Units summoned via `Summon` action can have composable passives. When classifying a summon word, include a `"unit"` field with stats, abilities, and passives. The `unit_id` is the word itself (lowercase).

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
- `actions`: array of word strings the unit can use in combat (e.g. `["scratch", "charge"]`). These must be words that exist in the DB with their own `word_actions` entries
- `passives`: array of passive objects (trigger + effect + target)
- `color`: `[r, g, b]` floats 0.0-1.0 (display color)
- Stats: `max_health` 5-100, others 0-15, `starting_shield` 0-20
- `taunt` passive: `{"trigger": "taunt"}` — no effect/target/value needed (marker only, forces enemy targeting)

### Available triggers

| Trigger | trigger_param | When it fires |
|---------|--------------|---------------|
| `on_ally_hit` | — | When any ally takes damage |
| `on_self_hit` | — | When this unit takes damage |
| `on_round_end` | — | At end of each combat round |
| `on_round_start` | — | At start of each combat round |
| `on_turn_start` | — | At start of each turn |
| `on_turn_end` | — | At end of each turn |
| `on_word_played` | — | When any word is played |
| `on_word_length` | min length (e.g. `"6"`) | When a played word has >= N letters |
| `on_word_tag` | tag (e.g. `"NATURE"`) | When a played word has the specified tag |
| `on_kill` | — | When any enemy is killed |
| `taunt` | — | Marker: forces enemies to target this unit (no effect/target needed) |

### Available effects

| Effect | effect_param | What it does |
|--------|-------------|--------------|
| `heal` | — | Heal targets by `value` HP |
| `damage` | — | Deal `value` damage to targets |
| `shield` | — | Grant `value` shield to targets |
| `mana` | — | Restore `value` mana to targets |
| `apply_status` | status name (e.g. `"Burning"`) | Apply status effect to targets for `value` duration |

Available statuses for `apply_status`: `Burning`, `Wet`, `Poisoned`, `Frozen`, `Slowed`, `Cursed`, `Stun`, `Concussion`, `Fear`, `Bleeding`, `Concentrated`, `Growing`, `Thorns`, `Reflecting`, `Hardening`

### Available passive targets

| Target | Resolves to |
|--------|------------|
| `Self` | The unit itself |
| `AllAllies` | All friendly units (including player) |
| `AllEnemies` | All enemy units |
| `Injured` | The ally that was damaged (for hit triggers) |
| `Attacker` | The entity that dealt damage (for hit triggers) |

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
| Shield givers | structure | on_ally_hit | shield | Injured | sentinel, aegis, ward |
| Taunt tanks | structure | taunt | — | — | sentinel, guardian, decoy |
| Kill-reward | enemy | on_kill | heal | Self | predator, hunter, reaper |

---

## EQUIPPABLE ITEMS (for Item words)

Words with the `Item` action become equippable items. When classifying an item word, include an `"item"` field with type, stats, and optional passives. The `item_id` is the word itself (lowercase).

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

**Item tags**: Optional `"tags"` array on the `"item"` field. Used for inventory-gated mechanics like "give" prefix. Words with SILVER/VALUABLE word_tags require a matching tagged item in inventory when used with "give" — the item is consumed. Words with a Pay action (e.g. "give money", "give gold") deduct from the player's gold resource instead. Example: `"tags": ["SILVER"]` on a silver bar item means it can be consumed when the player types "give silver".

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

Word "beer" — item_type consumable, durability 3 (3 uses), ammo word "sip":
```json
{
    "word": "beer",
    "target": "Self",
    "cost": 0,
    "range": 0,
    "area": "Single",
    "tags": ["SUPPORT", "HEALING"],
    "actions": [
        {"action": "Item", "value": 3, "assoc_word": "sip"}
    ],
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

Word "sip" (ammo for beer) — heals 10 HP and applies 2 stacks of Drunk:
```json
{
    "word": "sip",
    "target": "Self",
    "cost": 0,
    "range": 0,
    "area": "Single",
    "tags": ["SUPPORT"],
    "actions": [
        {"action": "Heal", "value": 10},
        {"action": "Drunk", "value": 2}
    ]
}
```

### Item types and equipment slots

| item_type | Slot | Behavior |
|-----------|------|----------|
| `head` | HEAD | Passive stats only |
| `wear` | WEAR | Passive stats only |
| `accessory` | ACCESSORY | Stats + optional passives |
| `consumable` | CONSUMABLE | Auto-equips. Durability = uses. Ammo via `assoc_word` triggers any action (Heal, Drunk, etc.). Destroyed when uses run out. |
| `weapon` | WEAPON | Stats + ammo via `assoc_word`, durability |

### Item design archetypes

| Word Theme | item_type | Stats Focus | Passives | Examples |
|---|---|---|---|---|
| Crowns/Helms | head | magic_power, luck, phys_defense | on_self_hit: shield | crown, helm, tiara, hood |
| Armor/Cloaks | wear | phys_defense, magic_defense | — | cloak, robe, armor, vest |
| Rings/Bands | accessory | strength, luck | — | ring, band, bracelet, charm |
| Amulets/Pendants | accessory | magic_power | on_word_played: mana | amulet, pendant, talisman, locket |
| Potions/Drinks | consumable | — | — | beer, potion, elixir, wine, ale, mead |
| Food/Herbs | consumable | — | — | bread, apple, mushroom, herb, berry |
| Swords/Axes | weapon | strength | — | sword, axe, spear, dagger |
| Staves/Wands | weapon | magic_power | on_word_tag: damage | staff, wand, scepter, rod |
| Guns/Bows | weapon | — | — | gun, bow, crossbow, cannon |

### Item rules
- Item stats: 0-5 per stat, total stat budget roughly 2-5 depending on item rarity
- Item cost is always 0 (equipping is free — item goes to inventory first)
- Weapon durability: 3-10 (0 = infinite, avoid infinite for weapons)
- Non-weapon durability: 0 (equipment doesn't break)
- Item passives use the same trigger/effect/target system as unit passives
- Items should be ~2-3% of game words
- Item word target is always `Self`

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

## DESIGN AWARENESS

When classifying words, consider the FULL ecosystem of word relationships:
- **Actions**: direct combat effects (Damage, Heal, Burn, etc.)
- **Summons**: spawning units with their own abilities and composable passives
- **Items**: equippable gear that grants stats and/or passives (5 slots: head, wear, accessory, consumable, weapon)
- **Tags**: categories for passive triggers and thematic grouping

Each word should contribute to ONE of these roles. Don't make every word a simple damage dealer.

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
- **Item/equipment words**: Nouns that are wearable/holdable gear → `Item` action, target `Self`, cost `0`. Include `"item"` field with `item_type`, stats, and optional passives. Weapons have ammo via `assoc_word`. Consumables have ammo via `assoc_word` (ammo words trigger Heal, Drunk, buffs, etc.) and durability = number of uses. Examples: "crown" → head, "cloak" → wear, "ring" → accessory, "beer" → consumable (ammo: "sip" with Heal+Drunk actions), "sword" → weapon.
- **Balance tags**: ensure good coverage across all categories

---

## DIVERSITY RULES

Before each batch: run `stats.py` and review action distribution.

- Don't assign the same action combination to more than ~5% of game words
- Mix targeting types — if recent batches are >60% SingleEnemy, use more AllEnemies, RandomEnemy, etc.
- Vary tag combinations — don't always pair OFFENSIVE+PHYSICAL
- Summon words should be ~2-3% of game words
- **Summon unit diversity**: vary passive triggers — don't make every structure `on_ally_hit`
- At least 20% of summons should use parameterized triggers (`on_word_length`, `on_word_tag`, `apply_status`)
- Structures: 0-1 unit actions, 1-3 passives. Enemies: 1-2 unit actions, 0-1 passive.
- Words can (and should) have multiple tags — e.g. "grove" → `["NATURE", "RESTORATION"]`. Tags enable `on_word_tag` passives to fire, so diverse tagging creates richer synergies.

### After 5000+ words
Run `stats.py` and check for:
- Actions used by <1% of words → create more words using those actions
- Tags with <3% coverage → assign those tags to appropriate words
- Summon words all with same unit type → create more diverse unit types
- If any stat buff/debuff has <2% usage, intentionally add words using it

### Similarity prevention
- Don't create words with identical action+value+target as existing words
- If a word has the same meaning as an existing word (synonym), vary the action values or add different secondary effects
- Check recent 3 batches for patterns — if you've been assigning too much Damage, shift to other effects

---

## RULES

- Use the Python pipeline (batch_next → classify → batch_insert) — do NOT edit C# files
- Every game word MUST have at least one action mapping and at least one tag
- Non-game words get empty actions and tags — they're just marked processed
- Values must be 1-10 (enforced by DB constraint)
- Cost must be 0-10
- Range 0 = unlimited
- Each iteration processes one batch (~150 words)
- Summon words MUST have cost >= 1 (enforced by batch_insert.py). Summoning always costs mana.
- Item words (equipping) have cost 0 — equipping an item is free. Use `Item` action (not `Weapon`).
- Weapon-type items use `assoc_word` on the action to link ammo words.
