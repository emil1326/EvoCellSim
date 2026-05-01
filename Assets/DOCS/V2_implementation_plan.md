# V2 Implementation Plan

This plan turns the V2 spec into buildable engine work. It assumes one world seed, one world RNG, headless simulation core, and Unity as a read-only viewer and input layer.

## 1. Build Goals

- Make the simulation core run without Unity.
- Keep all authoritative world state inside the core.
- Use one deterministic RNG instance created from the seed at world creation.
- Keep snapshots simple: Unity asks for a region, the core returns read-only cell state for that region.
- Build the systems in an order that keeps the engine runnable at every stage.

## 2. Non-Goals

- No replay system.
- No versioned resume pipeline.
- No overbuilt snapshot history system.
- No hardcoded behavior tree for the whole simulation.
- No Unity-owned simulation logic.

## 3. Implementation Sequence

### Phase 0. Core project structure

Create the core engine boundaries first so later systems do not leak into Unity code.

Deliverables:
- A clear core assembly or namespace split between simulation logic and Unity presentation.
- A `WorldState` root object that owns seed, tick, settings, registries, and RNG.
- Flat stores for cell, genome, module, bond, cluster, field, signal, and intent data.

Acceptance:
- The core can be instantiated without any Unity scene objects.
- The world seed is read once from settings and stored on `WorldState`.

### Phase 1. Deterministic world loop

Implement the fixed tick loop before adding complex behavior.

Deliverables:
- A single tick runner that executes the canonical phase order.
- A single RNG instance owned by the world.
- A deterministic iteration order for cells and other world entities.
- Basic empty-state tick execution with no simulation content.

Recommended tick order:
1. Passive upkeep and bond upkeep.
2. Sample local context.
3. Update module and trait expression.
4. Evaluate runtime rules.
5. Queue intents.
6. Resolve intents.
7. Apply energy, mass, pressure, and damage changes.
8. Update bonds, clusters, and transfers.
9. Resolve reproduction, death, and mutation.
10. Update environment channels.
11. Build snapshot if needed.

Acceptance:
- Two runs with the same seed and the same inputs produce the same tick outputs.
- No code path outside the world RNG generates randomness.

### Phase 2. World data and store layout

Build the core data containers and the minimal access layer around them.

Deliverables:
- `CellStore` for runtime cell data.
- `GenomeStore` for genome data.
- `ModuleStore` for active module state.
- `BondStore` for bond edges and bond health.
- `ClusterStore` for cluster membership and cluster state.
- `FieldStore` and `SignalStore` for environmental and local signal data.
- `IntentQueue` for queued actions.

Implementation notes:
- Prefer flat arrays, chunked buffers, or other data-oriented storage.
- Keep world-owned references out of snapshot data.
- Use IDs or indices consistently inside the core.

Acceptance:
- Stores can be created, read, and updated without direct Unity dependencies.
- The core can query a cell by ID and return its current state from store data.

### Phase 3. Genome decoding pipeline

Implement genome decoding before behavior logic so later systems can consume decoded instructions.

Deliverables:
- A raw genome byte sequence format.
- Decoding rules for opcode tokens, operands, modifiers, control tokens, junk, and no-op tokens.
- A registry-backed decoder for instruction signatures.
- Malformed-chain handling that is deterministic and explicit.

Implementation notes:
- Keep the decoder strict enough to be deterministic, but permissive enough to preserve junk and mutation noise.
- Decode chains into runtime-friendly instruction records, not direct actions.
- Keep the three genome layers separate: species, module, and instruction genome.

Acceptance:
- The same genome always decodes to the same instruction stream.
- Malformed chains fail in the same way every time.
- Junk remains preserved as mutable raw sequence.

### Phase 4. Runtime registry and behavior dispatch

Build the registry system that turns decoded rules into actual simulation effects.

Deliverables:
- Opcode registry.
- Effect handler registry.
- Module definition registry.
- Mutation behavior hooks.
- Validation rules for permissions, input requirements, and target compatibility.

Implementation notes:
- Decode first, validate second, resolve third.
- A rule should produce an intent, not directly mutate the world.
- The resolver should be the only place that applies final effects.

Acceptance:
- A decoded rule can be turned into a queued intent.
- Invalid module or target combinations fail in the same canonical way.

### Phase 5. Energy, pressure, damage, and repair

Implement the physical survival loop next, because it drives most of the later multicell behavior.

Deliverables:
- Energy accounting.
- Passive upkeep and maintenance costs.
- Pressure calculation.
- Damage accumulation.
- Repair intents or repair module effects.
- Death handling.

