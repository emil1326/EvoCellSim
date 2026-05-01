// EvoSim.Core.cs
// High-performance C# namespace skeleton for an evolutionary cell simulation
// Designed for Unity: Burst + Jobs friendly, NativeArrays, Struct-of-Arrays (SoA)
// Focus: CPU-only, easily scales to 500k - 1M cells with low bandwidth overhead

using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

namespace Assets.EvoCellSim.Core
{
    // ---------------------------------------------------------------------
    // Tunables / Constants
    // ---------------------------------------------------------------------
    public static class SimConfig
    {
        public const int MaxCells = 1_200_000; // prealloc ceiling
        public const int MaxComponents = 9; // pooled components
        public const int ChunkSize = 1024; // cells per job batch
        public const int MaxBehaviorRulesPerCell = 8; // small simple rules

        // Fixed genome sizes (bytes) - chosen for speed and fixed memory layout
        public const int BaseGenomeBytes = 64;

        public const int geneSize = 19; // bytes per component gene
        public const int ComponentGenesBytes = geneSize * MaxComponents; // component gene packed list

        public const int BehaviorGenesBytes = 64; // simple rule encoding

        // Balance knobs
        public const float StretchCostFactor = 0.01f;
        public const float VolumeEfficiencyFactor = 0.3f;
        public const float UpkeepEfficiencyFactor = 0.2f;
        public const float EnergyGainFactor = 0.5f;
    }

    public static class CellConfig
    {
        public const float DiversityPenalty = 1.2f;
        public const float SpecialisationBonus = 0.4f;
    }

    public static class WorldConfig
    {

    }

    // ---------------------------------------------------------------------
    // Enums
    // ---------------------------------------------------------------------
    public enum ComponentType : byte
    {
        None = 0,
        EnergyHarvester = 1, // gathers energy from environment -> can specialise into light, chemical, thermal, radioactive, biological
        StorageSac = 2, // stores extra energy
        Movement = 3, // controls cell movement
        ShellEnhancer = 4, // strengthens cell membrane
        SignalEmitter = 5, // emits signals to nearby cells
        SignalDetector = 6, // detects signals from nearby cells
        ReplicationModule = 7, // enables cell replication
        ToxinGenerator = 8, // produces toxins to harm other cells
        ToxinCountermeasure = 9, // removes nearby toxins
        Adhesion = 10, // allows cells to stick together
        AttackSpike = 11, // enables aggressive behavior
        RepairModule = 12, // repairs cell damage
        GenomeReplicator = 13, // improves genome replication fidelity
    }

    public enum ReplicationMode : byte
    {
        BinarySplit = 0, // currently only option, to be expanded
        Budding = 1,
        Sporulation = 2,
        HighOutput = 3,
    }
    public enum ConditionType : byte
    {
        InternalEnergy = 0,
        Pressure = 1,
        NeighborSignal = 2,
        ClusterSize = 3,
        RandomChance = 4,
        Age = 5,
        CellIntegrity = 6,
        NearbyNutrient = 7,
    }
    //public enum ActionType : byte
    //{
    //    Move = 0,
    //    Bond = 1,
    //    ReleaseSignal = 2,
    //    Digest = 3,
    //    StoreEnergy = 4,
    //    Replicate = 5,
    //    Attack = 6,
    //    HardenShell = 7,
    //    Detach = 8,
    //    StealEnergy = 9
    //}
    // use organelle instead from index, to allow more flexible actions

    // ---------------------------------------------------------------------
    // Unmanaged data structs (Burst-friendly)
    // ---------------------------------------------------------------------

    public unsafe struct Cell
    {
        public byte isAlive; // alive or dead -> for reuse

        public Genome genome; // genome data

        public float3 position; // world position

        public ushort maxInternalEnergy; // max energy capacity based on components
        public ushort internalEnergy; // amount of energy in base of the cell
        public float volume; // size of the cell internal space

        public int CompStartIndex; // components start index
        public int CompCount; // number of components -> simulationSettings.MaxComponentsPerCell
    }

    public struct Genome
    {
        // base genome
        public BaseGenome baseGenome;
        // component genes
        public ComponentGenome componentGenes;
        // behavior genes
        public BehaviorGenome behaviorGenes;

        public unsafe struct BaseGenome
        {
            public fixed byte data[SimConfig.BaseGenomeBytes];
        }
        public unsafe struct BehaviorGenome
        {
            public fixed byte data[SimConfig.BehaviorGenesBytes];
        }
        public unsafe struct ComponentGenome
        {
            public fixed byte data[SimConfig.ComponentGenesBytes];
        }
    }

