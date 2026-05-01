# **EVOLUTIONARY CELL SIMULATION, FULL SYSTEM SPECIFICATION**

This simulation models emergent life using cells as modular entities, each driven by a genome that encodes both fundamental physical properties and component-based traits. Cells interact with an environment, evolve through mutations, and can form multicellular clusters. Behavior, energy management, and physical constraints all emerge naturally from the interaction of genome, components, and environment.

---

## **1\. Genome System**

Each cell has two genome layers:

1. **Base Genome** — defines the core species-level properties, physical limits, and constraints.

2. **Component Genome** — encodes modular components (organelles) that give the cell its functional abilities.

### **1.1 Base Genome**

The base genome sets hard ceilings and species-level characteristics:

* **membranePermeability** — how easily nutrients and toxins cross the membrane

* **membraneStrengthBase** — base membrane strength, affects maximum volume before rupture

* **metabolismEfficiencyBase** — baseline multiplier for energy gains from metabolic components

* **mutationBaseRate** — controls overall mutation probability

* **mutationSpikiness** — probability of catastrophic macro-mutations

* **replicationMode** — reproduction strategy (binary split, budding, sporulation, high-output replication)

* **replicationEnergyThreshold** — minimum energy required for replication

* **componentLimit** — maximum number of components allowed in a cell

* **pressureToleranceFactor** — scales how much volume a membrane can withstand

* **signalNoiseLevel** — baseline randomness for signal detection and processing

* **energyStorageBase** — baseline energy storage capacity

* **digestionBaseEfficiency** — baseline multiplier for digesting food or other cells

* **movementBaseEfficiency** — baseline energy cost modifier for movement actions

The base genome mutates **rarely and slowly**, setting a ceiling that prevents unlimited optimization and allows the species to specialize without runaway growth.

### **1.2 Component Genome**

The component genome is a flexible list of genes, each encoding a single component. Each gene contains:

`ComponentType (enum)`  
`SizeGene (float)`  
`EnergyGene (float)`  
`SignalGene (float)`  
`MutationRate (float)`  
`ActivationState (bool)`

During cell creation, the genome is interpreted to produce components. Mutations can:

* add, remove, duplicate, or fuse components

* change component type

* adjust gene values

* flip activation state

This allows **small mutations to create dramatic changes in behavior**.

---

## **2\. Component System**

Components are the building blocks of cells, analogous to organelles. They define capabilities, behaviors, and costs. Each component has properties:

* **volume** — physical size contributing to cell pressure

* **weight** — mass affecting movement and energy cost

* **upkeepCost** — energy cost per tick

* **activationCost** — energy cost to perform an action

* **energyGain** — energy gained when active (if applicable)

* **signalOutput** — ability to emit signals to environment or other cells

* **signalThreshold** — threshold needed to activate behavior

* **functionType** — e.g., movement, adhesion, digestion, replication, signaling

* **mutationSensitivity** — how likely this component is affected by mutation

Components provide **primitives** for behavior: movement, energy harvesting, adhesion, signaling, attacking, reproducing, and more. The combination of components and their interactions produces **emergent behavior** without hand-coded rules.

---

## **3\. Cell System**

A cell is a container for components with emergent behavior. Its properties include:

* **components\[\]** — list of components

* **totalVolume** — sum of component volumes

* **totalWeight** — sum of component weights

* **shellStrength** — derived from base genome and structural components

* **pressure** — totalVolume / shellStrength × pressureToleranceFactor

* **energy** — current energy available

* **surfaceArea** — affects nutrient absorption and signal emission

* **maxBondConnections** — max allowed bonds for multicell clusters

* **metabolismEfficiency** — derived from base genome \+ component modifiers

* **movementAbility** — derived from movement components \+ genome efficiency

* **signalState** — state of emitted and received signals

* **isAlive** — whether the cell is active

### **3.1 Cell Lifecycle (per tick)**

1. Sum component upkeep costs and subtract from energy

2. Add energy gained from metabolic components

3. Evaluate genome-derived behavior rules

4. Execute actions (movement, adhesion, signaling, digestion, replication, etc.)

5. Update internal pressure; check for rupture

