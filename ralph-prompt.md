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
python Tools/WordAction/batch_next.py --count 100
```
If output is `COMPLETE`, all words are done — stop.

### Step 1.5: Analyze current state
```bash
python Tools/WordAction/stats.py
python Tools/WordAction/audit.py
```
Review the output before classifying. Understand:
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
| `Push` | Push targets away (Value = tiles) |
| `Shock` | Lightning damage + bonus to Wet targets |
| `Fear` | Apply Fear debuff (Value = duration) |
| `Stun` | Apply Stun (Value = duration) |
| `Freeze` | Apply Frostbitten — attacks last, cumulative MagicPower loss per tick (Value = stacks). Removes Burning on apply (applies Wet). |
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
| `Energize` | Apply Energetic to self — next word's actions fire 2x at half value (rounded up), then applies Tired(1). Value = duration. |
| `Relax` | Noop action — tag-driven. RELAX-tagged words remove 1 Anxiety stack from source. Value ignored. |
| `Sleep` | Apply Sleep to self — skip turns, wake on damage (resist chance = 20% per stack). Value = stacks. |
| `RestHeal` | Heal self. Base heal from value, +5 if no enemies, +10 if DWELLING entity present. Scales with MagicPower/3. |
| `Scramble` | Swap target's slot position with another unit in the same row. Disrupts enemy formations. Value is ignored (use 1). Target should be an enemy — the handler picks the swap partner automatically. Great on AoE words (tsunami, earthquake) to represent chaotic displacement. |
| `Time` | Time manipulation (Value = intensity) |
| `Peck` | Physical damage (Str vs PDef) + apply Bleeding in one action. Value = base damage. Use for sharp, piercing animal attacks (beak, claw, fang). Reusable by bird/beast creature words. Always pair with `BEAST` or `MELEE` tag. |
| `Screech` | Apply Fear + Concussion to targets in one action. Value = Fear duration (Concussion is always 1 stack). A terrifying shriek that disorients. Use for loud, disruptive creature abilities. Always pair with `BEAST` tag. |
| `Purify` | Remove up to Value negative statuses from targets (prioritizes Stun > Fear > Sleep > Frostbitten > Burning > Poisoned > Bleeding > others). Use for cleansing, curing, washing away debuffs. Target should be Self or AllAlliesAndSelf. Always pair with `CLEANSING` or `HOLY` tag. |
| `Awaken` | Remove Sleep, Stun, and Frostbitten from targets, then apply Awakened status (+1 all stats for 1 turn). Use for rousing, reviving, energizing allies from crowd control. Value is ignored. Always pair with `LIGHT` or `HOLY` tag. |
| `Siphon` | Steal a random stat (Str/Mgc/PDef/MDef/Luck) from target and give it to caster. Value = amount stolen per stat. Use for draining, leeching, stealing power. Pair with `DEBUFF`, `STEALTH`, or `PSYCHIC` tag. |
| `Deceive` | Apply Fear (duration = value) + Concussion (permanent) to target. Use for tricking, confusing, misleading enemies. Pair with `PSYCHIC`, `SHADOW`, or `DEBUFF` tag. |
| `Recuperate` | Heal target (MagicPower-scaled) AND remove 1 random negative status. Combines healing + cleansing. Use for recovery, convalescence, restoring. Target should be AllAlliesAndSelf or Self. Pair with `RESTORATION`, `CLEANSING`, or `SUPPORT` tag. |
| `Comfort` | Apply Energetic status to target (not self). Grants extra turn next round. Use for encouraging, reassuring, inspiring allies. Pair with `SUPPORT` or `SOCIAL` tag. |
| `Overcharge` | Buff self MagicPower by Value + apply Energetic status. A power-up combo: use before Shock/MagicDamage for amplified hits. Value = buff amount AND Energetic duration. Pair with `ELEMENTAL`, `LIGHTNING`, or `SPELL` tag. |
| `Cannonade` | Multi-hit attack — fires Value shots at random enemies, each dealing 1 base damage (Str-scaled). Can hit same enemy multiple times. Use for artillery, volleys, barrages, bombardment. Pair with `NAVAL`, `OFFENSIVE` tag. |
| `Plunder` | Physical damage (Str vs PDef) + steal 1 random stat from target. Combines attack with theft. Use for pirate/raider/bandit words. Pair with `OFFENSIVE`, `SHADOW`, or `MELEE` tag. |
| `Attune` | Save the word's letters as "attuned" charges for the rest of the encounter. Future words that contain attuned letters get +20% power per attuned letter consumed (one-time per letter). No damage/status — pure utility. Value is ignored. Target should be `Self`. Pair with `ARCANE`, `SPELL`, or `SUPPORT` tag. Best on long words with common letters (e, t, a, n, s) to maximize future value. |
| `Ignite` | Magic damage (MagicPower vs MagicDefense) + apply Burning (duration = Value). Fire version of Peck. Use for fire/heat attack words. Pair with FIRE or ELEMENTAL tag. |
| `Combust` | Detonate Burning on target: if Burning, remove it and deal bonus MagicDamage (value + remaining stacks, MagicPower vs MagicDefense). If not Burning, deal base MagicDamage only. Pair with FIRE or ELEMENTAL tag. |
| `Cataclysm` | Massive magic damage (MagicPower vs MagicDefense) that hits ALL entities — enemies AND allies. High risk, high reward AoE nuke. Target should be `All`. Pair with `COSMIC`, `DESTRUCTION`, `FIRE`, or `ARCANE` tag. |
| `Cleave` | Physical damage (Str vs PDef) to primary target + half damage to one other random enemy. A sweeping melee strike with splash damage. Use for bladed/slashing weapon words. Pair with `BLADE`, `MELEE`, or `PHYSICAL` tag. |
| `Lockpick` | Attempt to pick a lock on a lockpickable target. Triggers a multi-step sequence with progress messages, success chance based on Dexterity + Luck. On success, opens the target (fires Open reaction chain). High mana cost (~8) when typed as a word. Use for lock-related, thief, and tool words. Pair with `TOOL` or `STEALTH` tag. |

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
| `on_death` | optional (`"siphon"`) | When this unit dies. If triggerParam=`"siphon"`, accumulated siphon total is used as heal value |
| `on_damage_dealt` | — | When this unit deals damage to any target |
| `on_letter_in_word` | `vowel`, `consonant`, `any`, `fixed:X` | When typed word contains a randomly selected letter (re-selected each turn) |
| `taunt` | — | Marker: forces enemies to target this unit (no effect/target needed) |

### Available effects

| Effect | effect_param | What it does |
|--------|-------------|--------------|
| `heal` | — | Heal targets by `value` HP |
| `damage` | — | Deal `value` damage to targets |
| `shield` | — | Grant `value` shield to targets |
| `mana` | — | Restore `value` mana to targets |
| `apply_status` | status name (e.g. `"Burning"`) | Apply status effect to targets for `value` duration |
| `steal_stat` | — | Steal `value` points of a random stat (Str/Mgc/PDef/MDef/Luck) from targets and give to owner |
| `gold` | — | Add `value` gold to the player's gold resource |
| `buff_stat` | stat name (e.g. `"Luck"`, `"Strength"`) | Permanently buff target's stat by `value` for the rest of combat (cumulative) |

Available statuses for `apply_status`: `Burning`, `Wet`, `Poisoned`, `Frozen`, `Slowed`, `Cursed`, `Stun`, `Concussion`, `Fear`, `Bleeding`, `Concentrated`, `Growing`, `Thorns`, `Reflecting`, `Hardening`, `Frostbitten`, `Energetic`, `Tired`, `Sleep`, `Anxiety`, `Awakened`

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
| Death release | structure | on_death (siphon) | heal | AllAllies | twinflower — siphons stats, heals allies on death |
| Shield givers | structure | on_ally_hit | shield | Injured | sentinel, aegis, ward |
| Taunt tanks | structure | taunt | — | — | sentinel, guardian, decoy |
| Kill-reward | enemy | on_kill | heal | Self | predator, hunter, reaper |

### Enemy mana balance (for units with abilities)

Enemy units have a fixed mana pool: **MaxMana=10, StartingMana=5, ManaRegen=2/turn**. Abilities should be designed so mana is a meaningful resource — enemies lead with their signature ability then build up mana before using it again.

| Cost | Turn 1 (5 mana) | Turn 2 (+2 regen) | Turn 3 (+2 regen) | Turn 4 (+2 regen) | Frequency |
|------|-----------------|-------------------|-------------------|-------------------|-----------|
| **5** | Use (5→0) | scratch (2) | scratch (4) | Use (6→1) | Every 3 turns |
| **4** | Use (5→1) | scratch (3) | Use (5→1) | scratch (3) | Every 2 turns |
| **3** | Use (5→2) | Use (4→1) | Use (3→0) | scratch (2) | ~2 of 3 turns |
| **2** | Use (5→3) | Use (5→3) | Use (5→3) | Use (5→3) | Every turn |

**Design rules:**
- **Signature ability** (cost 4-5): The enemy's defining move. Strong effect (AoE fear, summon, multi-action combo). Used turn 1, then every 2-3 turns.
- **Mid-tier filler** (cost 2-3): Secondary ability for enemies with two real moves (e.g. shaman's spark, wraith's hex). Used between signature cooldowns.
- **Free fallback** (cost 0): Basic attacks like "mace" or "slam" — better than scratch but not special. Only for brutes/beasts whose identity is "hit things".
- **Scratch** (cost 0, 1 dmg): Universal last resort. AI strongly avoids it — only used when completely out of mana.
- **Never cost 1**: Cost 1 with 2 regen means the enemy uses it every turn with no tradeoff. Minimum meaningful cost is 2.

**Enemy design patterns:**

| Pattern | Ability 1 | Ability 2 | Example | Behavior |
|---------|-----------|-----------|---------|----------|
| Signature + scratch | cost 4-5 | scratch (0) | bat, goblin, skeleton | Big move turn 1, scratch until recharged |
| Signature + free attack | cost 4-5 | basic attack (0) | orc, golem, predator | Big move turn 1, basic attack until recharged |
| Signature + filler | cost 5 | cost 2 | shaman, wraith | Signature turn 1, filler to stay active, signature again when full |
| All-in | cost 5 | scratch (0) | sellsword | One devastating combo, then weak until recharged |

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

Available status effects: `Burning`, `Wet`, `Poisoned`, `Frozen`, `Slowed`, `Cursed`, `Buffed`, `Shielded`, `Stun`, `Concussion`, `Fear`, `Bleeding`, `Concentrated`, `Growing`, `Thorns`, `Reflecting`, `Hardening`, `Frostbitten`, `Energetic`, `Tired`, `Sleep`, `Anxiety`, `Awakened`

---

## AREA SHAPES

**Note:** The combat system uses a slot-based layout (no 2D grid), so area shapes are ignored at runtime. Always use `"area": "Single"` (default). Targeting is fully determined by the `target` field.

---

## TAGS

`NATURE`, `ELEMENTAL`, `OFFENSIVE`, `RESTORATION`, `SHADOW`, `PHYSICAL`, `DEFENSIVE`, `ARCANE`, `HOLY`, `SUPPORT`, `PSYCHIC`, `THOUGHTS`, `RELAX`, `DWELLING`, `MELEE`, `SOCIAL`, `BEAST`, `FLYING`, `LIGHT`, `CLEANSING`, `DEBUFF`, `STEALTH`, `LIGHTNING`, `WEATHER`, `BOTANICAL`, `DRAIN`, `UNDEAD`, `NAVAL`, `FIRE`

- `MELEE`: Close-combat physical attacks (shove, thrust, hit, strike, smash, crush, charge, slam, pounce, etc.). Triggers Warrior's "Brute Force" passive (+50% damage).
- `SOCIAL`: Social interaction words (talk, speak, greet, trade, barter, charm, flatter, persuade). Triggers Merchant's "Charming Presence" passive (grants Shield).
- `BEAST`: Animals, creatures, and monsters (wolf, hawk, serpent, bear, spider, raven, bat, etc.). Use for creature-themed attack words.
- `FLYING`: Airborne creatures and wind-related effects (hawk, eagle, bat, owl, raven, fairy, moth, etc.).
- `LIGHT`: Light, radiance, dawn, sun. Distinct from HOLY (not religious). Use for: glow, shine, flash, beam, sunrise, aurora, luminous, bright.
- `CLEANSING`: Purification, removal, washing away. Use for: purify, wash, rinse, cleanse, baptize, scrub, disinfect, cure.
- `DEBUFF`: Weakening, draining, sabotaging. Use for: weaken, corrode, rust, wither, decay, sap, drain, curse, hex, blight, cripple, enfeeble, diminish, erode.
- `STEALTH`: Sneaky, covert, hidden. Use for: spy, assassin, thief, rogue, ninja, ghost, phantom, lurk, skulk, ambush, sneak, prowl, vanish.
- `LIGHTNING`: Electrical, shock, voltage. Use for: lightning, thunder, bolt, spark, shock, jolt, zap, surge, voltage, static, electrify, galvanize.
- `WEATHER`: Atmospheric phenomena, storms, climate. Use for: storm, rain, hail, blizzard, tornado, cyclone, gale, fog, thunder, hurricane, monsoon, tempest.
- `BOTANICAL`: Plants, flowers, herbs, vegetation. Use for: rose, vine, bloom, sprout, petal, moss, fern, orchid, lily, tulip, twinflower, blossom, seedling, root, ivy.
- `DRAIN`: Life-draining, parasitic, energy-sapping. Use for: leech, drain, absorb, siphon, sap, parasite, devour, consume, extract, deplete, wither, vampire.
- `FIRE`: Fire, flame, heat, combustion. Use for: fire, flame, blaze, inferno, scorch, ember, torch, pyre, ash, cinder, combustion, magma, lava, volcano, kindle, ignite, combust.

---

## WORD FAMILIES — Pre-Classified Action Hints

**IMPORTANT**: Before classifying a word, check this table first. If the word appears here, it has already been pre-approved for the listed actions/tags. Use these as your primary classification and add supplementary actions only if they fit naturally. Some of these words are already in the DB with `status='draft'` — when you encounter them, refine their classification (your INSERT OR REPLACE will overwrite the draft with a finalized entry).

| Action | Pre-classified words (use this action when you encounter them) |
|--------|--------------------------------------------------------------|
| `Peck` | peck, pecks, nip, nips, nibble, nibbles, jab, jabs, bite, bites, claw, claws, talon, talons, fang, fangs, gouge, gouges, stab (bird/beast variant) |
| `Screech` | screech, shriek, shrikes, howl, howls, wail, wails, roar, roars, bellow, bellows, screams, cry, cries (creature variant) |
| `Purify` | purify, cleanse, cure, remedy, antidote, baptize, absolve, wash, rinse, disinfect, sanitize, sterilize, detox, detoxify, exorcise, dispel |
| `Awaken` | awaken, rouse, wake, stir, revive, arouse, invigorate, resuscitate, animate, enliven, vivify, rally, rejuvenate |
| `Siphon` | siphon, drain, leech, absorb, steal, pilfer, extract, devour, vampiric, parasitic, latch, suck, tap |
| `Deceive` | deceive, trick, mislead, confuse, bewilder, bamboozle, dupe, hoodwink, bluff, feint, distract, misdirect, beguile |
| `Summon` (raven unit) | raven, ravens, crow, crows, jackdaw, magpie, corvid |
| `Summon` (treasonist unit) | treasonist, treasonists, spy, spies, traitor, traitors, saboteur, saboteurs, infiltrator, mole |
| `Summon` (lounge unit) | lounge, lounges, sofa, couch, settee, hammock, recliner, divan |
| `Summon` (twinflower unit) | twinflower, twinflowers |
| `Recuperate` | recuperate, recover, unwind, convalesce, nurse, rehabilitate, convalescent, restful, revitalize, renew |
| `Comfort` | comfort, encourage, reassure, inspire, uplift, embolden, motivate, hearten, bolster, rally |
| `Overcharge` | overcharge, surge, jolt, electrify, galvanize, supercharge, amplify, boost, energize (power-up variant), charge (electrical variant) |
| `Summon` (ghostship unit) | ghostship, ghost ship, phantom ship, spectral vessel, galleon |
| `Cannonade` | cannonade, volley, barrage, bombardment, salvo, broadside, fusillade, battery, shelling |
| `Plunder` | plunder, pillage, loot, raid, maraud, ransack, rob, pirate, buccaneer, brigand, corsair |
| `Attune` | attune, harmonize, resonate, synchronize, calibrate, align, tune, chord, frequency, attunement, resound, reverberate |
| `Ignite` | kindle, inflame, scald, sear, char, singe, cauterize, incinerate |
| `Combust` | detonate, explode, erupt, rupture, burst, implode |
| `Summon` (firemaster unit) | firemaster, firemasters |
| `Summon` (mercenary unit) | mercenary, mercenaries, soldier, sellsword, bodyguard, gladiator, enforcer, warden, conscript, knight |
| `Cataclysm` | supernova, supernovas, apocalypse, armageddon, cataclysm, annihilation, obliteration, doomsday, ragnarok, catastrophe, holocaust, extinction, devastation, calamity |
| `Cleave` | machete, machetes, axe, hatchet, cleaver, scythe, saber, katana, broadsword, falchion, glaive, halberd, slash, hack, chop, carve, sever, bisect, rend |
| `Item` (telescope accessory) | telescope, telescopes, spyglass, binoculars, lens, scope, monocle, periscope, prism, spectacles, microscope, beacon |
| `Lockpick` | lockpick, jimmy, picklock, skeleton_key, crowbar, jemmy, shiv, probe, tumbler, bypass, latch, keyhole, pin |

| Tag | Pre-classified words (apply this tag when you encounter them) |
|-----|--------------------------------------------------------------|
| `BEAST` | wolf, wolves, hawk, hawks, serpent, serpents, bear, bears, spider, spiders, eagle, eagles, falcon, falcons, vulture, vultures, hyena, hyenas, jackal, jackals, panther, panthers, tiger, tigers, lion, lions, shark, sharks |
| `FLYING` | hawk, hawks, eagle, eagles, falcon, falcons, owl, owls, bat, bats, moth, moths, butterfly, dragonfly, fairy, sprite, phoenix, griffin, pegasus, wyvern |
| `SIGHT` | telescope, telescopes, spyglass, binoculars, lens, scope, monocle, periscope, prism, spectacles, microscope, beacon, lantern, spotlight, observatory, lookout, watchtower |
| `LIGHT` | glow, shine, flash, beam, ray, sunrise, aurora, luminous, bright, radiant, brilliant, gleam, shimmer, sparkle, dazzle, illuminate, lantern, torch, candle, lighthouse |
| `CLEANSING` | purify, wash, rinse, cleanse, baptize, scrub, disinfect, cure, remedy, sanitize, sterilize, detox, soap, lather, bathe, shower, launder |
| `DEBUFF` | weaken, corrode, rust, wither, decay, sap, drain, curse, hex, blight, cripple, enfeeble, diminish, erode, siphon, leech, undermine, sabotage, impair |
| `STEALTH` | spy, assassin, thief, rogue, ninja, ghost, phantom, lurk, skulk, ambush, sneak, prowl, shadow, vanish, cloak, disguise, infiltrate, camouflage |
| `LIGHTNING` | lightning, thunder, bolt, spark, shock, jolt, zap, surge, voltage, static, electrify, galvanize, thunderbolt, thunderclap, electrode |
| `WEATHER` | storm, rain, hail, blizzard, tornado, cyclone, gale, fog, thunder, hurricane, monsoon, tempest, drought, frost, sleet, typhoon, squall |
| `RELAX` | lounge, relax, unwind, chill, rest, nap, doze, laze, idle, meditate, recline, slouch, snooze, siesta |
| `BOTANICAL` | rose, vine, bloom, sprout, petal, moss, fern, orchid, lily, tulip, blossom, seedling, root, ivy, herb, flora |
| `DRAIN` | leech, drain, absorb, siphon, sap, parasite, devour, consume, extract, deplete, wither, vampire |
| `UNDEAD` | ghost, phantom, specter, wraith, skeleton, zombie, lich, vampire, revenant, banshee, necromancer, corpse, ghoul, mummy, apparition |
| `NAVAL` | ship, anchor, cannon, sail, hull, mast, keel, stern, bow, fleet, armada, corsair, pirate, buccaneer, galleon, frigate, captain, sailor |
| `FIRE` | fire, flame, blaze, inferno, scorch, ember, torch, pyre, ash, cinder, combustion, magma, lava, volcano, kindle, ignite, combust, furnace, forge, smelt |
| `COSMIC` | supernova, comet, meteor, asteroid, nebula, galaxy, star, pulsar, quasar, cosmos, celestial, stellar, astral, eclipse, void, singularity, orbit |
| `DESTRUCTION` | supernova, apocalypse, armageddon, cataclysm, annihilation, obliteration, demolish, devastation, ruin, havoc, wreckage, carnage, rampage, ravage |
| `BLADE` | machete, axe, sword, dagger, knife, scythe, saber, katana, rapier, cutlass, cleaver, hatchet, scimitar, broadsword, falchion, glaive, halberd, stiletto, dirk |
| `JUNGLE` | machete, vine, python, piranha, jaguar, mosquito, canopy, swamp, tropical, bamboo, parrot, monkey, toucan, anaconda, orchid, fern, humidity, rainforest |
| `TOOL` | lockpick, jimmy, picklock, skeleton_key, crowbar, jemmy, shiv, probe, tumbler, bypass, latch, keyhole, pin, wrench, pliers, hammer, chisel, screwdriver |

---

## DESIGN AWARENESS

Every game word is a **design decision** — it permanently shapes the player's vocabulary and the game's possibility space. Treat each classification as if you're designing a card for a deckbuilder: it needs a clear identity, a reason to exist, and a role in the ecosystem.

When classifying words, consider the FULL ecosystem of word relationships:
- **Actions**: direct combat effects (Damage, Heal, Burn, etc.) — what makes THIS word's combo different from existing words?
- **Summons**: spawning units with their own abilities and composable passives — what unique battlefield role does this unit fill?
- **Items**: equippable gear that grants stats and/or passives (5 slots: head, wear, accessory, consumable, weapon) — which slot needs this item? What build does it enable?
- **Tags**: categories for passive triggers and thematic grouping — which existing passives does this tag activate?

Each word should contribute to ONE of these roles. Don't make every word a simple damage dealer.

**Design principle: fewer well-designed words > many generic ones.** If a word doesn't offer something distinct, classify it as a non-game word and move on.

### MANA COST CALIBRATION

When setting a word's `cost`, **always check the DB for words with the same primary action** and calibrate proportionally. The cost should reflect the word's power relative to its peers.

| Cost | Profile | Reference Examples |
|------|---------|-------------------|
| 0 | Weak single action (value 1), common word | drip → Water:1, scratch → Damage:1 |
| 1 | Single action (value 2-3) or weak combo | flame → Burn:2 + Damage:2, axe → Cleave:2 |
| 2 | Strong single action (value 3-4) or standard combo | machete → Cleave:3 + Bleed:1, saber → Cleave:3 |
| 3 | Multi-action combo or AoE | tornado → Damage:3 + Scramble:1, machetes → Cleave:3 + Bleed:1 (2 targets) |
| 4 | Premium multi-action or powerful AoE | supernova → Cataclysm:4 (All), flood → Water:4 + Damage:3 |
| 5 | Maximum power (3+ actions or devastating AoE) | tsunami → Water:5 + Damage:5 + Push:2 + Scramble:1 |

**Cost modifiers:**
- AoE targeting (AllEnemies, All): +1 cost over single-target equivalent
- Self-only beneficial (heal, buff, shield): -1 cost
- Combo with secondary debuff (Bleed, Burn, status): +1 cost
- Summon words: base ability cost + 3 (singular), + 6 (plural = dual summon)
- Items: usually cost 0 (go to inventory, not combat)

**IMPORTANT**: Before assigning a cost, query the DB: `SELECT word, cost FROM word_meta WHERE word IN (SELECT word FROM word_actions WHERE action_name = 'X') ORDER BY cost`. Use the results to ensure your new word fits the existing cost curve.

---

## QUALITY INVESTIGATION PROTOCOL

Every batch must go through three phases. Do NOT skip phases or combine them.

### Phase A — Triage (scan all 100 words)

Quick-scan the entire batch and separate words into two buckets:
- **Game word candidates**: words with clear combat, RPG, item, or summon meaning
- **Non-game words**: articles, prepositions, abstract nouns, obscure terms with no RPG relevance

Do NOT assign any actions during triage. Just categorize.

### Phase B — Deep Investigation (each game word candidate)

For each game word candidate, work through this checklist:

1. **Word identity**: What RPG archetype does this word evoke? (damage spell, healing, buff, summon creature, equipment, interaction)
2. **Uniqueness check**: Search your memory of recent batches and the DB audit — does an existing word already fill this role with a similar profile? If yes, either design a meaningfully different combo or skip the word.
3. **Action combo design**: Choose actions that create an interesting gameplay moment, not just "Damage N". Consider:
   - Multi-action combos that synergize (e.g. Water + Shock, Burn + Push)
   - Status effects that set up future turns
   - Targeting that isn't just SingleEnemy
4. **If Summon**: Design a unique battlefield role. Vary passive triggers (don't default to `on_ally_hit`). Write a 1-sentence design rationale in your working notes. Check that no existing summon has the same trigger+effect combo.
5. **If Item**: Run slot gap analysis — which `item_type` is underrepresented? Design stats that enable a specific build archetype (glass cannon, tank, spellcaster, etc.). Verify no existing item has the same type + stat profile.
6. **Tag synergies**: Choose tags that activate existing `on_word_tag` passives. If the word could reasonably have multiple tags, assign them — richer tagging creates richer gameplay.

### Phase C — Batch Cross-Reference (review before inserting)

Before generating the final JSON, review ALL game words in the batch together:
- **Diversity**: No two game words should have identical action+value+target profiles
- **Tag spread**: Ensure at least 3 different tags are represented across game words
- **Targeting variety**: Not all game words should be SingleEnemy — mix in Self, AllEnemies, positional, random, stat-based
- **Summon/item variety**: No two summons share the same passive archetype; no two items share the same slot+stats
- **Synergy potential**: At least one word should create a new combo with existing words (e.g. applying a status that an existing summon's passive triggers on)

Revise any weak entries. It's better to downgrade a borderline game word to non-game than to insert filler.

---

## CLASSIFICATION GUIDELINES

### CRITICAL: Multiple Actions Per Word

**Words should have as many actions as realistically make sense.** This is the core design philosophy — words are realistic representations of what would actually happen. A single action per word is almost always too simplistic. Think about ALL the effects a real-world phenomenon would cause:

- "tsunami" → Water (it's water) + Damage (destructive force) + Push (displacement) + Scramble (chaotic repositioning) = **4 actions**
- "avalanche" → Damage (crushing) + Push (displacement) + Water (snow/ice) + Freeze (cold) = **4 actions**
- "inferno" → Fire (elemental) + Burn (ignition) + Damage (heat) + Light (illumination) = **4 actions**
- "lightning" → Shock (electricity) + Damage (strike) + Light (flash) + Stun (paralyze) = **4 actions**
- "earthquake" → Damage (destruction) + Scramble (displacement) + Stun (disorientation) = **3 actions**

**The more actions that relate to the word's real meaning, the better.** Ask yourself: "What would ACTUALLY happen if this occurred?" Each real consequence should be an action. A word with 1 action should be the exception (simple words like "drip"), not the rule. Most game words should have 2-4 actions.

This makes the game richer — typing a powerful word triggers a cascade of realistic effects, making combat feel dynamic and rewarding vocabulary mastery.

- **Word meaning drives effects**: "avalanche" → Damage + Push + Water; "whisper" → Fear; "fortress" → Heal + Shield (Self)
- **Longer/rarer words = stronger**: 3-4 letter words are weak (1-2 value), 7+ letter words are powerful (4-6 value)
- **Cost scales with power**: powerful words cost more energy
- **Range is ignored** (slot system — everything is always in range). Use `0` for all words.
- **Area is ignored** (slot system — no area expansion). Use `"Single"` for all words. Targeting is handled by the `target` field (e.g. `AllEnemies` for AoE).
- **Most NLTK words are NOT game words** — articles, prepositions, obscure terms → empty actions array, they still get marked as processed
- **Quality over quantity** — there is no target percentage. Fewer well-designed words with interesting profiles are better than many generic ones. Only classify a word as a game word if you can articulate what makes it distinct.
- **Distinctiveness requirement** — before assigning actions, verify no existing word in the DB has the same action+value+target profile. If a near-duplicate exists, redesign the word's combo or skip it.
- **Summon design rationale** — every summon must have a 1-sentence explanation (in your working notes, not in JSON) of its unique battlefield role. If you can't explain why it's different from existing summons, don't add it.
- **Item ecosystem fit** — before creating an item, check which equipment slots are underrepresented (via `audit.py`). Verify the item's stat profile doesn't duplicate an existing item. Items should fill gaps, not stack duplicates.
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

Before each batch: run `stats.py` AND `audit.py` — review action distribution, tag coverage, unit diversity, item slot coverage, and duplicate profiles.

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

### Quality checkpoints

Before inserting any game words, review the batch as a whole:
- **Filler check**: Remove any game word that exists only because it vaguely relates to combat — if you can't articulate why it's interesting, it's filler
- **Summon diversity**: No two summons in the same batch should have the same passive trigger+effect combo
- **Item diversity**: No two items in the same batch should have the same item_type + stat profile
- **Profile uniqueness**: For each game word, mentally compare its action+value+target against the last 3 batches — if it's a duplicate profile, redesign or skip it
- **Synergy audit**: At least 1 game word per batch should create a NEW synergy with existing words (e.g. a word that benefits from a status no existing word applied, or a summon that triggers on an underused tag)

---

## RULES

- Use the Python pipeline (batch_next → classify → batch_insert) — do NOT edit C# files
- Every game word MUST have at least one action mapping and at least one tag
- Non-game words get empty actions and tags — they're just marked processed
- Values must be 1-10 (enforced by DB constraint)
- Cost must be 0-10
- Range 0 = unlimited
- Each iteration processes one batch (~100 words)
- Summon words MUST have cost >= 1 (enforced by batch_insert.py). Summoning always costs mana.
- Item words (equipping) have cost 0 — equipping an item is free. Use `Item` action (not `Weapon`).
- Weapon-type items use `assoc_word` on the action to link ammo words.
