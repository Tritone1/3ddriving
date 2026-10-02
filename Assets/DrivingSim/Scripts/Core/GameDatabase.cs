using System.Collections.Generic;
using DrivingSim.Garage;
using DrivingSim.Missions;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Core
{
    [CreateAssetMenu(menuName = "Driving Sim/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<CarData> cars = new List<CarData>();
        [SerializeField] private List<MissionData> missions = new List<MissionData>();
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();

        public IReadOnlyList<CarData> Cars => cars;
        public IReadOnlyList<MissionData> Missions => missions;
        public IReadOnlyList<UpgradeData> Upgrades => upgrades;
        public CarData FindCar(string id) => cars.Find(item => item != null && item.Id == id);
        public MissionData FindMission(string id) => missions.Find(item => item != null && item.Id == id);
        public UpgradeData FindUpgrade(string id) => upgrades.Find(item => item != null && item.Id == id);
    }
}