6. Attempt replication if energy and pressure conditions are met

7. Propagate signals to neighbors or cluster

Cells die if energy ≤ 0 or if pressure exceeds shell capacity.

---

## **4\. Energy System**

Energy is fully emergent:

* Each component has **energy cost** or **gain**

* Base genome sets **baseline metabolism**

* Action costs are dynamic: movement, adhesion, signaling, digestion, and replication all have costs scaled by component and genome efficiency

* Energy can come from environment: light, chemicals, consuming other cells

* Energy storage is capped by **energyStorageBase \+ storage components**

* Multicell clusters can share energy, introducing cooperative dynamics

This system allows evolution of energy-efficient or high-risk, high-gain strategies.

---

## **5\. Pressure and Size Constraints**

Cells have physical limits:

* **pressure \= totalVolume / (shellStrength × pressureToleranceFactor)**

* If pressure \> 1 → cell ruptures

* Membrane strength and pressure tolerance can evolve but trade off with diffusion and energy efficiency

* Multicellular clusters allow pressure sharing, enabling larger structures while introducing maintenance costs

---

## **6\. Behavior Encoding (Emergent Rules)**

Behavior emerges from the genome and components:

* Genome is chunked into **codons** (3–5 characters each)

* Codons can encode:

  * **Conditions**: internal state, signals, position, random triggers

  * **Actions**: movement, adhesion, signaling, digestion, replication

  * **Modifiers**: intensity, duration, probability, direction

  * **Control flow**: loops, start/end blocks

  * **Junk**: inert codons for mutation space

* Multi-codon instructions allow **complex actions** and **conditional logic**

* Codons are interpreted sequentially to form a **rule graph**: nodes \= conditions/actions/modifiers, edges \= flow, loops \= repeated actions

* Mutations shift frames, add/remove nodes, or modify parameters → **small changes can radically change behavior**

---

## **7\. Reproduction and Mutation**

Cells reproduce when conditions are met:

* Enough energy

* Pressure below threshold

* Replication components active

During replication:

* Genome is copied and mutated

* Mutations include: duplication, deletion, fusion, splitting, gene value drift, activation flips, type randomization

Base genome mutates **rarely**, producing slow macro-evolution. Component genome mutates more readily, creating fast emergent changes.

---

## **8\. Multicell Clusters**

Cells can bond to form clusters:

* Bonding is an energy-consuming component

* Clusters allow pressure sharing, defense, energy distribution

* Clusters enable specialization: different cells perform different roles

* Cluster maintenance introduces trade-offs: signaling delays, parasitism, cluster collapse

Emergent multicellularity arises naturally from component interactions and energy/pressure constraints.

---

## **9\. Environment System**

Environment defines:

* light, heat, chemicals, toxins

* terrain obstacles

* resource gradients

* other cells (predators, prey, clusters)

Cells sense and react via their components, which interpret the environment as part of their codon-based behavior graph.

---

## **10\. Emergent Dynamics**

With this architecture, you can observe:

* self-organizing clusters

* predator-prey cycles

* parasitism and symbiosis

* specialization of cells in clusters

* evolutionary trade-offs driven by energy, pressure, and component costs

* catastrophic or beneficial mutations reshaping behavior

---

### **✅ Summary**

* **Base genome** sets species-level physical and metabolic limits

* **Component genome** encodes organelles that generate abilities and actions

* **Codon-based genome interpretation** produces condition → action mappings dynamically

* **Energy and pressure systems** enforce natural ceilings and trade-offs

* **Mutations** drive evolution of structure, behavior, and energy efficiency

* **Clusters** allow emergent multicellularity

* **Environment** shapes selection pressure

Everything — structure, behavior, energy, and cooperation — is **emergent** and evolves naturally from genome, components, and constraints.

---

# **SYSTEM RELATIONSHIP OVERVIEW**

Think of the cell simulation as **a hierarchy of interacting systems**, each feeding into the next:

`[Environment] ↔ [Cell Clusters] ↔ [Cells] ↔ [Components] ↔ [Behavior Graphs from Genome]`

Here’s how each system relates:

---

