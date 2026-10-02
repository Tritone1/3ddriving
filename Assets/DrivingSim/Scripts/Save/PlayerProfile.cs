using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrivingSim.Save
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int version = 1;
        public int money = 5000;
        public string selectedCarId = "city-hatch";
        public List<string> ownedCarIds = new List<string> { "city-hatch" };
        public List<string> completedMissionIds = new List<string>();
        public List<CarProgressRecord> cars = new List<CarProgressRecord>();
        public SettingsRecord settings = new SettingsRecord();

        public bool OwnsCar(string carId) => ownedCarIds.Contains(carId);
        public bool HasCompleted(string missionId) => completedMissionIds.Contains(missionId);

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
            cars ??= new List<CarProgressRecord>();
            settings ??= new SettingsRecord();
            money = Mathf.Max(0, money);
            if (ownedCarIds.Count == 0) ownedCarIds.Add("city-hatch");
            if (string.IsNullOrEmpty(selectedCarId) || !ownedCarIds.Contains(selectedCarId)) selectedCarId = ownedCarIds[0];
            foreach (CarProgressRecord car in cars) car.Sanitize();
        }
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
