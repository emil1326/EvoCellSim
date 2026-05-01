namespace Assets.EvoCellSim.Core
{
    public sealed class SimulationSettings
    {
        public ulong Seed { get; }
        public int TicksPerSecond { get; }

        public SimulationSettings(ulong seed, int ticksPerSecond = 60)
        {
            Seed = seed;
            TicksPerSecond = ticksPerSecond > 0 ? ticksPerSecond : 60;
        }
    }
}
