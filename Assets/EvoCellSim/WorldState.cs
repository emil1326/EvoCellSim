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
