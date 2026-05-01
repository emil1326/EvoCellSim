using Assets.EvoCellSim.Core;
using UnityEngine;

namespace Assets.EvoCellSim
{
    public class SimRunnerMain : MonoBehaviour
    {
        public SimRunner simRunner;
        public int maxCells = 100;
        public int maxComponents = 1000;
        private int protoIndex = -1;
        // Use this for initialization
        void Start()
        {
            simRunner = new SimRunner(maxCells, maxComponents);
            // spawn a protocell at origin for debugging
            //protoIndex = simRunner.SpawnProtocell(0, 0, 20f);
        }

        // Update is called once per frame
        void Update()
        {
            simRunner.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            simRunner.Dispose();

        }

        private void OnGUI()
        {
            if (protoIndex >= 0)
            {
                //var cs = simRunner.GetCellState(protoIndex);
                //GUI.Label(new Rect(10, 10, 300, 20), $"Proto idx: {protoIndex} Alive: {cs.isAlive} Energy: {cs.energy:F2}");
                //GUI.Label(new Rect(10, 30, 300, 20), $"Pos: {cs.posX},{cs.posY} Pressure: {cs.pressure:F2}");
            }
        }
    }
}
