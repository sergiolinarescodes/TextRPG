#!/usr/bin/env python3
"""Reads classified word-action JSON from stdin and inserts into the DB.

Expected JSON format (array of objects):
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
        "word": "the",
        "target": "SingleEnemy",
        "cost": 0,
        "range": 0,
        "area": "Single",
        "tags": [],
        "actions": []
    }
]
"""

import json
import os
import sqlite3
import sys

DB_PATH = os.path.join(
    os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets", "wordactions.db"
)

VALID_ACTIONS = {
    "Water", "Earth", "Wind", "Push", "Damage", "MagicDamage", "Slow", "Burn",
    "Freeze", "Curse", "Heavy", "Shock", "Heal", "Dark", "Light",
    "Poison", "Shield", "Summon", "Time", "Fear", "Stun", "Concussion",
    "Concentrate", "Bleed", "Grow", "Thorns", "Reflect", "Hardening",
    "Drunk", "Smash", "Pay", "Energize", "Relax", "Sleep", "RestHeal", "Scramble", "Melt",
    "Peck", "Screech", "Purify", "Awaken", "Siphon", "Deceive", "Recuperate", "Comfort", "Overcharge",
    "Cannonade", "Plunder", "Attune", "Ignite", "Combust", "Cataclysm", "Cleave", "Lockpick",
    "BuffStrength", "BuffMagicPower", "BuffPhysicalDefense", "BuffMagicDefense", "BuffLuck",
    "DebuffStrength", "DebuffMagicPower", "DebuffPhysicalDefense", "DebuffMagicDefense", "DebuffLuck",
    "Item",
    "Enter", "Talk", "Steal", "Search", "Pray", "Rest", "Open", "Trade", "Recruit", "Leave",
    "Charm",
}

# Backward compatibility alias
ACTION_ALIASES = {"Weapon": "Item"}

VALID_TARGETS = {
    "Self", "SingleEnemy", "AreaEnemies", "AreaAll", "AllAllies", "AllAlliesAndSelf",
    "AllEnemies", "All",
    "Melee", "Area",
    "RandomEnemy", "RandomAlly", "RandomAny",
    "LowestHealthEnemy", "HighestHealthEnemy",
    "LowestDefenseEnemy", "HighestDefenseEnemy",
    "LowestStrengthEnemy", "HighestStrengthEnemy",
    "LowestMagicEnemy", "HighestMagicEnemy",
    "RandomLowestHealthEnemy", "RandomHighestHealthEnemy",
    "RandomEnemyWithStatus", "RandomEnemyWithoutStatus",
    "AllEnemiesWithStatus", "AllEnemiesWithoutStatus",
    "HalfEnemiesRandom", "TwoRandomEnemies", "ThreeRandomEnemies",
}

VALID_STATUS_EFFECTS = {
    "Burning", "Wet", "Poisoned", "Frozen", "Slowed", "Cursed",
    "Buffed", "Shielded", "ExtraTurn", "Stun", "Concussion", "Fear",
    "Bleeding", "Concentrated",
    "Growing", "Thorns", "Reflecting", "Hardening",
    "Awakened",
}

def is_valid_target(target):
    if target in VALID_TARGETS:
        return True
    if '+' in target:
        base, status = target.split('+', 1)
        return base in VALID_TARGETS and status in VALID_STATUS_EFFECTS
    return False

VALID_AREAS = {
    "Single", "Cross", "Square3x3", "Diamond2", "Line3", "VerticalLine",
}

VALID_TAGS = {
    "NATURE", "ELEMENTAL", "OFFENSIVE", "RESTORATION", "SHADOW",
    "PHYSICAL", "DEFENSIVE", "ARCANE", "HOLY", "SUPPORT", "PSYCHIC",
    "SPELL", "MELEE", "SOCIAL", "BEAST", "FLYING", "LIGHT", "CLEANSING",
    "DEBUFF", "STEALTH", "RELAX", "THOUGHTS", "DWELLING", "LIGHTNING", "WEATHER",
    "BOTANICAL", "DRAIN", "UNDEAD", "NAVAL", "FIRE", "COSMIC", "DESTRUCTION",
    "BLADE", "JUNGLE", "SIGHT",
}

VALID_TRIGGERS = {
    "on_ally_hit", "on_self_hit", "on_round_end", "on_round_start",
    "on_turn_start", "on_turn_end", "on_word_played", "on_word_length",
    "on_word_tag", "on_kill", "on_ally_death", "on_death", "on_damage_dealt",
    "on_letter_in_word", "taunt",
}

VALID_EFFECTS = {"heal", "damage", "shield", "mana", "apply_status", "steal_stat", "gold", "buff_stat"}

