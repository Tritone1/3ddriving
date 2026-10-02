using DrivingSim.Core;
using DrivingSim.Fuel;
using DrivingSim.Save;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Garage
{
    public static class VehicleUpgradeApplicator
    {
        public static CarStatSnapshot Calculate(CarData car, GameDatabase database, PlayerProfile profile, UpgradeData previewUpgrade = null)
        {
            float power = 1f;
            float grip = 1f;
            float brakes = 1f;
            float fuelBonus = 0f;
            if (car == null || database == null || profile == null) return new CarStatSnapshot(power, grip, brakes, fuelBonus);

            foreach (UpgradeData upgrade in database.Upgrades)
            {
                if (upgrade == null) continue;
                int level = profile.GetUpgradeLevel(car.Id, upgrade.Id);
                if (upgrade == previewUpgrade && level < upgrade.MaxLevel) level++;
                switch (upgrade.Type)
                {
                    case UpgradeType.Engine:
                    case UpgradeType.Turbo:
                        power += upgrade.ValuePerLevel * level;
                        break;
                    case UpgradeType.Tires:
                        grip += upgrade.ValuePerLevel * level;
                        break;
                    case UpgradeType.Brakes:
                        brakes += upgrade.ValuePerLevel * level;
                        break;
                    case UpgradeType.FuelTank:
                        fuelBonus += upgrade.ValuePerLevel * level;
                        break;
                }
            }
            return new CarStatSnapshot(power, grip, brakes, car.FuelCapacityLitres + fuelBonus);
        }

        public static void Apply(CarController controller, FuelSystem fuel, GameDatabase database, PlayerProfile profile)
        {
            if (controller == null || controller.Data == null) return;
            CarStatSnapshot stats = Calculate(controller.Data, database, profile);
            controller.ApplyRuntimeModifiers(stats.Power, stats.Grip, stats.Brakes);
            fuel?.SetCapacityBonus(Mathf.Max(0f, stats.FuelCapacity - controller.Data.FuelCapacityLitres));
        }
    }
}