    public unsafe struct Component
    {
        // component genome is 9 bytes sequencially the type, and 8 bytes of data
        // is setup during cell creation from genome decoding
        // only determines the behavior of the component in simulation

        public ComponentType type;

        // --- Universal fields ---
        public ushort efficiency;           // 0-65535 mutation efficiency -> use more energy but is faster/better
        public byte spaceOccupied;          // how much internal space it takes
        public byte health;                 // used for damage/deterioration

        // --- Specialized Fields where we actually store the data ---
        public ushort specA;               // component-specific stat A
        public ushort specB;               // component-specific stat B
        public ushort specC;               // component-specific stat C
        public ushort specD;               // component-specific stat D
        public ushort specE;               // component-specific stat E
        public ushort specF;               // component-specific stat F
        public ushort specG;               // component-specific stat G

        // --- Energy Harvester ---
        public readonly ushort harvestRate { get => specA; }                           // energy per tick
        public readonly byte lightHarvestEfficiency { get => (byte)specB; }
        public readonly byte chemicalHarvestEfficiency { get => (byte)specC; }
        public readonly byte thermalHarvestEfficiency { get => (byte)specD; }
        public readonly byte radioactiveHarvestEfficiency { get => (byte)specE; }
        public readonly byte bioHarvestEfficiency { get => (byte)specF; }
        public readonly byte harvesterRange { get => (byte)specG; }                    // light receptors / chem gradients etc.

        // --- Storage Sac ---
        public readonly ushort storageCapacity { get => specA; }      // extra energy the sac provides

        // --- Movement ---
        public readonly ushort moveForce { get => specA; }            // propulsion force
        public readonly ushort moveEfficiency { get => specB; }       // reduces energy cost

        // --- Shell Enhancer ---
        public readonly ushort membraneStrength { get => specA; }     // makes the cell harder to burst

        // --- Signal Emitter ---
        public readonly ushort signalChannel { get => specA; }        // emitted signal channel
        public readonly byte signalRange { get => (byte)specB; }      // diffusion radius
        public readonly byte signalIntensity { get => (byte)specC; }  // energy cost per emission

        // --- Signal Detector ---
        public readonly ushort receptorChannel { get => specA; }      // detection channel
        public readonly byte receptorRange { get => (byte)specB; }    // detection radius

        // --- Replication Module ---
        public readonly ushort replicationSpeed { get => specA; }     // faster division

        // --- Toxin Generator ---
        public readonly ushort toxinDamage { get => specA; }          // raw damage to others
        public readonly byte toxinRange { get => (byte)specB; }       // toxin cloud distance
        public readonly byte toxinChannel { get => (byte)specC; }     // toxin emission channel
        public readonly byte toxinChannelWidth { get => (byte)specD; }      // affects multiple channels

        // --- Toxin Countermeasure ---
        public readonly ushort toxinResistance { get => specA; }      // reduces incoming toxin damage
        public readonly byte resistanceRange { get => (byte)specB; }    // effective range
        public readonly byte resistanceChannel { get => (byte)specC; }      // detection channel
        public readonly byte toxinResistanceWidth { get => (byte)specD; }   // affects multiple channels

        // --- Adhesion ---
        public readonly ushort stickStrength { get => specA; }        // how strongly it binds cells together
        public readonly ushort adhesionRange { get => specB; }        // short-range attraction

        // --- Attack Spike ---
        public readonly ushort spikeDamage { get => specA; }          // melee damage
        public readonly ushort spikePenetration { get => specB; }     // armor penetration

        // --- Repair Module ---
        public readonly ushort repairRate { get => specA; }           // hp restored per tick
        public readonly ushort repairQuality { get => specB; }        // quality of repairs, low quality = more likely to make mutations

        // --- Genome Replicator ---
        public readonly ushort replicationAccuracy { get => specA; }        // reduces mutation errors

        // --- Common methods ---

