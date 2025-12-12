# TOOL System Diagrams

PlantUML diagrams for visualizing the TOOL architecture and workflows.

## 🎯 For Your 30-Minute Demo (Use These 3)

These diagrams are **self-contained** (no external includes) and work everywhere:

| File | What It Shows | When to Show |
|------|---------------|--------------|
| `c4_context_simple.puml` | System context: User, TOOL, GitHub, Ollama, NATS | Start here (2 min) |
| `c4_container_simple.puml` | Internal components: UI, API, Promoter, Projector, DB | Architecture (3 min) |
| `system_flow.puml` | All 3 flows: Ingestion, Chat, Replay | Main explanation (5 min) |

**Render here:** https://www.plantuml.com/plantuml/uml/

---

## Rendering Options

### Option 1: Online PlantUML Server (Recommended)
Copy `.puml` content → paste into https://www.plantuml.com/plantuml/uml/

### Option 2: VS Code Extension
Install "PlantUML" extension → right-click → "Preview Current Diagram"

### Option 3: Command Line
```bash
plantuml -tpng c4_context_simple.puml c4_container_simple.puml system_flow.puml
```

---

## All Available Diagrams

### Simple (No External Dependencies) ⭐
| File | Description |
|------|-------------|
| `c4_context_simple.puml` | System context - no includes required |
| `c4_container_simple.puml` | Container diagram - no includes required |
| `system_flow.puml` | Combined sequence showing all 3 main flows |

### Detailed (May Need Internet for !include)
| File | Description |
|------|-------------|
| `c4_1_context.puml` | C4 Context with C4-PlantUML styling |
| `c4_2_container.puml` | C4 Container with C4-PlantUML styling |
| `c4_3_component.puml` | C4 Component diagram |
| `seq_1_rule_ingestion.puml` | Detailed rule ingestion flow |
| `seq_2_agent_query.puml` | Detailed chat/query flow |
| `seq_3_replay.puml` | Detailed replay flow |
| `seq_4_realtime_update.puml` | Current vs planned SSE flow |
| `event_flow.puml` | Event sourcing overview |
| `usecase.puml` | Use case diagram |
| `hypotheses_mapping.puml` | H1-H5 to components mapping |
| `multi_agent_consistency.puml` | Multi-instance scenario (H5) |
