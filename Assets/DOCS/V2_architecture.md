# V2 Engine Architecture

This document defines the structure of the engine that will implement the V2 plan. It is intended to be directly actionable for code organization, data ownership, and runtime flow.

## 1. Architecture Summary

The engine is split into two parts:

- A headless simulation core that owns all authoritative state and all simulation rules.
- A Unity presentation layer that only queries read-only data and renders what the core produces.

The core must run without Unity.
The Unity layer must never own authoritative state.

The runtime model is:

World settings -> WorldState -> tick phases -> core stores -> snapshot query -> Unity render

The engine should be data-oriented, deterministic, and registry-driven. Complex behavior should come from genomes, modules, rules, and world state rather than from hardcoded special cases.

## 2. Layered System Model

### 2.1 World Layer

This is the top-level runtime owner.

Responsibilities:
- store the world seed
- own the single world RNG
- track the tick counter
- hold global settings
- own registries
- own all simulation stores
- execute the tick loop
- produce read-only snapshot data

### 2.2 Simulation Layer

This is the rule and state engine.

Responsibilities:
- decode genomes
- update expression
- evaluate runtime rules
- queue intents
- resolve intents
- apply physics and resource changes
- manage reproduction, death, bonding, clustering, and mutation

### 2.3 Presentation Layer

This is Unity only.

Responsibilities:
- request snapshots or region queries
- render cells, clusters, fields, and debug overlays
- show stats and inspection data
- provide camera, speed, and pause controls

Unity must not mutate core state directly.

## 3. Assembly and Namespace Layout

Use one clear split between core and presentation.

Recommended namespaces:

- `EvoCellSim.Core`
- `EvoCellSim.Core.World`
- `EvoCellSim.Core.Data`
- `EvoCellSim.Core.Genome`
- `EvoCellSim.Core.Behavior`
- `EvoCellSim.Core.Physics`
- `EvoCellSim.Core.Multicell`
- `EvoCellSim.Core.Snapshot`
- `EvoCellSim.Core.Registry`
- `EvoCellSim.Unity`
- `EvoCellSim.Unity.Rendering`
- `EvoCellSim.Unity.Inspection`

Recommended assembly split:

- Core simulation assembly
- Unity adapter assembly
- optional tests assembly

Rules:
- core assemblies cannot depend on Unity APIs
- Unity assemblies may depend on the core
- tests should be able to instantiate the core without a scene

## 4. Ownership Model

### 4.1 WorldState

`WorldState` is the top-level owner of simulation runtime data.

It owns:
- seed
- tick
- settings
- RNG
- registries
- store references
- snapshot buffer or snapshot output staging

### 4.2 Stores

The simulation state should live in flat stores or chunked buffers, not a deep object graph.

Recommended stores:
- `CellStore`
- `GenomeStore`
- `ModuleStore`
- `BondStore`
- `ClusterStore`
- `FieldStore`
- `SignalStore`
- `IntentQueue`

Optional support stores:
- `SpeciesStore`
- `EnvironmentStore`
- `DebugTraceStore`

Ownership rules:
- each store is owned by `WorldState`
- stores may reference each other by stable IDs or indices
- live objects should not be exposed to Unity
- snapshots must never contain live references back into stores

## 5. Core Data Model

### 5.1 Cell Record

A cell should have a compact runtime record with the fields needed for simulation and snapshot queries.

Typical fields:
- cell ID
- position
- velocity or movement state if needed
- energy
- mass
- volume
- membrane strength
- pressure
- damage
- alive state
- genome references or genome IDs
- module references
- cluster membership ID
- local signal state
- runtime flags

### 5.2 Genome Record

Genome data should be stored separately from runtime cell state.

A cell should not embed large genome blobs directly unless the storage model explicitly chooses that.

Genomes should be split conceptually into:
- species genome
- module genome
- instruction genome

The runtime should access them through genome IDs or stable store indices.

### 5.3 Module Record

Modules are sub-structures inside a cell.

Typical module fields:
- module ID
- owner cell ID
- module type ID
- activation state
- runtime parameters
- cost values
- output values
- compatibility flags
- mutation sensitivity

### 5.4 Bond Record

A bond connects two cells.

Typical fields:
- bond ID
- endpoint A cell ID
- endpoint B cell ID
- bond integrity or bond health
- bond maintenance cost
- transfer quality values
- decay state
- break threshold

### 5.5 Cluster Record

A cluster is a bonded connected component or cluster state object derived from bonds.

Typical fields:
- cluster ID
- member cell IDs or member references
- cluster root or leader marker if needed
- cluster-level cached metrics
- signal gradient data
- structural depth data

### 5.6 Signal and Field Records

Signals and fields are short-lived environment or neighborhood data.

Typical fields:
- signal source
- signal target or region
- signal type
- intensity
- lifetime
- attenuation data
- diffusion or propagation state

### 5.7 Intent Record

An intent is a queued action produced by rules and resolved later.

Typical fields:
- intent ID
- source cell ID
- target ID or target region
- intent type
- numeric payloads
- priority
- validity flags
- required resources

## 6. Registry Architecture

The architecture should use registries so behavior is data-driven.

### 6.1 Required Registries

- opcode registry
- effect handler registry
- module definition registry
- mutation operator registry
- token pattern registry
- condition and comparator registry
- snapshot field registry if needed

### 6.2 Registry Responsibilities

Registries should:
- map IDs or names to definitions
- supply decoding signatures
- describe validation rules
- provide effect execution hooks
- describe allowed module actions

Registries should not:
- hold live world state
- mutate cells directly
- depend on Unity

## 7. Runtime Flow

The core should execute the same logical flow every tick.

