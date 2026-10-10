using UnityEngine;

namespace DrivingSim.Vehicles
{
    public sealed class CarRigReferences : MonoBehaviour
    {
        [SerializeField] private Transform chaseCameraAnchor;
        [SerializeField] private Transform hoodCameraAnchor;
        [SerializeField] private Renderer[] paintRenderers;

        public Transform ChaseCameraAnchor => chaseCameraAnchor;
        public Transform HoodCameraAnchor => hoodCameraAnchor;

        public void ApplyPaint(Color color)
        {
            Renderer[] renderers = paintRenderers;
            bool hasVisibleConfiguredRenderer = false;
            if (renderers != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    hasVisibleConfiguredRenderer = true;
                    break;
                }
            }

            // Imported visuals replace and disable the original placeholder body.
            // Older prefabs can therefore contain valid-looking references that
            // point only to invisible renderers. Paint the active visual instead.
            if (!hasVisibleConfiguredRenderer)
                renderers = GetComponentsInChildren<Renderer>(true);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            foreach (Renderer item in renderers)
                if (item != null && item.enabled && item.gameObject.activeInHierarchy) item.SetPropertyBlock(block);
        }
    }
}