## **1️⃣ Base Genome → Physical Limits**

* The **base genome** defines hard ceilings: membrane strength, pressure tolerance, replication thresholds, metabolic efficiency, component limits, signal noise.

* It sets **the “physics” and “rules of life”** for that species.

* All other systems (components, actions, clusters) must operate **within these constraints**.

---

## **2️⃣ Component Genome → Functional Capabilities**

* Each gene produces a **component** (organelles).

* Components define **what the cell *can do***: move, digest, store energy, bond, emit signals, attack, etc.

* Components have costs: volume, weight, energy upkeep, activation energy.

* Components interact with the base genome: e.g., membrane strength caps maximum volume; metabolism efficiency affects component energy gains.

---

## **3️⃣ Components → Behavior**

* Components provide **primitive actions and sensors**.

* Codons in the genome are interpreted into **behavior graphs**, linking conditions to actions with modifiers.

* Components produce signals and actions; the genome determines **when** and **how strongly** actions are triggered.

* This system produces **emergent behaviors**, like movement toward food, energy storage, or defensive maneuvers.

---

## **4️⃣ Energy & Pressure Systems → Constraints & Trade-offs**

* Energy:

  * Components and actions consume energy (upkeep, activation, movement, signaling).

  * Metabolic components generate energy based on efficiency.

  * Energy drives survival and replication.

* Pressure:

  * Cell volume / membrane strength determines pressure.

  * If pressure \> tolerance → rupture and death.

* Both systems enforce **natural ceilings**: cells can’t grow forever, act endlessly, or stack components infinitely.

---

## **5️⃣ Behavior Graph → Action Execution**

* Codons generate a **rule graph**: conditions → actions → modifiers.

* During a tick:

  * Conditions are evaluated (internal state, neighbor signals, environmental cues).

  * Actions are selected probabilistically or weighted.

  * Energy costs and pressure effects are applied.

* This is the **decision-making core** of the cell.

---

## **6️⃣ Cells → Multicell Clusters**

* Cells can bond to form clusters:

  * Share pressure load

  * Redistribute energy

  * Enable division of labor (some cells move, others digest, others defend)

* Cluster formation adds **emergent multicellularity** while maintaining constraints (bond costs, communication noise).

---

## **7️⃣ Environment → Selection Pressure**

* Nutrients, toxins, light, terrain, other cells, and clusters create selective pressures.

* These pressures influence which mutations survive and which behaviors are advantageous.

* Over time, energy, pressure, and component constraints drive evolution toward **adaptive strategies**.

---

# **EXAMPLE OF A SINGLE TICK**

Let’s walk through a concrete tick for a **single cell in a small environment**:

---

### **Step 0 — Setup**

* Base genome: defines membrane strength, pressure tolerance, metabolism efficiency.

* Component genome: has 3 components:

  * Energy harvester

  * Movement organelle

  * Signal emitter

* Current energy \= 12

* Volume \= 5, Membrane strength \= 10 → pressure \= 0.5

* Behavior graph from codons:

  * If energy \< 10 → move toward light source

  * If energy \> 8 → digest environmental nutrients

  * If neighbor signal \> 5 → emit warning signal

---

### **Step 1 — Component Upkeep**

* Each component consumes upkeep energy:

  * Energy harvester \= 1

  * Movement organelle \= 0.5

  * Signal emitter \= 0.2

* **Energy now \= 12 \- 1.7 \= 10.3**

---

### **Step 2 — Evaluate Behavior Graph**

* **Condition 1**: energy \< 10 → false (10.3 ≥ 10\)

* **Condition 2**: energy \> 8 → true → action: digest nutrients

* **Condition 3**: neighbor signal \> 5 → false

---

### **Step 3 — Execute Actions**

* **Digest nutrients**:

  * Base energy gain from digestion \= 5

  * Modified by metabolismEfficiencyBase × component digestion efficiency \= 1 × 1.2 \= 1.2

  * Energy gained \= 5 × 1.2 \= 6

* **Energy now \= 10.3 \+ 6 \= 16.3**

* **Pressure check**: digest action increases volume slightly → new volume \= 5.2 → pressure \= 5.2 / 10 \= 0.52 → still safe