        public readonly ushort GetComponentUpkeep
        {
            get
            {
                // Sécurité : éviter division par zéro
                int occupied = math.max(1, (int)spaceOccupied);
                // baseCost est une estimation élémentaire : 100 * (efficiency + 1) / occupied
                // +1 sur efficiency pour éviter coût 0 quand efficiency == 0
                uint baseCost = (uint)100 * (uint)(efficiency + 1) / (uint)occupied;

                uint cost;
                switch (type)
                {
                    case ComponentType.None:
                        cost = 0;
                        break;

                    case ComponentType.EnergyHarvester:
                        {
                            // Favor a single strong specialization and *penalize* being a jack-of-all-trades.
                            uint a = (uint)lightHarvestEfficiency;
                            uint b = (uint)chemicalHarvestEfficiency;
                            uint c = (uint)thermalHarvestEfficiency;
                            uint d = (uint)radioactiveHarvestEfficiency;
                            uint e = (uint)bioHarvestEfficiency;

                            uint sum = a + b + c + d + e;
                            uint max = math.max(math.max(a, b), math.max(c, math.max(d, e)));

                            float maxF = (float)max / 255f;       // 0..1
                            float othersF = (float)(sum - max) / 255f; // 0..(<=4)

                            float specializationBonus = math.clamp(maxF * CellConfig.SpecialisationBonus, 0f, CellConfig.SpecialisationBonus); // up to 40% discount factor influence
                            float diversityPenalty = 1.0f + (othersF * CellConfig.DiversityPenalty); // e.g., othersF=1 -> *2.2

                            // Replace linear range growth by quadratic growth:
                            // multiplier includes quadratic penalty for range
                            float r = (float)harvesterRange / 100f; // normalized 0..~2.55
                            float rangeQuadratic = 1.0f + (r * r); // 1 + (range/100)^2

                            float multiplier = diversityPenalty / (1.0f + specializationBonus);

                            float rawCost = (float)baseCost * rangeQuadratic;
                            rawCost = rawCost * multiplier;
                            // slightly increase with harvestRate
                            rawCost += (float)harvestRate * 0.1f;

                            uint ucost = (uint)math.clamp(rawCost, 0f, (float)ushort.MaxValue);
                            cost = ucost;
                            break;
                        }

                    case ComponentType.StorageSac:
                        {
                            // capacité de stockage augmente upkeep mais reste modérée
                            cost = baseCost * (100u + (uint)storageCapacity / 10u) / 100u;
                            break;
                        }

                    case ComponentType.Movement:
                        {
                            // coût proportionnel à la force, réduit par l'efficacité de mouvement
                            uint eff = math.max(1, (uint)moveEfficiency);
                            cost = baseCost * (uint)moveForce / eff;
                            // garantir au moins baseCost
                            cost = math.max(cost, baseCost);
                            break;
                        }

                    case ComponentType.ShellEnhancer:
                        {
                            // plus la membrane est forte, plus l'entretien peut être élevé
                            cost = baseCost * (100u + (uint)membraneStrength / 10u) / 100u;
                            break;
                        }

                    case ComponentType.SignalEmitter:
                        {
                            // dépend de l'intensité et de la portée
                            // appliquer croissance quadratique sur la portée
                            float normRange = (float)signalRange / 100f;
                            float rangeQuadFactor = 1.0f + (normRange * normRange); // 1 + (range/100)^2
                            float intensityEffect = (float)signalIntensity * rangeQuadFactor;
                            float raw = (float)baseCost * (100f + intensityEffect) / 100f;

                            uint ucost = (uint)math.clamp(raw, 0f, (float)ushort.MaxValue);
                            cost = ucost;
                            break;
                        }

                    case ComponentType.SignalDetector:
                        {
                            // détecteurs sont peu coûteux, coût augmente avec la portée mais de façon quadratique
                            float rangeQuad = ((float)receptorRange * (float)receptorRange) / 100f; // receptorRange^2 / 100
                            float raw = (float)baseCost * (50f + rangeQuad) / 50f;
                            uint ucost = (uint)math.clamp(raw, 0f, (float)ushort.MaxValue);
                            cost = ucost;
                            break;
                        }

                    case ComponentType.ReplicationModule:
                        {
                            // la vitesse de réplication coûte de l'entretien
                            cost = baseCost * (100u + (uint)replicationSpeed / 10u) / 100u;
                            break;
                        }

                    case ComponentType.ToxinGenerator:
                        {
                            // dépend du dommage, de la portée et de la largeur de canal
                            // remplacer contribution linéaire de la portée par quadratique
                            float rangeQuad = ((float)toxinRange * (float)toxinRange) / 10f; // scale down
                            float rawVal = (float)toxinDamage + rangeQuad + (float)toxinChannelWidth * 5f;
                            float raw = (float)baseCost * rawVal / 100f;
                            cost = (uint)math.max((uint)baseCost, (uint)math.clamp(raw, 0f, (float)ushort.MaxValue));
                            break;
                        }

                    case ComponentType.ToxinCountermeasure:
                        {
                            // coût lié à la résistance et à la portée d'effet (quadratique)
                            float rangeQuad = ((float)resistanceRange * (float)resistanceRange) / 100f;
                            float raw = (float)baseCost * (100f + (float)toxinResistance / 10f + rangeQuad) / 100f;
                            uint ucost = (uint)math.clamp(raw, 0f, (float)ushort.MaxValue);
                            cost = ucost;
                            break;
                        }

                    case ComponentType.Adhesion:
                        {
                            // colle : dépend de la force d'adhérence et de la plage, plage quadratique
                            float rangeQuad = ((float)adhesionRange * (float)adhesionRange) / 20f;
                            float raw = (float)baseCost * (100f + (float)stickStrength / 10f + rangeQuad) / 100f;
                            uint ucost = (uint)math.clamp(raw, 0f, (float)ushort.MaxValue);
                            cost = ucost;
                            break;
                        }

                    case ComponentType.AttackSpike:
                        {
                            // attaque rapprochée : dommage + pénétration
                            cost = baseCost * ((uint)spikeDamage + (uint)spikePenetration) / 100u;
                            cost = math.max(cost, baseCost);
                            break;
                        }

                    case ComponentType.RepairModule:
                        {
                            // réparation : coût selon rate, réduit si qualité élevée
                            uint qual = math.max(1u, (uint)repairQuality);
                            cost = baseCost * (uint)repairRate / qual;
                            // garantir au moins baseCost/2
                            cost = math.max(cost, baseCost / 2u);
                            break;
                        }

                    case ComponentType.GenomeReplicator:
                        {
                            // meilleure exactitude coûte plus
                            cost = baseCost * (100u + (uint)replicationAccuracy / 10u) / 100u;
                            break;
                        }

                    default:
                        cost = baseCost;
                        break;
                }

                // Clamp sur ushort
                if (cost >= ushort.MaxValue)
                    return ushort.MaxValue;
                return (ushort)cost;
            }
        }
    }

