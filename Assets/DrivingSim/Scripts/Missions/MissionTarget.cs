using System.Collections.Generic;
using UnityEngine;

namespace DrivingSim.Missions
{
    public sealed class MissionTarget : MonoBehaviour
    {
        private static readonly Dictionary<string, MissionTarget> Targets = new Dictionary<string, MissionTarget>();
        [SerializeField] private string id = "target";

        public string Id => id;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(id)) Targets[id] = this;
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(id) && Targets.TryGetValue(id, out MissionTarget current) && current == this)
                Targets.Remove(id);
        }

        public static bool TryGet(string targetId, out MissionTarget target) => Targets.TryGetValue(targetId, out target);
    }
}
