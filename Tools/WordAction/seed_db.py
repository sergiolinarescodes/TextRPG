#!/usr/bin/env python3
"""Creates the wordactions.db SQLite database with ~50 hand-picked seed words."""

import os
import sqlite3

DB_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets")
DB_PATH = os.path.join(DB_DIR, "wordactions.db")

SCHEMA = """
CREATE TABLE IF NOT EXISTS word_actions (
    word        TEXT NOT NULL COLLATE NOCASE,
    action_name TEXT NOT NULL,
    value       INTEGER NOT NULL CHECK(value BETWEEN 1 AND 10),
    PRIMARY KEY (word, action_name)
);
CREATE INDEX IF NOT EXISTS idx_word ON word_actions(word);

CREATE TABLE IF NOT EXISTS word_meta (
    word   TEXT PRIMARY KEY COLLATE NOCASE,
    target TEXT NOT NULL DEFAULT 'SingleEnemy',
    cost   INTEGER NOT NULL DEFAULT 0 CHECK(cost BETWEEN 0 AND 10)
);
"""

# (word, action_name, value)
SEED_ACTIONS = [
    # Water words
    ("drip",       "Water", 1),
    ("splash",     "Water", 2),  ("splash",     "Damage", 1),
    ("wave",       "Water", 3),  ("wave",       "Push", 2),  ("wave",       "Damage", 2),
    ("stream",     "Water", 2),  ("stream",     "Damage", 1),
    ("torrent",    "Water", 4),  ("torrent",    "Damage", 3),  ("torrent",    "Push", 2),
    ("tsunami",    "Water", 5),  ("tsunami",    "Damage", 5),  ("tsunami",    "Push", 2),
    ("ocean",      "Water", 3),  ("ocean",      "Damage", 2),
    ("flood",      "Water", 4),  ("flood",      "Damage", 3),
    ("deluge",     "Water", 5),  ("deluge",     "Damage", 4),
    ("rain",       "Water", 1),  ("rain",       "Slow", 1),

    # Fire words
    ("ember",      "Fire", 1),   ("ember",      "Damage", 1),
    ("spark",      "Fire", 2),   ("spark",      "Light", 1),  ("spark",      "Damage", 1),
    ("flame",      "Fire", 3),   ("flame",      "Burn", 2),   ("flame",      "Damage", 2),
    ("blaze",      "Fire", 4),   ("blaze",      "Burn", 3),   ("blaze",      "Damage", 3),
    ("inferno",    "Fire", 5),   ("inferno",    "Burn", 4),   ("inferno",    "Damage", 4),  ("inferno", "Light", 2),
    ("torch",      "Fire", 2),   ("torch",      "Burn", 1),   ("torch",      "Damage", 1),
    ("scorch",     "Fire", 3),   ("scorch",     "Burn", 3),   ("scorch",     "Damage", 2),

    # Push / Wind words
    ("shove",      "Push", 2),   ("shove",      "Damage", 1),
    ("thrust",     "Push", 3),   ("thrust",     "Damage", 2),
    ("gust",       "Wind", 2),   ("gust",       "Push", 2),
    ("hurricane",  "Wind", 5),   ("hurricane",  "Push", 4),   ("hurricane",  "Damage", 4),

    # Physical words
    ("scratch",    "Damage", 1),
    ("hit",        "Damage", 2),
    ("strike",     "Damage", 3), ("strike",     "Shock", 1),
    ("smash",      "Damage", 4), ("smash",      "Heavy", 2),
    ("obliterate", "Damage", 5), ("obliterate", "Heavy", 3),
    ("crush",      "Damage", 3), ("crush",      "Heavy", 3),

    # Healing words
    ("bandage",    "Heal", 2),
    ("mend",       "Heal", 3),
    ("restore",    "Heal", 4),   ("restore",    "Buff", 1),
    ("rejuvenate", "Heal", 5),   ("rejuvenate", "Buff", 2),
    ("resurrect",  "Heal", 5),   ("resurrect",  "Light", 2),  ("resurrect",  "Buff", 3),

    # Light words
    ("glow",       "Light", 1),
    ("shine",      "Light", 2),
    ("radiance",   "Light", 3),  ("radiance",   "Heal", 1),
    ("beacon",     "Light", 2),  ("beacon",     "Buff", 1),
    ("flash",      "Light", 3),  ("flash",      "Shock", 2),  ("flash",      "Damage", 1),

    # Dark words
    ("shadow",     "Dark", 2),   ("shadow",     "Curse", 1),
    ("gloom",      "Dark", 1),   ("gloom",      "Slow", 1),
    ("void",       "Dark", 4),   ("void",       "Damage", 3),
    ("eclipse",    "Dark", 3),   ("eclipse",    "Curse", 2),
    ("abyss",      "Dark", 5),   ("abyss",      "Damage", 3),
]

