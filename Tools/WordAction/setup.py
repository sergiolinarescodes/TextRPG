#!/usr/bin/env python3
"""One-time setup: installs NLTK, downloads word corpus, creates processed_words table."""

import os
import sqlite3
import subprocess
import sys

DB_PATH = os.path.join(
    os.path.dirname(__file__), "..", "..", "Assets", "StreamingAssets", "wordactions.db"
)


def ensure_nltk():
    try:
        import nltk
    except ImportError:
        print("Installing nltk...", file=sys.stderr)
        subprocess.check_call([sys.executable, "-m", "pip", "install", "nltk"])
        import nltk
    return nltk


def main():
    nltk = ensure_nltk()

    print("Downloading NLTK words corpus...")
    nltk.download("words", quiet=True)

    from nltk.corpus import words as nltk_words

    all_words = set(w.lower() for w in nltk_words.words() if w.isalpha())

    if not os.path.exists(DB_PATH):
        print(f"Error: database not found at {DB_PATH}", file=sys.stderr)
        print("Run seed_db.py first to create the database.", file=sys.stderr)
        sys.exit(1)

    conn = sqlite3.connect(DB_PATH)
    conn.execute("""
        CREATE TABLE IF NOT EXISTS processed_words (
            word TEXT PRIMARY KEY COLLATE NOCASE
        )
    """)
    conn.execute("""
        CREATE TABLE IF NOT EXISTS word_meta (
            word   TEXT PRIMARY KEY COLLATE NOCASE,
            target TEXT NOT NULL DEFAULT 'SingleEnemy',
            cost   INTEGER NOT NULL DEFAULT 0 CHECK(cost BETWEEN 0 AND 10)
        )
    """)
    conn.commit()

    processed = conn.execute("SELECT COUNT(*) FROM processed_words").fetchone()[0]
    conn.close()

    total = len(all_words)
    remaining = total - processed
    print(f"NLTK words:  {total:,}")
    print(f"Processed:   {processed:,}")
    print(f"Remaining:   {remaining:,}")
    print("Setup complete.")


if __name__ == "__main__":
    main()
