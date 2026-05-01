# EVOLUTIONARY CELL SIMULATION, FULL SYSTEM SPECIFICATION V2

This document is the cleaned-up version of the simulation plan. The goal is to build a headless simulation core that can run without Unity, while Unity acts only as a visual frontend. The simulation should model evolution from simple cellular life toward multicellular forms, with as little hardcoding as possible and as much room for emergent behavior as possible.

The core idea is simple:

Genome -> expression -> phenotype -> interaction -> selection -> mutation -> evolution

The simulation should not depend on one handcrafted species tree or one fixed behavior script. It should expose a small set of universal rules, then let genomes, modules, environment, and selection pressure create complexity.

---

## 1. Design Goals

1. The simulation must run headless.

   - Unity is optional.
   - The sim core must be usable from a console app, tests, batch runs, or tools.
   - Unity should only render snapshots and inspect state.

2. Keep the core modular.

   - World logic, genome logic, behavior logic, physics, and rendering should be separate.
   - New features should come from data and registries before they come from new special-case code.

3. Hardcode as little as possible.

   - Keep only the universal laws in code: energy accounting, physical constraints, mutation mechanics, action resolution, and world ticking.
   - Everything else should be configured through data, registries, or genome expression.

4. Preserve evolutionary surprise.

   - Small genome changes should sometimes change whole rules, not just tiny numbers.
   - The system should allow neutral junk, frame shifts, duplication, inversion, and other mutation effects that can create large behavioral changes.

5. Support a path from single-cell life to multicellular life.

   - Single cells must be able to survive, reproduce, specialize, bond, cooperate, and form clusters.
   - Multicellularity should emerge from the same rules, not from a separate special system.

6. Keep terminology consistent.

   - Use one term for one concept.
   - Do not switch between multiple names for the same thing unless the spec explicitly says they are aliases.

---

## 2. Preferred Terminology

The earlier draft mixed terms. This version uses a cleaner vocabulary.

| Old draft term | V2 term | Meaning |
| --- | --- | --- |
| base genome | species genome | Slow-changing genome layer that sets ceilings and broad traits |
| component genome | module genome | Encoded functional parts inside the cell |
| behavior genome / behavior graph | instruction genome / instruction chain | Ordered decoded instructions that can resolve to actions, no-ops, or malformed effects |
| component | module | A functional sub-structure inside a cell |
| codon | token | Small interpreted unit of genome text or bytes |
| opcode | operation token | Token that selects an effect or primitive action |
| operand | value / target token | Token that supplies a number, target, or selector |
| intent | queued intent | A queued result produced by a decoded instruction chain before resolution |
| effect | effect handler | A registered effect handler looked up by ID or registry |
| junk | inert token / junk segment | Sequence that can mutate and replicate but does not directly change state |
| malformed instruction | malformed chain | A chain that fails grammar or resolves to a registered failure effect |
| no-op | null effect | A valid chain that performs no state change |
| shell strength | membrane strength | Resistance to rupture from internal load |
| weight | mass | Inertial load and movement cost factor |
| signalState | signal channels | The state of communication emitted and received |
| multicell cluster | cluster | Bonded group of cells behaving as a unit |
| behavior graph node | runtime rule | A condition-action entry evaluated at runtime |
| action execution | intent resolution | Applying a queued action to world state |
| phenotype | expressed phenotype | Runtime form produced by genome expression |
| state | runtime state | Mutable condition of a cell, module, or cluster |
| seed | world seed | User-provided deterministic simulation seed |
| RNG stream | world RNG | The single deterministic random source owned by the world |

Recommended defaults:

- Use `cell` for the basic living unit.
- Use `cluster` for bonded groups of cells.
- Use `module` for the functional substructures inside a cell.
- Use `trait` for derived numeric properties.
- Use `rule` for condition-action logic.
- Use `intent` for a pending action that has not yet been resolved.
- Use `instruction` for a decoded genome chain.
- Use `opcode` and `operand token` for token roles inside instruction chains.
- Use `snapshot` for the read-only data sent to Unity.

Always qualify `state` in code and docs. Use `cell state`, `module state`, `cluster state`, or `world state`; do not use bare `state` when a more specific term exists.

