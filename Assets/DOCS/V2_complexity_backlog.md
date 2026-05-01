# V2 Complexity Backlog

These are the remaining implementation choices that are still open enough to need code-level decisions or tuning. They are not missing high-level direction.

## 1. RNG and determinism wiring
- One seed creates one world RNG at time 0, and all random numbers come from that instance.
- Keep the evaluation order deterministic.

## 2. Snapshot query shape
- Unity asks for a bounded region and gets back the cells and their current states.
- Decide the simplest transport and buffer shape for that query.

## 3. Runtime registry content
- Decide the concrete opcode, module, and effect registry contents.

## 4. Global tuning values
- Keep the formulas mathematically sensible, then tune the global values in practice.

## 5. Multicell behavior detail
- Figure out the simplest deterministic way to handle signals, bonds, and cluster-level behavior.

## 6. Storage layout
- Decide whether the core uses indices, handles, pooling, or another layout for genome and module data.
