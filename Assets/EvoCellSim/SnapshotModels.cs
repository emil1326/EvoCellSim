using System.Collections.Generic;

namespace Assets.EvoCellSim.Core
{
    public readonly struct SnapshotRegion
    {
        public static readonly SnapshotRegion All = new SnapshotRegion { MaxCells = int.MaxValue };

        public int? ClusterId { get; init; }
        public int MaxCells { get; init; }

        public SnapshotRegion(int clusterId, int maxCells = int.MaxValue)
        {
            ClusterId = clusterId;
            MaxCells = maxCells;
        }
    }

    public readonly struct CellSnapshot
    {
        public int Id { get; init; }
        public int ClusterId { get; init; }
        public int GenomeId { get; init; }
        public int Energy { get; init; }
        public int MaxEnergy { get; init; }
        public int Damage { get; init; }
        public int NeighborCount { get; init; }
        public int BondDepth { get; init; }
        public float LocalSignal { get; init; }
        public float ReceivedSignal { get; init; }
        public int ReprodCooldown { get; init; }
    }

    public sealed class SnapshotBuffer
    {
        private readonly List<CellSnapshot> cells;

        public long Tick { get; }
        public SnapshotRegion Region { get; }
        public IReadOnlyList<CellSnapshot> Cells => cells;
        public int CellCount => cells.Count;

        internal SnapshotBuffer(long tick, SnapshotRegion region, List<CellSnapshot> cells)
        {
            Tick = tick;
            Region = region;
            this.cells = cells;
        }
    }
}