Use `upkeep` for direct per-tick costs on modules and bonds. Use `maintenance` for the broader whole-cell burden derived from multiple sources.

Use `snapshot` for the immutable read-only world view sent to Unity, and `SnapshotBuffer` for the store that holds that data.

---

## 3. Core Architecture

The project should be split into two layers.
- Decode genomes.
- Evaluate rules.
- Resolve actions.
- Apply physics and resource changes.
- Handle mutation, reproduction, bonding, and death.
- Produce snapshots for rendering and analysis.

Responsibilities:
 - Allow inspection, camera control, and pause/resume.
- Never directly author the authoritative simulation state.

### 3.3 Suggested Runtime Flow

World state -> simulation tick -> snapshot -> Unity renderer
---

## 4. World Model

The world is the shared simulation space. It contains all living things, fields, resources, and bookkeeping data.

### 4.1 Core World Stores

The world should not be a dense object graph. It should be organized as flat stores or chunked buffers.

Main stores:

- WorldState
- CellStore
- ModuleStore
- GenomeStore
- BondStore
- ClusterStore
- FieldStore
- SignalStore
- IntentQueue
- WorldState: top-level owner of world seed, tick count, registries, single RNG stream, and global settings.
- CellStore: runtime cell data such as energy, volume, mass, position, pressure, damage, and alive state.
- SignalStore: short-lived communication events and local signal diffusion data.
- IntentQueue: pending intents emitted by cells and clusters.
- SnapshotBuffer: compact read-only state for Unity.

### 4.3 Immutable World Rules

These rules should be universal:

- Energy cannot appear from nowhere.
- Mass and volume changes must be accounted for.
- Pressure and boundary limits must matter.
- Actions should cost something.
- Mutation should be noisy and sometimes damaging.
- Local context should matter more than global scripting.

---

## 5. Genome Model

The genome should be raw, mutable, and expressive. It should not read like a hand-authored behavior script.

### 5.1 Three Genome Layers

#### 5.1.1 Species Genome

This is the slow-changing layer.

It defines broad ceilings and tradeoffs such as:

- membrane permeability
- membrane strength ceiling
- energy storage ceiling
- mutation tendency
- replication style
- pressureToleranceFactor
- sensory noise
- maintenance bias
- digestion bias
- movement bias
- signal fidelity
- instructionBudget

This layer should mutate rarely.

#### 5.1.2 Module Genome

This layer encodes the modules that make up the cell.

Each module genome entry can express:

- module type
- size or footprint tendency
- energy use tendency
- output tendency
- signal tendency
- mutation sensitivity
- activation state
- compatibility hints

This layer should mutate more often than the species genome.

A module marked inactive remains stored and addressable. The instruction genome may activate or deactivate it only if that module definition lists activation as an allowed action.

#### 5.1.3 Instruction Genome

This layer controls how modules are used by encoding ordered instruction chains.

It describes strict token sequences such as:

- set a module value
- compare a sensor against a threshold
- activate or deactivate a module
- emit a signal
- apply a local modifier
- branch or terminate a chain

This layer should be expressive enough for complex emergent behavior, but still compact and data-driven. Small mutations should be able to turn a valid chain into a no-op, a malformed chain, or a harmful chain.

Mutated offspring are never rejected because their genomes are malformed. Broken chains are preserved and later decode to no-op or failure effects.

#### 5.1.4 Genome Layer Interaction

Each cell stores three separate genome buffers: `speciesGenome`, `moduleGenome`, and `instructionGenome`.

During reproduction, the cell copies all three buffers, then applies layer-specific mutation rates and operators.

Expression order is canonical:

1. The species genome sets ceilings, broad constraints, and species-level modifiers.
2. The module genome instantiates modules and applies module-level cost and output modifiers.
3. The instruction genome emits runtime rules and intent templates that operate within the limits set by the first two layers.

Later layers may modify behavior and cost, but they may not exceed species ceilings.

### 5.2 Genome Encoding Principles

The genome should support:

- raw symbol sequences
- fixed-size or variable-size decoding windows
- frame shifts
- insertions and deletions
- duplication
- inversion
- transposition
- multi-token instructions
- junk segments that do nothing but remain evolvable

