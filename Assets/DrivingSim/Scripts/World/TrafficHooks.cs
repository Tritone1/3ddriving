using UnityEngine;

namespace DrivingSim.World
{
    public interface ITrafficAgent
    {
        void SetRoute(Transform[] route);
        void Activate(Vector3 position, Quaternion rotation);
        void Deactivate();
    }

    public sealed class TrafficSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Transform[] route;
        public Transform[] Route => route;
    }

    public sealed class ObstacleMarker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float severity = 1f;
        public float Severity => severity;
    }
}
