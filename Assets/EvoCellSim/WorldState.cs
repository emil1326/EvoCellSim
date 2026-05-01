using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public sealed class WorldState
    {
        public SimulationSettings Settings { get; }
        public ulong Seed => Settings.Seed;
        public long Tick { get; private set; }
        public DeterministicRng Rng { get; private set; }
        public CoreRegistries Registries { get; }
        public CellStore Cells { get; }
        public GenomeStore Genomes { get; }
        public ModuleStore Modules { get; }
        public BondStore Bonds { get; }
        public ClusterStore Clusters { get; }
        public FieldStore Fields { get; }
        public SignalStore Signals { get; }
        public IntentQueue Intents { get; }

        public WorldState(SimulationSettings settings)
        {
            Settings = settings;
            Rng = new DeterministicRng(settings.Seed);
            Registries = new CoreRegistries();
            Cells = new CellStore();
            Genomes = new GenomeStore();
            Modules = new ModuleStore();
            Bonds = new BondStore();
            Clusters = new ClusterStore();
            Fields = new FieldStore();
            Signals = new SignalStore();
            Intents = new IntentQueue();
            Tick = 0;
        }

        public int AddCell(in CellRecord cell)
        {
            return Cells.Add(cell);
        }

        public bool TryGetCell(int id, out CellRecord cell)
        {
            return Cells.TryGetById(id, out cell);
        }

        public CellRecord GetCellById(int id)
        {
            return Cells.GetById(id);
        }

        public void UpdateCell(in CellRecord cell)
        {
            Cells.SetById(cell.Id, cell);
        }

        public bool CellExists(int id)
        {
            return Cells.ContainsId(id);
        }

        public int AddGenome(in GenomeRecord genome)
        {
            return Genomes.Add(genome);
        }

        public bool TryGetGenome(int id, out GenomeRecord genome)
        {
            return Genomes.TryGetById(id, out genome);
        }

        public void UpdateGenome(in GenomeRecord genome)
        {
            Genomes.SetById(genome.Id, genome);
        }

        public int AddModule(in ModuleRecord module)
        {
            return Modules.Add(module);
        }

        public bool TryGetModule(int id, out ModuleRecord module)
        {
            return Modules.TryGetById(id, out module);
        }

        public void AddIntent(IntentRecord intent)
        {
            Intents.Add(intent);
        }

        public int AddBond(in BondRecord bond)
        {
            return Bonds.Add(bond);
        }

        public bool TryCreateBond(int cellAId, int cellBId, float strength, out int bondId)
        {
            bondId = 0;

            if (cellAId == cellBId)
            {
                return false;
            }

            if (!TryGetCell(cellAId, out var cellA) || !TryGetCell(cellBId, out var cellB))
            {
                return false;
            }

            if (!cellA.Alive || !cellB.Alive)
            {
                return false;
            }

            for (var i = 0; i < Bonds.Count; i++)
            {
                var existingBond = Bonds.Get(i);
                var sameDirection = existingBond.CellAId == cellAId && existingBond.CellBId == cellBId;
                var oppositeDirection = existingBond.CellAId == cellBId && existingBond.CellBId == cellAId;
                if (sameDirection || oppositeDirection)
                {
                    return false;
                }
            }

            var bond = new BondRecord
            {
                Id = Bonds.Count + 1,
                CellAId = cellAId,
                CellBId = cellBId,
                Strength = strength
            };

            AddBond(in bond);
            bondId = bond.Id;
            return true;
        }

        public void PassiveUpkeep()
        {
            for (var i = 0; i < Cells.Count; i++)
            {
                var cell = Cells.Get(i);
                if (!cell.Alive)
                {
                    continue;
                }

                var updated = cell;
                updated.Energy -= Settings.PassiveUpkeepCost;
                updated.Pressure = CalculatePressure(cell.Id);

                if (updated.Energy < 0)
                {
                    updated.Energy = 0;
                    updated.Damage += 2;
                }

                var overpressure = updated.Pressure - Settings.PressurePerCell;
                if (overpressure > 0)
                {
                    updated.Damage += (int)MathF.Ceiling(overpressure) * Settings.DamagePerOverpressure;
                }

                if (updated.MaxEnergy <= 0)
                {
                    updated.MaxEnergy = Settings.MaxEnergy;
                }

                if (updated.Energy > updated.MaxEnergy)
                {
                    updated.Energy = updated.MaxEnergy;
                }

                UpdateCell(in updated);
            }
        }

        public void QueueRepairIntents()
        {
            foreach (var cell in Cells.Records)
            {
                if (!cell.Alive || cell.Damage <= 0 || cell.Energy < Settings.RepairEnergyCost)
                {
                    continue;
                }

                if (!HasActiveRepairModule(cell.Id))
                {
                    continue;
                }

                var intent = new IntentRecord
                {
                    Id = Intents.Count + 1,
                    SourceCellId = cell.Id,
                    TargetCellId = cell.Id,
                    Kind = IntentKind.Repair
                };

                AddIntent(intent);
            }
        }

        public bool HasActiveRepairModule(int cellId)
        {
            foreach (var module in Modules.Records)
            {
                if (module.OwnerCellId != cellId || !module.Active)
                {
                    continue;
                }

                if (module.ModuleTypeId == 3)
                {
                    return true;
                }
            }

            return false;
        }

        public float CalculatePressure(int cellId)
        {
            var cell = GetCellById(cellId);
            var clusterId = cell.ClusterId;
            var clusterSize = 0;

            foreach (var other in Cells.Records)
            {
                if (other.Alive && other.ClusterId == clusterId)
                {
                    clusterSize++;
                }
            }

            return clusterSize * Settings.PressurePerCell;
        }

        public void ApplyDeathAndRepair()
        {
            for (var i = 0; i < Cells.Count; i++)
            {
                var cell = Cells.Get(i);
                if (!cell.Alive)
                {
                    continue;
                }

                var updated = cell;
                if (updated.Damage >= Settings.DeathDamageThreshold)
                {
                    updated.Alive = false;
                    updated.Energy = 0;
                }

                UpdateCell(in updated);
            }
        }

        public void UpdateBondsAndClusters()
        {
            var keptBonds = new List<BondRecord>(Bonds.Count);
            var adjacency = new Dictionary<int, List<int>>();
            var previousClusterByCellId = new Dictionary<int, int>();
            var previousClusterSizes = new Dictionary<int, int>();
            var startingEnergyByCellId = new Dictionary<int, int>();
            var pendingEnergyDeltaByCellId = new Dictionary<int, int>();
            var reservedOutgoingByCellId = new Dictionary<int, int>();

            for (var i = 0; i < Cells.Count; i++)
            {
                var cell = Cells.Get(i);
                if (cell.Alive)
                {
                    adjacency[cell.Id] = new List<int>();
                    previousClusterByCellId[cell.Id] = cell.ClusterId;
                    startingEnergyByCellId[cell.Id] = cell.Energy;
                    pendingEnergyDeltaByCellId[cell.Id] = 0;
                    reservedOutgoingByCellId[cell.Id] = 0;
                    if (!previousClusterSizes.ContainsKey(cell.ClusterId))
                    {
                        previousClusterSizes[cell.ClusterId] = 0;
                    }

                    previousClusterSizes[cell.ClusterId]++;
                }
                else
                {
                    var deadCell = cell;
                    deadCell.ClusterId = 0;
                    UpdateCell(in deadCell);
                }
            }

            for (var i = 0; i < Bonds.Count; i++)
            {
                var bond = Bonds.Get(i);
                if (!CellExists(bond.CellAId) || !CellExists(bond.CellBId))
                {
                    continue;
                }

                var cellA = GetCellById(bond.CellAId);
                var cellB = GetCellById(bond.CellBId);
                if (!cellA.Alive || !cellB.Alive)
                {
                    continue;
                }

                var strength = bond.Strength - Settings.BondDecayPerTick;
                if (strength <= Settings.BondBreakThreshold)
                {
                    continue;
                }

                var startingEnergyA = startingEnergyByCellId[bond.CellAId];
                var startingEnergyB = startingEnergyByCellId[bond.CellBId];
                var energyDelta = startingEnergyA - startingEnergyB;
                if (Settings.BondTransferAmount > 0 && energyDelta != 0)
                {
                    var sourceCellId = energyDelta > 0 ? bond.CellAId : bond.CellBId;
                    var targetCellId = energyDelta > 0 ? bond.CellBId : bond.CellAId;
                    var transferAmount = Settings.BondTransferAmount;
                    var sourceStartingEnergy = energyDelta > 0 ? startingEnergyA : startingEnergyB;
                    var reservedOutgoing = reservedOutgoingByCellId[sourceCellId];
                    if (sourceStartingEnergy - reservedOutgoing >= transferAmount)
                    {
                        reservedOutgoingByCellId[sourceCellId] = reservedOutgoing + transferAmount;
                        pendingEnergyDeltaByCellId[sourceCellId] -= transferAmount;
                        pendingEnergyDeltaByCellId[targetCellId] += transferAmount;
                    }
                }

                bond.Strength = strength;
                keptBonds.Add(bond);

                adjacency[bond.CellAId].Add(bond.CellBId);
                adjacency[bond.CellBId].Add(bond.CellAId);
            }

            Bonds.Clear();
            for (var i = 0; i < keptBonds.Count; i++)
            {
                Bonds.Add(keptBonds[i]);
            }

            for (var i = 0; i < Cells.Count; i++)
            {
                var cell = Cells.Get(i);
                if (!cell.Alive)
                {
                    continue;
                }

                var netDelta = pendingEnergyDeltaByCellId[cell.Id];
                if (netDelta == 0)
                {
                    continue;
                }

                var updatedCell = cell;
                updatedCell.Energy += netDelta;
                UpdateCell(in updatedCell);
            }

            Clusters.Clear();
            var visited = new HashSet<int>();
            var processedOldClusters = new HashSet<int>();

            for (var i = 0; i < Cells.Count; i++)
            {
                var cell = Cells.Get(i);
                if (!cell.Alive || visited.Contains(cell.Id))
                {
                    continue;
                }

                var component = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(cell.Id);
                visited.Add(cell.Id);

                while (queue.Count > 0)
                {
                    var currentCellId = queue.Dequeue();
                    component.Add(currentCellId);

                    if (!adjacency.TryGetValue(currentCellId, out var neighbors))
                    {
                        continue;
                    }

                    for (var neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                    {
                        var neighborId = neighbors[neighborIndex];
                        if (visited.Contains(neighborId))
                        {
                            continue;
                        }

                        visited.Add(neighborId);
                        queue.Enqueue(neighborId);
                    }
                }

                var rootCellId = component[0];
                var rootCellIndex = -1;
                var oldClusterId = previousClusterByCellId[rootCellId];
                var clusterSplit = oldClusterId != 0
                    && previousClusterSizes.TryGetValue(oldClusterId, out var previousSize)
                    && previousSize > component.Count;
                var applyCascadeDamage = clusterSplit && processedOldClusters.Contains(oldClusterId);

                if (oldClusterId != 0)
                {
                    processedOldClusters.Add(oldClusterId);
                }

                if (applyCascadeDamage)
                {
                    for (var componentIndex = 0; componentIndex < component.Count; componentIndex++)
                    {
                        var fracturedCell = GetCellById(component[componentIndex]);
                        fracturedCell.Damage += 1;
                        fracturedCell.Energy = fracturedCell.Energy > 0 ? fracturedCell.Energy - 1 : 0;
                        UpdateCell(in fracturedCell);
                    }
                }

                for (var cellIndex = 0; cellIndex < Cells.Count; cellIndex++)
                {
                    if (Cells.Get(cellIndex).Id == rootCellId)
                    {
                        rootCellIndex = cellIndex;
                        break;
                    }
                }

                for (var componentIndex = 0; componentIndex < component.Count; componentIndex++)
                {
                    var member = GetCellById(component[componentIndex]);
                    member.ClusterId = rootCellId;
                    UpdateCell(in member);
                }

                Clusters.Add(new ClusterRecord
                {
                    Id = rootCellId,
                    FirstCellIndex = rootCellIndex,
                    CellCount = component.Count
                });
            }
        }

        public BehaviorDispatchResult QueueIntentsFromGenome(int cellId, int genomeId)
        {
            return BehaviorDispatcher.QueueIntentsFromGenome(this, cellId, genomeId);
        }

        public BehaviorDispatchResult QueueIntentsForAllCells()
        {
            var aggregate = new BehaviorDispatchResult();

            foreach (var cell in Cells.Records)
            {
                var result = BehaviorDispatcher.QueueIntentsFromGenome(this, cell.Id, cell.GenomeId);
                aggregate.QueuedIntents += result.QueuedIntents;
                aggregate.FailureReasons.AddRange(result.FailureReasons);
            }

            return aggregate;
        }

        public int ResolveQueuedIntents()
        {
            return BehaviorDispatcher.ResolveQueuedIntents(this);
        }

        public GenomeDecodeResult DecodeInstructionGenome(int genomeId)
        {
            var genome = GetGenomeById(genomeId);
            return GenomeDecoder.DecodeInstructionGenome(genome.InstructionGenome.AsSpan(), Registries.Tokens, Registries.Opcodes);
        }

        public GenomeRecord GetGenomeById(int id)
        {
            return Genomes.GetById(id);
        }

        public void AdvanceTick()
        {
            Tick++;
        }
    }
}