---

### **Step 4 — Emit/Propagate Signals**

* No signal triggered this tick (condition false)

* Neighbor cells receive no new signals

---

### **Step 5 — Movement**

* Condition for movement false → no energy spent

* Position unchanged

---

### **Step 6 — Replication Check**

* Energy threshold \= 20 → not reached, so no reproduction

---

### **Step 7 — Update Environment**

* Nutrients in local patch decrease slightly due to digestion

* Other cells may read signals or compete next tick

---

### **Step 8 — Mutations (if applicable)**

* Mutation probabilities checked for base genome (low chance) and component genome (higher chance)

* No mutations triggered this tick

---

**End of Tick**

* Cell has energy \= 16.3

* Pressure \= 0.52

* Volume \= 5.2

* Components intact

* Behavior rules unchanged

* Ready for next tick

---

# **✅ Key Takeaways from the Tick**

* **Genome → components → behavior graph → actions → energy/pressure effects**

* **Energy and pressure enforce natural limits**

* **Behavior emerges dynamically** from codons and components

* **Environment provides feedback** that affects survival and evolution

* **Clusters and multicell strategies** can emerge if cells bond

* Small genome mutations could drastically alter next tick behavior

---

# **1️⃣ Classes, Structs, and Enums**

Here’s a clean object-oriented design:

---

## **Enums**

`enum ComponentType`  
`{`  
    `EnergyHarvester,`  
    `StorageSac,`  
    `Movement,`  
    `ShellEnhancer,`  
    `SignalEmitter,`  
    `SignalDetector,`  
    `ReplicationModule,`  
    `ToxinGenerator,`  
    `Adhesion,`  
    `AttackSpike`  
`}`

`enum ReplicationMode`  
`{`  
    `BinarySplit,`  
    `Budding,`  
    `Sporulation,`  
    `HighOutput`  
`}`

`enum ConditionType`  
`{`  
    `InternalEnergy,`  
    `Pressure,`  
    `NeighborSignal,`  
    `ClusterSize,`  
    `RandomChance`  
`}`

`enum ActionType`  
`{`  
    `Move,`  
    `Bond,`  
    `ReleaseSignal,`  
    `Digest,`  
    `StoreEnergy,`  
    `Replicate,`  
    `Attack,`  
    `HardenShell,`  
    `Detach,`  
    `StealEnergy`  
`}`

---

## **Structs**

### **1\. BaseGenome**

`struct BaseGenome`  
`{`  
    `float membranePermeability;`  
    `float membraneStrengthBase;`  
    `float metabolismEfficiencyBase;`  
    `float mutationBaseRate;`  
    `float mutationSpikiness;`  
    `ReplicationMode replicationMode;`  
    `float replicationEnergyThreshold;`  
    `int componentLimit;`  
    `float pressureToleranceFactor;`  
    `float signalNoiseLevel;`  
    `float energyStorageBase;`  
    `float digestionBaseEfficiency;`  
    `float movementBaseEfficiency;`  
`}`

### **2\. ComponentGene**

`struct ComponentGene`  
`{`  
    `ComponentType type;`  
    `float sizeGene;`  
    `float energyGene;`  
    `float signalGene;`  
    `float mutationRate;`  
    `bool activationState;`  
`}`

### **3\. Codon (for behavior genome)**

`struct Codon`  
`{`  
    `ConditionType conditionType;`  
    `ActionType actionType;`  
    `float threshold;`  
    `float probability;`  
    `int repeatCount; // for loops`  
`}`

---

## **Classes**

### **1\. Component**

`class Component`  
`{`  
    `ComponentType type;`  
    `float volume;`  
    `float weight;`  
    `float upkeepCost;`  
    `float activationCost;`  
    `float energyGain;`  
    `float signalOutput;`  
    `float signalThreshold;`  
    `float mutationSensitivity;`

    `void Activate(Cell cell); // execute component action`  
`}`

### **2\. Cell**