### 7.1 Tick Phases

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

### 7.2 Phase Contracts

Each phase should have a clear input and output.

Examples:
- upkeep consumes energy from cells and bonds
- context sampling reads from the world and stores local values
- rule evaluation reads expression state and produces intents
- intent resolution validates and applies effects
- physics phases update derived loads and damage
- cluster phases maintain bond graph consistency
- reproduction phases may create new genomes and cells
- snapshot phases copy read-only values out for Unity

### 7.3 Deterministic Ordering

Determinism requires stable ordering.

Rules:
- the tick phase order is fixed
- entity iteration order must be stable
- tie-breaks must be deterministic
- randomness can only come from the world RNG
- no Unity code may alter simulation ordering

## 8. Genome and Behavior Architecture

### 8.1 Genome Layers

The architecture uses three genome layers:
- species genome
- module genome
- instruction genome

### 8.2 Decoding Pipeline

Genome processing should follow this path:

raw genome bytes -> token scan -> chain decode -> rule/trait/module expression -> runtime phenotype

The decoder should:
- read tokens from the genome registry
- preserve junk
- preserve malformed sequences as deterministic failures
- emit structured decoded instructions rather than direct simulation effects

### 8.3 Behavior Pipeline

Instruction chains should not directly mutate world state.

They should produce:
- runtime rules
- intent templates
- trait deltas
- module state changes
- signal actions

Then the resolver validates and applies them.

### 8.4 Mutation Architecture

Mutation should operate on the genome layers separately.

Required mutation operators:
- substitution
- insertion
- deletion
- duplication
- inversion
- transposition
- frame shift effects

Mutation should be noisy enough to create new behavior, but deterministic under the same seed and event order.

## 9. Physics and Survival Architecture

### 9.1 Core Physical Variables

The simulation should track:
- energy
- mass
- volume
- pressure
- damage
- membrane strength
- upkeep and maintenance costs

### 9.2 Pressure and Damage

Pressure should be derived from world state and stored in the cell record.
Damage should accumulate when pressure or other stressors exceed safe limits.
Repair should reduce accumulated damage through explicit effects.

### 9.3 Cost Model

Costs should be split conceptually:
- upkeep for direct per-tick expenses
- maintenance for broader whole-cell burden
- bond costs for network structure
- action costs for intents and active effects

This keeps the balance model simple enough to tune globally.

## 10. Multicell Architecture

### 10.1 Bonds

Bonds are explicit graph edges between cells.

Bond logic should support:
- creation
- upkeep
- decay
- breakage
- transfer across bonds
- cluster membership changes when bonds fail

### 10.2 Clusters

Clusters should be derived from the active bond graph.

Cluster logic should support:
- connected component updates
- cached cluster metrics
- local structural position values
- depth or distance data
- cascade failure when the bond graph breaks

### 10.3 Differentiation Inputs

To support multicell emergence, the core should provide deterministic local inputs such as:
- local neighbor state
- signal gradients
- bond depth
- structural position inside the cluster

These should be sampled from core stores, not from Unity.

## 11. Snapshot Architecture

### 11.1 Snapshot Role

Snapshots are read-only outputs for Unity and analysis.

They should be:
- immutable
- value-based
- copied or materialized from core state
- free of live world references

### 11.2 Snapshot Shape

Unity should be able to request a bounded region and receive:
- cell states in that region
- cluster outlines if needed
- signal or field display data if needed
- the tick number

### 11.3 Snapshot Rules

- snapshot generation should not mutate world state
- snapshot reads should not trigger lazy evaluation
- snapshot fields should be computed in the core and copied out
- snapshot storage should be simple enough for rendering and inspection

## 12. Unity Integration Architecture

Unity should stay thin.

### 12.1 Unity Responsibilities

- request snapshots or region data
- render visual state
- provide camera and UI controls
- show debug overlays and statistics

### 12.2 Unity Restrictions

- no authoritative cell state
- no direct rule execution
- no random generation
- no mutation of world stores

### 12.3 Adapter Boundary

The Unity layer should call a small adapter surface on the core rather than reaching into stores directly.

Recommended adapter concepts:
- world runner
- snapshot provider
- inspection query API
- optional debug hooks

## 13. Determinism Architecture

The entire design depends on deterministic execution.

Rules:
- one seed creates one world RNG
- all randomness comes from the world RNG
- iteration order is fixed and stable
- no hidden side effects in snapshot reads
- no Unity-owned randomness in the core path
- tie-breaks are explicit

Determinism should be maintained by design rather than patched afterward.

## 14. Recommended Implementation Shapes

These are practical choices that fit the architecture.

- Use integer IDs or stable indices for store references.
- Use flat arrays or chunked buffers for high-volume state.
- Use registries for behavior lookup instead of large switch chains.
- Use intent queues instead of direct mutation from rule evaluation.
- Use snapshots as copied read-only data rather than live references.
- Keep the core world runner as the only place that advances time.

## 15. Build-Level Architecture Milestones

1. Create `WorldState` and store containers.
2. Implement deterministic tick execution.
3. Implement genome decoding and registries.
4. Implement intent generation and resolution.
5. Implement survival physics.
6. Implement bonds and clusters.
7. Implement mutation and reproduction.
8. Implement region snapshot queries.
9. Build the Unity adapter and renderer.
10. Tune global constants.

## 16. Architecture Completion Criteria

The architecture is ready when:

- the core can run headless,
- all authoritative state lives inside the core,
- the tick loop is deterministic,
- genomes decode through registries,
- intents are queued and resolved in order,
- bonds and clusters behave consistently,
- snapshots are read-only and region-based,
- and Unity only renders what the core provides.
