#!/usr/bin/env python3
"""Progress utility: shows total/processed/remaining/action breakdown."""

import os
import sqlite3
import sys

DB_PATH = os.path.join(
    os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets", "wordactions.db"
)


def main():
    try:
        from nltk.corpus import words as nltk_words
        all_words = set(w.lower() for w in nltk_words.words() if w.isalpha())
        total_nltk = len(all_words)
    except Exception:
        print("Warning: NLTK not available, run setup.py first", file=sys.stderr)
        total_nltk = None

    if not os.path.exists(DB_PATH):
        print(f"Error: database not found at {DB_PATH}", file=sys.stderr)
        sys.exit(1)

    conn = sqlite3.connect(DB_PATH)

    # Check if processed_words table exists
    has_tracking = conn.execute(
        "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='processed_words'"
    ).fetchone()[0]

    if not has_tracking:
        print("Error: processed_words table not found. Run setup.py first.", file=sys.stderr)
        sys.exit(1)

    processed = conn.execute("SELECT COUNT(*) FROM processed_words").fetchone()[0]
    with_actions = conn.execute(
        "SELECT COUNT(DISTINCT pw.word) FROM processed_words pw "
        "INNER JOIN word_actions wa ON LOWER(pw.word) = LOWER(wa.word)"
    ).fetchone()[0]
    without_actions = processed - with_actions
    total_mappings = conn.execute("SELECT COUNT(*) FROM word_actions").fetchone()[0]

    # Action breakdown
    action_counts = conn.execute(
        "SELECT action_name, COUNT(*) FROM word_actions GROUP BY action_name ORDER BY COUNT(*) DESC"
    ).fetchall()

    conn.close()

    if total_nltk is not None:
        remaining = total_nltk - processed
        pct = (processed / total_nltk * 100) if total_nltk > 0 else 0
        print(f"Total NLTK words:   {total_nltk:>10,}")
        print(f"Processed:          {processed:>10,} ({pct:.1f}%)")
    else:
        print(f"Processed:          {processed:>10,}")

    print(f"  With actions:     {with_actions:>10,}")
    print(f"  Without actions:  {without_actions:>10,}")
    print(f"Action mappings:    {total_mappings:>10,}")

    if total_nltk is not None:
        remaining = total_nltk - processed
        print(f"Remaining:          {remaining:>10,}")

    if action_counts:
        print("\nAction breakdown:")
        for action, count in action_counts:
            print(f"  {action:<12} {count:>6,}")

    # Unit / passive stats
    try:
        conn2 = sqlite3.connect(DB_PATH)

        unit_total = conn2.execute("SELECT COUNT(*) FROM units").fetchone()[0]
        unit_type_counts = conn2.execute(
            "SELECT unit_type, COUNT(*) FROM units GROUP BY unit_type ORDER BY COUNT(*) DESC"
        ).fetchall()
        passive_total = conn2.execute("SELECT COUNT(*) FROM unit_passives").fetchone()[0]
        trigger_counts = conn2.execute(
            "SELECT trigger_id, COUNT(*) FROM unit_passives GROUP BY trigger_id ORDER BY COUNT(*) DESC"
        ).fetchall()
        effect_counts = conn2.execute(
            "SELECT effect_id, COUNT(*) FROM unit_passives WHERE effect_id != '' GROUP BY effect_id ORDER BY COUNT(*) DESC"
        ).fetchall()
        target_counts = conn2.execute(
            "SELECT target, COUNT(*) FROM unit_passives GROUP BY target ORDER BY COUNT(*) DESC"
        ).fetchall()

        # Summon words with/without unit definitions
        summon_total = conn2.execute(
            "SELECT COUNT(DISTINCT word) FROM word_actions WHERE action_name = 'Summon'"
        ).fetchone()[0]
        summon_with_unit = conn2.execute(
            "SELECT COUNT(DISTINCT wa.word) FROM word_actions wa "
            "INNER JOIN units u ON LOWER(wa.word) = LOWER(u.unit_id) "
            "WHERE wa.action_name = 'Summon'"
        ).fetchone()[0]
        summon_without_unit = summon_total - summon_with_unit

        conn2.close()

        print(f"\nUnit stats:")
        print(f"  Total units:        {unit_total:>6,}")
        if unit_type_counts:
            by_type = ", ".join(f"{t}: {c}" for t, c in unit_type_counts)
            print(f"  By type:            {by_type}")
        print(f"  Total passives:     {passive_total:>6,}")
        if trigger_counts:
            triggers = ", ".join(f"{t}: {c}" for t, c in trigger_counts)
            print(f"  Trigger dist:       {triggers}")
        if effect_counts:
            effects = ", ".join(f"{e}: {c}" for e, c in effect_counts)
            print(f"  Effect dist:        {effects}")
        if target_counts:
            targets = ", ".join(f"{t}: {c}" for t, c in target_counts)
            print(f"  Target dist:        {targets}")
        print(f"Summon words:         {summon_total:>6,}")
        print(f"  With unit def:      {summon_with_unit:>6,}")
        print(f"  Without unit def:   {summon_without_unit:>6,}")
    except Exception:
        pass  # Backward compatibility with older DBs

    # Item stats
    try:
        conn3 = sqlite3.connect(DB_PATH)
        item_total = conn3.execute("SELECT COUNT(*) FROM items").fetchone()[0]
        item_type_counts = conn3.execute(
            "SELECT item_type, COUNT(*) FROM items GROUP BY item_type ORDER BY COUNT(*) DESC"
        ).fetchall()
        item_passive_total = conn3.execute("SELECT COUNT(*) FROM item_passives").fetchone()[0]
        conn3.close()

        print(f"\nItem stats:")
        print(f"  Total items:        {item_total:>6,}")
        if item_type_counts:
            by_type = ", ".join(f"{t}: {c}" for t, c in item_type_counts)
            print(f"  By type:            {by_type}")
        print(f"  Item passives:      {item_passive_total:>6,}")
    except Exception:
        pass


if __name__ == "__main__":
    main()
