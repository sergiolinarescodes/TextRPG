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
    target      TEXT DEFAULT NULL,
    range       INTEGER DEFAULT NULL,
    area        TEXT DEFAULT NULL,
    assoc_word  TEXT NOT NULL DEFAULT '',
    PRIMARY KEY (word, action_name, assoc_word)
);
CREATE INDEX IF NOT EXISTS idx_word ON word_actions(word);

CREATE TABLE IF NOT EXISTS word_meta (
    word   TEXT PRIMARY KEY COLLATE NOCASE,
    target TEXT NOT NULL DEFAULT 'SingleEnemy',
    cost   INTEGER NOT NULL DEFAULT 0 CHECK(cost BETWEEN 0 AND 10),
    range  INTEGER NOT NULL DEFAULT 0,
    area   TEXT NOT NULL DEFAULT 'Single'
);

CREATE TABLE IF NOT EXISTS word_tags (
    word TEXT NOT NULL COLLATE NOCASE,
    tag  TEXT NOT NULL,
    PRIMARY KEY (word, tag)
);
CREATE INDEX IF NOT EXISTS idx_word_tags ON word_tags(word);
"""

# (word, action_name, value, target, range, area, assoc_word)  — last 4 are nullable per-action overrides, assoc_word defaults to ''
SEED_ACTIONS = [
    # Water words
    ("drip",       "Water", 1, None, None, None, ""),
    ("splash",     "Water", 2, None, None, None, ""),  ("splash",     "Damage", 1, None, None, None, ""),
    ("wave",       "Water", 3, None, None, None, ""),  ("wave",       "Push", 2, None, None, None, ""),  ("wave",       "Damage", 2, None, None, None, ""),
    ("stream",     "Water", 2, None, None, None, ""),  ("stream",     "Damage", 1, None, None, None, ""),
    ("torrent",    "Water", 4, None, None, None, ""),  ("torrent",    "Damage", 3, None, None, None, ""),  ("torrent",    "Push", 2, None, None, None, ""),
    ("tsunami",    "Water", 5, None, None, None, ""),  ("tsunami",    "Damage", 5, None, None, None, ""),  ("tsunami",    "Push", 2, None, None, None, ""),
    ("ocean",      "Water", 3, None, None, None, ""),  ("ocean",      "Damage", 2, None, None, None, ""),
    ("flood",      "Water", 4, None, None, None, ""),  ("flood",      "Damage", 3, None, None, None, ""),
    ("deluge",     "Water", 5, None, None, None, ""),  ("deluge",     "Damage", 4, None, None, None, ""),
    ("rain",       "Water", 1, None, None, None, ""),  ("rain",       "Slow", 1, None, None, None, ""),

    # Fire words
    ("ember",      "Fire", 1, None, None, None, ""),   ("ember",      "Damage", 1, None, None, None, ""),
    ("spark",      "Fire", 2, None, None, None, ""),   ("spark",      "Light", 1, None, None, None, ""),  ("spark",      "Damage", 1, None, None, None, ""),
    ("flame",      "Fire", 3, None, None, None, ""),   ("flame",      "Burn", 2, None, None, None, ""),   ("flame",      "Damage", 2, None, None, None, ""),
    ("blaze",      "Fire", 4, None, None, None, ""),   ("blaze",      "Burn", 3, None, None, None, ""),   ("blaze",      "Damage", 3, None, None, None, ""),
    ("inferno",    "Fire", 5, None, None, None, ""),   ("inferno",    "Burn", 4, None, None, None, ""),   ("inferno",    "Damage", 4, None, None, None, ""),  ("inferno", "Light", 2, None, None, None, ""),
    ("torch",      "Fire", 2, None, None, None, ""),   ("torch",      "Burn", 1, None, None, None, ""),   ("torch",      "Damage", 1, None, None, None, ""),
    ("scorch",     "Fire", 3, None, None, None, ""),   ("scorch",     "Burn", 3, None, None, None, ""),   ("scorch",     "Damage", 2, None, None, None, ""),

    # Push / Wind words
    ("shove",      "Push", 2, None, None, None, ""),   ("shove",      "Damage", 1, None, None, None, ""),
    ("thrust",     "Push", 3, None, None, None, ""),   ("thrust",     "Damage", 2, None, None, None, ""),
    ("gust",       "Wind", 2, None, None, None, ""),   ("gust",       "Push", 2, None, None, None, ""),
    ("hurricane",  "Wind", 5, None, None, None, ""),   ("hurricane",  "Push", 4, None, None, None, ""),   ("hurricane",  "Damage", 4, None, None, None, ""),

    # Physical words
    ("scratch",    "Damage", 1, None, None, None, ""),
    ("hit",        "Damage", 2, None, None, None, ""),
    ("strike",     "Damage", 3, None, None, None, ""), ("strike",     "Shock", 1, None, None, None, ""),
    ("smash",      "Damage", 4, None, None, None, ""), ("smash",      "Heavy", 2, None, None, None, ""),
    ("obliterate", "Damage", 5, None, None, None, ""), ("obliterate", "Heavy", 3, None, None, None, ""),
    ("crush",      "Damage", 3, None, None, None, ""), ("crush",      "Heavy", 3, None, None, None, ""),

    # Healing words
    ("bandage",    "Heal", 2, None, None, None, ""),
    ("mend",       "Heal", 3, None, None, None, ""),
    ("restore",    "Heal", 4, None, None, None, ""),   ("restore",    "BuffStrength", 1, None, None, None, ""),
    ("rejuvenate", "Heal", 5, None, None, None, ""),   ("rejuvenate", "BuffStrength", 2, None, None, None, ""),
    ("resurrect",  "Heal", 5, None, None, None, ""),   ("resurrect",  "Light", 2, None, None, None, ""),  ("resurrect",  "BuffStrength", 3, None, None, None, ""),

    # Light words
    ("glow",       "Light", 1, None, None, None, ""),
    ("shine",      "Light", 2, None, None, None, ""),
    ("radiance",   "Light", 3, None, None, None, ""),  ("radiance",   "Heal", 1, None, None, None, ""),
    ("beacon",     "Light", 2, None, None, None, ""),  ("beacon",     "BuffLuck", 1, None, None, None, ""),
    ("flash",      "Light", 3, None, None, None, ""),  ("flash",      "Shock", 2, None, None, None, ""),  ("flash",      "Damage", 1, None, None, None, ""),

    # Dark words
    ("shadow",     "Dark", 2, None, None, None, ""),   ("shadow",     "Curse", 1, None, None, None, ""),
    ("gloom",      "Dark", 1, None, None, None, ""),   ("gloom",      "Slow", 1, None, None, None, ""),
    ("void",       "Dark", 4, None, None, None, ""),   ("void",       "Damage", 3, None, None, None, ""),
    ("eclipse",    "Dark", 3, None, None, None, ""),   ("eclipse",    "Curse", 2, None, None, None, ""),
    ("abyss",      "Dark", 5, None, None, None, ""),   ("abyss",      "Damage", 3, None, None, None, ""),

    # Per-action targeting example
    ("absorb",     "Damage", 1, "SingleEnemy", 3, "Single", ""),  ("absorb", "Heal", 1, "Self", 0, "Single", ""),

    # Movement words
    ("sprint",     "Move", 5, None, None, None, ""),
    ("dash",       "Move", 3, None, None, None, ""),
    ("charge",     "Move", 4, None, None, None, ""),  ("charge",     "Damage", 3, None, None, None, ""),
    ("blink",      "Teleport", 4, None, None, None, ""),
    ("warp",       "Teleport", 7, None, None, None, ""),
    ("scatter",    "MoveRandom", 5, None, None, None, ""),
    ("rally",      "MoveNearAlly", 4, None, None, None, ""),
    ("engage",     "MoveNearEnemy", 3, None, None, None, ""),  ("engage",     "Damage", 2, None, None, None, ""),
    ("flank",      "MoveFlank", 5, None, None, None, ""),  ("flank",      "Damage", 4, None, None, None, ""),
    ("retreat",    "Move", 3, None, None, None, ""),  ("retreat",    "Shield", 2, None, None, None, ""),
    ("teleport",   "Teleport", 6, None, None, None, ""),
    ("rush",       "Move", 2, None, None, None, ""),  ("rush",       "Damage", 5, None, None, None, ""),

    # Weapon words — assoc_word links weapon to ammo
    ("gun",        "Weapon", 4, "Self", None, None, "9mm"),
    ("gun",        "Weapon", 4, "Self", None, None, "buckshot"),
    ("sword",      "Weapon", 5, "Self", None, None, "slash"),
    ("sword",      "Weapon", 5, "Self", None, None, "stab"),

    # Ammo words (only usable through weapon mode)
    ("9mm",        "Damage", 3, "SingleEnemy", 15, None, ""),
    ("buckshot",   "Damage", 4, "SingleEnemy", 15, "Cross", ""),
    ("slash",      "Damage", 3, "Melee", 1, None, ""),
    ("stab",       "Damage", 4, "SingleEnemy", 1, None, ""),
]

# (word, target, cost, range, area)
SEED_META = [
    ("drip",        "SingleEnemy",  0, 3, "Single"),
    ("splash",      "SingleEnemy",  0, 3, "Single"),
    ("wave",        "Melee",        2, 2, "Cross"),
    ("stream",      "SingleEnemy",  0, 4, "Single"),
    ("torrent",     "SingleEnemy",  2, 4, "Line3"),
    ("tsunami",     "AreaAll",      6, 0, "Diamond2"),
    ("ocean",       "AreaEnemies",  3, 0, "Square3x3"),
    ("flood",       "AreaAll",      4, 0, "Diamond2"),
    ("deluge",      "AreaAll",      5, 0, "Square3x3"),
    ("rain",        "AreaEnemies",  1, 0, "VerticalLine"),
    ("ember",       "SingleEnemy",  0, 3, "Single"),
    ("spark",       "SingleEnemy",  0, 3, "Single"),
    ("flame",       "SingleEnemy",  1, 3, "Single"),
    ("blaze",       "SingleEnemy",  2, 4, "Cross"),
    ("inferno",     "AreaEnemies",  5, 0, "Square3x3"),
    ("torch",       "SingleEnemy",  1, 2, "Single"),
    ("scorch",      "SingleEnemy",  2, 3, "Single"),
    ("shove",       "Melee",        0, 1, "Single"),
    ("thrust",      "Melee",        1, 1, "Single"),
    ("gust",        "SingleEnemy",  0, 4, "Single"),
    ("hurricane",   "AreaAll",      5, 0, "Diamond2"),
    ("scratch",     "Melee",        0, 1, "Single"),
    ("hit",         "Melee",        0, 1, "Single"),
    ("strike",      "Melee",        1, 1, "Single"),
    ("smash",       "Melee",        2, 1, "Cross"),
    ("obliterate",  "AreaEnemies",  4, 0, "Square3x3"),
    ("crush",       "Melee",        2, 1, "Single"),
    ("bandage",     "Self",         0, 0, "Single"),
    ("mend",        "Self",         1, 0, "Single"),
    ("restore",     "AllAlliesAndSelf", 2, 0, "Single"),
    ("rejuvenate",  "AllAlliesAndSelf", 3, 0, "Single"),
    ("resurrect",   "AllAlliesAndSelf", 5, 0, "Single"),
    ("glow",        "Self",         0, 0, "Single"),
    ("shine",       "Self",         0, 0, "Single"),
    ("radiance",    "AllAlliesAndSelf", 1, 0, "Cross"),
    ("beacon",      "AllAlliesAndSelf", 1, 0, "Single"),
    ("flash",       "AreaEnemies",  1, 0, "Cross"),
    ("shadow",      "SingleEnemy",  1, 4, "Single"),
    ("gloom",       "AreaEnemies",  1, 0, "Single"),
    ("void",        "AreaAll",      3, 0, "Diamond2"),
    ("eclipse",     "AreaAll",      3, 0, "Square3x3"),
    ("abyss",       "AreaAll",      4, 0, "Diamond2"),
    ("absorb",      "SingleEnemy",  0, 3, "Single"),
    ("sprint",      "Self",         2, 0, "Single"),
    ("dash",        "Self",         1, 0, "Single"),
    ("charge",      "Self",         3, 0, "Single"),
    ("blink",       "Self",         2, 0, "Single"),
    ("warp",        "Self",         4, 0, "Single"),
    ("scatter",     "Self",         2, 0, "Single"),
    ("rally",       "Self",         2, 0, "Single"),
    ("engage",      "Self",         3, 0, "Single"),
    ("flank",       "Self",         4, 0, "Single"),
    ("retreat",     "Self",         2, 0, "Single"),
    ("teleport",    "Self",         3, 0, "Single"),
    ("rush",        "Self",         2, 0, "Single"),
    # Weapon meta
    ("gun",         "Self",         0, 0, "Single"),
    ("sword",       "Self",         0, 0, "Single"),
    # Ammo meta
    ("9mm",         "SingleEnemy",  0, 15, "Single"),
    ("buckshot",    "SingleEnemy",  0, 15, "Cross"),
    ("slash",       "Melee",        0, 1, "Single"),
    ("stab",        "SingleEnemy",  0, 1, "Single"),
]

# (word, tag)
SEED_TAGS = [
    ("drip", "ELEMENTAL"), ("drip", "NATURE"),
    ("splash", "ELEMENTAL"), ("splash", "NATURE"),
    ("wave", "ELEMENTAL"), ("wave", "NATURE"), ("wave", "OFFENSIVE"),
    ("stream", "ELEMENTAL"), ("stream", "NATURE"),
    ("torrent", "ELEMENTAL"), ("torrent", "NATURE"), ("torrent", "OFFENSIVE"),
    ("tsunami", "ELEMENTAL"), ("tsunami", "NATURE"), ("tsunami", "OFFENSIVE"),
    ("ocean", "ELEMENTAL"), ("ocean", "NATURE"), ("ocean", "OFFENSIVE"),
    ("flood", "ELEMENTAL"), ("flood", "NATURE"), ("flood", "OFFENSIVE"),
    ("deluge", "ELEMENTAL"), ("deluge", "NATURE"), ("deluge", "OFFENSIVE"),
    ("rain", "ELEMENTAL"), ("rain", "NATURE"),
    ("ember", "ELEMENTAL"), ("ember", "OFFENSIVE"),
    ("spark", "ELEMENTAL"), ("spark", "OFFENSIVE"),
    ("flame", "ELEMENTAL"), ("flame", "OFFENSIVE"),
    ("blaze", "ELEMENTAL"), ("blaze", "OFFENSIVE"),
    ("inferno", "ELEMENTAL"), ("inferno", "OFFENSIVE"),
    ("torch", "ELEMENTAL"), ("torch", "OFFENSIVE"),
    ("scorch", "ELEMENTAL"), ("scorch", "OFFENSIVE"),
    ("shove", "PHYSICAL"), ("shove", "OFFENSIVE"),
    ("thrust", "PHYSICAL"), ("thrust", "OFFENSIVE"),
    ("gust", "ELEMENTAL"), ("gust", "NATURE"),
    ("hurricane", "ELEMENTAL"), ("hurricane", "NATURE"), ("hurricane", "OFFENSIVE"),
    ("scratch", "PHYSICAL"), ("scratch", "OFFENSIVE"),
    ("hit", "PHYSICAL"), ("hit", "OFFENSIVE"),
    ("strike", "PHYSICAL"), ("strike", "OFFENSIVE"),
    ("smash", "PHYSICAL"), ("smash", "OFFENSIVE"),
    ("obliterate", "PHYSICAL"), ("obliterate", "OFFENSIVE"),
    ("crush", "PHYSICAL"), ("crush", "OFFENSIVE"),
    ("bandage", "RESTORATION"),
    ("mend", "RESTORATION"),
    ("restore", "RESTORATION"), ("restore", "SUPPORT"),
    ("rejuvenate", "RESTORATION"), ("rejuvenate", "SUPPORT"),
    ("resurrect", "RESTORATION"), ("resurrect", "HOLY"),
    ("glow", "HOLY"),
    ("shine", "HOLY"),
    ("radiance", "HOLY"), ("radiance", "RESTORATION"),
    ("beacon", "HOLY"), ("beacon", "SUPPORT"),
    ("flash", "HOLY"), ("flash", "OFFENSIVE"),
    ("shadow", "SHADOW"), ("shadow", "OFFENSIVE"),
    ("gloom", "SHADOW"),
    ("void", "SHADOW"), ("void", "OFFENSIVE"),
    ("eclipse", "SHADOW"), ("eclipse", "OFFENSIVE"),
    ("abyss", "SHADOW"), ("abyss", "OFFENSIVE"),
    ("absorb", "SHADOW"), ("absorb", "RESTORATION"),
    ("sprint", "PHYSICAL"), ("dash", "PHYSICAL"),
    ("charge", "PHYSICAL"), ("charge", "OFFENSIVE"),
    ("blink", "ARCANE"), ("warp", "ARCANE"),
    ("scatter", "ARCANE"), ("rally", "SUPPORT"),
    ("engage", "PHYSICAL"), ("engage", "OFFENSIVE"),
    ("flank", "PHYSICAL"), ("flank", "OFFENSIVE"),
    ("retreat", "DEFENSIVE"), ("teleport", "ARCANE"),
    ("rush", "PHYSICAL"), ("rush", "OFFENSIVE"),
]


def main():
    os.makedirs(DB_DIR, exist_ok=True)

    if os.path.exists(DB_PATH):
        os.remove(DB_PATH)

    conn = sqlite3.connect(DB_PATH)
    cur = conn.cursor()
    cur.executescript(SCHEMA)
    cur.executemany(
        "INSERT OR REPLACE INTO word_actions (word, action_name, value, target, range, area, assoc_word) VALUES (?, ?, ?, ?, ?, ?, ?)",
        SEED_ACTIONS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO word_meta (word, target, cost, range, area) VALUES (?, ?, ?, ?, ?)",
        SEED_META,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO word_tags (word, tag) VALUES (?, ?)",
        SEED_TAGS,
    )
    conn.commit()

    action_count = cur.execute("SELECT COUNT(*) FROM word_actions").fetchone()[0]
    unique_words = cur.execute("SELECT COUNT(DISTINCT word) FROM word_actions").fetchone()[0]
    meta_count = cur.execute("SELECT COUNT(*) FROM word_meta").fetchone()[0]
    tag_count = cur.execute("SELECT COUNT(*) FROM word_tags").fetchone()[0]
    conn.close()

    print(f"Created {DB_PATH}")
    print(f"  {action_count} action rows, {unique_words} unique words, {meta_count} meta, {tag_count} tags")


if __name__ == "__main__":
    main()
