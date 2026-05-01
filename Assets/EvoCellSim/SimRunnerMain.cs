using Assets.EvoCellSim.Core;
using UnityEngine;

namespace Assets.EvoCellSim
{
    public class SimRunnerMain : MonoBehaviour
    {
        public SimulationRunner simRunner;
        public int maxCells = 100;
        public int maxComponents = 1000;
        public ulong worldSeed = 465124787894UL;
        // Use this for initialization
        void Start()
        {
            var settings = new SimulationSettings(worldSeed);
            simRunner = new SimulationRunner(settings);
        }

        // FixedUpdate is used so the simulation advances in a stable tick cadence.
        void FixedUpdate()
        {
            simRunner?.Tick();
        }

        private void OnDestroy()
        {
        }

        private void OnGUI()
        {
            if (simRunner == null)
            {
                return;
            }

            GUI.Label(new Rect(10, 10, 400, 20), $"Seed: {simRunner.World.Seed}");
            GUI.Label(new Rect(10, 30, 400, 20), $"Tick: {simRunner.World.Tick}");
            GUI.Label(new Rect(10, 50, 400, 20), $"Last phase count: {simRunner.LastTickTrace.Count}");
        }
    }
}