`class Cell`  
`{`  
    `BaseGenome baseGenome;`  
    `List<Component> components;`  
    `List<Codon> behaviorGenome;`

    `float energy;`  
    `float totalVolume;`  
    `float totalWeight;`  
    `float pressure;`  
    `bool isAlive;`

    `void EvaluateBehavior(Environment env, List<Cell> neighbors);`  
    `void ApplyEnergyCosts();`  
    `void CheckPressure();`  
    `void Reproduce();`  
    `void Mutate();`  
`}`

### **3\. Cluster**

`class Cluster`  
`{`  
    `List<Cell> cells;`

    `void SharePressure();`  
    `void ShareEnergy();`  
    `void CoordinateBehavior();`  
`}`

### **4\. Environment**

`class Environment`  
`{`  
    `float light;`  
    `float temperature;`  
    `Dictionary<Vector2, float> nutrients; // example: 2D nutrient map`  
    `List<Cluster> clusters;`

    `void Update(); // updates global signals, nutrient diffusion`  
`}`

---

## **Optional Helper Classes**

* **Vector2 / Vector3** — for positions

* **Signal** — for transmitting messages between cells or clusters

* **GenomeInterpreter** — converts codons into behavior graphs for evaluation

---

# **2️⃣ Performance Considerations & Recommended Tech Stack**

### **Core Requirements**

* Must handle **thousands to multiple millions of cells**

* Real-time UI visualization optional but desirable

* Efficient memory and CPU usage

---

### **2.1 Language & Runtime**

**Best options:**

---

### **2.2 Data Structure Optimization**

* Use **structs** (value types) for components/codons to reduce GC pressure

* Use **arrays or NativeArrays** for bulk updates

* Minimize heap allocations inside tick loops

* Multi-thread behavior evaluation per cell or cluster (parallelization)

---

### **2.3 Rendering Optimizations**

* Use **GPU instancing** to render millions of cells efficiently

* Keep visualization **separate from simulation logic** — simulation can run at higher tick rates without slowing UI

* Aggregate signals or stats to reduce per-frame rendering computations

---

### **2.4 Tick Loop Example**

* Parallelize `EvaluateBehavior` and `ApplyEnergyCosts`

* Update positions and pressure in bulk

* After all cells processed → render or update cluster/neighbor interactions

---

# V2

# **✅ High-level rules (kept & clarified)**

* Genome \= **3 layers**:

  1. **Base genome** — species-level physical/metabolic limits (rarely mutates).

  2. **Component genome** — list of component (organelle) blueprints; each gene spawns a component instance (mutable).

  3. **Behavior genome** — extremely simple condition→action rules (if X then Y), referencing component state / sensors. Rules are short and interpreted each tick.

* Components are **organelle primitives** (one gene → one component instance). A component can provide multiple predefined functions; efficiency modifies component-derived numeric stats (volume, weight, upkeep, activation cost, energyGain, signal strength).

* Pressure favors multicell: adjacent cells contribute to effective shell strength (simple and fast formula).

* Replication requires a **ReplicationModule** component (gate), energy, and acceptable pressure.

* Environment includes light, nutrients, heat, toxins, terrain, resource sources and point-sources; cells sense local values.

* Behavior rules are simple, deterministic or probabilistic (small probability field); mutations change probabilities and parameters, biasing growth not immediate micro-decisions.

---

# **1 — Genome (redefined)**

**Layer A — BaseGenome (rare mutation)**  
 fields (float unless noted)

* `membranePermeability`

* `membraneStrengthBase`

* `metabolismEfficiencyBase`

* `mutationBaseRate`

* `mutationSpikiness`

* `replicationMode` (enum)

* `replicationEnergyThreshold`

* `componentLimit` (int)

* `pressureToleranceFactor`

* `signalNoiseLevel`

* `energyStorageBase`

* `digestionBaseEfficiency`

* `movementBaseEfficiency`

**Layer B — ComponentGenome**  
 A list of `ComponentGene` (one gene → one component instance on creation). Genes mutate: add/remove/duplicate/fuse/drift activation.  
 `ComponentGene`:

* `ComponentType type`

* `float efficiency` (0..∞ — scales derived stats)

* `float sizeBias` (affects volume)

* `float energyBias` (affects upkeep & activation)

* `float signalBias` (affects signal output/threshold)

