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

        private Vector3 previousPosition;
        private float distanceTravelled;
        private float elapsed;
        private bool collisionDuringMission;

        public event Action<MissionData> MissionStarted;
        public event Action<MissionData> MissionSucceeded;
        public event Action<MissionData, string> MissionFailed;
        public event Action ProgressChanged;

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
            missions = definitions == null ? new List<MissionData>() : new List<MissionData>(definitions);
            playerCar = car;
            wallet = currency;
            saveService = saves;
            if (isActiveAndEnabled && playerCar != null) playerCar.CollisionOccurred += HandleCollision;
        }

        public bool StartMissionById(string id)
        {
            MissionData mission = missions.Find(item => item != null && item.Id == id);
            return StartMission(mission);
        }

        public bool StartMission(MissionData mission)
        {
            if (mission == null || playerCar == null || State == MissionState.Active) return false;
            ActiveMission = mission;
            State = MissionState.Active;
            elapsed = 0f;
            distanceTravelled = 0f;
            collisionDuringMission = false;
            previousPosition = playerCar.transform.position;
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
            if (target != null && Vector3.Distance(playerCar.transform.position, target.position) <= ActiveMission.CompletionRadius)
                Complete();
        }

        private void HandleCollision() => collisionDuringMission = true;

        private void Complete()
        {
            State = MissionState.Succeeded;
            wallet?.Add(ActiveMission.RewardMoney);
            PlayerProfile profile = saveService != null ? saveService.Profile : null;
            if (profile != null)
            {
                if (!profile.completedMissionIds.Contains(ActiveMission.Id)) profile.completedMissionIds.Add(ActiveMission.Id);
                if (!string.IsNullOrEmpty(ActiveMission.UnlockCarId) && !profile.ownedCarIds.Contains(ActiveMission.UnlockCarId))
                    profile.ownedCarIds.Add(ActiveMission.UnlockCarId);
                saveService.Save();
            }
            MissionSucceeded?.Invoke(ActiveMission);
        }

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
