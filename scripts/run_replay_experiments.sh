#!/bin/bash
# Replay Experiment Harness for H3 (Replayability & Determinism)
# Runs multiple replay trials and logs SRA scores

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$PROJECT_ROOT/experiments"
OUTPUT_FILE="$OUTPUT_DIR/replay_results_$(date +%Y%m%d_%H%M%S).jsonl"

TRIALS=${1:-10}  # Default: 10 trials
NS=${2:-ravvnen.consulting}

echo "======================================"
echo "Replay Experiment Harness (H3)"
echo "======================================"
echo "Trials: $TRIALS"
echo "Namespace: $NS"
echo "Output: $OUTPUT_FILE"
echo "======================================"
echo ""

# Ensure output directory exists
mkdir -p "$OUTPUT_DIR"

# Clear output file
> "$OUTPUT_FILE"

# Run trials
for i in $(seq 1 $TRIALS); do
    echo "[$i/$TRIALS] Running trial $i..."

    START_TIME=$(date +%s%3N)

    # Run the integration test and capture output
    dotnet test "$PROJECT_ROOT/tests/TOOL.IntegrationTests" \
        --filter "FullyQualifiedName~ReplayCorrectnessTests.FullReplay_ProducesSameStateHash" \
        --logger "console;verbosity=minimal" \
        2>&1 | tee /tmp/replay_trial_${i}.log

    END_TIME=$(date +%s%3N)
    DURATION=$((END_TIME - START_TIME))

    # Extract metrics from test output (parse console logs)
    # This is a simple parser - in production, tests would emit structured JSON
    EVENTS_PROCESSED=$(grep -oP 'events=\K\d+' /tmp/replay_trial_${i}.log | head -1 || echo "0")
    ACTIVE_COUNT=$(grep -oP 'activeCount=\K\d+' /tmp/replay_trial_${i}.log | head -1 || echo "0")
    IM_HASH=$(grep -oP 'hash=[A-F0-9]+' /tmp/replay_trial_${i}.log | head -1 | cut -d= -f2 || echo "unknown")
    REPLAY_TIME_MS=$(grep -oP 'durationMs=\K\d+' /tmp/replay_trial_${i}.log | head -1 || echo "0")

    # Check test result
    if grep -q "Passed!" /tmp/replay_trial_${i}.log; then
        TEST_PASSED="true"
        SRA_SCORE=1.0
    else
        TEST_PASSED="false"
        SRA_SCORE=0.0
    fi

    # Write result as JSON line
    cat >> "$OUTPUT_FILE" <<EOF
{"trial":$i,"ns":"$NS","events_processed":$EVENTS_PROCESSED,"active_count":$ACTIVE_COUNT,"im_hash":"$IM_HASH","replay_time_ms":$REPLAY_TIME_MS,"test_passed":$TEST_PASSED,"sra_score":$SRA_SCORE,"total_duration_ms":$DURATION,"timestamp":"$(date -Iseconds)"}
EOF

    echo "   ✓ Trial $i complete: SRA=$SRA_SCORE, events=$EVENTS_PROCESSED, hash=${IM_HASH:0:16}..."
    echo ""

    # Clean up temp log
    rm -f /tmp/replay_trial_${i}.log
done

echo "======================================"
echo "Experiments Complete!"
echo "======================================"
echo "Results saved to: $OUTPUT_FILE"
echo ""
echo "Run analysis:"
echo "  python3 analysis/replay_statistics.py $OUTPUT_FILE"
echo ""