# (word, target, cost)
SEED_META = [
    ("drip",        "SingleEnemy",  0),
    ("splash",      "SingleEnemy",  0),
    ("wave",        "MeleeArea",    2),
    ("stream",      "SingleEnemy",  0),
    ("torrent",     "SingleEnemy",  2),
    ("tsunami",     "AreaAll",      6),
    ("ocean",       "AreaEnemies",  3),
    ("flood",       "AreaAll",      4),
    ("deluge",      "AreaAll",      5),
    ("rain",        "AreaAll",      1),
    ("ember",       "SingleEnemy",  0),
    ("spark",       "SingleEnemy",  0),
    ("flame",       "SingleEnemy",  1),
    ("blaze",       "SingleEnemy",  2),
    ("inferno",     "AreaEnemies",  5),
    ("torch",       "SingleEnemy",  1),
    ("scorch",      "SingleEnemy",  2),
    ("shove",       "MeleeInFront", 0),
    ("thrust",      "MeleeInFront", 1),
    ("gust",        "SingleEnemy",  0),
    ("hurricane",   "AreaAll",      5),
    ("scratch",     "MeleeInFront", 0),
    ("hit",         "MeleeInFront", 0),
    ("strike",      "MeleeInFront", 1),
    ("smash",       "MeleeInFront", 2),
    ("obliterate",  "AreaEnemies",  4),
    ("crush",       "MeleeInFront", 2),
    ("bandage",     "Self",         0),
    ("mend",        "Self",         1),
    ("restore",     "SingleAlly",   2),
    ("rejuvenate",  "SingleAlly",   3),
    ("resurrect",   "SingleAlly",   5),
    ("glow",        "Self",         0),
    ("shine",       "Self",         0),
    ("radiance",    "AreaAllies",   1),
    ("beacon",      "AreaAllies",   1),
    ("flash",       "AreaEnemies",  1),
    ("shadow",      "SingleEnemy",  1),
    ("gloom",       "AreaEnemies",  1),
    ("void",        "AreaAll",      3),
    ("eclipse",     "AreaAll",      3),
    ("abyss",       "AreaAll",      4),
]


def main():
    os.makedirs(DB_DIR, exist_ok=True)

    if os.path.exists(DB_PATH):
        os.remove(DB_PATH)

    conn = sqlite3.connect(DB_PATH)
    cur = conn.cursor()
    cur.executescript(SCHEMA)
    cur.executemany(
        "INSERT OR REPLACE INTO word_actions (word, action_name, value) VALUES (?, ?, ?)",
        SEED_ACTIONS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO word_meta (word, target, cost) VALUES (?, ?, ?)",
        SEED_META,
    )
    conn.commit()

    action_count = cur.execute("SELECT COUNT(*) FROM word_actions").fetchone()[0]
    unique_words = cur.execute("SELECT COUNT(DISTINCT word) FROM word_actions").fetchone()[0]
    meta_count = cur.execute("SELECT COUNT(*) FROM word_meta").fetchone()[0]
    conn.close()

    print(f"Created {DB_PATH}")
    print(f"  {action_count} action rows, {unique_words} unique words, {meta_count} word meta entries")


if __name__ == "__main__":
    main()
