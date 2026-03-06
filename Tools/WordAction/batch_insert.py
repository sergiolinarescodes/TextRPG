#!/usr/bin/env python3
"""Reads classified word-action JSON from stdin and inserts into the DB.

Expected JSON format (array of objects):
[
    {
        "word": "tsunami",
        "target": "AreaAll",
        "cost": 6,
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
    "Freeze", "Curse", "Buff", "Heavy", "Shock", "Heal", "Dark", "Light",
    "Poison", "Shield", "Summon", "Time",
}

VALID_TARGETS = {
    "SingleEnemy", "SingleAlly", "Self",
    "AreaEnemies", "AreaAllies", "AreaAll",
    "MeleeInFront", "MeleeArea",
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
    processed_rows = []
    skipped_actions = 0

    for entry in data:
        word = entry["word"].strip().lower()
        processed_rows.append((word,))

        target = entry.get("target", "SingleEnemy")
        if target not in VALID_TARGETS:
            print(f"Warning: unknown target '{target}' for '{word}', defaulting to SingleEnemy", file=sys.stderr)
            target = "SingleEnemy"
        cost = max(0, min(10, int(entry.get("cost", 0))))
        meta_rows.append((word, target, cost))

        for action in entry.get("actions", []):
            action_name = action["action"]
            if action_name not in VALID_ACTIONS:
                print(f"Warning: unknown action '{action_name}' for '{word}', skipping", file=sys.stderr)
                skipped_actions += 1
                continue
            value = max(1, min(10, int(action["value"])))
            action_rows.append((word, action_name, value))

    conn = sqlite3.connect(DB_PATH)
    conn.executemany(
        "INSERT OR REPLACE INTO word_actions (word, action_name, value) VALUES (?, ?, ?)",
        action_rows,
    )
    conn.executemany(
        "INSERT OR REPLACE INTO word_meta (word, target, cost) VALUES (?, ?, ?)",
        meta_rows,
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
        f"Inserted {len(action_rows)} action rows, {len(meta_rows)} meta from {len(data)} words "
        f"({words_with_actions} with actions, {words_without} without)",
        file=sys.stderr,
    )
    if skipped_actions:
        print(f"Skipped {skipped_actions} invalid actions", file=sys.stderr)


if __name__ == "__main__":
    main()
