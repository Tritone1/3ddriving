using UnityEngine;
using UnityEngine.EventSystems;

namespace DrivingSim.Garage
{
    public sealed class PreviewTurntable : MonoBehaviour, IDragHandler
    {
        [SerializeField, Min(0f)] private float idleDegreesPerSecond = 18f;
        [SerializeField, Min(0f)] private float dragSensitivity = 0.35f;

        private void Update() => transform.Rotate(Vector3.up, idleDegreesPerSecond * Time.unscaledDeltaTime, Space.Self);

        public void OnDrag(PointerEventData eventData) => transform.Rotate(Vector3.up, -eventData.delta.x * dragSensitivity, Space.Self);
    }
}
