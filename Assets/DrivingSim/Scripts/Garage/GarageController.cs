using System;
using DrivingSim.Core;
using DrivingSim.Economy;
using DrivingSim.Save;
using DrivingSim.UI;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Garage
{
    public sealed class GarageController : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;
        [SerializeField] private SaveService saveService;
        [SerializeField] private EconomyService economy;
        [SerializeField] private Transform previewRoot;
        private GameObject previewInstance;
        private int index;

        public event Action SelectionChanged;
        public event Action<string> TransactionFailed;
        public event Action TransactionSucceeded;

        public CarData CurrentCar => database != null && database.Cars.Count > 0 ? database.Cars[index] : null;
        public bool IsOwned => CurrentCar != null && saveService.Profile.OwnsCar(CurrentCar.Id);
        public bool IsSelected => CurrentCar != null && saveService.Profile.selectedCarId == CurrentCar.Id;

        private void Start()
        {
            if (database == null || saveService == null || database.Cars.Count == 0) return;
            int selected = -1;
            for (int i = 0; i < database.Cars.Count; i++)
                if (database.Cars[i] != null && database.Cars[i].Id == saveService.Profile.selectedCarId) selected = i;
            index = Mathf.Max(0, selected);
            RefreshPreview();
            if (FindFirstObjectByType<GarageView>() == null)
                GarageView.CreateRuntime(this, economy, database.Upgrades);
        }

        public void Configure(GameDatabase data, SaveService saves, EconomyService currency, Transform root)
        {
            database = data;
            saveService = saves;
            economy = currency;
            previewRoot = root;
        }

        public void NextCar() => ChangeCar(1);
        public void PreviousCar() => ChangeCar(-1);

        public void ChangeCar(int direction)
        {
            if (database == null || database.Cars.Count == 0) return;
            index = (index + direction + database.Cars.Count) % database.Cars.Count;
            RefreshPreview();
            SelectionChanged?.Invoke();
        }

        public bool BuyCurrentCar()
        {
            CarData car = CurrentCar;
            if (car == null || saveService == null || economy == null) return false;
            if (IsOwned) return SelectCurrentCar();
            if (!string.IsNullOrEmpty(car.UnlockMissionId))
            {
                TransactionFailed?.Invoke("Complete the required mission to unlock this car.");
                return false;
            }
            if (!economy.TrySpend(car.Price))
            {
                TransactionFailed?.Invoke("Not enough money.");
                return false;
            }
            saveService.Profile.ownedCarIds.Add(car.Id);
            saveService.Profile.GetOrCreateCar(car.Id, car.FuelCapacityLitres);
            saveService.Profile.selectedCarId = car.Id;
            saveService.Save();
            TransactionSucceeded?.Invoke();
            SelectionChanged?.Invoke();
            return true;
        }

        public bool SelectCurrentCar()
        {
            if (CurrentCar == null || !IsOwned) return false;
            saveService.Profile.selectedCarId = CurrentCar.Id;
            saveService.Save();
            TransactionSucceeded?.Invoke();
            SelectionChanged?.Invoke();
            return true;
        }

        public bool BuyUpgrade(UpgradeData upgrade)
        {
            if (CurrentCar == null || !IsOwned || upgrade == null) return false;
            int level = saveService.Profile.GetUpgradeLevel(CurrentCar.Id, upgrade.Id);
            if (level >= upgrade.MaxLevel)
            {
                TransactionFailed?.Invoke("Upgrade is already at maximum level.");
                return false;
            }
            int price = upgrade.PriceForNextLevel(level);
            if (!economy.TrySpend(price))
            {
                TransactionFailed?.Invoke("Not enough money.");
                return false;
            }
            saveService.Profile.SetUpgradeLevel(CurrentCar.Id, upgrade.Id, level + 1);
            saveService.Profile.selectedCarId = CurrentCar.Id;
            saveService.Save();
            TransactionSucceeded?.Invoke();
            SelectionChanged?.Invoke();
            return true;
        }

        public void SetPaint(Color color)
        {
            if (CurrentCar == null || !IsOwned) return;
            saveService.Profile.selectedCarId = CurrentCar.Id;
            saveService.Profile.GetOrCreateCar(CurrentCar.Id, CurrentCar.FuelCapacityLitres).paintColor = color;
            ApplyPreviewPaint(color);
            saveService.Save();
            SelectionChanged?.Invoke();
        }

        public CarStatSnapshot GetCurrentStats() => VehicleUpgradeApplicator.Calculate(CurrentCar, database, saveService.Profile);
        public CarStatSnapshot GetStatsAfter(UpgradeData upgrade) => VehicleUpgradeApplicator.Calculate(CurrentCar, database, saveService.Profile, upgrade);

        private void RefreshPreview()
        {
            if (previewInstance != null) Destroy(previewInstance);
            CarData car = CurrentCar;
            if (car == null || car.Prefab == null || previewRoot == null) return;
            previewInstance = Instantiate(car.Prefab, previewRoot);
            previewInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (MonoBehaviour behaviour in previewInstance.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
            foreach (Rigidbody rigidbody in previewInstance.GetComponentsInChildren<Rigidbody>()) rigidbody.isKinematic = true;
            CarProgressRecord record = saveService.Profile.GetOrCreateCar(car.Id, car.FuelCapacityLitres);
            ApplyPreviewPaint(record.paintColor);
        }

        private void ApplyPreviewPaint(Color color)
        {
            if (previewInstance == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            foreach (Renderer renderer in previewInstance.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(block);
        }
    }
}
