using UnityEngine;

namespace DrivingSim.Missions
{
    public enum MissionType
    {
        ReachCheckpoint,
        TimedDelivery,
        DistanceWithoutCrash
    }

    [CreateAssetMenu(menuName = "Driving Sim/Mission", fileName = "Mission_")]
    public sealed class MissionData : ScriptableObject
    {
        [SerializeField] private string id = "mission";
        [SerializeField] private string displayName = "Mission";
        [SerializeField, TextArea] private string description;
        [SerializeField] private MissionType type;
        [SerializeField] private string targetId;
        [SerializeField, Min(1f)] private float targetDistanceMetres = 1000f;
        [SerializeField, Min(0f)] private float timeLimitSeconds;
        [SerializeField, Min(0)] private int rewardMoney = 1000;
        [SerializeField] private string unlockCarId;
        [SerializeField, Min(1f)] private float completionRadius = 8f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionType Type => type;
        public string TargetId => targetId;
        public float TargetDistanceMetres => targetDistanceMetres;
        public float TimeLimitSeconds => timeLimitSeconds;
        public int RewardMoney => rewardMoney;
        public string UnlockCarId => unlockCarId;
        public float CompletionRadius => completionRadius;

        private void OnValidate() => id = string.IsNullOrWhiteSpace(id) ? name.ToLowerInvariant().Replace(' ', '-') : id.Trim();
    }
}