* `float mutationRate` (per-component)

* `bool activationState` (default active/inactive)

**Layer C — BehaviorGenome (very simple)**  
 A list of short rules (codons as compact if→then) executed sequentially or as priority list:  
 `BehaviorRule`:

* `ConditionType condition`

* `SensorTarget sensorTarget` (e.g., componentId or env variable)

* `float threshold`

* `ActionType action`

* `int targetComponentIndex` (optional)

* `float probability` (0..1) — optional randomization

* `int repeatCount` (optional, keep small)

Notes:

* Rules are intentionally simple: `if(sensor >= threshold) then action(target) with probability p`.

* Mutation affects `threshold`, `probability`, `targetComponent` or adds/removes rules.

* Complex control flow and graphs are **not** used — this keeps behavior cheap and interpretable.

---

# **2 — Component model & mapping (explicit)**

**ComponentGene → Component mapping rules (deterministic transformation)**  
 When a component is constructed from `ComponentGene`, derive final runtime component stats:

Given `efficiency E`, `sizeBias S`, `energyBias B`, base type constants (`baseVolume`, `baseWeight`, `baseUpkeep`, `baseActivationCost`, `baseEnergyGain`, `baseSignalOutput`):

* `volume = baseVolume * (1 + S) / (1 + E * volumeEfficiencyFactor)`

  * (higher efficiency can shrink or increase physical footprint depending on design; choose factor sign to match desired tradeoff)

* `weight = baseWeight * (1 + S)`

* `upkeepCost = baseUpkeep * (1 + B) / (1 + E * upkeepEfficiencyFactor)`

* `activationCost = baseActivationCost * (1 + B) / (1 + sqrt(E))`

* `energyGain = baseEnergyGain * (1 + E * energyGainFactor)`

* `signalOutput = baseSignalOutput * (1 + signalBias) * (1 + E * signalEfficiencyFactor)`

* `mutationSensitivity = gene.mutationRate` (plus a small base)

You can treat the `*Factor` constants as tunables in code (balance knobs). This mapping removes the earlier ambiguity.

**Component fields (runtime)**

* `id`

* `ComponentType type`

* `float volume`

* `float weight`

* `float upkeepCost`

* `float activationCost`

* `float energyGain` (if applicable)

* `float signalOutput`

* `float signalRange`

* `float signalThreshold`

* `bool active`

* `float mutationSensitivity`

---

# **3 — Pressure & multicell sharing (simple, favors clusters)**

We keep formula fast and intuitive.

**Single cell effective pressure:**

`pressure = totalVolume / (shellStrength * pressureToleranceFactor)`

Where `shellStrength = baseGenome.membraneStrengthBase + sum(shellModifiers from ShellEnhancer components)`

**Cluster-sharing heuristic (simple, per cell):**

* Let `S_own = shellStrength` of this cell.

* Let `S_neighbors = sum(shellStrength of bonded neighbors weighted by adjacencyWeight)` (use 1.0 for equal).

* Let `n = 1 + neighborCount` (include self).

* Define `effectiveShell = (S_own + S_neighbors) / n`.

* Then:

`pressure = totalVolume / (effectiveShell * pressureToleranceFactor)`

This naturally reduces pressure if neighbors have strong shells. It's simple, local, and O(k) where k \= bonds.

**Membrane stretching cost (energy upkeep penalty):**

* Compute `stretchRatio = max(0, (totalVolume / (baseVolumeCapacity)) - 1)`

* Additional upkeep cost per tick:

`stretchCost = stretchRatio * stretchCostFactor * totalVolume`

This makes larger cells more costly, discouraging infinite growth. `baseVolumeCapacity` can be `shellStrength`\-proportional.

If `pressure > 1` → rupture (instant death) OR probabilistic rupture if `pressure` slightly above 1 and `mutationSpikiness` low (optional).

---

# **4 — Energy & storage**

* `energyStorageCapacity = baseGenome.energyStorageBase + sum(storageIncrease from StorageSac components)`

* Energy flows:

  * `perTickEnergy = - sum(component.upkeepCost) - activityCosts + energyGained`

