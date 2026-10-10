using System;
using System.Collections.Generic;
using DrivingSim.Core;
using DrivingSim.Save;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Missions
{
    public sealed class MissionManager : MonoBehaviour
    {
        public enum MissionState { Idle, Active, Succeeded, Failed }

        [SerializeField] private List<MissionData> missions = new List<MissionData>();
        [SerializeField] private CarController playerCar;
        [SerializeField] private WalletBehaviour wallet;
        [SerializeField] private SaveService saveService;
        [SerializeField] private bool generateRuntimeMissions = true;
        [SerializeField, Min(600f)] private float missionCooldownSeconds = 600f;

        private readonly List<MissionData> generatedMissions = new List<MissionData>();
        private List<MissionData> missionTemplates = new List<MissionData>();
        private Vector3 previousPosition;
        private float distanceTravelled;
        private float elapsed;
        private bool collisionDuringMission;
        private bool destinationArmed;

        public event Action<MissionData> MissionStarted;
        public event Action<MissionData> MissionSucceeded;
        public event Action<MissionData, string> MissionFailed;
        public event Action ProgressChanged;
        public event Action MissionListChanged;

        public IReadOnlyList<MissionData> Missions => missions;
        public MissionData ActiveMission { get; private set; }
        public MissionState State { get; private set; }
        public float DistanceTravelled => distanceTravelled;
        public float RemainingTime => ActiveMission == null || ActiveMission.TimeLimitSeconds <= 0f ? 0f : Mathf.Max(0f, ActiveMission.TimeLimitSeconds - elapsed);
        public Transform CurrentTarget
        {
            get
            {
                if (ActiveMission == null || string.IsNullOrEmpty(ActiveMission.TargetId)) return null;
                return MissionTarget.TryGet(ActiveMission.TargetId, out MissionTarget target) ? target.transform : null;
            }
        }

        private void OnEnable()
        {
            if (playerCar != null) playerCar.CollisionOccurred += HandleCollision;
        }

        private void OnDisable()
        {
            if (playerCar != null) playerCar.CollisionOccurred -= HandleCollision;
        }

        private void OnDestroy()
        {
            foreach (MissionData mission in generatedMissions)
                if (mission != null) Destroy(mission);
        }

        private void Update()
        {
            if (State != MissionState.Active || ActiveMission == null || playerCar == null) return;
            elapsed += Time.deltaTime;
            distanceTravelled += Vector3.Distance(playerCar.transform.position, previousPosition);
            previousPosition = playerCar.transform.position;

            switch (ActiveMission.Type)
            {
                case MissionType.ReachCheckpoint:
                    CheckDestination();
                    break;
                case MissionType.TimedDelivery:
                    if (ActiveMission.TimeLimitSeconds > 0f && elapsed >= ActiveMission.TimeLimitSeconds)
                        Fail("Time expired");
                    else
                        CheckDestination();
                    break;
                case MissionType.DistanceWithoutCrash:
                    if (collisionDuringMission) Fail("Vehicle damaged");
                    else if (distanceTravelled >= ActiveMission.TargetDistanceMetres) Complete();
                    break;
            }
            ProgressChanged?.Invoke();
        }

        public void Configure(IEnumerable<MissionData> definitions, CarController car, WalletBehaviour currency, SaveService saves)
        {
            if (playerCar != null) playerCar.CollisionOccurred -= HandleCollision;
            missionTemplates = definitions == null ? new List<MissionData>() : new List<MissionData>(definitions);
            playerCar = car;
            wallet = currency;
            saveService = saves;
            if (isActiveAndEnabled && playerCar != null) playerCar.CollisionOccurred += HandleCollision;
            if (generateRuntimeMissions) GenerateMissionBoard();
            else
            {
                missions = new List<MissionData>(missionTemplates);
                MissionListChanged?.Invoke();
            }
        }

        public void GenerateMissionBoard()
        {
            if (State == MissionState.Active) return;
            foreach (MissionData mission in generatedMissions)
                if (mission != null) Destroy(mission);
            generatedMissions.Clear();

            List<string> targets = new List<string>();
            foreach (MissionData template in missionTemplates)
                if (template != null && !string.IsNullOrEmpty(template.TargetId) && !targets.Contains(template.TargetId))
                    targets.Add(template.TargetId);

            string checkpointTarget = SelectTargetAwayFromPlayer(targets, string.Empty);
            string deliveryTarget = SelectTargetAwayFromPlayer(targets, checkpointTarget);
            string deliveryUnlock = string.Empty;
            foreach (MissionData template in missionTemplates)
                if (template != null && template.Type == MissionType.TimedDelivery && !string.IsNullOrEmpty(template.UnlockCarId))
                {
                    deliveryUnlock = template.UnlockCarId;
                    break;
                }
            generatedMissions.Add(CreateGeneratedMission(MissionType.ReachCheckpoint, MissionDifficulty.Easy,
                checkpointTarget, 0f, 0f));
            generatedMissions.Add(CreateGeneratedMission(MissionType.TimedDelivery, MissionDifficulty.Medium,
                deliveryTarget, 0f, UnityEngine.Random.Range(70f, 96f), deliveryUnlock));
            generatedMissions.Add(CreateGeneratedMission(MissionType.DistanceWithoutCrash, MissionDifficulty.Hard,
                string.Empty, UnityEngine.Random.Range(1000f, 1601f), 0f));
            missions = new List<MissionData>(generatedMissions);
            MissionListChanged?.Invoke();
        }

        private string SelectTargetAwayFromPlayer(List<string> targets, string excludedTarget)
        {
            if (targets == null || targets.Count == 0) return string.Empty;

            string bestTarget = string.Empty;
            float bestDistance = -1f;
            foreach (string targetId in targets)
            {
                if (targetId == excludedTarget || !MissionTarget.TryGet(targetId, out MissionTarget target)) continue;
                float distance = playerCar == null ? 0f : (target.transform.position - playerCar.transform.position).sqrMagnitude;
                if (distance <= bestDistance) continue;
                bestDistance = distance;
                bestTarget = targetId;
            }

            if (!string.IsNullOrEmpty(bestTarget)) return bestTarget;
            foreach (string targetId in targets)
                if (targetId != excludedTarget) return targetId;
            return targets[0];
        }

        private static MissionData CreateGeneratedMission(MissionType type, MissionDifficulty requestedDifficulty,
            string targetId, float distance, float timeLimit, string unlockCarId = "")
        {
            MissionDifficulty evaluatedDifficulty = MissionDifficultyRules.Evaluate(type, distance, timeLimit);
            if (evaluatedDifficulty != requestedDifficulty) requestedDifficulty = evaluatedDifficulty;
            Vector2Int rewardRange = MissionDifficultyRules.RewardRange(requestedDifficulty);
            int reward = UnityEngine.Random.Range(rewardRange.x / 50, rewardRange.y / 50 + 1) * 50;
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            MissionData mission = ScriptableObject.CreateInstance<MissionData>();
            mission.name = $"Generated_{requestedDifficulty}_{suffix}";

            string title;
            string description;
            switch (type)
            {
                case MissionType.TimedDelivery:
                    title = "Express Delivery";
                    description = $"Reach the delivery point within {timeLimit:0} seconds.";
                    break;
                case MissionType.DistanceWithoutCrash:
                    title = "Clean Distance Run";
                    description = $"Drive {distance / 1000f:0.0} km without a collision.";
                    break;
                default:
                    title = "City Checkpoint";
                    description = "Drive to the marked checkpoint.";
                    break;
            }

            mission.ConfigureRuntime($"generated-{suffix}", title, description, type, requestedDifficulty,
                targetId, distance, timeLimit, reward, unlockCarId);
            return mission;
        }

        public bool StartMissionById(string id)
        {
            MissionData mission = missions.Find(item => item != null && item.Id == id);
            return StartMission(mission);
        }

        public bool StartMission(MissionData mission)
        {
            if (mission == null || playerCar == null || State == MissionState.Active || IsMissionLocked(mission)) return false;
            ActiveMission = mission;
            State = MissionState.Active;
            elapsed = 0f;
            distanceTravelled = 0f;
            collisionDuringMission = false;
            previousPosition = playerCar.transform.position;
            Transform target = CurrentTarget;
            destinationArmed = target != null &&
                Vector3.Distance(playerCar.transform.position, target.position) > mission.CompletionRadius;
            MissionStarted?.Invoke(mission);
            ProgressChanged?.Invoke();
            return true;
        }

        public void CancelActiveMission()
        {
            if (State == MissionState.Active) Fail("Mission cancelled");
        }

        private void CheckDestination()
        {
            Transform target = CurrentTarget;
            if (target == null) return;

            float distance = Vector3.Distance(playerCar.transform.position, target.position);
            if (!destinationArmed)
            {
                float armDistance = Mathf.Max(ActiveMission.CompletionRadius + 3f, ActiveMission.CompletionRadius * 1.25f);
                if (distance > armDistance) destinationArmed = true;
                return;
            }

            if (distance <= ActiveMission.CompletionRadius) Complete();
        }

        private void HandleCollision() => collisionDuringMission = true;

        private void Complete()
        {
            if (State != MissionState.Active || ActiveMission == null) return;
            State = MissionState.Succeeded;
            wallet?.Add(ActiveMission.RewardMoney);
            PlayerProfile profile = saveService != null ? saveService.Profile : null;
            if (profile != null)
            {
                if (!profile.completedMissionIds.Contains(ActiveMission.Id)) profile.completedMissionIds.Add(ActiveMission.Id);
                long cooldownLength = (long)Math.Ceiling(Mathf.Max(600f, missionCooldownSeconds));
                profile.SetMissionCooldown(GetCooldownKey(ActiveMission), DateTimeOffset.UtcNow.ToUnixTimeSeconds() + cooldownLength);
                if (!string.IsNullOrEmpty(ActiveMission.UnlockCarId) && !profile.ownedCarIds.Contains(ActiveMission.UnlockCarId))
                    profile.ownedCarIds.Add(ActiveMission.UnlockCarId);
                saveService.Save();
            }
            MissionSucceeded?.Invoke(ActiveMission);
        }

        public TimeSpan GetCooldownRemaining(MissionData mission)
        {
            PlayerProfile profile = saveService != null ? saveService.Profile : null;
            if (mission == null || profile == null) return TimeSpan.Zero;
            long remainingSeconds = profile.GetMissionCooldownEnd(GetCooldownKey(mission)) - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return remainingSeconds > 0L ? TimeSpan.FromSeconds(remainingSeconds) : TimeSpan.Zero;
        }

        public bool IsMissionLocked(MissionData mission) => GetCooldownRemaining(mission) > TimeSpan.Zero;

        // Call this only after the rewarded-ad SDK confirms that the player earned the reward.
        public bool UnlockMissionCooldownAfterRewardedAd(MissionData mission)
        {
            PlayerProfile profile = saveService != null ? saveService.Profile : null;
            if (mission == null || profile == null || !IsMissionLocked(mission)) return false;
            profile.ClearMissionCooldown(GetCooldownKey(mission));
            saveService.Save();
            MissionListChanged?.Invoke();
            return true;
        }

        private static string GetCooldownKey(MissionData mission) =>
            mission == null ? string.Empty : $"{mission.Type}:{mission.Difficulty}";

        private void Fail(string reason)
        {
            State = MissionState.Failed;
            MissionFailed?.Invoke(ActiveMission, reason);
        }

        public void ClearResult()
        {
            if (State == MissionState.Active) return;
            ActiveMission = null;
            State = MissionState.Idle;
            ProgressChanged?.Invoke();
        }
    }
}
