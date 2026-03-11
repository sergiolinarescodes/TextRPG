#!/usr/bin/env python3
"""Pre-registers word family entries as drafts in the DB.

These are words identified during Word Workshop sessions as likely matches
for new mechanics. They are marked status='draft' so ralph-loop knows they
need refinement. When ralph-loop re-classifies a draft word, it overwrites
the entry with status=NULL (refined).

Run after seed_db.py to layer draft words on top of seeds.
"""

import json
import os
import subprocess
import sys

SCRIPT_DIR = os.path.dirname(__file__)
BATCH_INSERT = os.path.join(SCRIPT_DIR, "batch_insert.py")

# Pre-registered word families from Word Workshop sessions.
# Each family groups words that share a mechanic (action/tag).
# These are preliminary classifications — ralph-loop should refine them.

FAMILIES = [
    # --- Purify family (cleanse negative statuses) ---
    {"word": "cleanse",  "target": "AllAlliesAndSelf", "cost": 2, "range": 0, "area": "Single",
     "tags": ["HOLY", "RESTORATION", "CLEANSING"],
     "actions": [{"action": "Purify", "value": 2}]},
    {"word": "cure",     "target": "Self",             "cost": 1, "range": 0, "area": "Single",
     "tags": ["RESTORATION", "SUPPORT", "CLEANSING"],
     "actions": [{"action": "Purify", "value": 1}]},
    {"word": "remedy",   "target": "Self",             "cost": 1, "range": 0, "area": "Single",
     "tags": ["RESTORATION", "SUPPORT", "CLEANSING"],
     "actions": [{"action": "Purify", "value": 1}, {"action": "Heal", "value": 2}]},
    {"word": "antidote", "target": "Self",             "cost": 1, "range": 0, "area": "Single",
     "tags": ["RESTORATION", "CLEANSING"],
     "actions": [{"action": "Purify", "value": 2}]},
    {"word": "rinse",    "target": "AllAlliesAndSelf", "cost": 1, "range": 0, "area": "Single",
     "tags": ["CLEANSING", "NATURE"],
     "actions": [{"action": "Purify", "value": 1}]},
    {"word": "wash",     "target": "AllAlliesAndSelf", "cost": 1, "range": 0, "area": "Single",
     "tags": ["CLEANSING", "NATURE"],
     "actions": [{"action": "Purify", "value": 1}, {"action": "Water", "value": 1}]},
    {"word": "baptize",  "target": "AllAlliesAndSelf", "cost": 3, "range": 0, "area": "Single",
     "tags": ["HOLY", "RESTORATION", "CLEANSING"],
     "actions": [{"action": "Purify", "value": 3}, {"action": "Heal", "value": 2}]},

    # --- Awaken family (remove CC + stat buff) ---
    {"word": "rouse",    "target": "AllAlliesAndSelf", "cost": 2, "range": 0, "area": "Single",
     "tags": ["SUPPORT", "LIGHT"],
     "actions": [{"action": "Awaken", "value": 1}]},
    {"word": "revive",   "target": "AllAlliesAndSelf", "cost": 2, "range": 0, "area": "Single",
     "tags": ["HOLY", "RESTORATION", "SUPPORT"],
     "actions": [{"action": "Awaken", "value": 1}, {"action": "Heal", "value": 2}]},
    {"word": "stir",     "target": "Self",             "cost": 1, "range": 0, "area": "Single",
     "tags": ["SUPPORT"],
     "actions": [{"action": "Awaken", "value": 1}]},
]


def main():
    proc = subprocess.run(
        [sys.executable, BATCH_INSERT, "--draft"],
        input=json.dumps(FAMILIES),
        capture_output=True,
        text=True,
    )
    if proc.stderr:
        print(proc.stderr, end="", file=sys.stderr)
    if proc.returncode != 0:
        print(f"preregister_families failed (exit {proc.returncode})", file=sys.stderr)
        sys.exit(proc.returncode)
    print(f"Pre-registered {len(FAMILIES)} draft words from word families.", file=sys.stderr)


if __name__ == "__main__":
    main()