    public static class ComponentFactory
    {
        // Parse a single component gene. Layout (per `geneSize` = 19):
        // byte 0: type
        // bytes 1-2: ushort efficiency (little-endian: low, high)
        // byte 3: spaceOccupied
        // byte 4: health
        // bytes 5-6: specA (ushort)
        // bytes 7-8: specB
        // bytes 9-10: specC
        // bytes 11-12: specD
        // bytes 13-14: specE
        // bytes 15-16: specF
        // bytes 17-18: specG
        public static void FillGlobalComponentsFromGenome(in Genome.ComponentGenome compGenome, NativeArray<Component> globalComponents, int cellCompStartIndex, int cellCompCount)
        {
            for (int i = 0; i < cellCompCount; ++i)
            {
                Component c = MakeComponentFromGenome(compGenome, i);
                globalComponents[cellCompStartIndex + i] = c;
            }
        }
        public static Component MakeComponentFromGenome(in Genome.ComponentGenome compGenome, int componentIndex)
        {
            Component c = default;

            int totalBytes = SimConfig.ComponentGenesBytes;
            int offset = componentIndex * SimConfig.geneSize;
            // validation unique en amont : garantit que la région du gène tient bien dans le buffer
            if (componentIndex < 0 || offset < 0 || offset + SimConfig.geneSize > totalBytes)
                return MakeDefault();

            unsafe
            {
                // pin the data of the passed-in genome reference without copying the struct
                fixed (byte* ptr = compGenome.data)
                {
                    byte typeByte = *(ptr + offset);
                    c.type = GetComponentTypeFromGene(typeByte);

                    // efficiency (bytes 1-2 : lo, hi)
                    {
                        byte lo = *(ptr + offset + 1);
                        byte hi = *(ptr + offset + 2);
                        c.efficiency = (ushort)((hi << 8) | lo);
                    }

                    // accès direct : on a déjà validé la région
                    c.spaceOccupied = *(ptr + offset + 3);
                    c.health = 255; // préservé tel quel (si vous voulez lire la valeur du gène, remplacez par *(ptr + offset + 4))

                    // specs (7 ushorts starting at byte index 5)
                    int specBase = offset + 5;
                    for (int s = 0; s < 7; ++s)
                    {
                        int p = specBase + s * 2;
                        ushort val = 0;
                        if (p + 1 < offset + SimConfig.geneSize)
                        {
                            byte lo = *(ptr + p);
                            byte hi = *(ptr + p + 1);
                            val = (ushort)((hi << 8) | lo);
                        }
                        switch (s)
                        {
                            case 0: c.specA = val; break;
                            case 1: c.specB = val; break;
                            case 2: c.specC = val; break;
                            case 3: c.specD = val; break;
                            case 4: c.specE = val; break;
                            case 5: c.specF = val; break;
                            case 6: c.specG = val; break;
                        }
                    }
                }
            }

            return c;
        }

