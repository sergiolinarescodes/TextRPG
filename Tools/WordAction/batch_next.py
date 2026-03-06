#!/usr/bin/env python3
"""Outputs the next batch of unclassified words as JSON for the Ralph Loop.

Usage:
    python batch_next.py [--count N]

Uses NLTK's words corpus as the word source, checks the processed_words table
to skip already-seen words, and outputs up to N words as a JSON array.
Prints COMPLETE when all words have been processed.
"""

import argparse
import json
import os
import sqlite3
import sys

DB_PATH = os.path.join(
    os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets", "wordactions.db"
)


def main():
    parser = argparse.ArgumentParser(description="Output next unclassified words as JSON")
    parser.add_argument("--count", type=int, default=150, help="Number of words to output")
    args = parser.parse_args()

    if not os.path.exists(DB_PATH):
        print(f"Error: database not found at {DB_PATH}", file=sys.stderr)
        sys.exit(1)

    try:
        from nltk.corpus import words as nltk_words
    except ImportError:
        print("Error: NLTK not installed. Run setup.py first.", file=sys.stderr)
        sys.exit(1)

    # Build sorted, lowercase, alpha-only, deduplicated word list
    all_words = sorted(set(w.lower() for w in nltk_words.words() if w.isalpha()))

    conn = sqlite3.connect(DB_PATH)
    processed = set(
        row[0].lower()
        for row in conn.execute("SELECT word FROM processed_words").fetchall()
    )
    conn.close()

    remaining = [w for w in all_words if w not in processed]

    if not remaining:
        print("COMPLETE")
        sys.exit(0)

    batch = remaining[: args.count]
    print(json.dumps(batch, indent=2))


if __name__ == "__main__":
    main()
