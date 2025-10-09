#!/bin/bash
# Simple experiment logging script for TOOL evaluation framework
# Usage: ./scripts/log_experiment.sh <experiment_name> <hypothesis>
# Example: ./scripts/log_experiment.sh baseline_comparison H1

set -e

EXPERIMENT_NAME=${1:-"unnamed_experiment"}
HYPOTHESIS=${2:-"unknown"}
DATE=$(date +%Y%m%d)
TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)

# Generate run directory
RUN_ID="${EXPERIMENT_NAME}_${DATE}_001"
RUN_DIR="experiments/${EXPERIMENT_NAME}/run_${DATE}_001"

# Auto-increment if directory exists
COUNTER=1
while [ -d "experiments/${EXPERIMENT_NAME}/run_${DATE}_$(printf "%03d" $COUNTER)" ]; do
    COUNTER=$((COUNTER + 1))
done
RUN_ID="${EXPERIMENT_NAME}_${DATE}_$(printf "%03d" $COUNTER)"
RUN_DIR="experiments/${EXPERIMENT_NAME}/run_${DATE}_$(printf "%03d" $COUNTER)"

# Create directory structure
mkdir -p "$RUN_DIR"

# Get current TOOL commit SHA
TOOL_COMMIT=$(git rev-parse HEAD)

# Get current IM state hash (query TOOL API)
IM_HASH=$(curl -s http://localhost:5000/api/v1/state?ns=ravvnen.consulting | jq -r '.im_hash // "unknown"')

# Create metadata.json
cat > "$RUN_DIR/metadata.json" <<EOF
{
  "experiment_id": "$RUN_ID",
  "hypothesis": "$HYPOTHESIS",
  "date": "$TIMESTAMP",
  "description": "Experiment for $EXPERIMENT_NAME",
  "researcher": "Ravvnen",
  "parameters": {
    "model": "llama3:8b",
    "model_hash": "TODO: get from ollama",
    "temperature": 0.0,
    "topK": 5,
    "test_set": "data/test_prompts_v1.json",
    "im_state_hash": "$IM_HASH",
    "tool_commit_sha": "$TOOL_COMMIT"
  },
  "sample_size": 50,
  "conditions": [],
  "status": "running",
  "started_at": "$TIMESTAMP",
  "completed_at": null,
  "analysis_notebook": null
}
EOF

echo "✅ Created experiment run: $RUN_DIR"
echo "📋 Metadata saved to: $RUN_DIR/metadata.json"
echo ""
echo "Next steps:"
echo "  1. Run your experiment and save outputs to: $RUN_DIR/"
echo "  2. Update metadata.json with actual parameters"
echo "  3. Run analysis notebook and update 'analysis_notebook' field"
echo "  4. Set 'status' to 'completed' when done"