        public static ComponentType GetComponentTypeFromGene(byte gene)
        {
            // simple mapping for now, can be expanded
            return gene switch
            {
                1 => ComponentType.EnergyHarvester,
                2 => ComponentType.StorageSac,
                3 => ComponentType.Movement,
                4 => ComponentType.ShellEnhancer,
                5 => ComponentType.SignalEmitter,
                6 => ComponentType.SignalDetector,
                7 => ComponentType.ReplicationModule,
                8 => ComponentType.ToxinGenerator,
                9 => ComponentType.ToxinCountermeasure,
                10 => ComponentType.Adhesion,
                11 => ComponentType.AttackSpike,
                12 => ComponentType.RepairModule,
                13 => ComponentType.GenomeReplicator,
                _ => ComponentType.None,
            };
        }

        public static Component MakeDefault()
        {
            Component c = default;
            c.type = ComponentType.None;
            c.efficiency = 0;
            c.spaceOccupied = 0;
            c.health = 0;
            c.specA = 0; c.specB = 0; c.specC = 0; c.specD = 0; c.specE = 0; c.specF = 0; c.specG = 0;
            return c;
        }
    }

    public struct SimWorld
    {
        public NativeArray<Cell> allCells;
        public NativeArray<Component> allComponents;

        public void InitializeCells(int maxCells, int maxComponents)
        {
            allCells = new NativeArray<Cell>(maxCells, Allocator.Persistent);
            allComponents = new NativeArray<Component>(maxComponents, Allocator.Persistent);
        }
        public void Dispose()
        {
            if (allCells.IsCreated)
                allCells.Dispose();
            if (allComponents.IsCreated)
                allComponents.Dispose();
        }
    }

    // ---------------------------------------------------------------------
    // Example simulation runner (scheduling jobs) - integrate into MonoBehaviour
    // ---------------------------------------------------------------------
    public class SimRunner
    {
        private SimWorld world;
        private int maxCells;
        private int maxComponents;

        public bool IsInitialized => world.allCells.IsCreated && world.allComponents.IsCreated && cellsInitialized && worldInitialized;
        private bool cellsInitialized = false;
        private bool worldInitialized = false;

        public SimRunner(int maxCells, int? maxComponents = null)
        {
            world = new SimWorld();
            this.maxCells = math.min(maxCells, SimConfig.MaxCells);
            this.maxComponents = maxComponents ?? SimConfig.MaxComponents;
        }

        public void InitializeWorld()
        {
            worldInitialized = true;
        }

        public void InitializeCells()
        {
            world.InitializeCells(maxCells, maxComponents);

            for (int i = 0; i < maxCells; i++)
            {
                world.allCells[i] = new Cell
                {
                    CompCount = maxComponents
                };
            }
            for (int i = 0; i < maxComponents; i++)
            {
                world.allComponents[i] = ComponentFactory.MakeDefault();
            }
            cellsInitialized = true;
        }

        public void Tick(float dt)
        {
            if (!IsInitialized) return;

            // Phase 1: decode genomes -> populate runtime components
            var decodeJob = new DecodeComponentsJob
            {
                cells = world.allCells,
                globalComponents = world.allComponents
            };
            JobHandle h1 = decodeJob.Schedule(world.allCells.Length, SimConfig.ChunkSize, default);

            // Phase 2: component actions (emit signals, produce energy, etc.) - stub
            var compActions = new ComponentActionsJob
            {
                cells = world.allCells,
                components = world.allComponents
            };
            JobHandle h2 = compActions.Schedule(world.allCells.Length, SimConfig.ChunkSize, h1);

            // Phase 3: upkeep - deduct upkeep costs from cells
            var upkeep = new UpkeepJobSimple
            {
                cells = world.allCells,
                components = world.allComponents,
                dt = dt
            };
            JobHandle h3 = upkeep.Schedule(world.allCells.Length, SimConfig.ChunkSize, h2);

            // Phase 4: pressure / rupture checks
            var pressure = new PressureJobSimple
            {
                cells = world.allCells
            };
            JobHandle h4 = pressure.Schedule(world.allCells.Length, SimConfig.ChunkSize, h3);

            // Phase 5: behavior evaluation (decide actions, replication requests)
            var behavior = new BehaviorJobSimple
            {
                cells = world.allCells
            };
            JobHandle h5 = behavior.Schedule(world.allCells.Length, SimConfig.ChunkSize, h4);

            // Phase 6: reproduction (spawn children into free slots) - simplified stub
            var reproduction = new ReproductionJobSimple
            {
                cells = world.allCells,
                globalComponents = world.allComponents
            };
            JobHandle h6 = reproduction.Schedule(world.allCells.Length, SimConfig.ChunkSize, h5);

            // Phase 7: movement
            var movement = new MovementJobSimple
            {
                cells = world.allCells
            };
            JobHandle h7 = movement.Schedule(world.allCells.Length, SimConfig.ChunkSize, h6);

            // Phase 8: diffuse signals / world updates (stub)
            var diffuse = new SignalDiffuseJobSimple
            {
                // placeholder - add world arrays when available
            };
            JobHandle h8 = diffuse.Schedule(world.allCells.Length, SimConfig.ChunkSize, h7);

            // Wait for completion for now (synchronous frame)
            h8.Complete();
        }

