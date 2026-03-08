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
    seq         INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (word, action_name, seq, assoc_word)
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

CREATE TABLE IF NOT EXISTS units (
    unit_id         TEXT PRIMARY KEY,
    display_name    TEXT NOT NULL,
    unit_type       TEXT NOT NULL DEFAULT 'enemy',
    max_health      INTEGER NOT NULL,
    strength        INTEGER NOT NULL DEFAULT 0,
    magic_power     INTEGER NOT NULL DEFAULT 0,
    phys_defense    INTEGER NOT NULL DEFAULT 0,
    magic_defense   INTEGER NOT NULL DEFAULT 0,
    luck            INTEGER NOT NULL DEFAULT 0,
    starting_shield INTEGER NOT NULL DEFAULT 0,
    color_r         REAL NOT NULL DEFAULT 0.5,
    color_g         REAL NOT NULL DEFAULT 0.5,
    color_b         REAL NOT NULL DEFAULT 0.5
);

CREATE TABLE IF NOT EXISTS unit_abilities (
    unit_id TEXT NOT NULL,
    word    TEXT NOT NULL,
    PRIMARY KEY (unit_id, word),
    FOREIGN KEY (unit_id) REFERENCES units(unit_id)
);

CREATE TABLE IF NOT EXISTS unit_passives (
    unit_id    TEXT NOT NULL,
    passive_id TEXT NOT NULL,
    value      INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY (unit_id, passive_id),
    FOREIGN KEY (unit_id) REFERENCES units(unit_id)
);
"""

# (word, action_name, value, target, range, area, assoc_word, seq)
SEED_ACTIONS = [
    # Water words
    ("drip",       "Water", 1, None, None, None, "", 0),
    ("splash",     "Water", 2, None, None, None, "", 0),  ("splash",     "Damage", 1, None, None, None, "", 0),
    ("wave",       "Water", 3, None, None, None, "", 0),  ("wave",       "Push", 2, None, None, None, "", 0),  ("wave",       "Damage", 2, None, None, None, "", 0),
    ("stream",     "Water", 2, None, None, None, "", 0),  ("stream",     "Damage", 1, None, None, None, "", 0),
    ("torrent",    "Water", 4, None, None, None, "", 0),  ("torrent",    "Damage", 3, None, None, None, "", 0),  ("torrent",    "Push", 2, None, None, None, "", 0),
    ("tsunami",    "Water", 5, None, None, None, "", 0),  ("tsunami",    "Damage", 5, None, None, None, "", 0),  ("tsunami",    "Push", 2, None, None, None, "", 0),
    ("ocean",      "Water", 3, None, None, None, "", 0),  ("ocean",      "Damage", 2, None, None, None, "", 0),
    ("flood",      "Water", 4, None, None, None, "", 0),  ("flood",      "Damage", 3, None, None, None, "", 0),
    ("deluge",     "Water", 5, None, None, None, "", 0),  ("deluge",     "Damage", 4, None, None, None, "", 0),
    ("rain",       "Water", 1, None, None, None, "", 0),  ("rain",       "Slow", 1, None, None, None, "", 0),

    # Fire words
    ("ember",      "Fire", 1, None, None, None, "", 0),   ("ember",      "Damage", 1, None, None, None, "", 0),
    ("spark",      "Fire", 2, None, None, None, "", 0),   ("spark",      "Light", 1, None, None, None, "", 0),  ("spark",      "Damage", 1, None, None, None, "", 0),
    ("flame",      "Fire", 3, None, None, None, "", 0),   ("flame",      "Burn", 2, None, None, None, "", 0),   ("flame",      "Damage", 2, None, None, None, "", 0),
    ("blaze",      "Fire", 4, None, None, None, "", 0),   ("blaze",      "Burn", 3, None, None, None, "", 0),   ("blaze",      "Damage", 3, None, None, None, "", 0),
    ("inferno",    "Fire", 5, None, None, None, "", 0),   ("inferno",    "Burn", 4, None, None, None, "", 0),   ("inferno",    "Damage", 4, None, None, None, "", 0),  ("inferno", "Light", 2, None, None, None, "", 0),
    ("torch",      "Fire", 2, None, None, None, "", 0),   ("torch",      "Burn", 1, None, None, None, "", 0),   ("torch",      "Damage", 1, None, None, None, "", 0),
    ("scorch",     "Fire", 3, None, None, None, "", 0),   ("scorch",     "Burn", 3, None, None, None, "", 0),   ("scorch",     "Damage", 2, None, None, None, "", 0),

    # Push / Wind words
    ("shove",      "Push", 2, None, None, None, "", 0),   ("shove",      "Damage", 1, None, None, None, "", 0),
    ("thrust",     "Push", 3, None, None, None, "", 0),   ("thrust",     "Damage", 2, None, None, None, "", 0),
    ("gust",       "Wind", 2, None, None, None, "", 0),   ("gust",       "Push", 2, None, None, None, "", 0),
    ("hurricane",  "Wind", 5, None, None, None, "", 0),   ("hurricane",  "Push", 4, None, None, None, "", 0),   ("hurricane",  "Damage", 4, None, None, None, "", 0),

    # Physical words
    ("scratch",    "Damage", 1, None, None, None, "", 0),
    ("hit",        "Damage", 2, None, None, None, "", 0),
    ("strike",     "Damage", 3, None, None, None, "", 0), ("strike",     "Shock", 1, None, None, None, "", 0),
    ("smash",      "Damage", 4, None, None, None, "", 0), ("smash",      "Heavy", 2, None, None, None, "", 0),
    ("obliterate", "Damage", 5, None, None, None, "", 0), ("obliterate", "Heavy", 3, None, None, None, "", 0),
    ("crush",      "Damage", 3, None, None, None, "", 0), ("crush",      "Heavy", 3, None, None, None, "", 0),

    # Healing words
    ("bandage",    "Heal", 2, None, None, None, "", 0),
    ("mend",       "Heal", 3, None, None, None, "", 0),
    ("restore",    "Heal", 4, None, None, None, "", 0),   ("restore",    "BuffStrength", 1, None, None, None, "", 0),
    ("rejuvenate", "Heal", 5, None, None, None, "", 0),   ("rejuvenate", "BuffStrength", 2, None, None, None, "", 0),
    ("resurrect",  "Heal", 5, None, None, None, "", 0),   ("resurrect",  "Light", 2, None, None, None, "", 0),  ("resurrect",  "BuffStrength", 3, None, None, None, "", 0),

    # Light words
    ("glow",       "Light", 1, None, None, None, "", 0),
    ("shine",      "Light", 2, None, None, None, "", 0),
    ("radiance",   "Light", 3, None, None, None, "", 0),  ("radiance",   "Heal", 1, None, None, None, "", 0),
    ("beacon",     "Light", 2, None, None, None, "", 0),  ("beacon",     "BuffLuck", 1, None, None, None, "", 0),
    ("flash",      "Light", 3, None, None, None, "", 0),  ("flash",      "Shock", 2, None, None, None, "", 0),  ("flash",      "Damage", 1, None, None, None, "", 0),

    # Dark words
    ("shadow",     "Dark", 2, None, None, None, "", 0),   ("shadow",     "Curse", 1, None, None, None, "", 0),
    ("gloom",      "Dark", 1, None, None, None, "", 0),   ("gloom",      "Slow", 1, None, None, None, "", 0),
    ("void",       "Dark", 4, None, None, None, "", 0),   ("void",       "Damage", 3, None, None, None, "", 0),
    ("eclipse",    "Dark", 3, None, None, None, "", 0),   ("eclipse",    "Curse", 2, None, None, None, "", 0),
    ("abyss",      "Dark", 5, None, None, None, "", 0),   ("abyss",      "Damage", 3, None, None, None, "", 0),

    # Per-action targeting example
    ("absorb",     "Damage", 1, "SingleEnemy", 3, "Single", "", 0),  ("absorb", "Heal", 1, "Self", 0, "Single", "", 0),

    # Former movement words (movement removed, combat actions retained)
    ("charge",     "Damage", 3, None, None, None, "", 0),
    ("engage",     "Damage", 2, None, None, None, "", 0),
    ("flank",      "Damage", 4, None, None, None, "", 0),
    ("retreat",    "Shield", 2, None, None, None, "", 0),
    ("rush",       "Damage", 5, None, None, None, "", 0),

    # Enemy ability words
    ("shout",      "Fear", 2, None, None, None, "", 0),
    ("mace",       "Damage", 2, None, None, None, "", 0),
    ("raise",      "Summon", 3, None, None, None, "", 0),

    # Concentrate words
    ("focus",      "Concentrate", 2, None, None, None, "", 0),
    ("meditate",   "Concentrate", 3, None, None, None, "", 0),
    ("channel",    "Concentrate", 2, None, None, None, "", 0),  ("channel",    "BuffMagicPower", 1, None, None, None, "", 0),

    # Poison words
    ("venom",      "Poison", 3, None, None, None, "", 0),  ("venom",      "Damage", 1, None, None, None, "", 0),
    ("toxin",      "Poison", 2, None, None, None, "", 0),
    ("plague",     "Poison", 4, None, None, None, "", 0),  ("plague",     "Damage", 2, None, None, None, "", 0),

    # Bleed words
    ("lacerate",   "Bleed", 3, None, None, None, "", 0),  ("lacerate",   "Damage", 2, None, None, None, "", 0),
    ("gash",       "Bleed", 2, None, None, None, "", 0),  ("gash",       "Damage", 2, None, None, None, "", 0),

    # Grow words
    ("flourish",   "Grow", 3, "Self", None, None, "", 0),
    ("bloom",      "Grow", 2, "Self", None, None, "", 0),  ("bloom",      "Heal", 1, "Self", None, None, "", 0),
    ("regenerate", "Grow", 4, "Self", None, None, "", 0),

    # Thorns words
    ("thorns",     "Thorns", 3, "Self", None, None, "", 0),
    ("bramble",    "Thorns", 2, "Self", None, None, "", 0),  ("bramble",    "Damage", 1, None, None, None, "", 0),
    ("barbs",      "Thorns", 4, "Self", None, None, "", 0),

    # Reflect words
    ("reflect",    "Reflect", 2, "Self", None, None, "", 0),
    ("mirror",     "Reflect", 3, "Self", None, None, "", 0),
    ("deflect",    "Reflect", 1, "Self", None, None, "", 0),  ("deflect",    "Shield", 2, "Self", None, None, "", 0),

    # Hardening words
    ("harden",     "Hardening", 3, "Self", None, None, "", 0),
    ("fortify",    "Hardening", 4, "Self", None, None, "", 0),  ("fortify",    "Shield", 1, "Self", None, None, "", 0),
    ("toughen",    "Hardening", 2, "Self", None, None, "", 0),

    # Structure summon words
    ("fortress",   "Summon", 5, "Self", None, None, "", 0),
    ("totem",      "Summon", 3, "Self", None, None, "", 0),
    ("turret",     "Summon", 3, "Self", None, None, "", 0),

    # Duplicate-action words
    ("barrage",    "Damage", 2, None, None, None, "", 0),  ("barrage",    "Damage", 2, None, None, None, "", 1),
    ("soothe",     "Heal", 2, None, None, None, "", 0),   ("soothe",     "Heal", 1, None, None, None, "", 1),

    # Weapon words — assoc_word links weapon to ammo
    ("gun",        "Weapon", 4, "Self", None, None, "9mm", 0),
    ("gun",        "Weapon", 4, "Self", None, None, "buckshot", 0),
    ("sword",      "Weapon", 5, "Self", None, None, "slash", 0),
    ("sword",      "Weapon", 5, "Self", None, None, "stab", 0),

    # Ammo words (only usable through weapon mode)
    ("9mm",        "Damage", 3, "RandomEnemy", 15, None, "", 0),
    ("buckshot",   "Damage", 4, "RandomEnemy", 15, "Cross", "", 0),
    ("slash",      "Damage", 3, "Melee", 1, None, "", 0),
    ("stab",       "Damage", 4, "RandomEnemy", 1, None, "", 0),
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
    ("charge",      "SingleEnemy",  3, 0, "Single"),
    ("engage",      "SingleEnemy",  3, 0, "Single"),
    ("flank",       "SingleEnemy",  4, 0, "Single"),
    ("retreat",     "Self",         2, 0, "Single"),
    ("rush",        "SingleEnemy",  2, 0, "Single"),
    # Enemy ability meta
    ("shout",       "AreaEnemies",  0, 0, "Single"),
    ("mace",        "Melee",        0, 1, "Single"),
    ("raise",       "Self",         2, 0, "Single"),
    # Concentrate meta
    ("focus",       "Self",         0, 0, "Single"),
    ("meditate",    "Self",         1, 0, "Single"),
    ("channel",     "Self",         1, 0, "Single"),
    # Poison meta
    ("venom",       "SingleEnemy",  1, 3, "Single"),
    ("toxin",       "SingleEnemy",  0, 3, "Single"),
    ("plague",      "AreaEnemies",  3, 0, "Single"),
    # Bleed meta
    ("lacerate",    "SingleEnemy",  1, 1, "Single"),
    ("gash",        "Melee",        0, 1, "Single"),
    # Grow meta
    ("flourish",    "Self",         1, 0, "Single"),
    ("bloom",       "Self",         0, 0, "Single"),
    ("regenerate",  "Self",         2, 0, "Single"),
    # Thorns meta
    ("thorns",      "Self",         1, 0, "Single"),
    ("bramble",     "Self",         1, 0, "Single"),
    ("barbs",       "Self",         2, 0, "Single"),
    # Reflect meta
    ("reflect",     "Self",         1, 0, "Single"),
    ("mirror",      "Self",         2, 0, "Single"),
    ("deflect",     "Self",         1, 0, "Single"),
    # Hardening meta
    ("harden",      "Self",         1, 0, "Single"),
    ("fortify",     "Self",         2, 0, "Single"),
    ("toughen",     "Self",         0, 0, "Single"),
    # Structure summon meta
    ("fortress",    "Self",         3, 0, "Single"),
    ("totem",       "Self",         2, 0, "Single"),
    ("turret",      "Self",         2, 0, "Single"),
    # Duplicate-action meta
    ("barrage",     "SingleEnemy",  2, 3, "Single"),
    ("soothe",      "Self",         1, 0, "Single"),
    # Weapon meta
    ("gun",         "Self",         0, 0, "Single"),
    ("sword",       "Self",         0, 0, "Single"),
    # Ammo meta
    ("9mm",         "RandomEnemy",  0, 15, "Single"),
    ("buckshot",    "RandomEnemy",  0, 15, "Cross"),
    ("slash",       "Melee",        0, 1, "Single"),
    ("stab",        "RandomEnemy",  0, 1, "Single"),
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
    ("charge", "PHYSICAL"), ("charge", "OFFENSIVE"),
    ("engage", "PHYSICAL"), ("engage", "OFFENSIVE"),
    ("flank", "PHYSICAL"), ("flank", "OFFENSIVE"),
    ("retreat", "DEFENSIVE"),
    ("rush", "PHYSICAL"), ("rush", "OFFENSIVE"),
    ("shout", "OFFENSIVE"), ("mace", "PHYSICAL"), ("mace", "OFFENSIVE"),
    ("raise", "SHADOW"),
    ("focus", "SUPPORT"), ("meditate", "SUPPORT"), ("channel", "ARCANE"), ("channel", "SUPPORT"),
    ("venom", "OFFENSIVE"), ("venom", "SHADOW"), ("toxin", "OFFENSIVE"), ("toxin", "SHADOW"),
    ("plague", "OFFENSIVE"), ("plague", "SHADOW"),
    ("lacerate", "PHYSICAL"), ("lacerate", "OFFENSIVE"), ("gash", "PHYSICAL"), ("gash", "OFFENSIVE"),
    ("barrage", "PHYSICAL"), ("barrage", "OFFENSIVE"), ("soothe", "RESTORATION"),
    ("flourish", "NATURE"), ("flourish", "RESTORATION"),
    ("bloom", "NATURE"), ("bloom", "RESTORATION"),
    ("regenerate", "NATURE"), ("regenerate", "RESTORATION"),
    ("thorns", "NATURE"), ("thorns", "DEFENSIVE"),
    ("bramble", "NATURE"), ("bramble", "DEFENSIVE"), ("bramble", "OFFENSIVE"),
    ("barbs", "NATURE"), ("barbs", "DEFENSIVE"),
    ("reflect", "ARCANE"), ("reflect", "DEFENSIVE"),
    ("mirror", "ARCANE"), ("mirror", "DEFENSIVE"),
    ("deflect", "DEFENSIVE"), ("deflect", "SUPPORT"),
    ("harden", "DEFENSIVE"), ("harden", "PHYSICAL"),
    ("fortify", "DEFENSIVE"), ("fortify", "SUPPORT"),
    ("toughen", "DEFENSIVE"), ("toughen", "PHYSICAL"),
    ("fortress", "DEFENSIVE"), ("fortress", "SUPPORT"),
    ("totem", "SUPPORT"), ("totem", "RESTORATION"),
    ("turret", "OFFENSIVE"), ("turret", "PHYSICAL"),
]

# (unit_id, display_name, unit_type, max_health, strength, magic_power, phys_defense, magic_defense, luck, starting_shield, color_r, color_g, color_b)
SEED_UNITS = [
    ("orc",      "ORC",      "enemy", 20, 8, 0, 4, 2, 1, 0, 0.0, 1.0, 0.0),
    ("goblin",   "GOBLIN",   "enemy", 50, 6, 0, 4, 2, 2, 0, 0.2, 0.8, 0.2),
    ("skeleton", "SKELETON", "enemy", 50, 8, 0, 5, 3, 2, 0, 0.9, 0.9, 0.8),
    ("bat",      "BAT",      "enemy", 30, 4, 0, 3, 2, 3, 0, 0.6, 0.3, 0.8),
    ("golem",    "GOLEM",    "enemy", 60, 5, 0, 8, 4, 0, 10, 0.5, 0.5, 0.6),
    ("fortress", "FORTRESS", "structure", 40, 0, 0, 8, 4, 0, 10, 0.4, 0.4, 0.5),
    ("totem",    "TOTEM",    "structure", 25, 0, 3, 2, 2, 0, 0,  0.6, 0.4, 0.2),
    ("turret",   "TURRET",   "structure", 20, 6, 0, 4, 2, 0, 0,  0.5, 0.5, 0.5),
]

# (unit_id, word)
SEED_UNIT_ABILITIES = [
    ("orc", "shout"), ("orc", "mace"),
    ("goblin", "scratch"), ("goblin", "hit"),
    ("skeleton", "slash"), ("skeleton", "strike"), ("skeleton", "charge"), ("skeleton", "raise"),
    ("bat", "scratch"),
    ("golem", "smash"), ("golem", "crush"),
    ("turret", "hit"),
]

# (unit_id, passive_id, value)
SEED_UNIT_PASSIVES = [
    ("fortress", "heal_on_ally_hit", 1),
    ("totem",    "heal_on_round_end", 2),
    ("turret",   "damage_on_ally_hit", 2),
]


def main():
    os.makedirs(DB_DIR, exist_ok=True)

    if os.path.exists(DB_PATH):
        os.remove(DB_PATH)

    conn = sqlite3.connect(DB_PATH)
    cur = conn.cursor()
    cur.executescript(SCHEMA)
    cur.executemany(
        "INSERT OR REPLACE INTO word_actions (word, action_name, value, target, range, area, assoc_word, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
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
    cur.executemany(
        "INSERT OR REPLACE INTO units (unit_id, display_name, unit_type, max_health, strength, magic_power, phys_defense, magic_defense, luck, starting_shield, color_r, color_g, color_b) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_UNITS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO unit_abilities (unit_id, word) VALUES (?, ?)",
        SEED_UNIT_ABILITIES,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO unit_passives (unit_id, passive_id, value) VALUES (?, ?, ?)",
        SEED_UNIT_PASSIVES,
    )
    conn.commit()

    action_count = cur.execute("SELECT COUNT(*) FROM word_actions").fetchone()[0]
    unique_words = cur.execute("SELECT COUNT(DISTINCT word) FROM word_actions").fetchone()[0]
    meta_count = cur.execute("SELECT COUNT(*) FROM word_meta").fetchone()[0]
    tag_count = cur.execute("SELECT COUNT(*) FROM word_tags").fetchone()[0]
    unit_count = cur.execute("SELECT COUNT(*) FROM units").fetchone()[0]
    ability_count = cur.execute("SELECT COUNT(*) FROM unit_abilities").fetchone()[0]
    passive_count = cur.execute("SELECT COUNT(*) FROM unit_passives").fetchone()[0]
    conn.close()

    print(f"Created {DB_PATH}")
    print(f"  {action_count} action rows, {unique_words} unique words, {meta_count} meta, {tag_count} tags")
    print(f"  {unit_count} units, {ability_count} unit abilities, {passive_count} unit passives")


if __name__ == "__main__":
    main()
