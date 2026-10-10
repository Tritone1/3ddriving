using System;
using UnityEngine;

namespace DrivingSim.Fuel
{
    /// <summary>
    /// Hides only the station roof while the chase camera is underneath it.
    /// The columns, pumps, shop, and refuel-zone marker remain visible.
    /// </summary>
    public sealed class StationCanopyOcclusion : MonoBehaviour
    {
        [SerializeField] private Vector2 localHalfExtents = new Vector2(3.2f, 3.2f);
        [SerializeField, Min(1f)] private float roofHeight = 5.7f;

        private Renderer[] roofRenderers = Array.Empty<Renderer>();
        private Camera worldCamera;
        private bool roofVisible = true;

        private void Awake()
        {
            roofRenderers = Array.FindAll(GetComponentsInChildren<Renderer>(true), IsRoofRenderer);
            worldCamera = Camera.main;
            ApplyVisibility(true);
        }

        private void LateUpdate()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            Vector3 cameraLocal = transform.InverseTransformPoint(worldCamera.transform.position);
            bool cameraUnderRoof = Mathf.Abs(cameraLocal.x) <= localHalfExtents.x &&
                                   Mathf.Abs(cameraLocal.z) <= localHalfExtents.y &&
                                   cameraLocal.y < roofHeight;
            ApplyVisibility(!cameraUnderRoof);
        }

        private static bool IsRoofRenderer(Renderer renderer)
        {
            string objectName = renderer.gameObject.name;
            return objectName.EndsWith("_4", StringComparison.Ordinal) ||
                   objectName.EndsWith("_5", StringComparison.Ordinal);
        }

        private void ApplyVisibility(bool visible)
        {
            if (roofVisible == visible && roofRenderers.Length > 0) return;
            roofVisible = visible;
            foreach (Renderer renderer in roofRenderers)
                if (renderer != null) renderer.enabled = visible;
        }

        private void OnDisable() => ApplyVisibility(true);
    }
}