        public void Dispose()
        {
            world.Dispose();
        }
    }

    [BurstCompile]
    public struct DecodeComponentsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Cell> cells;
        public NativeArray<Component> globalComponents;

        public void Execute(int index)
        {
            var cell = cells[index];
            if (cell.CompCount <= 0) return;

            // decode each component gene for this cell and write into global pool
            for (int j = 0; j < cell.CompCount; ++j)
            {
                var comp = ComponentFactory.MakeComponentFromGenome(in cell.genome.componentGenes, j);
                globalComponents[cell.CompStartIndex + j] = comp;
            }
        }
    }

    // -----------------------------------------------------------------
    // Job stubs for main simulation phases (fill logic later)
    // -----------------------------------------------------------------

    [BurstCompile]
    public struct ComponentActionsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Cell> cells;
        public NativeArray<Component> components;

        public void Execute(int index)
        {
            // placeholder: components might emit signals or produce energy
            // implement per-component behavior later
            var c = cells[index];
            if (c.CompCount <= 0) return;
            // no-op
        }
    }

    [BurstCompile]
    public struct UpkeepJobSimple : IJobParallelFor
    {
        public NativeArray<Cell> cells;
        [ReadOnly] public NativeArray<Component> components;
        public float dt;

        public void Execute(int index)
        {
            var cell = cells[index];
            if (cell.CompCount <= 0 || cell.isAlive == 0) return;
            // simple placeholder: reduce energy by a tiny amount
            int newEnergy = (int)cell.internalEnergy - (int)math.max(1, (int)(1f * dt));
            cell.internalEnergy = (ushort)math.max(0, newEnergy);
            cells[index] = cell;
        }
    }

    [BurstCompile]
    public struct PressureJobSimple : IJobParallelFor
    {
        public NativeArray<Cell> cells;

        public void Execute(int index)
        {
            var cell = cells[index];
            if (cell.isAlive == 0) return;
            // placeholder: compute a dummy pressure and mark dead if volume too large
            if (cell.volume > 1000f) { cell.isAlive = 0; cells[index] = cell; }
        }
    }

    [BurstCompile]
    public struct BehaviorJobSimple : IJobParallelFor
    {
        public NativeArray<Cell> cells;

        public void Execute(int index)
        {
            var cell = cells[index];
            if (cell.isAlive == 0) return;
            // placeholder: set cluster flags, trigger replication, etc.
        }
    }

    [BurstCompile]
    public struct ReproductionJobSimple : IJobParallelFor
    {
        public NativeArray<Cell> cells;
        public NativeArray<Component> globalComponents;

        public void Execute(int index)
        {
            var cell = cells[index];
            if (cell.isAlive == 0) return;
            // placeholder: reproduction logic handled on main thread in future
        }
    }

    [BurstCompile]
    public struct MovementJobSimple : IJobParallelFor
    {
        public NativeArray<Cell> cells;

        public void Execute(int index)
        {
            var cell = cells[index];
            if (cell.isAlive == 0) return;
            // tiny random or deterministic move stub
            cell.position.x += 0.0f;
            cell.position.y += 0.0f;
            cells[index] = cell;
        }
    }

    [BurstCompile]
    public struct SignalDiffuseJobSimple : IJobParallelFor
    {
        // placeholder: would operate on environment grids / signal buffers
        public void Execute(int index) { /* no-op */ }
    }
}
