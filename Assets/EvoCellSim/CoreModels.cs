namespace Assets.EvoCellSim.Core
{
    public interface IIdentifiable
    {
        int Id { get; }
    }

    public struct CellRecord : IIdentifiable
    {
        public int Id { get; set; }
        public bool Alive { get; set; }
        public int GenomeId { get; set; }
        public int ClusterId { get; set; }
        public int Energy { get; set; }
        public int Damage { get; set; }
        public float Pressure { get; set; }
        public int MaxEnergy { get; set; }
    }

    public struct GenomeRecord : IIdentifiable
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public byte[] SpeciesGenome { get; set; }
        public byte[] ModuleGenome { get; set; }
        public byte[] InstructionGenome { get; set; }
    }

    public struct ModuleRecord : IIdentifiable
    {
        public int Id { get; set; }
        public int OwnerCellId { get; set; }
        public int ModuleTypeId { get; set; }
        public bool Active { get; set; }
    }

    public struct BondRecord : IIdentifiable
    {
        public int Id { get; set; }
        public int CellAId { get; set; }
        public int CellBId { get; set; }
        public float Strength { get; set; }
    }

    public struct ClusterRecord : IIdentifiable
    {
        public int Id { get; set; }
        public int FirstCellIndex { get; set; }
        public int CellCount { get; set; }
    }

    public struct FieldRecord : IIdentifiable
    {
        public int Id { get; set; }
        public float Value { get; set; }
    }

    public struct SignalRecord : IIdentifiable
    {
        public int Id { get; set; }
        public int SourceCellId { get; set; }
        public int Channel { get; set; }
        public float Intensity { get; set; }
    }

    public enum IntentKind
    {
        Move = 1,
        Mutation = 2,
        Wait = 3,
        Repair = 4
    }

    public struct IntentRecord : IIdentifiable
    {
        public int Id { get; set; }
        public int SourceCellId { get; set; }
        public int TargetCellId { get; set; }
        public IntentKind Kind { get; set; }
    }
}