* Digestion/harvesting uses `digestionBaseEfficiency` × component energyGain modifiers.

* Sharing in cluster: bonded cells may transfer energy up to `transferRate`, with `transferLoss` (small percent). Transfers cost energy at donor end.

---

# **5 — Reproduction (ReplicationModule gating)**

Conditions to attempt replication:

* `energy >= replicationEnergyThreshold + replicationComponentCost`

* `pressure < replicationPressureThreshold` (e.g. 0.8)

* `ReplicationModule` component present and active

* optional replicationMode logic from base genome

During replication:

* Copy base genome with low mutation chance (rare).

* Copy component genome with higher mutation rates (per-gene `mutationRate`).

* Copy behavior genome with medium mutation.

* Component duplication can be biased by `growth` or `replicationMode`.

Replication cost is deducted and can be scaled by `replicationMode` (e.g., `HighOutput` produces multiple offspring at lower fidelity).

---

# **6 — Behavior execution & probabilistic influence**

* Each tick `EvaluateBehavior` runs rules in order until an action is executed, or all rules evaluated.

* For each rule:

  * Evaluate condition using sensor (component sensor or environment local reading).

  * If condition true:

    * Draw random `r` in \[0,1\]; if `r <= probability` then execute `action`.

* Mutations influence rule `probability` and thresholds — over generations this biases which actions are more common but not deterministic; this matches your "DNA influences growth and tendency" idea.

Actions are simple: `Move(direction / toward source)`, `EmitSignal(type, strength)`, `ActivateComponent(i)`, `Digest()`, `Replicate()`, `Bond(cellId)`, `Detach(cellId)`, `Attack(target)`.

---

# **7 — Movement (simple numeric model)**

Each movement component defines:

* `strength` (force)

* `maxSpeedMultiplier`

* `activationCost` (per action)

* `movementEfficiency` derived from `movementBaseEfficiency × component efficiency`

Movement resolution per tick:

`moveEnergyCost = activationCost * (1 / movementEfficiency) * (distanceFactor)`  
`speed = baseSpeed * strength * speedMultiplier`

`distanceFactor` \= desired move distance / baseDistance. Keep simple: if moving 1 unit, cost \= activationCost × factor.

---

# **8 — Signals (chemicals)**

* `signalOutput` and `signalRange` per component.

* Emission cost \= `signalOutputEnergyCostScale × emittedStrength`.

* Signals diffuse outward by radius; strength decays with distance (linear or inverse-square; linear is simpler).

* `signalNoiseLevel` in base genome adds Gaussian noise to received signal amplitude. `signalThreshold` on components or behavior rules decide reaction.

---

# **9 — Environment (expanded)**

Make environment a 2D grid or continuous space with sampling functions:

Fields:

* `lightSources[]` (position, intensity, radius)

* `nutrientPatches[]` (position, quantity, regenerateRate)

* `heatSources[]` (position, intensity)

* `toxins[]` (position, concentration)

* `terrainMap` (impassable/friction zones)

* `resourceGradients` computed from sources

* `timeOfDay` (optional) — light cycles

* `weather / diffusion` (optional)

API:

* `float SampleLight(Vector2 pos)`

* `float SampleNutrients(Vector2 pos)`

* `float SampleHeat(Vector2 pos)`

* `void ConsumeNutrients(Vector2 pos, float amount)`

Cells receive local samples and may react using behavior rules.

---

# **10 — Classes / Structs (cleaned & consistent)**

### **Enums (unchanged)**

`ComponentType`, `ReplicationMode`, `ConditionType`, `ActionType` (same as your list, keep as-is)

### **Structs**

`struct BaseGenome { ... } // as above`  
`struct ComponentGene {`  
    `ComponentType type;`  
    `float efficiency;`  
    `float sizeBias;`  
    `float energyBias;`  
    `float signalBias;`  
    `float mutationRate;`  
    `bool activationState;`  
`}`  
`struct BehaviorRule {`  
    `ConditionType condition;`  
    `SensorTarget target; // env var or component index or neighbor signal`  
    `float threshold;`  
    `ActionType action;`  
    `int targetComponentIndex; // optional`  
    `float probability; // 0..1`  
    `int repeatCount;`  
