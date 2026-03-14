#!/bin/bash
# Ralph Loop — Fresh Context Edition
# Runs claude as a one-shot command in a loop, each iteration gets a clean context window.
# State is persisted in the SQLite DB and filesystem, not in chat history.
#
# Usage:
#   bash Tools/WordAction/ralph-loop.sh              # run 10 iterations (default)
#   bash Tools/WordAction/ralph-loop.sh 25           # run 25 iterations
#   bash Tools/WordAction/ralph-loop.sh 0            # run until COMPLETE
#
# To run in background:
#   nohup bash Tools/WordAction/ralph-loop.sh 50 > /dev/null 2>&1 &

set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

MAX_ITERATIONS="${1:-10}"
ITERATION=0
PROMPT_FILE="Tools/WordAction/ralph-oneshot-prompt.md"
LOG_FILE="Tools/WordAction/ralph-loop.log"

echo "=== Ralph Loop (Fresh Context) ==="
echo "Max iterations: $MAX_ITERATIONS (0 = unlimited)"
echo "Prompt: $PROMPT_FILE"
echo "Log: $LOG_FILE"
echo ""

while true; do
    ITERATION=$((ITERATION + 1))

    # Check max iterations (0 = unlimited)
    if [[ "$MAX_ITERATIONS" -gt 0 ]] && [[ "$ITERATION" -gt "$MAX_ITERATIONS" ]]; then
        echo "=== Max iterations ($MAX_ITERATIONS) reached. Stopping. ==="
        break
    fi

    echo "--- Iteration $ITERATION / $MAX_ITERATIONS ($(date '+%H:%M:%S')) ---"

    # Run claude with the one-shot prompt as a fresh session
    # -p: non-interactive, outputs result and exits
    # Each invocation = completely fresh context window (no accumulated history)
    # --dangerously-skip-permissions: unattended mode (no permission prompts)
    OUTPUT=$(claude -p \
        --model claude-opus-4-6 \
        --dangerously-skip-permissions \
        "$(cat "$PROMPT_FILE")" 2>&1) || true

    # Log the full output
    echo "=== Iteration $ITERATION — $(date) ===" >> "$LOG_FILE"
    echo "$OUTPUT" >> "$LOG_FILE"
    echo "" >> "$LOG_FILE"

    # Print summary (last 20 lines usually has the stats/summary)
    echo "$OUTPUT" | tail -20
    echo ""

    # Check for completion signal
    if echo "$OUTPUT" | grep -q "RALPH_COMPLETE"; then
        echo "=== All words classified! ==="
        break
    fi

    # Brief pause between iterations to avoid rate limiting
    sleep 3
done

echo "=== Ralph Loop finished after $((ITERATION - 1)) iterations ==="
echo "Full log: $LOG_FILE"
