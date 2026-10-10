using UnityEngine;

namespace DrivingSim.Missions
{
    public enum MissionType
    {
        ReachCheckpoint,
        TimedDelivery,
        DistanceWithoutCrash
    }

    public enum MissionDifficulty
    {
        Easy,
        Medium,
        Hard
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
        [SerializeField] private MissionDifficulty difficulty = MissionDifficulty.Easy;
        [SerializeField, Range(500, 2000)] private int rewardMoney = 500;
        [SerializeField] private string unlockCarId;
        [SerializeField, Min(1f)] private float completionRadius = 8f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionType Type => type;
        public string TargetId => targetId;
        public float TargetDistanceMetres => targetDistanceMetres;
        public float TimeLimitSeconds => timeLimitSeconds;
        public MissionDifficulty Difficulty => difficulty;
        public int RewardMoney => rewardMoney;
        public string UnlockCarId => unlockCarId;
        public float CompletionRadius => completionRadius;

        internal void ConfigureRuntime(string missionId, string missionName, string missionDescription,
            MissionType missionType, MissionDifficulty missionDifficulty, string missionTargetId,
            float distance, float timeLimit, int reward, string carUnlockId = "")
        {
            id = missionId;
            displayName = missionName;
            description = missionDescription;
            type = missionType;
            difficulty = missionDifficulty;
            targetId = missionTargetId;
            targetDistanceMetres = Mathf.Max(1f, distance);
            timeLimitSeconds = Mathf.Max(0f, timeLimit);
            rewardMoney = Mathf.Clamp(reward, 500, 2000);
            unlockCarId = carUnlockId ?? string.Empty;
        }

        private void OnValidate()
        {
            id = string.IsNullOrWhiteSpace(id) ? name.ToLowerInvariant().Replace(' ', '-') : id.Trim();
            rewardMoney = Mathf.Clamp(rewardMoney, 500, 2000);
        }
    }

    public static class MissionDifficultyRules
    {
        public static MissionDifficulty Evaluate(MissionType type, float distanceMetres, float timeLimitSeconds)
        {
            if (type == MissionType.DistanceWithoutCrash)
                return distanceMetres >= 1000f ? MissionDifficulty.Hard : MissionDifficulty.Medium;
            if (type == MissionType.TimedDelivery)
                return timeLimitSeconds <= 60f ? MissionDifficulty.Hard :
                    timeLimitSeconds <= 100f ? MissionDifficulty.Medium : MissionDifficulty.Easy;
            return MissionDifficulty.Easy;
        }

        public static Vector2Int RewardRange(MissionDifficulty difficulty)
        {
            return difficulty switch
            {
                MissionDifficulty.Easy => new Vector2Int(500, 900),
                MissionDifficulty.Medium => new Vector2Int(1000, 1450),
                _ => new Vector2Int(1500, 2000)
            };
        }
    }
}