`}`

### **Classes (runtime)**

`class Component {`  
    `int id;`  
    `ComponentType type;`  
    `float volume, weight;`  
    `float upkeepCost, activationCost;`  
    `float energyGain; // harvest/digest`  
    `float signalOutput, signalRange, signalThreshold;`  
    `bool active;`  
    `float mutationSensitivity;`

    `void Activate(Cell host, Environment env);`  
`}`

`class Cell {`  
    `BaseGenome baseGenome;`  
    `List<Component> components;`  
    `List<BehaviorRule> behaviorGenome;`  
    `float energy;`  
    `float totalVolume, totalWeight;`  
    `float shellStrength; // computed`  
    `float pressure;`  
    `Vector2 position;`  
    `bool isAlive;`  
    `int maxBonds;`  
    `List<int> bonds; // neighbor cell IDs`

    `void ApplyUpkeep();`  
    `void EvaluateBehavior(Environment env, List<Cell> neighbors);`  
    `void ExecuteAction(ActionType action, int targetIndex, Environment env);`  
    `void CheckPressure(List<Cell> bondedNeighbors);`  
    `void Reproduce(Environment env);`  
    `void MutateOnReplication();`  
`}`

`class Cluster {`  
    `List<Cell> cells;`  
    `void SharePressure(); // uses neighbor shell sums`  
    `void ShareEnergy();`  
    `void CoordinateBehavior(); // optional aggregations`  
`}`

`class Environment {`  
    `List<LightSource> lights;`  
    `List<NutrientPatch> nutrients;`  
    `List<HeatSource> heats;`  
    `TerrainMap terrain; // optional`  
    `void Update(float dt);`  
    `// sampling API`  
`}`

---

# **11 — Tick loop (clear, consistent)**

Per tick (for all cells, in this order — parallelizable with care):

1. **Apply upkeep** (component upkeep deducted).

2. **Environmental sampling** (cached per grid cell to avoid repeats).

3. **EvaluateBehavior** (each cell reads local sensors and neighbors).

4. **ExecuteActions** (move, digest, emit signal, bond, attack, replicate). Actions can change energy and volume.

5. **Update pressure** (use cluster-sharing formula for bonded neighbors).

6. **Check death/rupture** (energy ≤ 0 or pressure \> ruptureThreshold).

7. **Replication attempts** (if gated; spawn new cells as needed).

8. **Mutation events** (handled during replication copy; occasional somatic mutation optional).

9. **Environment update** (diffuse signals, resource regeneration).

10. **Optional render** (separate — do not block sim).

---

# **12 — Backend systems to implement (list)**

* **Spatial index** (grid/quadtree) for neighborhood queries and environment sampling.

* **GenomeInterpreter**: builds components & behavior rules from genomes.

* **Mutation engine**: deterministic random mutations with per-gene rates and spikiness rules.

* **Scheduler / tick manager**: parallel safe worker pools for large cell counts.

* **Bond manager**: consistent bond creation/destruction and cluster updates.

* **Energy transfer module**: secure, loss-applied energy sharing between bonded cells.

* **Persistence/save**: genome and environment snapshotting.

* **Profiler & telemetry**: to tune balance knobs.

* **Rendering abstraction**: GPU instancing \+ separate transform sync.

---

# **13 — Balance knobs / tunables to add**

* `stretchCostFactor`

* `volumeEfficiencyFactor`, `upkeepEfficiencyFactor`, `energyGainFactor`, `signalEfficiencyFactor`

* `pressureToleranceFactor` default and per-species

* `bondEnergyCost`, `transferLoss`

* `mutationRates` (base, per-component, spikiness)

* `replicationPressureThreshold`, `replicationCostMultiplier`

---

# **14 — Missing or optional features I added that are useful**

* **Somatic (non-reproductive) mutation events** — optional but can create within-lifetime changes.

* **Signal decay model** — linear falloff by distance (cheap).

* **Environmental time cycles** — day/night for light-based selection.

* **Probabilistic rupture** when `pressure` slightly \>1 to avoid immediate death for tiny overshoots.