In V2, the genome is stored as a compact byte sequence over a fixed symbol alphabet. One raw symbol is one byte. Human-readable text is only a debugging view of that sequence. Registered token patterns are 1 to 4 bytes long.

The decoder should interpret the genome as ordered instruction chains using a longest-match scan over the fixed alphabet. A registered match is classified as one of: opcode, operand token, modifier token, or control token. Any unmatched symbol is junk.

Valid chain grammar:

- optional modifier tokens
- exactly one opcode token
- zero or more operand tokens, in the opcode's declared signature order
- optional control token terminator

Each opcode definition declares which operand slots are required and which are optional.

Loop and block markers are optional control tokens. If enabled for a scenario, a loop repeats the immediately previous valid rule block up to a decoder-defined `maxLoopCount`; if disabled, loop markers decode as junk.

Malformed-chain policy:

- If no opcode is decoded, the sequence is junk and emits no intent.
- If an opcode is decoded but required operands are missing, out of range, wrong type, or otherwise invalid, the chain aborts without effect unless the opcode defines a registered failure effect.
- Partial execution is not allowed except through an opcode's registered failure effect.

Unknown tokens are treated as junk and advance the scanner by one symbol, which allows insertions, deletions, and frame shifts to change downstream parsing without requiring a special case.

A frame shift mutation changes the decode alignment from the mutation point forward by one symbol. Earlier decoded chains stay fixed; only downstream parsing changes.

A valid no-op is an explicit opcode whose registered effect is empty. Junk is raw unmatched symbol stream and is not a no-op.

This gives the genome the behavior the transcripts described: precise chains work, broken chains do nothing or do the wrong thing, and junk can still exist and be inherited.

Important rule:

- Do not make one character position always mean one thing.
- Do not make the first symbol always be a condition.
- Do not force one codon to map to one action forever.

The decoder can use multiple passes or segmentation schemes, but those schemes should be data-driven and mutation-sensitive.

### 5.3 Expression Pipeline

The genome should be interpreted through a pipeline:

raw genome -> decoded tokens -> instruction chains / traits / module expression -> runtime phenotype

Possible outputs of decoding:

- typed trait deltas
- module blueprints
- active modules
- dormant modules
- runtime rules
- intent templates
- thresholds
- priorities
- probabilities
- replication parameters
- communication parameters

When multiple outputs target the same trait or module field, combine them in this order: species ceilings first, then additive module modifiers, then multiplicative module modifiers, then instruction-driven overrides within the allowed range. Within a layer, additive modifiers are summed first and multiplicative modifiers are applied second in token order. Species ceilings are absolute caps. Lower layers may choose a value inside the cap, but they do not raise the cap itself. If an instruction requests a value outside the allowed range and the opcode has no registered failure effect, the value is clamped instead of mutating the ceiling.

The same genome may express differently depending on species settings, environment, and mutation history.

---

## 6. Module Model

Modules are the functional building blocks inside a cell.

A module is not a special kind of cell. It is a sub-structure that contributes capabilities, costs, and constraints.

### 6.1 Module Philosophy

- Modules should be generic and data-driven.
- The core should not need to know whether a module is a motor, a sensor, a pump, or a membrane patch.
- Module behavior should come from registered definitions and effect handlers.
- New module families should be addable without rewriting the whole sim.

### 6.2 Module Families

Examples only, not a closed list:

- metabolic modules
- storage modules
- movement modules
- adhesion modules
- membrane reinforcement modules
- sensory modules
- signaling modules
- digestive modules
- defensive modules
- offensive modules
- replication modules
- exchange modules
- structural modules

### 6.3 Module Definition Data

Each module definition can include:

- module ID
- category tags
- base upkeep cost
- base activation cost
- output modifiers
- passive effects
- active effects
- allowed actions
- input requirements
- failure behavior
- mutation sensitivity
- compatibility rules
- activation conditions

The resolver must validate `allowed actions` and `input requirements` before applying an intent. If validation fails, the intent aborts or uses the module's registered failure behavior.

### 6.4 Module Runtime State

Runtime module state can include:

