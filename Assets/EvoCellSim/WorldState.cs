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

        public int AddModule(in ModuleRecord module)
        {
            return Modules.Add(module);
        }

        public bool TryGetModule(int id, out ModuleRecord module)
        {
            return Modules.TryGetById(id, out module);
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
