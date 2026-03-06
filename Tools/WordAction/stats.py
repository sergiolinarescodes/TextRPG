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


if __name__ == "__main__":
    main()
