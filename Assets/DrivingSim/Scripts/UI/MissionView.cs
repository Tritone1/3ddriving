using System;
using System.Collections;
using DrivingSim.Missions;
using UnityEngine;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    public sealed class MissionView : MonoBehaviour
    {
        [SerializeField] private MissionManager manager;
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text reward;
        [SerializeField] private Text result;
        private int index;
        private Coroutine hideResultRoutine;
        private float nextCooldownRefresh;

        private void Start() => Refresh();

        private void Update()
        {
            if (Time.unscaledTime < nextCooldownRefresh) return;
            nextCooldownRefresh = Time.unscaledTime + 1f;
            Refresh();
        }

        private void OnEnable()
        {
            if (manager != null)
            {
                manager.MissionSucceeded += HandleSuccess;
                manager.MissionFailed += HandleFailure;
                manager.MissionListChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.MissionSucceeded -= HandleSuccess;
                manager.MissionFailed -= HandleFailure;
                manager.MissionListChanged -= Refresh;
            }
        }

        public void Next() { index++; Refresh(); }
        public void Previous() { index--; Refresh(); }
        public void StartSelected()
        {
            if (manager == null || manager.Missions.Count == 0) return;
            MissionData mission = manager.Missions[index];
            TimeSpan remaining = manager.GetCooldownRemaining(mission);
            if (remaining > TimeSpan.Zero)
            {
                ShowResult($"MISSION LOCKED\n{FormatCooldown(remaining)}\nWATCH AD TO UNLOCK EARLY");
                return;
            }
            if (manager.StartMission(mission)) HideResultImmediately();
        }

        // Wire the rewarded-ad success callback to this method when the ad SDK is added.
        public void UnlockSelectedMissionAfterRewardedAd()
        {
            if (manager == null || manager.Missions.Count == 0) return;
            if (!manager.UnlockMissionCooldownAfterRewardedAd(manager.Missions[index])) return;
            ShowResult("MISSION UNLOCKED");
            Refresh();
        }
        public void DismissResult()
        {
            manager?.ClearResult();
            HideResultImmediately();
        }

        private void Refresh()
        {
            if (manager == null || manager.Missions.Count == 0) return;
            index = (index % manager.Missions.Count + manager.Missions.Count) % manager.Missions.Count;
            MissionData mission = manager.Missions[index];
            if (title != null) title.text = mission.DisplayName;
            if (description != null) description.text = mission.Description;
            if (reward != null)
            {
                TimeSpan remaining = manager.GetCooldownRemaining(mission);
                reward.text = remaining > TimeSpan.Zero
                    ? $"{mission.Difficulty.ToString().ToUpperInvariant()}  |  LOCKED {FormatCooldown(remaining)}\nWATCH AD TO UNLOCK EARLY"
                    : $"{mission.Difficulty.ToString().ToUpperInvariant()}  |  Reward: ${mission.RewardMoney:N0}";
            }
        }

        private void HandleSuccess(MissionData mission)
        {
            ShowResult($"SUCCESS\n+${mission.RewardMoney:N0}\nMISSION LOCKED FOR 10 MIN");

            // Move the mission board to the item after the mission that just finished.
            // ClearResult returns the manager to Idle so its Start button can launch it.
            if (manager != null && manager.Missions.Count > 0)
            {
                int completedIndex = index;
                for (int i = 0; i < manager.Missions.Count; i++)
                {
                    if (manager.Missions[i] == mission || manager.Missions[i].Id == mission.Id)
                    {
                        completedIndex = i;
                        break;
                    }
                }

                index = (completedIndex + 1) % manager.Missions.Count;
                manager.ClearResult();
                manager.GenerateMissionBoard();
                SelectNextUnlockedMission();
                Refresh();
            }
        }

        private void SelectNextUnlockedMission()
        {
            if (manager == null || manager.Missions.Count == 0) return;
            index = (index % manager.Missions.Count + manager.Missions.Count) % manager.Missions.Count;
            for (int offset = 0; offset < manager.Missions.Count; offset++)
            {
                int candidate = (index + offset) % manager.Missions.Count;
                if (manager.IsMissionLocked(manager.Missions[candidate])) continue;
                index = candidate;
                return;
            }
        }

        private static string FormatCooldown(TimeSpan remaining)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)remaining.TotalSeconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
        private void HandleFailure(MissionData mission, string reason) => ShowResult($"FAILED\n{reason}");
        private void ShowResult(string message)
        {
            if (result == null) return;
            if (hideResultRoutine != null) StopCoroutine(hideResultRoutine);
            result.gameObject.SetActive(true);
            result.text = message;
            hideResultRoutine = StartCoroutine(HideResultAfterDelay());
        }

        private IEnumerator HideResultAfterDelay()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (result != null) result.gameObject.SetActive(false);
            hideResultRoutine = null;
        }

        private void HideResultImmediately()
        {
            if (hideResultRoutine != null)
            {
                StopCoroutine(hideResultRoutine);
                hideResultRoutine = null;
            }
            if (result != null) result.gameObject.SetActive(false);
        }
    }
}
