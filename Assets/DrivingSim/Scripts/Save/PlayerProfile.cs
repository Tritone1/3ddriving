using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrivingSim.Save
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int version = 3;
        public int money = 1000;
        public string selectedCarId = "city-hatch";
        public List<string> ownedCarIds = new List<string> { "city-hatch" };
        public List<string> completedMissionIds = new List<string>();
        public List<MissionCooldownRecord> missionCooldowns = new List<MissionCooldownRecord>();
        public List<CarProgressRecord> cars = new List<CarProgressRecord>();
        public SettingsRecord settings = new SettingsRecord();

        public bool OwnsCar(string carId) => ownedCarIds.Contains(carId);
        public bool HasCompleted(string missionId) => completedMissionIds.Contains(missionId);

        public long GetMissionCooldownEnd(string key)
        {
            MissionCooldownRecord record = missionCooldowns?.Find(item => item != null && item.key == key);
            return record?.unlockAtUnixSeconds ?? 0L;
        }

        public void SetMissionCooldown(string key, long unlockAtUnixSeconds)
        {
            if (string.IsNullOrEmpty(key)) return;
            missionCooldowns ??= new List<MissionCooldownRecord>();
            MissionCooldownRecord record = missionCooldowns.Find(item => item != null && item.key == key);
            if (record == null)
            {
                record = new MissionCooldownRecord { key = key };
                missionCooldowns.Add(record);
            }
            record.unlockAtUnixSeconds = unlockAtUnixSeconds;
        }

        public void ClearMissionCooldown(string key)
        {
            if (string.IsNullOrEmpty(key) || missionCooldowns == null) return;
            missionCooldowns.RemoveAll(item => item == null || item.key == key);
        }

        public CarProgressRecord GetOrCreateCar(string carId, float defaultFuel)
        {
            CarProgressRecord result = cars.Find(item => item.carId == carId);
            if (result != null) return result;
            result = new CarProgressRecord { carId = carId, fuelLitres = defaultFuel };
            cars.Add(result);
            return result;
        }

        public int GetUpgradeLevel(string carId, string upgradeId)
        {
            CarProgressRecord car = cars.Find(item => item.carId == carId);
            UpgradeLevelRecord upgrade = car?.upgrades.Find(item => item.upgradeId == upgradeId);
            return upgrade?.level ?? 0;
        }

        public void SetUpgradeLevel(string carId, string upgradeId, int level)
        {
            CarProgressRecord car = GetOrCreateCar(carId, 0f);
            UpgradeLevelRecord upgrade = car.upgrades.Find(item => item.upgradeId == upgradeId);
            if (upgrade == null)
            {
                upgrade = new UpgradeLevelRecord { upgradeId = upgradeId };
                car.upgrades.Add(upgrade);
            }
            upgrade.level = Mathf.Max(0, level);
        }

        public void Sanitize()
        {
            ownedCarIds ??= new List<string>();
            completedMissionIds ??= new List<string>();
            missionCooldowns ??= new List<MissionCooldownRecord>();
            cars ??= new List<CarProgressRecord>();
            settings ??= new SettingsRecord();
            version = Mathf.Max(version, 3);
            money = Mathf.Max(0, money);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            missionCooldowns.RemoveAll(item => item == null || string.IsNullOrEmpty(item.key) || item.unlockAtUnixSeconds <= now);
            if (ownedCarIds.Count == 0) ownedCarIds.Add("city-hatch");
            if (string.IsNullOrEmpty(selectedCarId) || !ownedCarIds.Contains(selectedCarId)) selectedCarId = ownedCarIds[0];
            foreach (CarProgressRecord car in cars) car.Sanitize();
        }
    }

    [Serializable]
    public sealed class MissionCooldownRecord
    {
        public string key;
        public long unlockAtUnixSeconds;
    }

    [Serializable]
    public sealed class CarProgressRecord
    {
        public string carId;
        public float fuelLitres = -1f;
        public Color paintColor = Color.white;
        public List<UpgradeLevelRecord> upgrades = new List<UpgradeLevelRecord>();

        public void Sanitize() => upgrades ??= new List<UpgradeLevelRecord>();
    }

    [Serializable]
    public sealed class UpgradeLevelRecord
    {
        public string upgradeId;
        public int level;
    }

    [Serializable]
    public sealed class SettingsRecord
    {
        public int steeringMode;
        public int qualityLevel = 1;
        public float masterVolume = 1f;
    }
}
