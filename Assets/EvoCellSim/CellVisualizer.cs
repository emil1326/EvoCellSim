using Assets.EvoCellSim.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.EvoCellSim
{
    // Reads WorldState.LastSnapshot and LastBondSnapshot each frame (snapshot data only —
    // no live-store access) and syncs a pool of sphere GameObjects.
    //
    // Layout: clusters are placed on an outer ring; cells within each cluster on an inner ring.
    // Bond lines are rendered via GL in OnRenderObject.
    // Cell colour encodes: cluster identity (hue), energy (brightness), local signal (yellow tint).
    //
    // Click a cell sphere to select it for inspection in SimulationHUD.
    public sealed class CellVisualizer : MonoBehaviour
    {
        [SerializeField] private SimRunnerMain simRunnerMain;
        [SerializeField] private float clusterRingRadius = 7f;
        [SerializeField] private float cellRingRadius = 1.6f;
        [SerializeField] private float cellDiameter = 0.5f;

        public int? SelectedCellId { get; private set; }
        public int? SelectedClusterId { get; private set; }

        // Stable ring-slot per cluster ID (never shrinks so hues don't shift when a cluster dies).
        private readonly Dictionary<int, int> clusterSlot = new Dictionary<int, int>();
        private readonly Dictionary<int, GameObject> pool = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, Vector3> cellPositions = new Dictionary<int, Vector3>();

        // Reused each frame to avoid per-frame heap allocations.
        private readonly Dictionary<int, int> clusterSizes = new Dictionary<int, int>();
        private readonly Dictionary<int, int> subIndex = new Dictionary<int, int>();
        private readonly HashSet<int> seenThisFrame = new HashSet<int>();
        private readonly List<int> toRemove = new List<int>();

        private MaterialPropertyBlock mpb;
        private Material lineMaterial;

        private void Awake()
        {
            mpb = new MaterialPropertyBlock();

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        private void Update()
        {
            if (simRunnerMain?.SimRunner == null) return;
            SyncPool();
            HandleClick();
        }

        // ── Cell pool ────────────────────────────────────────────────────────────

        private void SyncPool()
        {
            var snapshot = simRunnerMain.SimRunner.World.LastSnapshot;
            seenThisFrame.Clear();
            clusterSizes.Clear();
            subIndex.Clear();

            // Pass 1: stable slot assignment and cluster size count.
            foreach (var cell in snapshot)
            {
                if (!clusterSlot.ContainsKey(cell.ClusterId))
                    clusterSlot[cell.ClusterId] = clusterSlot.Count;

                clusterSizes[cell.ClusterId] = clusterSizes.ContainsKey(cell.ClusterId)
                    ? clusterSizes[cell.ClusterId] + 1
                    : 1;
            }

            int totalClusters = clusterSlot.Count;

            // Pass 2: create/move/recolor GameObjects.
            foreach (var cell in snapshot)
            {
                seenThisFrame.Add(cell.Id);

                if (!pool.TryGetValue(cell.Id, out var go))
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = "Cell_" + cell.Id;
                    go.transform.SetParent(transform, false);
                    go.transform.localScale = Vector3.one * cellDiameter;
                    pool[cell.Id] = go;
                }

                var idx = subIndex.ContainsKey(cell.ClusterId) ? subIndex[cell.ClusterId] : 0;
                subIndex[cell.ClusterId] = idx + 1;

                var pos = CellPosition(cell.ClusterId, idx, clusterSizes[cell.ClusterId], totalClusters);
                go.transform.position = pos;
                cellPositions[cell.Id] = pos;

                mpb.SetColor("_Color", CellColor(cell));
                go.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
            }

            // Retire objects for cells no longer in the snapshot.
            toRemove.Clear();
            foreach (var kvp in pool)
            {
                if (!seenThisFrame.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    toRemove.Add(kvp.Key);
                    cellPositions.Remove(kvp.Key);
                }
            }
            foreach (var id in toRemove) pool.Remove(id);
        }

        private Vector3 CellPosition(int clusterId, int subIdx, int clusterSize, int totalClusters)
        {
            int slot = clusterSlot.TryGetValue(clusterId, out var s) ? s : 0;

            Vector3 center;
            if (totalClusters <= 1)
            {
                center = Vector3.zero;
            }
            else
            {
                var angle = slot * (2f * Mathf.PI / totalClusters);
                center = new Vector3(
                    Mathf.Cos(angle) * clusterRingRadius,
                    Mathf.Sin(angle) * clusterRingRadius, 0f);
            }

            if (clusterSize <= 1) return center;

            var cellAngle = subIdx * (2f * Mathf.PI / clusterSize);
            return center + new Vector3(
                Mathf.Cos(cellAngle) * cellRingRadius,
                Mathf.Sin(cellAngle) * cellRingRadius, 0f);
        }

        // Hue = cluster identity, brightness = energy ratio, yellow tint = local signal.
        private static Color CellColor(CellRecord cell)
        {
            var hue = ((cell.ClusterId * 0.618034f) % 1f + 1f) % 1f;
            var energyRatio = cell.MaxEnergy > 0 ? Mathf.Clamp01((float)cell.Energy / cell.MaxEnergy) : 0f;
            var baseColor = Color.HSVToRGB(hue, 0.7f, 0.3f + 0.7f * energyRatio);

            // Blend in yellow for cells actively emitting signal.
            var signalStrength = Mathf.Clamp01(cell.LocalSignal);
            if (signalStrength > 0.05f)
                baseColor = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.2f), signalStrength * 0.45f);

            return baseColor;
        }

        // ── Bond line rendering ──────────────────────────────────────────────────

        private void OnRenderObject()
        {
            if (simRunnerMain?.SimRunner == null || lineMaterial == null) return;

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            foreach (var bond in simRunnerMain.SimRunner.World.LastBondSnapshot)
            {
                if (!cellPositions.TryGetValue(bond.CellAId, out var posA)) continue;
                if (!cellPositions.TryGetValue(bond.CellBId, out var posB)) continue;

                var alpha = 0.25f + 0.45f * Mathf.Clamp01(bond.Strength);
                GL.Color(new Color(0.85f, 0.9f, 1f, alpha));
                GL.Vertex(posA);
                GL.Vertex(posB);
            }

            GL.End();
            GL.PopMatrix();
        }

        // ── Click selection ──────────────────────────────────────────────────────

        private void HandleClick()
        {
#if ENABLE_INPUT_SYSTEM
            if (!UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame || Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
#else
            if (!Input.GetMouseButtonDown(0) || Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var go = hit.collider.gameObject;
                if (go.name.StartsWith("Cell_") && int.TryParse(go.name.Substring(5), out var id))
                {
                    SelectedCellId = id;
                    SelectedClusterId = simRunnerMain.SimRunner.World.TryGetCell(id, out var cell)
                        ? cell.ClusterId
                        : (int?)null;
                    return;
                }
            }

            SelectedCellId = null;
            SelectedClusterId = null;
        }

        private void OnDestroy()
        {
            foreach (var go in pool.Values)
                if (go != null) Destroy(go);
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