Implementation notes:
- Keep formulas simple enough to tune with global values.
- Let the world compute the pressure and damage math from store data.
- Keep repair as a normal simulation effect with an energy cost.

Acceptance:
- Cells can survive, degrade, repair, and die deterministically.
- Pressure and damage change only through the intended simulation phases.

### Phase 6. Bonds and clusters

Add multicell structure after the cell survival loop works on its own.

Deliverables:
- Bond creation and bond decay.
- Bond break thresholds.
- Cluster membership updates.
- Cluster splitting and cascade failure handling.
- Energy or material transfer across bonds.

Implementation notes:
- Keep bond behavior deterministic and tied to the fixed tick order.
- Use clear rules for what happens when a bond fails or a cell can no longer pay its costs.
- Treat cluster membership as derived from the active bond graph.

Acceptance:
- Bond loss changes cluster structure predictably.
- Cluster failure propagates the same way every run.

### Phase 7. Local context and differentiation inputs

Implement the inputs that allow multicell specialization to emerge.

Deliverables:
- Local neighbor context sampling.
- Signal diffusion or signal delivery over the cluster.
- Bond depth or equivalent cluster-distance data.
- Cluster position or local structural position data.

Implementation notes:
- Keep the inputs deterministic and based on core stores.
- Use the same sampling rules every tick.
- Do not let Unity influence the values.

Acceptance:
- Two cells in different cluster positions can receive different deterministic context values.
- The same world state yields the same local inputs.

### Phase 8. Reproduction, mutation, and inheritance

Add the evolutionary loop after the core cell and cluster systems are stable.

Deliverables:
- Reproduction rules.
- Inheritance of genome data.
- Mutation operators.
- Mutation sensitivity or tendency settings.
- Species ceilings and module activation limits.

Implementation notes:
- Keep mutation noisy and capable of large behavior changes from small edits.
- Make inheritance and mutation operate on the genome stores, not on Unity objects.
- Keep the results deterministic for the same seed and event order.

Acceptance:
- Offspring inherit genome data and mutate predictably from the same seed.
- Genome changes can alter behavior without requiring hardcoded branch logic.

### Phase 9. Snapshot generation for Unity

Build the read-only query path once the core data and tick loop are stable.

Deliverables:
- A snapshot query API that accepts a bounded region.
- A read-only list of cells and their states for that region.
- A simple `SnapshotBuffer` or equivalent packaging layer if needed.
- A minimal schema for Unity-facing state.

Implementation notes:
- Keep snapshots as copied or immutable value data.
- Do not expose live world references.
- Compute all shown values before handing the snapshot out.

Acceptance:
- Unity can request a region and display the returned cells.
- Snapshot reads never mutate the simulation world.

### Phase 10. Unity presentation layer

Wire Unity only after the core can run by itself.

Deliverables:
- A viewer for cell and cluster visualization.
- UI for pause, speed control, and inspection.
- Debug overlays and basic stats.
- Input plumbing for region queries and inspection.

Implementation notes:
- Unity should only render and query.
- Unity should not own authoritative simulation data.
- Keep the presentation layer thin.

Acceptance:
- The simulation can run headless without Unity.
- Unity can attach to a running core and display the current state.

### Phase 11. Balance tuning

Once the engine runs end to end, tune the global values.

Deliverables:
- Tuned maintenance, pressure, repair, bond, transfer, and mutation constants.
- A small set of world settings for experimentation.
- A repeatable tuning workflow.

Implementation notes:
- Use values that are mathematically sensible first, then adjust by observed behavior.
- Prefer global tuning values over hidden hardcoded special cases.

Acceptance:
- The simulation behaves stably enough to observe evolution rather than collapse immediately.

## 4. Suggested Build Order by Risk

1. Core world state and deterministic tick loop.
2. Store layout and genome decoding.
3. Runtime registry and intent dispatch.
4. Energy, pressure, and death handling.
5. Bonds and clusters.
6. Reproduction and mutation.
7. Snapshot query path.
8. Unity rendering and inspection.
9. Balance tuning.

## 5. Implementation Rules

- Keep the core independent from Unity.
- Keep every random decision inside the world RNG.
- Keep all authoritative state in the core stores.
- Keep snapshots read-only and value-based.
- Keep the phase order fixed unless you intentionally version the change.
- Prefer simple, deterministic data flow over clever shortcuts.

## 6. Completion Criteria

The plan is ready to be considered implemented when:

- the core runs headless,
- a seeded world creates the same initial RNG-driven behavior every time,
- genome decoding and intent resolution work from registry data,
- cells can survive, mutate, bond, and die inside the tick loop,
- Unity can query bounded snapshots and render them,
- and the simulation stays deterministic for the same seed and update order.
