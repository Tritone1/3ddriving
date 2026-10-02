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
            if (paintRenderers == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            foreach (Renderer item in paintRenderers)
                if (item != null) item.SetPropertyBlock(block);
        }
    }
}
