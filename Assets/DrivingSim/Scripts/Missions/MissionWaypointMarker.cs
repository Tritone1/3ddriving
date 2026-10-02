using UnityEngine;

namespace DrivingSim.Missions
{
    public sealed class MissionWaypointMarker : MonoBehaviour
    {
        [SerializeField] private MissionManager missionManager;
        [SerializeField, Min(0f)] private float height = 3f;
        [SerializeField, Min(0f)] private float bobAmount = 0.35f;
        [SerializeField, Min(0f)] private float bobSpeed = 2f;
        private Renderer[] renderers;

        private void Awake() => renderers = GetComponentsInChildren<Renderer>(true);

        private void LateUpdate()
        {
            Transform target = missionManager != null ? missionManager.CurrentTarget : null;
            bool visible = target != null && missionManager.State == MissionManager.MissionState.Active;
            foreach (Renderer item in renderers) item.enabled = visible;
            if (!visible) return;
            transform.position = target.position + Vector3.up * (height + Mathf.Sin(Time.time * bobSpeed) * bobAmount);
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
        }
    }
}