VALID_PASSIVE_TARGETS = {"Self", "AllAllies", "AllEnemies", "Injured", "Attacker"}

VALID_UNIT_TYPES = {"enemy", "structure"}

VALID_ITEM_TYPES = {"weapon", "consumable", "trinket", "head", "wear", "accessory"}


def main():
    if not os.path.exists(DB_PATH):
        print(f"Error: database not found at {DB_PATH}", file=sys.stderr)
        sys.exit(1)

    is_draft = "--draft" in sys.argv
    data = json.load(sys.stdin)
    if not isinstance(data, list):
        print("Error: expected a JSON array", file=sys.stderr)
        sys.exit(1)

    action_rows = []
    meta_rows = []
    tag_rows = []
    unit_rows = []
    ability_rows = []
    passive_rows = []
    item_rows = []
    item_passive_rows = []
    item_tag_rows = []
    processed_rows = []
    skipped_actions = 0

    for entry in data:
        word = entry["word"].strip().lower()
        processed_rows.append((word,))

        target = entry.get("target", "SingleEnemy")
        if not is_valid_target(target):
            print(f"Warning: unknown target '{target}' for '{word}', defaulting to SingleEnemy", file=sys.stderr)
            target = "SingleEnemy"
        cost = max(0, min(10, int(entry.get("cost", 0))))
        range_val = max(0, int(entry.get("range", 0)))
        area = entry.get("area", "Single")
        if area not in VALID_AREAS:
            print(f"Warning: unknown area '{area}' for '{word}', defaulting to Single", file=sys.stderr)
            area = "Single"

        # Enforce summon mana cost
        actions = entry.get("actions", [])
        has_summon = any(a["action"] == "Summon" for a in actions)
        if has_summon and cost < 1:
            print(f"Warning: Summon word '{word}' has cost {cost}, clamping to 1", file=sys.stderr)
            cost = 1

        status = "draft" if is_draft else None
        meta_rows.append((word, target, cost, range_val, area, status))

        for seq, action in enumerate(actions):
            action_name = ACTION_ALIASES.get(action["action"], action["action"])
            if action_name not in VALID_ACTIONS:
                print(f"Warning: unknown action '{action_name}' for '{word}', skipping", file=sys.stderr)
                skipped_actions += 1
                continue
            value = max(1, min(10, int(action["value"])))
            act_target = action.get("target")
            act_range = action.get("range")
            act_area = action.get("area")
            if act_target is not None and not is_valid_target(act_target):
                print(f"Warning: unknown action target '{act_target}' for '{word}:{action_name}', ignoring", file=sys.stderr)
                act_target = None
            if act_area is not None and act_area not in VALID_AREAS:
                print(f"Warning: unknown action area '{act_area}' for '{word}:{action_name}', ignoring", file=sys.stderr)
                act_area = None
            act_assoc = action.get("assoc_word", "")
            action_rows.append((word, action_name, value, act_target, act_range, act_area, act_assoc, seq))

        for tag in entry.get("tags", []):
            tag_upper = tag.upper()
            if tag_upper not in VALID_TAGS:
                print(f"Warning: unknown tag '{tag_upper}' for '{word}', skipping", file=sys.stderr)
                continue
            tag_rows.append((word, tag_upper))

        # Unit definition for summon words
        if "unit" in entry and has_summon:
            unit = entry["unit"]
            unit_id = word

            unit_type = unit.get("unit_type", "enemy")
            if unit_type not in VALID_UNIT_TYPES:
                print(f"Warning: unknown unit_type '{unit_type}' for '{word}', defaulting to enemy", file=sys.stderr)
                unit_type = "enemy"

            display_name = unit.get("display_name", word.upper())
            max_health = max(5, min(100, int(unit.get("max_health", 20))))
            strength = max(0, min(15, int(unit.get("strength", 0))))
            magic_power = max(0, min(15, int(unit.get("magic_power", 0))))
            phys_defense = max(0, min(15, int(unit.get("phys_defense", 0))))
            magic_defense = max(0, min(15, int(unit.get("magic_defense", 0))))
            luck = max(0, min(15, int(unit.get("luck", 0))))
            starting_shield = max(0, min(20, int(unit.get("starting_shield", 0))))

            color = unit.get("color", [0.5, 0.5, 0.5])
            color_r = max(0.0, min(1.0, float(color[0]))) if len(color) > 0 else 0.5
            color_g = max(0.0, min(1.0, float(color[1]))) if len(color) > 1 else 0.5
            color_b = max(0.0, min(1.0, float(color[2]))) if len(color) > 2 else 0.5

            unit_rows.append((
                unit_id, display_name, unit_type, max_health,
                strength, magic_power, phys_defense, magic_defense,
                luck, starting_shield, color_r, color_g, color_b,
            ))

            for ability_word in unit.get("actions", unit.get("abilities", [])):
                ability_rows.append((unit_id, ability_word.strip().lower()))

            for seq, passive in enumerate(unit.get("passives", [])):
                trigger = passive.get("trigger", "")
                if trigger not in VALID_TRIGGERS:
                    print(f"Warning: unknown trigger '{trigger}' for unit '{unit_id}', skipping passive", file=sys.stderr)
                    continue

                if trigger == "taunt":
                    passive_rows.append((unit_id, "taunt", None, "", None, 0, "Self", seq))
                    continue

                effect = passive.get("effect", "")
                if effect not in VALID_EFFECTS:
                    print(f"Warning: unknown effect '{effect}' for unit '{unit_id}', skipping passive", file=sys.stderr)
                    continue

                p_target = passive.get("target", "Self")
                if p_target not in VALID_PASSIVE_TARGETS:
                    print(f"Warning: unknown passive target '{p_target}' for unit '{unit_id}', defaulting to Self", file=sys.stderr)
                    p_target = "Self"

                trigger_param = passive.get("trigger_param")
                effect_param = passive.get("effect_param")
                p_value = max(0, min(10, int(passive.get("value", 1))))

                # Validate trigger_param
                if trigger == "on_word_length" and trigger_param is not None:
                    try:
                        int(trigger_param)
                    except ValueError:
                        print(f"Warning: on_word_length trigger_param '{trigger_param}' is not an int for unit '{unit_id}', skipping passive", file=sys.stderr)
                        continue
                if trigger == "on_word_tag" and trigger_param is not None:
                    if trigger_param.upper() not in VALID_TAGS:
                        print(f"Warning: on_word_tag trigger_param '{trigger_param}' is not a valid tag for unit '{unit_id}', skipping passive", file=sys.stderr)
                        continue

                # Validate effect_param for apply_status
                if effect == "apply_status":
                    if effect_param not in VALID_STATUS_EFFECTS:
                        print(f"Warning: apply_status effect_param '{effect_param}' is not a valid status for unit '{unit_id}', skipping passive", file=sys.stderr)
                        continue

                passive_rows.append((unit_id, trigger, trigger_param, effect, effect_param, p_value, p_target, seq))

        elif "unit" in entry and not has_summon:
            print(f"Warning: '{word}' has unit definition but no Summon action, ignoring unit", file=sys.stderr)

        # Item definition for equipment words
        has_item = any(ACTION_ALIASES.get(a.get("action", ""), a.get("action", "")) == "Item" for a in actions)
        if "item" in entry and has_item:
            item = entry["item"]
            item_id = word

            item_type = item.get("item_type", "accessory")
            if item_type not in VALID_ITEM_TYPES:
                print(f"Warning: unknown item_type '{item_type}' for '{word}', defaulting to accessory", file=sys.stderr)
                item_type = "accessory"

            display_name = item.get("display_name", word.upper())
            durability = max(0, min(100, int(item.get("durability", 0))))
            i_strength = max(0, min(15, int(item.get("strength", 0))))
            i_magic_power = max(0, min(15, int(item.get("magic_power", 0))))
            i_phys_defense = max(0, min(15, int(item.get("phys_defense", 0))))
            i_magic_defense = max(0, min(15, int(item.get("magic_defense", 0))))
            i_luck = max(0, min(15, int(item.get("luck", 0))))
            i_max_health = max(0, min(100, int(item.get("max_health", 0))))
            i_max_mana = max(0, min(50, int(item.get("max_mana", 0))))

            color = item.get("color", [0.5, 0.5, 0.5])
            color_r = max(0.0, min(1.0, float(color[0]))) if len(color) > 0 else 0.5
            color_g = max(0.0, min(1.0, float(color[1]))) if len(color) > 1 else 0.5
            color_b = max(0.0, min(1.0, float(color[2]))) if len(color) > 2 else 0.5

            item_rows.append((
                item_id, display_name, item_type, durability,
                i_strength, i_magic_power, i_phys_defense, i_magic_defense,
                i_luck, i_max_health, i_max_mana, color_r, color_g, color_b,
            ))

            for i_seq, i_passive in enumerate(item.get("passives", [])):
                i_trigger = i_passive.get("trigger", "")
                if i_trigger not in VALID_TRIGGERS:
                    print(f"Warning: unknown trigger '{i_trigger}' for item '{item_id}', skipping passive", file=sys.stderr)
                    continue

                i_effect = i_passive.get("effect", "")
                if i_effect not in VALID_EFFECTS:
                    print(f"Warning: unknown effect '{i_effect}' for item '{item_id}', skipping passive", file=sys.stderr)
                    continue

                ip_target = i_passive.get("target", "Self")
                if ip_target not in VALID_PASSIVE_TARGETS:
                    print(f"Warning: unknown passive target '{ip_target}' for item '{item_id}', defaulting to Self", file=sys.stderr)
                    ip_target = "Self"

                i_trigger_param = i_passive.get("trigger_param")
                i_effect_param = i_passive.get("effect_param")
                ip_value = max(0, min(10, int(i_passive.get("value", 1))))

                if i_trigger == "on_word_length" and i_trigger_param is not None:
                    try:
                        int(i_trigger_param)
                    except ValueError:
                        print(f"Warning: on_word_length trigger_param '{i_trigger_param}' is not an int for item '{item_id}', skipping passive", file=sys.stderr)
                        continue
                if i_trigger == "on_word_tag" and i_trigger_param is not None:
                    if i_trigger_param.upper() not in VALID_TAGS:
                        print(f"Warning: on_word_tag trigger_param '{i_trigger_param}' is not a valid tag for item '{item_id}', skipping passive", file=sys.stderr)
                        continue

                if i_effect == "apply_status":
                    if i_effect_param not in VALID_STATUS_EFFECTS:
                        print(f"Warning: apply_status effect_param '{i_effect_param}' is not a valid status for item '{item_id}', skipping passive", file=sys.stderr)
                        continue

                item_passive_rows.append((item_id, i_trigger, i_trigger_param, i_effect, i_effect_param, ip_value, ip_target, i_seq))

            for i_tag in item.get("tags", []):
                i_tag_upper = i_tag.upper()
                item_tag_rows.append((item_id, i_tag_upper))

        elif "item" in entry and not has_item:
            print(f"Warning: '{word}' has item definition but no Item action, ignoring item", file=sys.stderr)

    conn = sqlite3.connect(DB_PATH)
    conn.executemany(
        "INSERT OR REPLACE INTO word_actions (word, action_name, value, target, range, area, assoc_word, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
        action_rows,
    )
    conn.executemany(
        "INSERT OR REPLACE INTO word_meta (word, target, cost, range, area, status) VALUES (?, ?, ?, ?, ?, ?)",
        meta_rows,
    )
    conn.executemany(
        "INSERT OR REPLACE INTO word_tags (word, tag) VALUES (?, ?)",
        tag_rows,
    )
    if unit_rows:
        conn.executemany(
            "INSERT OR REPLACE INTO units (unit_id, display_name, unit_type, max_health, "
            "strength, magic_power, phys_defense, magic_defense, luck, starting_shield, "
            "color_r, color_g, color_b) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            unit_rows,
        )
    if ability_rows:
        conn.executemany(
            "INSERT OR REPLACE INTO unit_abilities (unit_id, word) VALUES (?, ?)",
            ability_rows,
        )
    if passive_rows:
        conn.executemany(
            "INSERT OR REPLACE INTO unit_passives (unit_id, trigger_id, trigger_param, "
            "effect_id, effect_param, value, target, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            passive_rows,
        )
    if item_rows:
        conn.executemany(
            "INSERT OR REPLACE INTO items (item_id, display_name, item_type, durability, "
            "strength, magic_power, phys_defense, magic_defense, luck, max_health, max_mana, "
            "color_r, color_g, color_b) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            item_rows,
        )
    if item_passive_rows:
        conn.executemany(
            "INSERT OR REPLACE INTO item_passives (item_id, trigger_id, trigger_param, "
            "effect_id, effect_param, value, target, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            item_passive_rows,
        )
    if item_tag_rows:
        conn.executemany(
            "INSERT OR REPLACE INTO item_tags (item_id, tag) VALUES (?, ?)",
            item_tag_rows,
        )
    conn.executemany(
        "INSERT OR IGNORE INTO processed_words (word) VALUES (?)",
        processed_rows,
    )
    conn.commit()
    conn.close()

    words_with_actions = sum(1 for e in data if e.get("actions"))
    words_without = len(data) - words_with_actions
    draft_label = " [DRAFT — needs refinement]" if is_draft else ""
    print(
        f"Inserted {len(action_rows)} actions, {len(meta_rows)} meta, {len(tag_rows)} tags "
        f"from {len(data)} words ({words_with_actions} with actions, {words_without} without){draft_label}",
        file=sys.stderr,
    )
    if unit_rows:
        print(
            f"  {len(unit_rows)} units, {len(ability_rows)} abilities, {len(passive_rows)} passives",
            file=sys.stderr,
        )
    if item_rows:
        print(
            f"  {len(item_rows)} items, {len(item_passive_rows)} item passives, {len(item_tag_rows)} item tags",
            file=sys.stderr,
        )
    if skipped_actions:
        print(f"Skipped {skipped_actions} invalid actions", file=sys.stderr)


if __name__ == "__main__":
    main()
