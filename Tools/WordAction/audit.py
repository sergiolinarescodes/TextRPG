#!/usr/bin/env python3
"""Deep audit of word action distribution — finds imbalances and suggests rebalancing.

Imports VALID_* sets from batch_insert.py (single source of truth) and cross-references
against the DB + filesystem to find blind spots in either direction.

Outputs a detailed report covering:
- Action usage distribution with under/over-represented flags
- Target type distribution
- Tag coverage
- Value distribution per action (are values too clustered?)
- Cost distribution
- Summon/unit coverage
- Passive trigger/effect diversity
- Tag definitions (filesystem scan)
- Handler file counts (filesystem scan)
"""

import glob
import os
import sqlite3
import sys
from collections import Counter, defaultdict

SCRIPT_DIR = os.path.dirname(__file__)

DB_PATH = os.path.join(SCRIPT_DIR, "..", "..", "Assets", "StreamingAssets", "wordactions.db")

ASSETS_DIR = os.path.join(SCRIPT_DIR, "..", "..", "Assets", "Scripts", "Core")

# Import from batch_insert.py — single source of truth, zero duplication.
from batch_insert import (
    VALID_ACTIONS, VALID_TAGS, VALID_TRIGGERS, VALID_EFFECTS,
    VALID_TARGETS, VALID_PASSIVE_TARGETS, VALID_ITEM_TYPES,
    VALID_STATUS_EFFECTS, VALID_UNIT_TYPES,
)


def pct(n, total):
    return f"{n / total * 100:.1f}%" if total > 0 else "0.0%"


def section(title):
    print(f"\n{'=' * 60}")
    print(f"  {title}")
    print(f"{'=' * 60}")


