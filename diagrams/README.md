# TOOL System Diagrams

PlantUML diagrams for visualizing the TOOL architecture and workflows.

## Rendering Diagrams

### Option 1: Online PlantUML Server
Copy the `.puml` file content to: https://www.plantuml.com/plantuml/uml/

### Option 2: VS Code Extension
Install "PlantUML" extension, then right-click → "Preview Current Diagram"

### Option 3: Command Line
```bash
# Install PlantUML
brew install plantuml  # macOS
apt install plantuml   # Ubuntu

# Render all diagrams to PNG
for f in *.puml; do plantuml "$f"; done
```

### Option 4: Generate All as PNG
```bash
cd diagrams
plantuml -tpng *.puml
```

---

## Diagram Index

### C4 Architecture Diagrams

| File | Description |
|------|-------------|
| `c4_1_context.puml` | **System Context** - TOOL and external actors (users, GitHub, LLM) |
| `c4_2_container.puml` | **Container Diagram** - Internal components (API, NATS, SQLite, UI) |
| `c4_3_component.puml` | **Component Diagram** - API internal structure |

### Sequence Diagrams

| File | Description |
|------|-------------|
| `seq_1_rule_ingestion.puml` | Rule change from GitHub → EVENTS → Promoter → DELTAS → Projection |
| `seq_2_agent_query.puml` | Agent queries rules, gets LLM response with citations |
| `seq_3_replay.puml` | Full state replay from DELTAS (proves H3) |
| `seq_4_realtime_update.puml` | Real-time rule update via SSE (proves H4) |

### Other Diagrams

| File | Description |
|------|-------------|
| `usecase.puml` | Use case diagram - all system actors and their interactions |
| `event_flow.puml` | Simplified event flow: EVENTS → DELTAS → Projection |
| `hypotheses_mapping.puml` | Maps H1-H5 hypotheses to system components |
| `multi_agent_consistency.puml` | Shows how multiple agents share state via DELTAS |

---

## Recommended Order for Presentation

1. **event_flow.puml** - Start with the big picture
2. **c4_1_context.puml** - Show the actors
3. **c4_2_container.puml** - Show the components
4. **seq_2_agent_query.puml** - Show a typical use case
5. **hypotheses_mapping.puml** - Connect to research questions

---

## Quick Copy-Paste for PlantUML Server

Just copy the content of any `.puml` file and paste it into:
https://www.plantuml.com/plantuml/uml/

The diagram will render instantly.
