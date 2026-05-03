using Assets.EvoCellSim.Core;
using UnityEngine;

namespace Assets.EvoCellSim
{
    // Immediate-mode GUI: stats overlay, pause/speed controls, and cell/cluster inspection panel.
    public sealed class SimulationHUD : MonoBehaviour
    {
        [SerializeField] private SimRunnerMain simRunnerMain;
        [SerializeField] private CellVisualizer cellVisualizer;

        private static readonly float[] SpeedPresets = { 0.25f, 0.5f, 1f, 2f, 4f };

        private string lastQueryResult;
        private int? lastQueriedCluster;

        private void OnGUI()
        {
            if (simRunnerMain?.SimRunner == null) return;
            DrawStatsPanel();
            if (cellVisualizer != null && cellVisualizer.SelectedCellId.HasValue)
                DrawInspectionPanel();
        }

        // ── Stats + controls ────────────────────────────────────────────────────

        private void DrawStatsPanel()
        {
            var world = simRunnerMain.SimRunner.World;

            GUI.Box(new Rect(10, 10, 270, 138), "EvoCellSim");

            var x = 18f;
            var y = 34f;
            const float lh = 20f;

            GUI.Label(new Rect(x, y, 250, lh), $"Tick:     {world.Tick}");
            y += lh;
            GUI.Label(new Rect(x, y, 250, lh), $"Cells:    {simRunnerMain.AliveCellCount}   "
                                               + $"Clusters: {simRunnerMain.ClusterCount}");
            y += lh;
            GUI.Label(new Rect(x, y, 250, lh),
                simRunnerMain.IsPaused ? "[ PAUSED ]" : $"Speed:    {simRunnerMain.SimSpeed:0.##}×");
            y += lh + 4f;

            // Pause / resume
            var pauseLabel = simRunnerMain.IsPaused ? "▶ Resume" : "■ Pause";
            if (GUI.Button(new Rect(x, y, 76, 22), pauseLabel))
                simRunnerMain.IsPaused = !simRunnerMain.IsPaused;

            y += 28f;

            // Speed presets
            GUI.Label(new Rect(x, y, 46, lh), "Speed:");
            var bx = x + 48f;
            foreach (var preset in SpeedPresets)
            {
                var label = preset < 1f ? $"{preset:0.##}×" : $"{(int)preset}×";
                var active = Mathf.Approximately(simRunnerMain.SimSpeed, preset);
                var btnLabel = active ? $"[{label}]" : label;
                if (GUI.Button(new Rect(bx, y, 38, 22), btnLabel) && !active)
                    simRunnerMain.SimSpeed = preset;
                bx += 42f;
            }
        }

        // ── Cell inspection ─────────────────────────────────────────────────────

        private void DrawInspectionPanel()
        {
            var cellId = cellVisualizer.SelectedCellId.Value;
            var world = simRunnerMain.SimRunner.World;
            if (!world.TryGetCell(cellId, out var cell)) return;

            var panelY = Screen.height - 215f;
            GUI.Box(new Rect(10, panelY, 270, 205), $"Cell #{cellId}  (Cluster #{cell.ClusterId})");

            var x = 18f;
            var y = panelY + 24f;
            const float lh = 18f;
            const float bw = 234f;

            // Energy bar
            var energyRatio = cell.MaxEnergy > 0 ? (float)cell.Energy / cell.MaxEnergy : 0f;
            GUI.Label(new Rect(x, y, bw, lh), $"Energy:  {cell.Energy} / {cell.MaxEnergy}  ({energyRatio:P0})");
            y += lh;
            DrawBar(new Rect(x, y, bw, 7f), Mathf.Clamp01(energyRatio), Color.green);
            y += 11f;

            // Damage bar
            var deathThreshold = world.Settings.DeathDamageThreshold;
            var damageRatio = deathThreshold > 0 ? (float)cell.Damage / deathThreshold : 0f;
            GUI.Label(new Rect(x, y, bw, lh), $"Damage:  {cell.Damage} / {deathThreshold}");
            y += lh;
            DrawBar(new Rect(x, y, bw, 7f), Mathf.Clamp01(damageRatio), Color.red);
            y += 11f;

            GUI.Label(new Rect(x, y, bw, lh), $"Neighbors: {cell.NeighborCount}   Bond depth: {cell.BondDepth}");
            y += lh;
            GUI.Label(new Rect(x, y, bw, lh), $"Signal: {cell.LocalSignal:F2}   Recv: {cell.ReceivedSignal:F2}");
            y += lh;
            GUI.Label(new Rect(x, y, bw, lh), $"Reprod cooldown: {cell.ReprodCooldown}");
            y += lh + 6f;

            // Cluster query button
            if (GUI.Button(new Rect(x, y, 116, 22), "Query cluster"))
            {
                var buf = world.QuerySnapshot(new SnapshotRegion(cell.ClusterId));
                lastQueriedCluster = cell.ClusterId;
                lastQueryResult = $"Cluster #{cell.ClusterId}: {buf.CellCount} cells @ tick {buf.Tick}";
            }

            if (lastQueryResult != null && lastQueriedCluster == cell.ClusterId)
                GUI.Label(new Rect(x, y + 26f, bw, lh), lastQueryResult);
        }

        private static void DrawBar(Rect rect, float fill, Color barColor)
        {
            GUI.color = new Color(0.25f, 0.25f, 0.25f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            if (fill > 0f)
            {
                GUI.color = barColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * fill, rect.height),
                    Texture2D.whiteTexture, ScaleMode.StretchToFill);
            }
            GUI.color = Color.white;
        }
    }
}
