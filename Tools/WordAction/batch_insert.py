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
    "Water", "Fire", "Earth", "Wind", "Push", "Damage", "Slow", "Burn",
    "Freeze", "Curse", "Heavy", "Shock", "Heal", "Dark", "Light",
    "Poison", "Shield", "Summon", "Time", "Fear", "Stun", "Concussion",
    "Concentrate", "Bleed", "Grow", "Thorns", "Reflect", "Hardening",
    "BuffStrength", "BuffMagicPower", "BuffPhysicalDefense", "BuffMagicDefense", "BuffLuck",
    "DebuffStrength", "DebuffMagicPower", "DebuffPhysicalDefense", "DebuffMagicDefense", "DebuffLuck",
}

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
}


def main():
    if not os.path.exists(DB_PATH):
        print(f"Error: database not found at {DB_PATH}", file=sys.stderr)
        sys.exit(1)

    data = json.load(sys.stdin)
    if not isinstance(data, list):
        print("Error: expected a JSON array", file=sys.stderr)
        sys.exit(1)

    action_rows = []
    meta_rows = []
    tag_rows = []
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
        meta_rows.append((word, target, cost, range_val, area))

        for seq, action in enumerate(entry.get("actions", [])):
            action_name = action["action"]
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

    conn = sqlite3.connect(DB_PATH)
    conn.executemany(
        "INSERT OR REPLACE INTO word_actions (word, action_name, value, target, range, area, assoc_word, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
        action_rows,
    )
    conn.executemany(
        "INSERT OR REPLACE INTO word_meta (word, target, cost, range, area) VALUES (?, ?, ?, ?, ?)",
        meta_rows,
    )
    conn.executemany(
        "INSERT OR REPLACE INTO word_tags (word, tag) VALUES (?, ?)",
        tag_rows,
    )
    conn.executemany(
        "INSERT OR IGNORE INTO processed_words (word) VALUES (?)",
        processed_rows,
    )
    conn.commit()
    conn.close()

    words_with_actions = sum(1 for e in data if e.get("actions"))
    words_without = len(data) - words_with_actions
    print(
        f"Inserted {len(action_rows)} actions, {len(meta_rows)} meta, {len(tag_rows)} tags "
        f"from {len(data)} words ({words_with_actions} with actions, {words_without} without)",
        file=sys.stderr,
    )
    if skipped_actions:
        print(f"Skipped {skipped_actions} invalid actions", file=sys.stderr)


if __name__ == "__main__":
    main()
