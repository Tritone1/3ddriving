using System;
using System.Collections.Generic;
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
        private const float RoadSurfaceY = RoadCenterY + RoadThickness * 0.5f;
        private const float MarkingCenterY = 0.112f;
        private const string ContinuousColliderName = "__ContinuousRoadCollision";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void NormalizeGeneratedRoads()
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<BoxCollider> roadColliders = new List<BoxCollider>();

            foreach (Transform item in transforms)
            {
                if (item == null) continue;

                if (item.name.StartsWith("Downtown Building ", StringComparison.Ordinal))
                {
                    EnsureBuildingCollider(item);
                    continue;
                }

                if (IsGeneratedRoad(item.name))
                {
                    Vector3 position = item.position;
                    position.y = RoadCenterY;
                    item.position = position;

                    Vector3 scale = item.localScale;
                    scale.y = RoadThickness;
                    item.localScale = scale;
                    BoxCollider roadCollider = item.GetComponent<BoxCollider>();
                    if (roadCollider != null) roadColliders.Add(roadCollider);
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
            BuildContinuousRoadCollider(roadColliders);
            Physics.SyncTransforms();
        }

        private static bool IsGeneratedRoad(string objectName) =>
            string.Equals(objectName, "Road_North", StringComparison.Ordinal) ||
            string.Equals(objectName, "Road_South", StringComparison.Ordinal) ||
            string.Equals(objectName, "Road_East", StringComparison.Ordinal) ||
            string.Equals(objectName, "Road_West", StringComparison.Ordinal) ||
            string.Equals(objectName, "Intersection_NS", StringComparison.Ordinal) ||
            string.Equals(objectName, "Intersection_EW", StringComparison.Ordinal);

        private static void BuildContinuousRoadCollider(List<BoxCollider> roadColliders)
        {
            GameObject existing = GameObject.Find(ContinuousColliderName);
            if (existing != null) UnityEngine.Object.Destroy(existing);
            if (roadColliders == null || roadColliders.Count == 0) return;

            List<Vector3> vertices = new List<Vector3>(roadColliders.Count * 4);
            List<int> triangles = new List<int>(roadColliders.Count * 6);
            foreach (BoxCollider roadCollider in roadColliders)
            {
                if (roadCollider == null) continue;
                Bounds bounds = roadCollider.bounds;
                int start = vertices.Count;
                vertices.Add(new Vector3(bounds.min.x, RoadSurfaceY, bounds.min.z));
                vertices.Add(new Vector3(bounds.min.x, RoadSurfaceY, bounds.max.z));
                vertices.Add(new Vector3(bounds.max.x, RoadSurfaceY, bounds.max.z));
                vertices.Add(new Vector3(bounds.max.x, RoadSurfaceY, bounds.min.z));
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
                roadCollider.enabled = false;
            }

            Mesh mesh = new Mesh { name = "Continuous Road Collision Mesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            GameObject surface = new GameObject(ContinuousColliderName);
            surface.layer = roadColliders[0].gameObject.layer;
            MeshCollider meshCollider = surface.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static void EnsureBuildingCollider(Transform building)
        {
            if (building.GetComponent<Collider>() != null) return;
            Renderer[] renderers = building.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);

            BoxCollider collider = building.gameObject.AddComponent<BoxCollider>();
            collider.center = building.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = building.InverseTransformVector(worldBounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            collider.isTrigger = false;
        }
    }
}