- active or inactive: whether the module may emit effects this tick
- local charge or readiness: the module's current activation state
- damage: accumulated module damage
- efficiency: current output multiplier after wear and state modifiers
- wear: accumulated degradation used to reduce efficiency
- cooldown: remaining ticks before the module may reactivate
- current output strength: the module's final output after modifiers

### 6.5 Why Modules Matter

Modules are how evolution gets variety.

- Duplicated modules can specialize.
- Fused modules can become more efficient or more fragile.
- Mutated modules can become obsolete or unexpectedly useful.
- The same basic module family can support many life strategies.

---

## 7. Cell Model

A cell is the basic autonomous living unit.

### 7.1 Cell Identity

Each cell should have:

- a unique ID
- a genome reference
- a module set
- a position
- an orientation if needed
- an energy state
- a physical state
- a cluster membership reference if bonded
- an alive or dead flag

### 7.2 Cell Traits

Derived traits should include values such as:

- energy capacity
- membrane strength
- membrane permeability
- mass
- volume
- pressure load
- storage capacity
- movement efficiency
- signaling efficiency
- digestion efficiency
- replication readiness
- bond capacity
- sensory noise

### 7.3 Cell State vs Expression

Separate these concepts clearly:

- Genome: inherited data
- Expression: what the genome currently produces
- State: the runtime condition of the cell

This avoids the old confusion where the same term was used for both inherited structure and active behavior.

---

## 8. Behavior Model

Behavior should be rule-based, but not rigid.

### 8.1 Instruction Chain Model

An instruction chain should be a compact ordered unit with fields like:

- token sequence
- opcode
- operand token list
- target or scope
- payload parameters
- priority
- probability
- repeat count
- optional loop or block markers

Loop and block markers are optional control tokens. If enabled for a scenario, a loop repeats the immediately previous valid rule block up to a decoder-defined `maxLoopCount`; if disabled, loop markers decode as junk.

An instruction chain is the genome-side representation. When it validates, it emits one runtime rule or a small runtime rule block. Rules are the runtime objects that the sim evaluates.

If the chain is valid, it emits a runtime rule. When that runtime rule executes, it can produce one or more intents. If the chain is malformed, it aborts without effect unless the opcode defines a registered failure effect. A no-op is valid and explicit; it is not a malformed chain.

### 8.2 Rule Evaluation

Runtime rules should be evaluated from local context:

- self state
- module state
- neighbor state
- cluster state
- environment fields
- signal values
- random input from seeded RNG

### 8.3 Intent-Based Execution

Instruction chains should not directly mutate the world.

Instead, they decode into runtime rules, and runtime rules create intents.

Example intent types:

- move
- attach
- detach
- emit signal
- absorb resource
- digest material
- reinforce membrane
- exchange energy
- repair
- replicate
- split
- bud
- defend
- attack
- idle


The resolver is part of the simulation core. It validates module permissions, input requirements, and target compatibility immediately before applying an effect; presentation code never performs this validation.
A resolver system then applies those intents in a deterministic order through registry-backed effect handlers.

The resolver must validate module permissions, input requirements, and target compatibility before applying any effect.

This is important because it keeps the sim modular, testable, and parallel-friendly.

### 8.4 Why This Is Better Than a Handwritten AI Tree

- Small genome changes can alter entire instruction chains.
- Behavior stays evolvable instead of becoming a fixed script.
- The same instruction engine can handle many life strategies.
- The simulation core does not need to know the meaning of every organism type.

---

## 9. Energy, Mass, and Pressure

These are the main physical constraints that keep the sim grounded.

### 9.1 Energy

Energy is the universal currency.

It is used for:

- upkeep
- movement
- communication
- module activation
- bonding
- replication
- repair
- defense
- digestion

Energy should come from:

- environment uptake
- digestion of resources or prey
- cooperative transfer inside clusters
- specialized metabolic modules

Tick accounting is explicit: passive upkeep is paid first, then resolved intents pay their action costs, then gains are applied, then energy is clamped to storage capacity. Recovery means gaining energy through uptake, digestion, cluster transfer, or repair before the end-of-energy-phase check. If energy remains below zero after that check, the cell dies. A failing cell may finish intents already queued for the current tick, but it may not queue new intents after it enters the failing state.

