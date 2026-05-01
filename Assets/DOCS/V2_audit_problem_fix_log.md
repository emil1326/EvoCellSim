# V2 Audit Problem / Fix Log

This file records the meaningful audit findings and the corresponding spec fixes applied to `EVOLUTIONARY CELL SIMULATION, FULL SYSTEM SPECIFICATION_V2.md`.

## 1. Snapshot boundary was too loose
- Problem: Unity could infer or mutate live core state through an underspecified snapshot contract.
- Fix: Snapshots are now immutable, versioned value data owned by the world. The world owns `SnapshotBuffer`, Unity only reads the latest completed snapshot, and read-only queries cannot trigger lazy recomputation or consume RNG.
- Remaining complexity: exact runtime snapshot transport / UI plumbing is still an implementation task.

## 2. RNG contract was implicit
- Problem: Determinism existed only as a goal, not a complete serialization contract.
- Fix: The world seed is immutable after creation; the world owns a single RNG stream; replay/save data must include `worldSeed`, `rngState`, `tick`, and authoritative stores at the end of the snapshot phase.
- Remaining complexity: exact RNG serialization format and replay validation tooling still need implementation.

## 3. Genome decoding was underspecified
- Problem: Frame shifts, junk, malformed chains, and operand validation could be implemented differently by different developers.
- Fix: Genome is a compact byte sequence over a fixed alphabet; one raw symbol is one byte; registered token patterns are 1 to 4 bytes; opcode signatures declare required and optional operands; malformed chains abort unless an opcode has a registered failure effect; no-op is explicit and distinct from junk.
- Remaining complexity: token registry content and concrete opcode tables are still to be authored.

## 4. Frame-shift behavior was ambiguous
- Problem: The spec said frame shifts should change downstream parsing but did not define scope tightly enough.
- Fix: A frame shift changes decode alignment from the mutation point forward by one symbol only; already decoded chains remain fixed.
- Remaining complexity: exact mutation operator implementation still needs code.

## 5. Rule / intent pipeline was unclear
- Problem: It was unclear whether chains, rules, and intents were separate objects and where validation happened.
- Fix: Instruction chains decode into runtime rules; runtime rules produce intents; the core resolver validates module permissions, input requirements, and target compatibility immediately before applying effects.
- Remaining complexity: the effect registry and runtime rule shapes still need implementation.

## 6. Module activation semantics were vague
- Problem: Inactive modules could be interpreted as pruned, hidden, or still addressable.
- Fix: Inactive modules remain stored and addressable; activation or deactivation is only allowed if the module definition lists it as an allowed action.
- Remaining complexity: exact module registry data still needs to be defined.

## 7. Pressure and maintenance were not computable enough
- Problem: The cost and pressure formulas named factors that were not precisely defined.
- Fix: Pressure now uses `supportFactor`, `pressureToleranceFactor`, and `pressureDamageScale`; maintenance now uses `baseMaintenance`, `moduleCount`, `totalVolume`, `totalMass`, `bondCount`, `signalActivity`, and `regulatoryComplexity`, with `signalActivity` and `distinctConditionTypeCount` explicitly defined.
- Remaining complexity: balance values and scenario tuning still need gameplay iteration.

## 8. Bond failure and cluster cascade were underspecified
- Problem: Bond decay, break thresholds, energy transfer, and cascade failure order were too vague.
- Fix: Bonds now decay via `bondDecayRate`, break at `bondBreakThreshold`, and energy transfer is capped by `energyTransferRate` with `energyTransferLossRate`. Cascade failure is breadth-first once per tick and each cell or bond may fail at most once per tick.
- Remaining complexity: numeric tuning and large-scale stress testing still need implementation.

## 9. Repair path was missing
- Problem: Damage had no explicit way to decrease.
- Fix: Repair intents or repair modules reduce accumulated damage during the damage phase.
- Remaining complexity: repair module definitions and costs still need implementation.

## 10. Snapshot versioning and ownership were unclear
- Problem: Snapshot cadence, ownership, and version compatibility were not explicit enough.
- Fix: The world owns `SnapshotBuffer`; snapshots are produced by the simulation runner at a configured cadence or explicit capture request; each snapshot includes `snapshotVersion`; snapshot readers may reject incompatible versions or route them through migration.
- Remaining complexity: the concrete runner/scheduler integration still needs code.

## 11. Cluster specialization was only claimed, not mechanized
- Problem: Multicell differentiation was expected to emerge but no direct mechanism was named.
- Fix: Cells in clusters receive different inputs from local signal gradients, bond depth, and position in the cluster, allowing the same genome to activate different modules without a separate coordinator.
- Remaining complexity: this still needs runtime validation in the simulation.

## 12. Remaining implementation-only complexity
- The current spec still leaves several things intentionally open so evolution can explore them:
- exact opcode registry content
- exact module families and effect tables
- exact balance constants
- exact snapshot transport implementation
- exact Unity visualization behavior
- exact serializer format for save/replay data

## 13. Current unresolved implementation-heavy items
- The core repo still needs a headless runtime implementation.
- The spec now defines contracts, but the code path for `SnapshotBuffer`, RNG serialization, and effect registry dispatch is still to be built.
- Those are implementation tasks rather than remaining spec logic problems.