def main():
    if not os.path.exists(DB_PATH):
        print(f"Error: database not found at {DB_PATH}", file=sys.stderr)
        sys.exit(1)

    conn = sqlite3.connect(DB_PATH)

    # ── Basic counts ──
    total_words_with_actions = conn.execute(
        "SELECT COUNT(DISTINCT word) FROM word_actions"
    ).fetchone()[0]
    total_action_rows = conn.execute("SELECT COUNT(*) FROM word_actions").fetchone()[0]
    total_meta = conn.execute("SELECT COUNT(*) FROM word_meta").fetchone()[0]

    section("OVERVIEW")
    print(f"Words with actions:  {total_words_with_actions:>6,}")
    print(f"Total action rows:   {total_action_rows:>6,}")
    print(f"Total meta entries:  {total_meta:>6,}")
    print(f"Avg actions/word:    {total_action_rows / max(1, total_words_with_actions):>6.1f}")

    # ── Action distribution (dual-check: DB ↔ VALID_ACTIONS) ──
    section("ACTION DISTRIBUTION")
    action_counts = dict(conn.execute(
        "SELECT action_name, COUNT(*) FROM word_actions GROUP BY action_name ORDER BY COUNT(*) DESC"
    ).fetchall())

    action_word_counts = dict(conn.execute(
        "SELECT action_name, COUNT(DISTINCT word) FROM word_actions GROUP BY action_name ORDER BY COUNT(DISTINCT word) DESC"
    ).fetchall())

    # Show ALL actions from both DB and VALID set
    all_known_actions = set(action_counts.keys()) | VALID_ACTIONS

    print(f"{'Action':<25} {'Rows':>6} {'Words':>6} {'% Words':>8}  Notes")
    print(f"{'-'*25} {'-'*6} {'-'*6} {'-'*8}  {'-'*30}")

    for action in sorted(all_known_actions, key=lambda a: action_word_counts.get(a, 0), reverse=True):
        rows = action_counts.get(action, 0)
        words = action_word_counts.get(action, 0)
        p = pct(words, total_words_with_actions)
        notes = ""
        if action in action_counts and action not in VALID_ACTIONS:
            notes = "!! In DB but NOT in batch_insert.py"
        elif action not in action_counts and action in VALID_ACTIONS:
            notes = "!! UNUSED — needs words"
        elif words > 0 and words / total_words_with_actions < 0.01:
            notes = "! Under-represented (<1%)"
        elif words > 0 and words / total_words_with_actions > 0.25:
            notes = "! Over-represented (>25%)"
        print(f"{action:<25} {rows:>6} {words:>6} {p:>8}  {notes}")

    db_only = set(action_counts.keys()) - VALID_ACTIONS
    valid_only = VALID_ACTIONS - set(action_counts.keys())
    if db_only:
        print(f"\n!! Actions in DB but NOT in VALID_ACTIONS ({len(db_only)}): {', '.join(sorted(db_only))}")
    if valid_only:
        print(f"\n!! Actions in VALID_ACTIONS but NOT in DB ({len(valid_only)}): {', '.join(sorted(valid_only))}")

    # ── Value distribution per action ──
    section("VALUE DISTRIBUTION PER ACTION")
    value_dist = conn.execute(
        "SELECT action_name, value, COUNT(*) FROM word_actions GROUP BY action_name, value ORDER BY action_name, value"
    ).fetchall()

    action_values = defaultdict(list)
    for action, value, count in value_dist:
        action_values[action].append((value, count))

    for action in sorted(action_values.keys()):
        values = action_values[action]
        total = sum(c for _, c in values)
        dist = " ".join(f"v{v}:{c}" for v, c in values)
        avg = sum(v * c for v, c in values) / max(1, total)
        print(f"  {action:<22} avg={avg:.1f}  {dist}")

    # ── Target distribution ──
    section("TARGET DISTRIBUTION (word-level)")
    target_counts = conn.execute(
        "SELECT target, COUNT(*) FROM word_meta GROUP BY target ORDER BY COUNT(*) DESC"
    ).fetchall()

    print(f"{'Target':<30} {'Count':>6} {'%':>8}  Notes")
    print(f"{'-'*30} {'-'*6} {'-'*8}  {'-'*20}")
    for target, count in target_counts:
        p = pct(count, total_meta)
        notes = ""
        if count / total_meta > 0.50:
            notes = "! Dominant (>50%)"
        print(f"{target:<30} {count:>6} {p:>8}  {notes}")

    used_targets = set(t for t, _ in target_counts)
    unused_targets = VALID_TARGETS - used_targets
    if unused_targets:
        print(f"\nUnused target types ({len(unused_targets)}): {', '.join(sorted(unused_targets))}")

    # ── Cost distribution ──
    section("COST DISTRIBUTION")
    cost_counts = conn.execute(
        "SELECT cost, COUNT(*) FROM word_meta GROUP BY cost ORDER BY cost"
    ).fetchall()
    for cost, count in cost_counts:
        bar = "#" * min(50, count)
        print(f"  Cost {cost}: {count:>5}  {bar}")

    # ── Tag coverage (dual-check: DB ↔ VALID_TAGS) ──
    section("TAG COVERAGE")
    tag_counts = conn.execute(
        "SELECT tag, COUNT(DISTINCT word) FROM word_tags GROUP BY tag ORDER BY COUNT(DISTINCT word) DESC"
    ).fetchall()
    total_tagged = conn.execute("SELECT COUNT(DISTINCT word) FROM word_tags").fetchone()[0]

    all_known_tags = set(t for t, _ in tag_counts) | VALID_TAGS

    print(f"Words with tags: {total_tagged:>6,}")
    print(f"{'Tag':<15} {'Words':>6} {'%':>8}  Notes")
    print(f"{'-'*15} {'-'*6} {'-'*8}  {'-'*30}")

    tag_count_dict = dict(tag_counts)
    for tag in sorted(all_known_tags, key=lambda t: tag_count_dict.get(t, 0), reverse=True):
        count = tag_count_dict.get(tag, 0)
        p = pct(count, total_tagged)
        notes = ""
        if tag in tag_count_dict and tag not in VALID_TAGS:
            notes = "!! In DB but NOT in batch_insert.py"
        elif tag not in tag_count_dict and tag in VALID_TAGS:
            notes = "!! UNUSED — needs words"
        elif count > 0 and count / max(1, total_tagged) < 0.03:
            notes = "! Under-represented (<3%)"
        print(f"{tag:<15} {count:>6} {p:>8}  {notes}")

    db_only_tags = set(t for t, _ in tag_counts) - VALID_TAGS
    valid_only_tags = VALID_TAGS - set(t for t, _ in tag_counts)
    if db_only_tags:
        print(f"\n!! Tags in DB but NOT in VALID_TAGS ({len(db_only_tags)}): {', '.join(sorted(db_only_tags))}")
    if valid_only_tags:
        print(f"\n!! Tags in VALID_TAGS but NOT in DB ({len(valid_only_tags)}): {', '.join(sorted(valid_only_tags))}")

    # ── Tags per word distribution ──
    tags_per_word = conn.execute(
        "SELECT word, COUNT(*) as cnt FROM word_tags GROUP BY word ORDER BY cnt DESC"
    ).fetchall()
    tag_count_dist = Counter(c for _, c in tags_per_word)
    print(f"\nTags-per-word distribution:")
    for n in sorted(tag_count_dist.keys()):
        print(f"  {n} tag(s): {tag_count_dist[n]} words")

    # ── TAG DEFINITIONS (filesystem scan) ──
    section("TAG DEFINITIONS (filesystem)")
    tag_def_files = sorted(glob.glob(os.path.join(
        ASSETS_DIR, "EventEncounter", "Reactions", "Tags", "Definitions", "*TagDefinition.cs")))
    print(f"Tag definition files: {len(tag_def_files)}")
    for f in tag_def_files:
        print(f"  {os.path.basename(f)}")

    try:
        unit_tag_counts = conn.execute(
            "SELECT tag, COUNT(*) FROM unit_tags GROUP BY tag ORDER BY COUNT(*) DESC"
        ).fetchall()
        print(f"\nUnit tags in DB: {sum(c for _, c in unit_tag_counts)} entries across {len(unit_tag_counts)} tags")
        for tag, count in unit_tag_counts:
            print(f"  {tag}: {count}")
    except Exception:
        print("\n  (unit_tags table not found)")

    # ── HANDLER FILE COUNTS (filesystem scan) ──
    section("HANDLER FILE COUNTS (filesystem)")
    handler_dirs = {
        "Action handlers": os.path.join(ASSETS_DIR, "ActionExecution", "Handlers", "*.cs"),
        "Status handlers": os.path.join(ASSETS_DIR, "StatusEffect", "Handlers", "*Handler.cs"),
        "Passive triggers": os.path.join(ASSETS_DIR, "Passive", "Triggers", "*Trigger.cs"),
        "Passive effects": os.path.join(ASSETS_DIR, "Passive", "Effects", "*Effect.cs"),
        "Reaction outcomes": os.path.join(ASSETS_DIR, "EventEncounter", "Reactions", "Outcomes", "*.cs"),
    }

    for label, pattern in handler_dirs.items():
        files = sorted(glob.glob(pattern))
        print(f"\n{label}: {len(files)}")
        for f in files:
            print(f"  {os.path.basename(f)}")

    # ── Duplicate action profiles ──
    section("DUPLICATE ACTION PROFILES")
    word_profiles = conn.execute(
        "SELECT word, GROUP_CONCAT(action_name || ':' || value, ',') as profile "
        "FROM (SELECT word, action_name, value FROM word_actions ORDER BY word, action_name, value) "
        "GROUP BY word ORDER BY word"
    ).fetchall()

    profile_map = defaultdict(list)
    for word, profile in word_profiles:
        profile_map[profile].append(word)

    dupe_count = 0
    for profile, words in sorted(profile_map.items(), key=lambda x: -len(x[1])):
        if len(words) > 1:
            dupe_count += 1
            print(f"  [{profile}] -> {', '.join(words)}")

    if dupe_count == 0:
        print("  No duplicate profiles found.")
    else:
        print(f"\n  {dupe_count} duplicate profile groups — consider varying values or adding secondary effects")

    # ── Unit / Passive analysis (dual-check: DB ↔ VALID_*) ──
    try:
        unit_total = conn.execute("SELECT COUNT(*) FROM units").fetchone()[0]
    except Exception:
        unit_total = 0

    if unit_total > 0:
        section("UNIT & PASSIVE ANALYSIS")

        unit_type_counts = conn.execute(
            "SELECT unit_type, COUNT(*) FROM units GROUP BY unit_type ORDER BY COUNT(*) DESC"
        ).fetchall()
        print(f"Total units: {unit_total}")
        for utype, count in unit_type_counts:
            print(f"  {utype}: {count}")

        passive_total = conn.execute("SELECT COUNT(*) FROM unit_passives").fetchone()[0]
        print(f"\nTotal passives: {passive_total}")

        # Triggers: dual-check DB ↔ VALID_TRIGGERS
        trigger_counts = dict(conn.execute(
            "SELECT trigger_id, COUNT(*) FROM unit_passives GROUP BY trigger_id ORDER BY COUNT(*) DESC"
        ).fetchall())
        all_known_triggers = set(trigger_counts.keys()) | VALID_TRIGGERS

        print(f"\n{'Trigger':<20} {'Count':>6}  Notes")
        print(f"{'-'*20} {'-'*6}  {'-'*30}")
        for trigger in sorted(all_known_triggers, key=lambda t: trigger_counts.get(t, 0), reverse=True):
            count = trigger_counts.get(trigger, 0)
            notes = ""
            if trigger in trigger_counts and trigger not in VALID_TRIGGERS:
                notes = "!! In DB but NOT in batch_insert.py"
            elif trigger not in trigger_counts and trigger in VALID_TRIGGERS:
                notes = "!! UNUSED in DB"
            print(f"{trigger:<20} {count:>6}  {notes}")

        # Effects: dual-check DB ↔ VALID_EFFECTS
        effect_counts = dict(conn.execute(
            "SELECT effect_id, COUNT(*) FROM unit_passives WHERE effect_id != '' GROUP BY effect_id ORDER BY COUNT(*) DESC"
        ).fetchall())
        all_known_effects = set(effect_counts.keys()) | VALID_EFFECTS

        print(f"\n{'Effect':<20} {'Count':>6}  Notes")
        print(f"{'-'*20} {'-'*6}  {'-'*30}")
        for effect in sorted(all_known_effects, key=lambda e: effect_counts.get(e, 0), reverse=True):
            count = effect_counts.get(effect, 0)
            notes = ""
            if effect in effect_counts and effect not in VALID_EFFECTS:
                notes = "!! In DB but NOT in batch_insert.py"
            elif effect not in effect_counts and effect in VALID_EFFECTS:
                notes = "!! UNUSED in DB"
            print(f"{effect:<20} {count:>6}  {notes}")

        # Passive targets: dual-check
        ptarget_counts = dict(conn.execute(
            "SELECT target, COUNT(*) FROM unit_passives GROUP BY target ORDER BY COUNT(*) DESC"
        ).fetchall())
        all_known_ptargets = set(ptarget_counts.keys()) | VALID_PASSIVE_TARGETS

        print(f"\n{'Passive Target':<20} {'Count':>6}  Notes")
        print(f"{'-'*20} {'-'*6}  {'-'*30}")
        for pt in sorted(all_known_ptargets, key=lambda t: ptarget_counts.get(t, 0), reverse=True):
            count = ptarget_counts.get(pt, 0)
            notes = ""
            if pt in ptarget_counts and pt not in VALID_PASSIVE_TARGETS:
                notes = "!! In DB but NOT in batch_insert.py"
            elif pt not in ptarget_counts and pt in VALID_PASSIVE_TARGETS:
                notes = "!! UNUSED in DB"
            print(f"{pt:<20} {count:>6}  {notes}")

        # Summon coverage
        summon_total = conn.execute(
            "SELECT COUNT(DISTINCT word) FROM word_actions WHERE action_name = 'Summon'"
        ).fetchone()[0]
        summon_with_unit = conn.execute(
            "SELECT COUNT(DISTINCT wa.word) FROM word_actions wa "
            "INNER JOIN units u ON LOWER(wa.word) = LOWER(u.unit_id) "
            "WHERE wa.action_name = 'Summon'"
        ).fetchone()[0]
        summon_without = summon_total - summon_with_unit

        print(f"\nSummon words: {summon_total}")
        print(f"  With unit definition: {summon_with_unit}")
        print(f"  Without definition:   {summon_without}")

        if summon_without > 0:
            missing = conn.execute(
                "SELECT DISTINCT wa.word FROM word_actions wa "
                "LEFT JOIN units u ON LOWER(wa.word) = LOWER(u.unit_id) "
                "WHERE wa.action_name = 'Summon' AND u.unit_id IS NULL"
            ).fetchall()
            print(f"  Missing units: {', '.join(w for w, in missing)}")

    # ── Item analysis ──
    try:
        item_total = conn.execute("SELECT COUNT(*) FROM items").fetchone()[0]
    except Exception:
        item_total = 0

    if item_total > 0:
        section("ITEM ANALYSIS")

        item_type_counts = conn.execute(
            "SELECT item_type, COUNT(*) FROM items GROUP BY item_type ORDER BY COUNT(*) DESC"
        ).fetchall()
        print(f"Total items: {item_total}")
        for itype, count in item_type_counts:
            print(f"  {itype}: {count}")

        item_passive_total = conn.execute("SELECT COUNT(*) FROM item_passives").fetchone()[0]
        print(f"\nItem passives: {item_passive_total}")

    # ── Summary & recommendations ──
    section("RECOMMENDATIONS")
    recs = []

    if item_total > 0:
        unused_item_types = VALID_ITEM_TYPES - set(t for t, _ in item_type_counts)
        if unused_item_types:
            recs.append(f"Add items of unused types: {', '.join(sorted(unused_item_types))}")

    unused_actions = VALID_ACTIONS - set(action_counts.keys())
    if unused_actions:
        recs.append(f"Add words using unused actions: {', '.join(sorted(unused_actions))}")

    under_actions = [a for a in VALID_ACTIONS
                     if action_word_counts.get(a, 0) > 0
                     and action_word_counts[a] / max(1, total_words_with_actions) < 0.01]
    if under_actions:
        recs.append(f"Boost under-represented actions (<1%): {', '.join(sorted(under_actions))}")

    over_actions = [a for a in VALID_ACTIONS
                    if action_word_counts.get(a, 0) / max(1, total_words_with_actions) > 0.25]
    if over_actions:
        recs.append(f"Reduce over-represented actions (>25%): {', '.join(sorted(over_actions))}")

    dominant_targets = [t for t, c in target_counts if c / max(1, total_meta) > 0.50]
    if dominant_targets:
        recs.append(f"Diversify targeting — dominant targets (>50%): {', '.join(dominant_targets)}")

    unused_tags = VALID_TAGS - set(t for t, _ in tag_counts)
    if unused_tags:
        recs.append(f"Use unused tags: {', '.join(sorted(unused_tags))}")

    under_tags = [t for t, c in tag_counts if c / max(1, total_tagged) < 0.03]
    if under_tags:
        recs.append(f"Boost under-represented tags (<3%): {', '.join(sorted(under_tags))}")

    if dupe_count > 0:
        recs.append(f"{dupe_count} word groups share identical action profiles — add variety")

    if unit_total > 0 and summon_without > 0:
        recs.append(f"{summon_without} summon words lack unit definitions — add unit data via batch_insert")

    if not recs:
        print("  Distribution looks balanced!")
    else:
        for i, rec in enumerate(recs, 1):
            print(f"  {i}. {rec}")

    conn.close()


if __name__ == "__main__":
    main()