### 9.2 Mass and Volume

Use these terms consistently:

- mass: inertial and load cost
- volume: occupied space
- membrane strength: how much load the boundary can support

Do not use weight and mass interchangeably.

### 9.3 Pressure

Pressure should be derived, not arbitrary.

A simple conceptual model:

- more volume pushes against the membrane
- more membrane strength lowers pressure
- cluster support can reduce effective pressure
- too much pressure causes damage or rupture

Use a canonical derived pressure model:

- `supportFactor = 1 + sum(bondSupportContribution from live bonds)`
- `pressure = totalVolume / (membraneStrength * pressureToleranceFactor * supportFactor)`
- `pressure <= 1` is safe
- `pressure > 1` accumulates damage at `pressureDamageScale * (pressure - 1)` per tick
- `pressure >= ruptureThreshold` or unrepaired damage above tolerance causes rupture

If a bond breaks or a bonded neighbor dies, the lost support is removed immediately. This can raise pressure on surviving members and trigger cascade failure.

Repair intents or repair modules reduce accumulated damage during the damage phase. Pressure damage is not the only damage source; registered damage effects may also apply damage explicitly.

### 9.4 Maintenance Cost

Every cell should have a maintenance burden.

This can depend on:

- total module count
- total volume
- total mass
- number of bonds
- signal activity
- movement activity
- regulatory complexity

This is one of the main ways to create an evolutionary ceiling.

Canonical maintenance cost:

- `maintenanceCost = baseMaintenance + moduleCount * moduleMaintenance + totalVolume * volumeMaintenance + totalMass * massMaintenance + bondCount * bondMaintenance + signalActivity * signalMaintenance + regulatoryComplexity * regulatoryMaintenance`

Where `regulatoryComplexity = activeRuleCount + distinctConditionTypeCount`, and `distinctConditionTypeCount` means the number of unique `(condition source, comparator)` pairs used by active runtime rules.

`signalActivity` means the total emitted signal strength accumulated by the cell during the current tick after intents resolve and before maintenance is computed.

`baseMaintenance` is the species- or scenario-defined baseline upkeep for an otherwise idle cell.

### 9.5 Cost Generalization

No action should have a universal permanent cost unless that cost comes from a module or a species genome setting.

A good general formula pattern is:

base cost x species modifier x module modifier x state modifier x environment modifier

That keeps the sim flexible and evolvable.

`state modifier` is derived from current runtime state such as energy ratio, damage, bond count, and active/inactive module state.

---

## 10. Multicell and Cluster Model

Multicellularity should emerge from the same rules as single-cell life.

### 10.1 Cluster Definition

A cluster is a bonded graph of cells.

It is not a separate magic species class.

A cluster can be treated as an organism-level structure for convenience, but the simulation should still operate on cells and bonds.

The exact path from single cells to clusters is intentionally left open. The core only needs the ingredients and constraints that let evolution discover whether bonding, sharing, and specialization are useful in a given environment.

### 10.2 Bonding

Bonds should have data such as:

- bond strength
- bond maintenance cost
- signal transfer quality
- energy transfer quality
- material transfer quality
- mechanical support contribution
- maximum distance or adjacency rule

Bond upkeep is paid by the bonded endpoints, split evenly unless a scenario overrides the split. If one endpoint cannot pay its share, the unpaid portion contributes to decay; the bond breaks when integrity crosses the break threshold.

Bond integrity decays by `bondDecayRate` each tick when upkeep is unpaid or the bond is damaged. If integrity drops to or below `bondBreakThreshold`, the bond breaks.

Energy transfer quality governs explicit energy-sharing intents. Material transfer quality governs matter exchange. They both use the bond graph, but they are separate effects.

### 10.3 Cluster Benefits

Clusters can provide:

- pressure sharing
- energy sharing
- structural support
- division of labor
- coordinated movement
- internal signaling
- specialized cell roles

Pressure sharing is passive and comes from the live bond graph. Energy sharing is not automatic; it happens only when a cell emits a registered transfer intent through a valid bond, subject to `energyTransferRate` and `energyTransferLossRate`. Material transfer is separate and uses `materialTransferRate`. This keeps clustering beneficial without making it free.

