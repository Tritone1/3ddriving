using System;
using UnityEngine;

namespace DrivingSim.World
{
    /// <summary>
    /// Keeps the generated road collision surface continuous and makes painted
    /// crosswalk meshes visual-only. This runs once whenever a gameplay scene loads.
    /// </summary>
    public static class RoadSurfaceSafety
    {
        private const float RoadCenterY = 0f;
        private const float RoadThickness = 0.2f;
        private const float MarkingCenterY = 0.112f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void NormalizeGeneratedRoads()
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Transform item in transforms)
            {
                if (item == null) continue;

                if (string.Equals(item.name, "Intersection_NS", StringComparison.Ordinal) ||
                    string.Equals(item.name, "Intersection_EW", StringComparison.Ordinal))
                {
                    Vector3 position = item.position;
                    position.y = RoadCenterY;
                    item.position = position;

                    Vector3 scale = item.localScale;
                    scale.y = RoadThickness;
                    item.localScale = scale;
                    continue;
                }

                if (!string.Equals(item.name, "Crosswalk", StringComparison.Ordinal)) continue;

                // The stripe is paint, not a speed bump. Keep it just above the
                // asphalt to avoid z-fighting and remove any accidental collider.
                Vector3 markingPosition = item.position;
                markingPosition.y = MarkingCenterY;
                item.position = markingPosition;

                foreach (Collider collider in item.GetComponents<Collider>())
                    collider.enabled = false;
            }

            Physics.SyncTransforms();
        }
    }
}
