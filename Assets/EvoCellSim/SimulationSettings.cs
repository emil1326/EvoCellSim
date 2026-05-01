namespace Assets.EvoCellSim.Core
{
    public sealed class SimulationSettings
    {
        public ulong Seed { get; }
        public int TicksPerSecond { get; }
        public int PassiveUpkeepCost { get; } = 1;
        public int RepairEnergyCost { get; } = 2;
        public float PressurePerCell { get; } = 0.25f;
        public int DamagePerOverpressure { get; } = 1;
        public int DeathDamageThreshold { get; } = 10;
        public int RepairPower { get; } = 3;
        public int MaxEnergy { get; } = 20;
        public float BondDecayPerTick { get; } = 0.1f;
        public float BondBreakThreshold { get; } = 0.05f;
        public int BondTransferAmount { get; } = 1;

        public SimulationSettings(ulong seed, int ticksPerSecond = 60)
        {
            Seed = seed;
            TicksPerSecond = ticksPerSecond > 0 ? ticksPerSecond : 60;
        }
    }
}
