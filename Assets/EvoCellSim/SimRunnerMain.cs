using Assets.EvoCellSim.Core;
using UnityEngine;

namespace Assets.EvoCellSim
{
    public sealed class SimRunnerMain : MonoBehaviour
    {
        [SerializeField] private ulong worldSeed = 465124787894UL;
        [SerializeField] private int targetTicksPerSecond = 20;

        public SimulationRunner SimRunner { get; private set; }
        public bool IsPaused { get; set; }
        public float SimSpeed { get; set; } = 1f;
        public int AliveCellCount { get; private set; }
        public int ClusterCount { get; private set; }

        private float tickAccumulator;

        private void Start()
        {
            EnsureCamera();

            var settings = new SimulationSettings(worldSeed)
            {
                MaxCellCount = 200,
                MaxEnergy = 50,
                PassiveEnergyGain = 2,
                PassiveUpkeepCost = 1,
                DeathDamageThreshold = 100,
                BondDecayPerTick = 0.003f,
                ReproductionCooldown = 20,
                MutationRate = 0.05f,
            };
            SimRunner = new SimulationRunner(settings);
            SpawnInitialCells();
            RefreshStats();
        }

        private void Update()
        {
            if (IsPaused || SimRunner == null) return;

            tickAccumulator += Time.deltaTime * Mathf.Max(0.1f, SimSpeed) * targetTicksPerSecond;

            var maxPerFrame = Mathf.Max(1, targetTicksPerSecond * 4);
            var ticked = 0;
            while (tickAccumulator >= 1f && ticked < maxPerFrame)
            {
                SimRunner.Tick();
                ticked++;
                tickAccumulator -= 1f;
            }

            if (ticked > 0) RefreshStats();
        }

        private void RefreshStats()
        {
            var alive = 0;
            foreach (var cell in SimRunner.World.Cells.Records)
                if (cell.Alive) alive++;
            AliveCellCount = alive;
            ClusterCount = SimRunner.World.Clusters.Count;
        }

        private void SpawnInitialCells()
        {
            SpawnCluster(4, new byte[] { 1, 0, 0 });
            SpawnCluster(4, new byte[] { 0, 1, 0 });
            SpawnCluster(3, new byte[] { 0, 0, 1 });
        }

        private void SpawnCluster(int cellCount, byte[] speciesTag)
        {
            var world = SimRunner.World;

            var genomeId = world.Genomes.Count + 1;
            var genome = new GenomeRecord
            {
                Id = genomeId,
                ParentId = 0,
                SpeciesGenome = speciesTag,
                ModuleGenome = new byte[] { 1, 4 },
                // 0x13 = Reproduce opcode token, 0xFF = control/terminate
                InstructionGenome = new byte[] { 0x13, 0xFF }
            };
            world.AddGenome(in genome);

            var firstCellId = world.Cells.Count + 1;

            for (var i = 0; i < cellCount; i++)
            {
                var cellId = world.Cells.Count + 1;
                var cell = new CellRecord
                {
                    Id = cellId,
                    Alive = true,
                    GenomeId = genomeId,
                    ClusterId = firstCellId,
                    Energy = world.Settings.MaxEnergy,
                    MaxEnergy = world.Settings.MaxEnergy,
                    Damage = 0,
                    Pressure = 0,
                    MaintenanceDebt = 0,
                    NeighborCount = 0,
                    BondDepth = 0,
                    ClusterPosition = i,
                    LocalSignal = 0,
                    ReceivedSignal = 0,
                    ReprodCooldown = 0
                };
                world.AddCell(in cell);

                var movMod = new ModuleRecord { Id = world.Modules.Count + 1, OwnerCellId = cellId, ModuleTypeId = 1, Active = true };
                world.AddModule(in movMod);
                var repMod = new ModuleRecord { Id = world.Modules.Count + 1, OwnerCellId = cellId, ModuleTypeId = 4, Active = true };
                world.AddModule(in repMod);
                var repairMod = new ModuleRecord { Id = world.Modules.Count + 1, OwnerCellId = cellId, ModuleTypeId = 3, Active = true };
                world.AddModule(in repairMod);

                if (i > 0)
                    world.TryCreateBond(firstCellId, cellId, 1f, out _);
            }
        }

        // Creates an orthographic camera if none exists so the scene is self-contained.
        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 14f;
            cam.transform.position = new Vector3(0f, 0f, -20f);
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
