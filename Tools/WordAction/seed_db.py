#!/usr/bin/env python3
"""Creates the wordactions.db SQLite database with ~50 hand-picked seed words."""

import os
import sqlite3
import subprocess
import sys

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
    area   TEXT NOT NULL DEFAULT 'Single',
    status TEXT DEFAULT NULL
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
    tier            INTEGER NOT NULL DEFAULT 1,
    dexterity       INTEGER NOT NULL DEFAULT 0,
    constitution    INTEGER NOT NULL DEFAULT 0,
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
    unit_id       TEXT NOT NULL,
    trigger_id    TEXT NOT NULL,
    trigger_param TEXT,
    effect_id     TEXT NOT NULL,
    effect_param  TEXT,
    value         INTEGER NOT NULL DEFAULT 1,
    target        TEXT NOT NULL DEFAULT 'Self',
    seq           INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (unit_id, trigger_id, effect_id, target, seq),
    FOREIGN KEY (unit_id) REFERENCES units(unit_id)
);

CREATE TABLE IF NOT EXISTS unit_tags (
    unit_id TEXT NOT NULL,
    tag     TEXT NOT NULL,
    PRIMARY KEY (unit_id, tag),
    FOREIGN KEY (unit_id) REFERENCES units(unit_id)
);
CREATE INDEX IF NOT EXISTS idx_unit_tags ON unit_tags(unit_id);

CREATE TABLE IF NOT EXISTS items (
    item_id         TEXT PRIMARY KEY,
    display_name    TEXT NOT NULL,
    item_type       TEXT NOT NULL DEFAULT 'accessory',
    durability      INTEGER NOT NULL DEFAULT 0,
    strength        INTEGER NOT NULL DEFAULT 0,
    magic_power     INTEGER NOT NULL DEFAULT 0,
    phys_defense    INTEGER NOT NULL DEFAULT 0,
    magic_defense   INTEGER NOT NULL DEFAULT 0,
    luck            INTEGER NOT NULL DEFAULT 0,
    max_health      INTEGER NOT NULL DEFAULT 0,
    max_mana        INTEGER NOT NULL DEFAULT 0,
    color_r         REAL NOT NULL DEFAULT 0.5,
    color_g         REAL NOT NULL DEFAULT 0.5,
    color_b         REAL NOT NULL DEFAULT 0.5
);

CREATE TABLE IF NOT EXISTS item_passives (
    item_id       TEXT NOT NULL,
    trigger_id    TEXT NOT NULL,
    trigger_param TEXT,
    effect_id     TEXT NOT NULL,
    effect_param  TEXT,
    value         INTEGER NOT NULL DEFAULT 1,
    target        TEXT NOT NULL DEFAULT 'Self',
    seq           INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (item_id, trigger_id, effect_id, target, seq),
    FOREIGN KEY (item_id) REFERENCES items(item_id)
);