An energy transfer intent moves at most `energyTransferRate` energy per tick. If the donor requests more than it can currently afford, the transfer is capped to the donor's available post-upkeep energy. The donor pays the sent amount, and the recipient receives `sentAmount * (1 - energyTransferLossRate)`. Material transfer uses the same bond network but does not imply energy transfer.

### 10.4 Cluster Costs

Clusters should also have costs and failure modes:

- bond upkeep
- communication delay
- parasitism
- resource theft
- collapse when critical links fail
- increased maintenance load
- slower response to local changes

The exact coefficients for these costs are tuning values, not hardcoded biology rules.

If a cluster member dies or a critical bond fails, neighboring members immediately lose the bond support contribution from that link. A critical bond is any bond whose loss disconnects a live cell from the cluster graph or removes the last support contribution keeping that cell below pressure threshold. Cascade failure is processed breadth-first once per tick, and each cell or bond may fail at most once per tick. If that loss pushes pressure above threshold, the cluster can cascade into further rupture. Cluster fragmentation is therefore a normal failure mode, not a special case.

### 10.5 Differentiation

Cells in clusters should be able to become specialized by local context.

Examples:

- outer cells protect
- inner cells store or reproduce
- some cells move
- some cells digest
- some cells signal

This should emerge from local rules and environmental pressure, not from a separate multicell controller. Local signal gradients, bond depth, and position in the cluster provide different inputs to the same genome, so outer and inner cells can activate different modules without a separate coordinator.

---

## 11. Environment Model

The environment should be generic and extensible.

### 11.1 Environment as Channels and Sources

Instead of hardcoding only one or two environment variables, treat the environment as a set of channels.

Examples of channels:

- nutrients
- light
- heat
- toxins
- viscosity
- pH
- obstacles
- pheromones
- waste
- resource gradients

Each channel can be represented by a field, map, or source model depending on the scenario.

### 11.2 Environment API

The core should expose generic sampling functions such as:

- sample channel at position
- emit into a channel
- consume from a channel
- diffuse a channel
- decay a channel
- regenerate a channel

### 11.3 Why Channels Help

This keeps the sim open-ended.

- New ecological pressures can be added without changing the core loop.
- Different scenarios can choose different channels.
- Cells can evolve toward different niches depending on which channels matter.

---

## 12. Mutation and Reproduction

Reproduction and mutation are where new possibilities appear.

### 12.1 Reproduction

Replication should be another action, not a special outside-the-sim event.

A reproduction mode might include:

- split
- bud
- spore
- burst release
- fragment release
- clustered reproduction

### 12.2 Gating Conditions

Replication can require:

- enough energy
- acceptable pressure
- required modules active
- sufficient membrane stability
- suitable environment or signal context

### 12.3 Mutation Operators

Mutation should include more than just tiny numeric drift.

Possible operators:

- substitution
- insertion
- deletion
- duplication
- inversion
- transposition
- frame shift
- split
- fuse
- parameter drift
- activation flip
- module duplication
- module loss
- rule add/remove
- rule relink

### 12.4 Mutation Sources

Mutation rate can depend on:

- species genome
- module sensitivity
- replication mode
- environmental stress
- damage state
- random spikiness

### 12.5 Junk and Neutral Space

Not every genome region should do something useful.

Neutral or junk segments matter because they give evolution room to explore without immediate failure.

---

## 13. Simulation Loop

The simulation should run in a fixed logical tick.

### 13.1 Recommended Tick Phases

1. Apply passive upkeep and bond upkeep.
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

This phase order is canonical. Parallel work is allowed only if it preserves the same logical order of effects and keeps the tick semantics unchanged.

### 13.2 Why This Order Matters

- Decision and resolution stay separate.
- Conflicting actions can be resolved deterministically.
- The world becomes easier to debug.
- Parallelization becomes easier later if needed.

### 13.3 Determinism

The sim should be deterministic for a given seed and update order. The seed comes from simulation settings, is read once at world creation, and then remains immutable for that world. The world owns the single RNG stream used by all simulation code.

