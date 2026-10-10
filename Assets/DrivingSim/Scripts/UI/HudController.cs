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
        private GameObject refuelControl;
        private Text refuelText;
        private Image refuelBackground;
        private Button engineButton;
        private Text engineButtonText;
        private Image engineButtonBackground;
        private GasStation[] gasStations;

        public void Configure(CarController vehicle, FuelSystem tank, WalletBehaviour currency, MissionManager missionManager,
            MobileInputState controls, UnityEngine.Camera camera)
        {
            car = vehicle;
            fuel = tank;
            wallet = currency;
            missions = missionManager;
            mobileInput = controls;
            worldCamera = camera;
            Transform refuelTransform = transform.Find("Refuel");
            if (refuelTransform != null)
            {
                refuelControl = refuelTransform.gameObject;
                refuelText = refuelTransform.GetComponentInChildren<Text>(true);
                refuelBackground = refuelTransform.GetComponent<Image>();
                refuelControl.SetActive(false);
            }
            EnsureEngineButton();
            gasStations = FindObjectsByType<GasStation>(FindObjectsSortMode.None);
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
            UpdateEngineButton();
            UpdateRefuelPrompt();
            UpdateMission();
        }

        private void UpdateRefuelPrompt()
        {
            if (refuelControl == null || fuel == null) return;
            bool ready = false;
            if (gasStations != null)
            {
                foreach (GasStation station in gasStations)
                {
                    if (station == null || !station.IsVehicleInRange(fuel)) continue;
                    ready = true;
                    break;
                }
            }

            if (refuelControl.activeSelf != ready) refuelControl.SetActive(ready);
            if (!ready) return;
            if (refuelText != null) refuelText.text = "REFUEL\nTAP OR HOLD";
            if (refuelBackground != null)
                refuelBackground.color = new Color(0.08f, 0.48f, 0.28f, 0.92f);
        }

        private void EnsureEngineButton()
        {
            Transform existing = transform.Find("EngineStartStop");
            GameObject buttonObject;
            if (existing != null) buttonObject = existing.gameObject;
            else
            {
                buttonObject = new GameObject("EngineStartStop", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
            }

            RectTransform rect = buttonObject.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-330f, 430f);
            rect.sizeDelta = new Vector2(200f, 82f);

            engineButtonBackground = buttonObject.GetComponent<Image>();
            engineButton = buttonObject.GetComponent<Button>();
            engineButton.transition = Selectable.Transition.None;
            engineButton.onClick.RemoveListener(ToggleEngine);
            engineButton.onClick.AddListener(ToggleEngine);

            engineButtonText = buttonObject.GetComponentInChildren<Text>(true);
            if (engineButtonText == null)
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                RectTransform labelRect = labelObject.transform as RectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                engineButtonText = labelObject.GetComponent<Text>();
                engineButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                engineButtonText.fontSize = 20;
                engineButtonText.alignment = TextAnchor.MiddleCenter;
                engineButtonText.color = Color.white;
                engineButtonText.raycastTarget = false;
            }
            buttonObject.transform.SetAsLastSibling();
        }

        private void UpdateEngineButton()
        {
            if (engineButton == null || engineButtonText == null || car == null || mobileInput == null) return;
            bool parked = mobileInput.SelectedGear == MobileInputState.TransmissionGear.Park;
            bool stationary = car.SpeedKph <= 0.35f;
            bool canToggle = parked && stationary;
            engineButton.interactable = canToggle;

            if (car.IsEngineRunning)
                engineButtonText.text = canToggle ? "ENGINE\nSTOP" : "PARK TO\nSTOP ENGINE";
            else
                engineButtonText.text = canToggle ? "ENGINE\nSTART" : "SELECT P\nTO START";

            if (engineButtonBackground != null)
                engineButtonBackground.color = !canToggle
                    ? new Color(0.16f, 0.18f, 0.22f, 0.72f)
                    : car.IsEngineRunning
                        ? new Color(0.75f, 0.22f, 0.08f, 0.94f)
                        : new Color(0.08f, 0.5f, 0.25f, 0.94f);
        }

        private void ToggleEngine()
        {
            if (car == null || mobileInput == null) return;
            if (mobileInput.SelectedGear != MobileInputState.TransmissionGear.Park || car.SpeedKph > 0.35f) return;
            car.TrySetEngineRunning(!car.IsEngineRunning);
            UpdateEngineButton();
        }

        private void OnDestroy()
        {
            if (engineButton != null) engineButton.onClick.RemoveListener(ToggleEngine);
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
