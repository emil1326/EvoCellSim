namespace Assets.EvoCellSim.Core
{
    public sealed class SimulationSettings
    {
        public ulong Seed { get; }
        public int TicksPerSecond { get; }
        public int PassiveUpkeepCost { get; init; } = 1;
        public int PassiveEnergyGain { get; init; } = 0;
        public int RepairEnergyCost { get; init; } = 2;
        public float PressurePerCell { get; init; } = 0.25f;
        public int DamagePerOverpressure { get; init; } = 1;
        public int DeathDamageThreshold { get; init; } = 10;
        public int RepairPower { get; init; } = 3;
        public int MaxEnergy { get; init; } = 20;
        public float BondDecayPerTick { get; init; } = 0.1f;
        public float BondBreakThreshold { get; init; } = 0.05f;
        public int BondTransferAmount { get; init; } = 1;
        public int ReproductionEnergyCost { get; init; } = 8;
        public int ReproductionCooldown { get; init; } = 10;
        public float MutationRate { get; init; } = 0.1f;
        public int MaxCellCount { get; init; } = int.MaxValue;
        public int MaxModulesPerCell { get; init; } = 8;

        public SimulationSettings(ulong seed, int ticksPerSecond = 60)
        {
            Seed = seed;
            TicksPerSecond = ticksPerSecond > 0 ? ticksPerSecond : 60;
        }
    }
}
