using DrivingSim.CameraSystem;
using DrivingSim.Economy;
using DrivingSim.Fuel;
using DrivingSim.Garage;
using DrivingSim.Missions;
using DrivingSim.Save;
using DrivingSim.UI;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Core
{
    [DefaultExecutionOrder(-500)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;
        [SerializeField] private SaveService saveService;
        [SerializeField] private EconomyService economy;
        [SerializeField] private MissionManager missionManager;
        [SerializeField] private MobileInputState mobileInput;
        [SerializeField] private DrivingCameraController drivingCamera;
        [SerializeField] private HudController hud;
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;

        private CarController playerCar;
        private FuelSystem fuel;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            if (database == null || saveService == null)
            {
                Debug.LogError("GameBootstrap requires a GameDatabase and SaveService.");
                return;
            }

            economy?.Configure(saveService);
            CarData selected = database.FindCar(saveService.Profile.selectedCarId);
            if (selected == null && database.Cars.Count > 0) selected = database.Cars[0];
            if (selected == null || selected.Prefab == null)
            {
                Debug.LogError("Selected car has no prefab.");
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.up;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            GameObject instance = Instantiate(selected.Prefab, position, rotation);
            playerCar = instance.GetComponent<CarController>();
            PlayerCarInput input = instance.GetComponent<PlayerCarInput>();
            input?.Configure(mobileInput);
            playerCar?.Configure(selected, input);
            instance.GetComponent<EngineAudio>()?.Configure(playerCar);

            fuel = instance.GetComponent<FuelSystem>();
            CarProgressRecord progress = saveService.Profile.GetOrCreateCar(selected.Id, selected.FuelCapacityLitres);
            VehicleUpgradeApplicator.Apply(playerCar, fuel, database, saveService.Profile);
            fuel?.Initialize(progress.fuelLitres, Mathf.Max(0f, VehicleUpgradeApplicator.Calculate(selected, database, saveService.Profile).FuelCapacity - selected.FuelCapacityLitres));
            if (fuel != null) fuel.FuelChanged += HandleFuelChanged;

            CarRigReferences rig = instance.GetComponent<CarRigReferences>();
            rig?.ApplyPaint(progress.paintColor);
            drivingCamera?.SetTarget(instance.transform, rig != null ? rig.ChaseCameraAnchor : null, rig != null ? rig.HoodCameraAnchor : null);
            missionManager?.Configure(database.Missions, playerCar, economy, saveService);
            hud?.Configure(playerCar, fuel, economy, missionManager, mobileInput, UnityEngine.Camera.main);
            foreach (GasStation station in FindObjectsByType<GasStation>(FindObjectsSortMode.None))
                station.Configure(economy, mobileInput, saveService, fuel);
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.C)) drivingCamera?.ToggleView();
#endif
        }

        private void HandleFuelChanged(float current, float capacity)
        {
            if (playerCar?.Data == null || saveService?.Profile == null) return;
            saveService.Profile.GetOrCreateCar(playerCar.Data.Id, capacity).fuelLitres = current;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) saveService?.Save();
        }

        private void OnApplicationQuit() => saveService?.Save();
    }
}