The tick phase order in Section 13.1 is canonical and may not be reordered without a versioned compatibility change.

No code path outside the world RNG may consume randomness; that includes debug code, inspection code, and Unity presentation code.

That helps with:

- debugging
- comparison of evolutionary runs
- automated testing

---

## 14. Unity Frontend

Unity is only the viewer and interaction layer.

### 14.1 What Unity Does

- display cells and clusters
- visualize signals and fields
- show stats and graphs
- allow pause, speed control, and inspection
- render debug overlays
- load and save snapshots

### 14.2 What Unity Does Not Do

- it does not own authoritative cell state
- it does not decide evolution rules
- it does not contain the simulation core logic
- it does not need to be present for headless runs

### 14.3 Snapshot Contract

The core should provide snapshots on demand. Unity can request a bounded region such as between `(x1, y1)` and `(x2, y2)`, and the core returns a read-only list of cells and their current states for that region. The world owns `SnapshotBuffer` if the core uses one to package that data, but Unity only reads the latest completed result and never decides authoritative state.

Snapshots are immutable value data. They contain no live references into the simulation world.

Each snapshot must include:

- `tick`
- the read-only fields required by Unity or analysis tools

Any metric shown in a snapshot must be computed before the result is handed out. No snapshot field may trigger lazy evaluation or mutate world state when read.

Snapshots and read-only views must contain only value types, immutable records, or deep-copied arrays. Mutable reference fields are forbidden.

A snapshot can include:

- cell positions
- cell sizes
- cell colors or state tags
- cluster outlines
- signal intensity maps
- resource fields
- selected debug metrics

Unity can render from those snapshots without touching live simulation state.

### 14.4 Read-Only Inspection API

Unity and external tools may only inspect snapshot data or explicitly exposed read-only query objects.

Read-only queries must not:

- mutate world state
- allocate new simulation objects
- consume RNG
- trigger lazy recomputation of traits, rules, or modules

If a value is needed for inspection, it must already exist in the snapshot or in a cached read-only view generated during the tick. A cached read-only view is derived data, not an authoritative store, and it must obey the same immutability rules as the snapshot.

---

## 15. Extensibility Rules

These rules keep the design modular instead of turning into a pile of special cases.

1. If something can be expressed as data, prefer data.
2. If something can be registered, prefer a registry.
3. If something can be generic, avoid species-specific code.
4. If something is a runtime effect, separate it from presentation.
5. If something is a decision, route it through the rule and intent system.
6. If something is a constraint, keep it universal.
7. If something is a new life strategy, try to express it through new genome content or module definitions before adding new core logic.

### 15.1 What Should Be Hardcoded

Only the core physical and computational laws:

- tick order
- energy accounting
- pressure and damage rules
- mutation resolution
- intent resolution
- state storage model
- snapshot extraction

### 15.2 What Should Not Be Hardcoded

- species-specific behaviors
- one-off cell classes
- bespoke multicell logic
- custom behavior trees for a single organism type
- rendering details
- hardwired environmental niches

---

## 16. Suggested Core Project Shape

One clean way to organize the project:

- EvoSim.Core
  - world state
  - stores
  - genome decoding
  - module and rule systems
  - mutation and reproduction
  - environment logic
  - snapshot generation

- EvoSim.Unity
  - rendering
  - UI
  - debugging views
  - playback controls

- EvoSim.Tools
  - headless runner
  - benchmark runner
  - save/load utilities

- EvoSim.Tests
  - deterministic behavior tests
  - mutation tests
  - reproduction tests
  - snapshot tests

---

## 17. Open Choices Left Intentionally Flexible

These are design decisions that should remain configurable rather than locked too early:

- exact token size or genome decoding scheme
- exact module families at v1
- exact set of environment channels
- exact reproduction modes
- exact mutation operator weights
- exact rendering style
- exact field/grid resolution
- exact size of snapshots and render cadence

The core architecture should support different answers to these choices without needing a rewrite.

---

## 18. One-Line Summary

This v2 should be a headless, modular, data-driven evolutionary simulation core where genomes express modules, traits, and rules, cells interact through energy and physics, clusters emerge from bonds, and Unity only renders snapshots of the result.