CREATE TABLE IF NOT EXISTS event_encounters (
    encounter_id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS interactables (
    interactable_id TEXT NOT NULL,
    encounter_id    TEXT NOT NULL,
    display_name    TEXT NOT NULL,
    max_health      INTEGER NOT NULL DEFAULT 5,
    color_r         REAL NOT NULL DEFAULT 0.5,
    color_g         REAL NOT NULL DEFAULT 0.5,
    color_b         REAL NOT NULL DEFAULT 0.5,
    description     TEXT,
    slot_index      INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (encounter_id, interactable_id),
    FOREIGN KEY (encounter_id) REFERENCES event_encounters(encounter_id)
);

CREATE TABLE IF NOT EXISTS interactable_reactions (
    encounter_id    TEXT NOT NULL,
    interactable_id TEXT NOT NULL,
    action_id       TEXT NOT NULL,
    outcome_id      TEXT NOT NULL,
    outcome_param   TEXT,
    value           INTEGER NOT NULL DEFAULT 0,
    chance          REAL NOT NULL DEFAULT 1.0,
    seq             INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (encounter_id, interactable_id, action_id, outcome_id, seq),
    FOREIGN KEY (encounter_id, interactable_id) REFERENCES interactables(encounter_id, interactable_id)
);

CREATE TABLE IF NOT EXISTS processed_words (
    word TEXT PRIMARY KEY COLLATE NOCASE
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
    ("tsunami",    "Water", 5, None, None, None, "", 0),  ("tsunami",    "Damage", 5, None, None, None, "", 0),  ("tsunami",    "Push", 2, None, None, None, "", 0),  ("tsunami",    "Scramble", 1, None, None, None, "", 0),
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
    ("raise",      "Summon", 1, None, None, None, "bones", 0),
    ("screech",    "Screech", 1, "AllEnemies", None, None, "", 0),
    ("slam",       "Damage", 2, None, None, None, "", 0),
    ("pounce",     "Damage", 4, None, None, None, "", 0),
    ("hex",        "Curse", 2, None, None, None, "", 0),  ("hex", "MagicDamage", 1, None, None, None, "", 1),
    ("drain",      "MagicDamage", 2, "SingleEnemy", None, None, "", 0),  ("drain", "Heal", 2, "Self", None, None, "", 1),

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
    ("library",    "Summon", 3, "Self", None, None, "", 0),
    ("grove",      "Summon", 3, "Self", None, None, "", 0),
    ("sentinel",   "Summon", 4, "Self", None, None, "", 0),
    ("pyre",       "Summon", 3, "Self", None, None, "", 0),
    ("predator",   "Summon", 4, "Self", None, None, "", 0),

    # Duplicate-action words
    ("barrage",    "Damage", 2, None, None, None, "", 0),  ("barrage",    "Damage", 2, None, None, None, "", 1),
    ("soothe",     "Heal", 2, None, None, None, "", 0),   ("soothe",     "Heal", 1, None, None, None, "", 1),

    # Weapon words — assoc_word links weapon to ammo
    ("gun",        "Item", 4, "Self", None, None, "9mm", 0),
    ("gun",        "Item", 4, "Self", None, None, "buckshot", 0),
    ("sword",      "Item", 5, "Self", None, None, "slash", 0),
    ("sword",      "Item", 5, "Self", None, None, "stab", 0),

    # Ammo words (only usable through weapon mode)
    ("9mm",        "WeaponDamage", 3, "RandomEnemy", 15, None, "", 0),
    ("buckshot",   "WeaponDamage", 4, "RandomEnemy", 15, "Cross", "", 0),
    ("slash",      "WeaponDamage", 3, "Melee", 1, None, "", 0),
    ("stab",       "WeaponDamage", 4, "RandomEnemy", 1, None, "", 0),

    # Item words (non-weapon equipment — items with stats/passives)
    ("crown",      "Item", 1, "Self", None, None, "", 0),
    ("cloak",      "Item", 1, "Self", None, None, "", 0),
    ("ring",       "Item", 1, "Self", None, None, "", 0),
    ("amulet",     "Item", 1, "Self", None, None, "", 0),
    ("helm",       "Item", 1, "Self", None, None, "", 0),

    # Consumable words — assoc_word links consumable to ammo
    ("beer",       "Item", 3, "Self", None, None, "sip", 0),

    # Consumable ammo words (only usable through consumable mode)
    ("sip",        "Heal", 10, "Self", None, None, "", 0),
    ("sip",        "Drunk", 2, "Self", None, None, "", 1),

    # Melt words
    ("laser",      "Damage", 3, None, None, None, "", 0),  ("laser",      "Melt", 2, None, None, None, "", 0),   ("laser",      "Burn", 1, None, None, None, "", 0),
    ("forge",      "Melt", 3, None, None, None, "", 0),    ("forge",      "Fire", 2, None, None, None, "", 0),
    ("furnace",    "Melt", 4, None, None, None, "", 0),    ("furnace",    "Fire", 3, None, None, None, "", 0),
    ("smelt",      "Melt", 3, None, None, None, "", 0),    ("smelt",      "Fire", 1, None, None, None, "", 0),
    ("thaw",       "Melt", 2, None, None, None, "", 0),    ("thaw",       "Heal", 1, None, None, None, "", 0),

    # Charm words (social interaction)
    ("charm",      "Charm", 1, "SingleEnemy", None, None, "", 0),
    ("flatter",    "Charm", 1, "SingleEnemy", None, None, "", 0),
    ("persuade",   "Charm", 1, "SingleEnemy", None, None, "", 0),

    # Interaction words
    ("enter",      "Enter", 1, "SingleEnemy", None, None, "", 0),
    ("go",         "Enter", 1, "SingleEnemy", None, None, "", 0),
    ("visit",      "Enter", 1, "SingleEnemy", None, None, "", 0),
    ("talk",       "Talk", 1, "SingleEnemy", None, None, "", 0),
    ("speak",      "Talk", 1, "SingleEnemy", None, None, "", 0),
    ("greet",      "Talk", 1, "SingleEnemy", None, None, "", 0),
    ("steal",      "Steal", 1, "SingleEnemy", None, None, "", 0),
    ("swipe",      "Steal", 1, "SingleEnemy", None, None, "", 0),
    ("pilfer",     "Steal", 1, "SingleEnemy", None, None, "", 0),
    ("search",     "Search", 1, "SingleEnemy", None, None, "", 0),
    ("examine",    "Search", 1, "SingleEnemy", None, None, "", 0),
    ("inspect",    "Search", 1, "SingleEnemy", None, None, "", 0),
    ("pray",       "Pray", 1, "SingleEnemy", None, None, "", 0),
    ("worship",    "Pray", 1, "SingleEnemy", None, None, "", 0),
    ("rest",       "Rest", 1, "Self", None, None, "", 0),
    ("sleep",      "Rest", 1, "Self", None, None, "", 0),
    ("nap",        "Rest", 1, "Self", None, None, "", 0),
    ("open",       "Open", 1, "SingleEnemy", None, None, "", 0),
    ("unlock",     "Open", 1, "SingleEnemy", None, None, "", 0),
    ("trade",      "Trade", 1, "SingleEnemy", None, None, "", 0),
    ("barter",     "Trade", 1, "SingleEnemy", None, None, "", 0),
    ("recruit",    "Recruit", 1, "SingleEnemy", None, None, "", 0),
    ("hire",       "Recruit", 1, "SingleEnemy", None, None, "", 0),
    ("leave",      "Leave", 1, "SingleEnemy", None, None, "", 0),
    ("exit",       "Leave", 1, "SingleEnemy", None, None, "", 0),
    ("depart",     "Leave", 1, "SingleEnemy", None, None, "", 0),

    # Peck words (damage + bleed)
    ("peck",       "Peck", 2, "SingleEnemy", None, None, "", 0),
    ("pecks",      "Peck", 2, "SingleEnemy", None, None, "", 0),

    # Purify showcase (family words are pre-registered as drafts in the DB)
    ("purify",     "Purify", 2, "AllAlliesAndSelf", None, None, "", 0),

    # Awaken showcase (family words are pre-registered as drafts in the DB)
    ("awaken",     "Awaken", 1, "AllAlliesAndSelf", None, None, "", 0),

    # Dawning/Dawn (combo: purify + buff + awaken — premium cost)
    ("dawning",    "Purify", 2, "AllAlliesAndSelf", None, None, "", 0),
    ("dawning",    "BuffMagicDefense", 1, "Self", None, None, "", 1),
    ("dawning",    "Awaken", 1, "AllAlliesAndSelf", None, None, "", 2),
    ("dawn",       "Purify", 2, "AllAlliesAndSelf", None, None, "", 0),
    ("dawn",       "BuffMagicDefense", 1, "Self", None, None, "", 1),
    ("dawn",       "Awaken", 1, "AllAlliesAndSelf", None, None, "", 2),

    # Raven summon words (singular=1 unit, plural=2 units, higher cost)
    ("raven",      "Summon", 1, "Self", None, None, "", 0),
    ("ravens",     "Summon", 2, "Self", None, None, "", 0),
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
    ("shout",       "AreaEnemies",  2, 0, "Single"),
    ("mace",        "Melee",        0, 1, "Single"),
    ("raise",       "Self",         1, 0, "Single"),
    # New enemy ability meta
    ("screech",     "AllEnemies",   1, 0, "Single"),
    ("peck",        "SingleEnemy",  0, 0, "Single"),
    ("pecks",       "SingleEnemy",  0, 0, "Single"),
    ("purify",      "AllAlliesAndSelf", 2, 0, "Single"),
    ("awaken",      "AllAlliesAndSelf", 2, 0, "Single"),
    ("dawning",     "AllAlliesAndSelf", 4, 0, "Single"),
    ("dawn",        "AllAlliesAndSelf", 4, 0, "Single"),
    ("raven",       "Self",         5, 0, "Single"),
    ("ravens",      "Self",         10, 0, "Single"),
    ("slam",        "SingleEnemy",  0, 0, "Single"),
    ("pounce",      "SingleEnemy",  2, 0, "Single"),
    ("hex",         "SingleEnemy",  0, 0, "Single"),
    ("drain",       "SingleEnemy",  3, 0, "Single"),
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
    ("library",     "Self",         3, 0, "Single"),
    ("grove",       "Self",         2, 0, "Single"),
    ("sentinel",    "Self",         3, 0, "Single"),
    ("pyre",        "Self",         2, 0, "Single"),
    ("predator",    "Self",         3, 0, "Single"),
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
    # Item meta
    ("crown",       "Self",         0, 0, "Single"),
    ("cloak",       "Self",         0, 0, "Single"),
    ("ring",        "Self",         0, 0, "Single"),
    ("amulet",      "Self",         0, 0, "Single"),
    ("helm",        "Self",         0, 0, "Single"),
    # Consumable meta
    ("beer",        "Self",         0, 0, "Single"),
    ("sip",         "Self",         0, 0, "Single"),
    # Melt meta
    ("laser",       "SingleEnemy",  2, 3, "Single"),
    ("forge",       "SingleEnemy",  1, 2, "Single"),
    ("furnace",     "SingleEnemy",  2, 2, "Single"),
    ("smelt",       "SingleEnemy",  1, 2, "Single"),
    ("thaw",        "SingleEnemy",  0, 3, "Single"),
    # Charm meta
    ("charm",       "SingleEnemy",  0, 0, "Single"),
    ("flatter",     "SingleEnemy",  0, 0, "Single"),
    ("persuade",    "SingleEnemy",  0, 0, "Single"),
    # Interaction meta
    ("enter",       "SingleEnemy",  0, 0, "Single"),
    ("go",          "SingleEnemy",  0, 0, "Single"),
    ("visit",       "SingleEnemy",  0, 0, "Single"),
    ("talk",        "SingleEnemy",  0, 0, "Single"),
    ("speak",       "SingleEnemy",  0, 0, "Single"),
    ("greet",       "SingleEnemy",  0, 0, "Single"),
    ("steal",       "SingleEnemy",  0, 0, "Single"),
    ("swipe",       "SingleEnemy",  0, 0, "Single"),
    ("pilfer",      "SingleEnemy",  0, 0, "Single"),
    ("search",      "SingleEnemy",  0, 0, "Single"),
    ("examine",     "SingleEnemy",  0, 0, "Single"),
    ("inspect",     "SingleEnemy",  0, 0, "Single"),
    ("pray",        "SingleEnemy",  0, 0, "Single"),
    ("worship",     "SingleEnemy",  0, 0, "Single"),
    ("rest",        "Self",         0, 0, "Single"),
    ("sleep",       "Self",         0, 0, "Single"),
    ("nap",         "Self",         0, 0, "Single"),
    ("open",        "SingleEnemy",  0, 0, "Single"),
    ("unlock",      "SingleEnemy",  0, 0, "Single"),
    ("trade",       "SingleEnemy",  0, 0, "Single"),
    ("barter",      "SingleEnemy",  0, 0, "Single"),
    ("recruit",     "SingleEnemy",  0, 0, "Single"),
    ("hire",        "SingleEnemy",  0, 0, "Single"),
    ("leave",       "SingleEnemy",  0, 0, "Single"),
    ("exit",        "SingleEnemy",  0, 0, "Single"),
    ("depart",      "SingleEnemy",  0, 0, "Single"),
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
    ("shove", "PHYSICAL"), ("shove", "OFFENSIVE"), ("shove", "MELEE"),
    ("thrust", "PHYSICAL"), ("thrust", "OFFENSIVE"), ("thrust", "MELEE"),
    ("gust", "ELEMENTAL"), ("gust", "NATURE"),
    ("hurricane", "ELEMENTAL"), ("hurricane", "NATURE"), ("hurricane", "OFFENSIVE"),
    ("scratch", "PHYSICAL"), ("scratch", "OFFENSIVE"), ("scratch", "MELEE"),
    ("hit", "PHYSICAL"), ("hit", "OFFENSIVE"), ("hit", "MELEE"),
    ("strike", "PHYSICAL"), ("strike", "OFFENSIVE"), ("strike", "MELEE"),
    ("smash", "PHYSICAL"), ("smash", "OFFENSIVE"), ("smash", "MELEE"),
    ("obliterate", "PHYSICAL"), ("obliterate", "OFFENSIVE"), ("obliterate", "MELEE"),
    ("crush", "PHYSICAL"), ("crush", "OFFENSIVE"), ("crush", "MELEE"),
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
    ("charge", "PHYSICAL"), ("charge", "OFFENSIVE"), ("charge", "MELEE"),
    ("engage", "PHYSICAL"), ("engage", "OFFENSIVE"), ("engage", "MELEE"),
    ("flank", "PHYSICAL"), ("flank", "OFFENSIVE"), ("flank", "MELEE"),
    ("retreat", "DEFENSIVE"),
    ("rush", "PHYSICAL"), ("rush", "OFFENSIVE"), ("rush", "MELEE"),
    ("shout", "OFFENSIVE"), ("mace", "PHYSICAL"), ("mace", "OFFENSIVE"), ("mace", "MELEE"),
    ("raise", "SHADOW"),
    ("screech", "OFFENSIVE"), ("screech", "SHADOW"), ("screech", "BEAST"),
    ("slam", "PHYSICAL"), ("slam", "OFFENSIVE"), ("slam", "MELEE"),
    ("pounce", "PHYSICAL"), ("pounce", "OFFENSIVE"), ("pounce", "MELEE"),
    ("hex", "SHADOW"), ("hex", "OFFENSIVE"),
    ("drain", "SHADOW"), ("drain", "RESTORATION"),
    ("focus", "SUPPORT"), ("meditate", "SUPPORT"), ("channel", "ARCANE"), ("channel", "SUPPORT"),
    ("venom", "OFFENSIVE"), ("venom", "SHADOW"), ("toxin", "OFFENSIVE"), ("toxin", "SHADOW"),
    ("plague", "OFFENSIVE"), ("plague", "SHADOW"),
    ("lacerate", "PHYSICAL"), ("lacerate", "OFFENSIVE"), ("lacerate", "MELEE"),
    ("gash", "PHYSICAL"), ("gash", "OFFENSIVE"), ("gash", "MELEE"),
    ("barrage", "PHYSICAL"), ("barrage", "OFFENSIVE"), ("barrage", "MELEE"),
    ("soothe", "RESTORATION"),
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
    ("library", "SUPPORT"), ("library", "ARCANE"),
    ("grove", "NATURE"), ("grove", "RESTORATION"),
    ("sentinel", "DEFENSIVE"), ("sentinel", "SUPPORT"),
    ("pyre", "OFFENSIVE"), ("pyre", "ELEMENTAL"),
    ("predator", "OFFENSIVE"), ("predator", "SHADOW"),
    ("crown", "ARCANE"), ("crown", "SUPPORT"),
    ("cloak", "DEFENSIVE"), ("cloak", "SUPPORT"),
    ("ring", "ARCANE"), ("ring", "SUPPORT"),
    ("amulet", "ARCANE"), ("amulet", "SUPPORT"),
    ("helm", "DEFENSIVE"), ("helm", "PHYSICAL"),
    ("beer", "RESTORATION"), ("sip", "RESTORATION"),
    # Melt tags
    ("laser", "ELEMENTAL"), ("laser", "OFFENSIVE"),
    ("forge", "ELEMENTAL"), ("forge", "OFFENSIVE"),
    ("furnace", "ELEMENTAL"), ("furnace", "OFFENSIVE"),
    ("smelt", "ELEMENTAL"), ("smelt", "OFFENSIVE"),
    ("thaw", "ELEMENTAL"), ("thaw", "RESTORATION"),
    # Interaction tags
    ("enter", "SUPPORT"), ("go", "SUPPORT"), ("visit", "SUPPORT"),
    ("talk", "SUPPORT"), ("talk", "SOCIAL"), ("speak", "SUPPORT"), ("speak", "SOCIAL"), ("greet", "SUPPORT"), ("greet", "SOCIAL"),
    ("steal", "SHADOW"), ("swipe", "SHADOW"), ("pilfer", "SHADOW"),
    ("search", "SUPPORT"), ("examine", "SUPPORT"), ("inspect", "SUPPORT"),
    ("pray", "HOLY"), ("worship", "HOLY"),
    ("rest", "RESTORATION"), ("sleep", "RESTORATION"), ("nap", "RESTORATION"),
    ("open", "SUPPORT"), ("unlock", "SUPPORT"),
    ("trade", "SUPPORT"), ("trade", "SOCIAL"), ("barter", "SUPPORT"), ("barter", "SOCIAL"),
    ("recruit", "SUPPORT"), ("hire", "SUPPORT"),
    ("leave", "SUPPORT"), ("exit", "SUPPORT"), ("depart", "SUPPORT"),
    # Charm tags
    ("charm", "SUPPORT"), ("charm", "SOCIAL"),
    ("flatter", "SUPPORT"), ("flatter", "SOCIAL"),
    ("persuade", "SUPPORT"), ("persuade", "SOCIAL"),
    # Purify showcase tags (family words are pre-registered as drafts in the DB)
    ("purify", "HOLY"), ("purify", "RESTORATION"), ("purify", "SUPPORT"), ("purify", "CLEANSING"),
    # Awaken showcase tags (family words are pre-registered as drafts in the DB)
    ("awaken", "HOLY"), ("awaken", "RESTORATION"), ("awaken", "SUPPORT"), ("awaken", "LIGHT"),
    # Dawning/Dawn tags
    ("dawning", "HOLY"), ("dawning", "RESTORATION"), ("dawning", "SUPPORT"), ("dawning", "DEFENSIVE"), ("dawning", "LIGHT"), ("dawning", "CLEANSING"),
    ("dawn", "HOLY"), ("dawn", "RESTORATION"), ("dawn", "SUPPORT"), ("dawn", "DEFENSIVE"), ("dawn", "LIGHT"), ("dawn", "CLEANSING"),
    # Peck tags
    ("peck", "PHYSICAL"), ("peck", "OFFENSIVE"), ("peck", "MELEE"), ("peck", "BEAST"),
    ("pecks", "PHYSICAL"), ("pecks", "OFFENSIVE"), ("pecks", "MELEE"), ("pecks", "BEAST"),
    # Raven tags
    ("raven", "SHADOW"), ("raven", "NATURE"), ("raven", "MELEE"), ("raven", "BEAST"), ("raven", "FLYING"),
    ("ravens", "SHADOW"), ("ravens", "NATURE"), ("ravens", "MELEE"), ("ravens", "BEAST"), ("ravens", "FLYING"),
]

# (unit_id, display_name, unit_type, max_health, strength, magic_power, phys_defense, magic_defense, luck, starting_shield, color_r, color_g, color_b, tier, dexterity, constitution)
SEED_UNITS = [
    # Tier 1 — Early Game
    ("bat",      "BAT",      "enemy",     6,  3, 0, 1, 1, 3, 0, 0.6, 0.3, 0.8, 1, 0, 0),
    ("goblin",   "GOBLIN",   "enemy",     10, 4, 0, 2, 1, 2, 0, 0.2, 0.8, 0.2, 1, 0, 0),
    ("orc",      "ORC",      "enemy",     14, 6, 0, 3, 1, 1, 0, 0.0, 1.0, 0.0, 1, 0, 0),
    ("skeleton", "SKELETON", "enemy",     8,  4, 2, 3, 2, 1, 0, 0.9, 0.9, 0.8, 1, 0, 0),
    # Tier 2 — Mid/Late Game
    ("golem",    "GOLEM",    "enemy",     25, 5, 0, 6, 3, 0, 5, 0.5, 0.5, 0.6, 2, 0, 0),
    ("predator", "PREDATOR", "enemy",     12, 8, 0, 2, 1, 3, 0, 0.5, 0.1, 0.1, 2, 0, 0),
    ("wraith",   "WRAITH",   "enemy",     10, 0, 7, 1, 5, 2, 0, 0.4, 0.2, 0.6, 2, 0, 0),
    ("shaman",   "SHAMAN",   "enemy",     15, 2, 6, 2, 4, 2, 0, 0.5, 0.3, 0.7, 2, 0, 0),
    # Summons (tier 0)
    ("raven",    "RAVEN",    "enemy",     12, 4, 2, 1, 2, 3, 0, 0.2, 0.2, 0.3, 1, 0, 0),
    ("bones",    "BONES",    "enemy",     1,  1, 0, 0, 0, 0, 0, 0.9, 0.9, 0.8, 0, 0, 0),
    # Structures (tier 0)
    ("fortress", "FORTRESS", "structure", 20, 0, 0, 5, 3, 0, 5, 0.4, 0.4, 0.5, 0, 0, 0),
    ("totem",    "TOTEM",    "structure", 12, 0, 3, 1, 1, 0, 0, 0.6, 0.4, 0.2, 0, 0, 0),
    ("turret",   "TURRET",   "structure", 10, 4, 0, 2, 1, 0, 0, 0.5, 0.5, 0.5, 0, 0, 0),
    ("library",  "LIBRARY",  "structure", 10, 0, 4, 2, 3, 0, 0, 0.6, 0.5, 0.3, 0, 0, 0),
    ("grove",    "GROVE",    "structure", 12, 0, 3, 1, 2, 0, 0, 0.2, 0.7, 0.2, 0, 0, 0),
    ("sentinel", "SENTINEL", "structure", 15, 0, 0, 5, 3, 0, 4, 0.3, 0.3, 0.5, 0, 0, 0),
    ("pyre",     "PYRE",     "structure", 8,  0, 5, 1, 1, 0, 0, 0.9, 0.3, 0.1, 0, 0, 0),
]

# (unit_id, word)
SEED_UNIT_ABILITIES = [
    # Tier 1
    ("bat", "scratch"), ("bat", "screech"),
    ("goblin", "scratch"), ("goblin", "venom"),
    ("orc", "mace"), ("orc", "shout"),
    ("skeleton", "scratch"), ("skeleton", "raise"),
    ("raven", "peck"), ("raven", "screech"),
    # Tier 2
    ("golem", "slam"), ("golem", "smash"),
    ("predator", "scratch"), ("predator", "pounce"),
    ("wraith", "hex"), ("wraith", "drain"),
    ("shaman", "spark"), ("shaman", "plague"),
    # Structures
    ("turret", "hit"),
]

# (unit_id, trigger_id, trigger_param, effect_id, effect_param, value, target, seq)
SEED_UNIT_PASSIVES = [
    ("fortress", "on_ally_hit",    None, "heal",         None,      1, "Injured",    0),
    ("totem",    "on_round_end",  None, "heal",         None,      2, "AllAllies",  0),
    ("turret",   "on_ally_hit",   None, "damage",       None,      2, "Attacker",   0),
    ("library",  "on_word_length", "6", "heal",         None,      2, "AllAllies",  0),
    ("library",  "on_word_length", "8", "shield",       None,      1, "AllAllies",  1),
    ("grove",    "on_word_tag",  "NATURE", "heal",      None,      2, "AllAllies",  0),
    ("sentinel", "on_ally_hit",  None, "shield",        None,      2, "Injured",    0),
    ("sentinel", "taunt",        None, "",              None,      0, "Self",        1),
    ("pyre",     "on_round_start", None, "apply_status", "Burning", 2, "AllEnemies", 0),
    ("predator", "on_kill",      None, "heal",          None,      5, "Self",        0),
    ("raven",    "on_ally_hit",  None, "damage",        None,      1, "Attacker",    0),
]

# (unit_id, tag) — material/property tags for tag-based reactions
SEED_UNIT_TAGS = [
    ("golem", "conductive"),
    ("golem", "meltable"),
    ("fortress", "breakable"),
    ("turret", "conductive"),
    ("pyre", "flammable"),
    ("grove", "flammable"),
]

# (item_id, display_name, item_type, durability, strength, magic_power, phys_defense, magic_defense, luck, max_health, max_mana, color_r, color_g, color_b)
SEED_ITEMS = [
    ("gun",    "GUN",    "weapon",    4, 0, 0, 0, 0, 0, 0, 0,  0.5, 0.5, 0.5),
    ("sword",  "SWORD",  "weapon",    5, 0, 0, 0, 0, 0, 0, 0,  0.7, 0.7, 0.7),
    ("crown",  "CROWN",  "head",      0, 0, 2, 0, 0, 1, 0, 0,  1.0, 0.84, 0.0),
    ("cloak",  "CLOAK",  "wear",      0, 0, 0, 2, 1, 0, 0, 0,  0.3, 0.2, 0.5),
    ("ring",   "RING",   "accessory", 0, 1, 0, 0, 0, 1, 0, 0,  0.9, 0.75, 0.0),
    ("amulet", "AMULET", "accessory", 0, 0, 1, 0, 0, 0, 0, 0,  0.4, 0.8, 0.4),
    ("helm",   "HELM",   "head",      0, 0, 0, 3, 0, 0, 0, 0,  0.6, 0.6, 0.6),
    ("beer",   "BEER",   "consumable", 3, 0, 0, 0, 0, 0, 0, 0,  1.0, 0.85, 0.2),
]

# (item_id, trigger_id, trigger_param, effect_id, effect_param, value, target, seq)
SEED_ITEM_PASSIVES = [
    ("amulet", "on_word_played", None, "mana", None, 1, "Self", 0),
    ("helm",   "on_self_hit",    None, "shield", None, 1, "Self", 0),
]

# (encounter_id, display_name)
SEED_EVENT_ENCOUNTERS = [
    ("roadside_inn", "Roadside Inn"),
    ("inn_interior", "Inn Interior"),
]

# (interactable_id, encounter_id, display_name, max_health, color_r, color_g, color_b, description, slot_index)
SEED_INTERACTABLES = [
    ("inn",    "roadside_inn", "INN",    5,  0.8, 0.6, 0.2, "A cozy roadside inn",       0),
    ("chest",  "roadside_inn", "CHEST",  3,  0.6, 0.4, 0.1, "A dusty wooden chest",      1),
    ("shrine", "roadside_inn", "SHRINE", 10, 0.4, 0.6, 0.9, "An ancient stone shrine",   2),
    ("barkeep","inn_interior", "BARKEEP", 5, 0.8, 0.5, 0.3, "The innkeeper",             0),
    ("bed",    "inn_interior", "BED",     1, 0.6, 0.5, 0.4, "A comfortable bed",         1),
]

# (encounter_id, interactable_id, action_id, outcome_id, outcome_param, value, chance, seq)
SEED_INTERACTABLE_REACTIONS = [
    ("roadside_inn", "inn",    "Enter",  "transition", "inn_interior",              0, 1.0, 0),
    ("roadside_inn", "inn",    "Talk",   "message",    "The innkeeper nods warmly.", 0, 1.0, 0),
    ("roadside_inn", "chest",  "Open",   "reward",     "gold",                     25, 1.0, 0),
    ("roadside_inn", "chest",  "Open",   "consume",    None,                        0, 1.0, 1),
    ("roadside_inn", "chest",  "Steal",  "reward",     "gold",                     10, 0.5, 0),
    ("roadside_inn", "shrine", "Pray",   "heal",       None,                       20, 1.0, 0),
    ("roadside_inn", "shrine", "Pray",   "mana",       None,                       10, 1.0, 1),
    ("roadside_inn", "shrine", "Pray",   "message",    "The shrine glows softly.",   0, 1.0, 2),
    ("inn_interior", "barkeep","Talk",   "message",    "Welcome to my inn!",         0, 1.0, 0),
    ("inn_interior", "barkeep","Trade",  "message",    "I have rooms and drinks.",    0, 1.0, 0),
    ("inn_interior", "bed",    "Rest",   "heal",       None,                       30, 1.0, 0),
    ("inn_interior", "bed",    "Rest",   "mana",       None,                       15, 1.0, 1),
    ("inn_interior", "bed",    "Rest",   "message",    "You feel well rested.",      0, 1.0, 2),
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
        "INSERT OR REPLACE INTO units (unit_id, display_name, unit_type, max_health, strength, magic_power, phys_defense, magic_defense, luck, starting_shield, color_r, color_g, color_b, tier, dexterity, constitution) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_UNITS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO unit_abilities (unit_id, word) VALUES (?, ?)",
        SEED_UNIT_ABILITIES,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO unit_passives (unit_id, trigger_id, trigger_param, effect_id, effect_param, value, target, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_UNIT_PASSIVES,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO unit_tags (unit_id, tag) VALUES (?, ?)",
        SEED_UNIT_TAGS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO items (item_id, display_name, item_type, durability, "
        "strength, magic_power, phys_defense, magic_defense, luck, max_health, max_mana, "
        "color_r, color_g, color_b) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_ITEMS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO item_passives (item_id, trigger_id, trigger_param, "
        "effect_id, effect_param, value, target, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_ITEM_PASSIVES,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO event_encounters (encounter_id, display_name) VALUES (?, ?)",
        SEED_EVENT_ENCOUNTERS,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO interactables (interactable_id, encounter_id, display_name, "
        "max_health, color_r, color_g, color_b, description, slot_index) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_INTERACTABLES,
    )
    cur.executemany(
        "INSERT OR REPLACE INTO interactable_reactions (encounter_id, interactable_id, action_id, "
        "outcome_id, outcome_param, value, chance, seq) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
        SEED_INTERACTABLE_REACTIONS,
    )
    conn.commit()

    action_count = cur.execute("SELECT COUNT(*) FROM word_actions").fetchone()[0]
    unique_words = cur.execute("SELECT COUNT(DISTINCT word) FROM word_actions").fetchone()[0]
    meta_count = cur.execute("SELECT COUNT(*) FROM word_meta").fetchone()[0]
    tag_count = cur.execute("SELECT COUNT(*) FROM word_tags").fetchone()[0]
    unit_count = cur.execute("SELECT COUNT(*) FROM units").fetchone()[0]
    ability_count = cur.execute("SELECT COUNT(*) FROM unit_abilities").fetchone()[0]
    passive_count = cur.execute("SELECT COUNT(*) FROM unit_passives").fetchone()[0]
    unit_tag_count = cur.execute("SELECT COUNT(*) FROM unit_tags").fetchone()[0]
    item_count = cur.execute("SELECT COUNT(*) FROM items").fetchone()[0]
    item_passive_count = cur.execute("SELECT COUNT(*) FROM item_passives").fetchone()[0]
    encounter_count = cur.execute("SELECT COUNT(*) FROM event_encounters").fetchone()[0]
    interactable_count = cur.execute("SELECT COUNT(*) FROM interactables").fetchone()[0]
    reaction_count = cur.execute("SELECT COUNT(*) FROM interactable_reactions").fetchone()[0]
    conn.close()

    print(f"Created {DB_PATH}")
    print(f"  {action_count} action rows, {unique_words} unique words, {meta_count} meta, {tag_count} tags")
    print(f"  {unit_count} units, {ability_count} unit abilities, {passive_count} unit passives, {unit_tag_count} unit tags")
    print(f"  {item_count} items, {item_passive_count} item passives")
    print(f"  {encounter_count} event encounters, {interactable_count} interactables, {reaction_count} reactions")

    # Layer draft word families on top of seeds
    families_script = os.path.join(os.path.dirname(__file__), "preregister_families.py")
    if os.path.exists(families_script):
        subprocess.run([sys.executable, families_script], check=True)


if __name__ == "__main__":
    main()
