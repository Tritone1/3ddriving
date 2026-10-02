using DrivingSim.Core;
using DrivingSim.Fuel;
using DrivingSim.Missions;
using DrivingSim.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private CarController car;
        [SerializeField] private FuelSystem fuel;
        [SerializeField] private WalletBehaviour wallet;
        [SerializeField] private MissionManager missions;
        [SerializeField] private MobileInputState mobileInput;
        [SerializeField] private UnityEngine.Camera worldCamera;
        [Header("Widgets")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text gearText;
        [SerializeField] private Text fuelText;
        [SerializeField] private Image fuelFill;
        [SerializeField] private GameObject lowFuelWarning;
        [SerializeField] private Text moneyText;
        [SerializeField] private Text missionText;
        [SerializeField] private RectTransform directionArrow;

        public void Configure(CarController vehicle, FuelSystem tank, WalletBehaviour currency, MissionManager missionManager,
            MobileInputState controls, UnityEngine.Camera camera)
        {
            car = vehicle;
            fuel = tank;
            wallet = currency;
            missions = missionManager;
            mobileInput = controls;
            worldCamera = camera;
        }

        private void Update()
        {
            if (car != null)
            {
                if (speedText != null) speedText.text = $"{Mathf.RoundToInt(car.SpeedKph):000} km/h";
                if (gearText != null)
                {
                    if (mobileInput == null) gearText.text = $"D{car.CurrentGear}";
                    else
                    {
                        switch (mobileInput.SelectedGear)
                        {
                            case MobileInputState.TransmissionGear.Park: gearText.text = "P"; break;
                            case MobileInputState.TransmissionGear.Reverse: gearText.text = "R"; break;
                            case MobileInputState.TransmissionGear.Neutral: gearText.text = "N"; break;
                            default: gearText.text = $"D{car.CurrentGear}"; break;
                        }
                    }
                }
            }
            if (fuel != null)
            {
                if (fuelFill != null) fuelFill.fillAmount = fuel.NormalizedFuel;
                if (fuelText != null)
                    fuelText.text = $"FUEL  {fuel.CurrentLitres:0.00} / {fuel.CapacityLitres:0.0} L   {fuel.NormalizedFuel * 100f:0.0}%";
                if (lowFuelWarning != null) lowFuelWarning.SetActive(fuel.IsLow);
            }
            if (moneyText != null && wallet != null) moneyText.text = $"${wallet.Balance:N0}";
            UpdateMission();
        }

        private void UpdateMission()
        {
            MissionData mission = missions != null ? missions.ActiveMission : null;
            bool active = mission != null && missions.State == MissionManager.MissionState.Active;
            if (missionText != null)
            {
                missionText.gameObject.SetActive(active);
                if (active)
                {
                    string progress = mission.Type == MissionType.TimedDelivery
                        ? $"{missions.RemainingTime:0}s"
                        : mission.Type == MissionType.DistanceWithoutCrash
                            ? $"{missions.DistanceTravelled:0}/{mission.TargetDistanceMetres:0} m"
                            : "Reach the marker";
                    missionText.text = $"{mission.DisplayName}\n{progress}";
                }
            }

            Transform target = active ? missions.CurrentTarget : null;
            if (directionArrow != null) directionArrow.gameObject.SetActive(target != null);
            if (target == null || directionArrow == null || worldCamera == null) return;
            Vector3 local = worldCamera.transform.InverseTransformDirection(target.position - car.transform.position);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            directionArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
