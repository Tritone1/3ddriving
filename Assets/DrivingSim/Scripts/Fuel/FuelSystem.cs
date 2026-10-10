using System;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Fuel
{
    [RequireComponent(typeof(CarController))]
    public sealed class FuelSystem : MonoBehaviour
    {
        [SerializeField] private CarController car;
        [SerializeField, Min(0f)] private float idleLitresPerHour = 0.8f;
        [Tooltip("Speeds up real-world fuel consumption so it is meaningful during short mobile sessions.")]
        [SerializeField, Range(1f, 250f)] private float gameplayConsumptionMultiplier = 100f;
        [SerializeField, Range(0.01f, 0.5f)] private float lowFuelThreshold = 0.2f;
        private float capacityBonus;
        private Vector3 previousPosition;
        private bool initialized;
        private bool wasLow;

        public event Action<float, float> FuelChanged;
        public event Action<bool> LowFuelChanged;
        public event Action FuelEmpty;

        public float CurrentLitres { get; private set; }
        public float CapacityLitres => car != null && car.Data != null ? car.Data.FuelCapacityLitres + capacityBonus : capacityBonus;
        public float NormalizedFuel => CapacityLitres <= 0f ? 0f : CurrentLitres / CapacityLitres;
        public bool IsEmpty => CurrentLitres <= 0.001f;
        public bool IsLow => NormalizedFuel <= lowFuelThreshold;

        private void Awake()
        {
            if (car == null) car = GetComponent<CarController>();
            previousPosition = transform.position;
        }

        private void Start()
        {
            if (!initialized) SetFuel(CapacityLitres);
        }

        private void Update()
        {
            if (car == null || car.Data == null || IsEmpty || !car.IsEngineRunning)
            {
                previousPosition = transform.position;
                return;
            }

            float distanceKm = Vector3.Distance(transform.position, previousPosition) / 1000f;
            previousPosition = transform.position;
            float throttleLoad = Mathf.Lerp(0.65f, 1.8f, Mathf.Abs(car.ThrottleInput));
            float speedLoad = Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(car.SpeedKph / car.Data.TopSpeedKph));
            float drivingUse = car.Data.ConsumptionLitresPer100Km * distanceKm / 100f * throttleLoad * speedLoad;
            float idleUse = idleLitresPerHour / 3600f * Time.deltaTime;
            drivingUse *= gameplayConsumptionMultiplier;
            idleUse *= gameplayConsumptionMultiplier;
            Consume(drivingUse + idleUse);
        }

        public void Initialize(float litres, float extraCapacity = 0f)
        {
            capacityBonus = Mathf.Max(0f, extraCapacity);
            initialized = true;
            SetFuel(litres < 0f ? CapacityLitres : litres);
        }

        public void SetCapacityBonus(float litres)
        {
            capacityBonus = Mathf.Max(0f, litres);
            SetFuel(CurrentLitres);
        }

        public float AddFuel(float requestedLitres)
        {
            float accepted = Mathf.Min(Mathf.Max(0f, requestedLitres), CapacityLitres - CurrentLitres);
            SetFuel(CurrentLitres + accepted);
            return accepted;
        }

        public void SetFuel(float litres)
        {
            bool wasEmpty = IsEmpty;
            CurrentLitres = Mathf.Clamp(litres, 0f, CapacityLitres);
            car?.SetEngineEnabled(!IsEmpty);
            FuelChanged?.Invoke(CurrentLitres, CapacityLitres);
            UpdateLowFuelEvent();
            if (!wasEmpty && IsEmpty) FuelEmpty?.Invoke();
        }

        private void Consume(float litres)
        {
            if (litres <= 0f) return;
            SetFuel(CurrentLitres - litres);
        }

        private void UpdateLowFuelEvent()
        {
            if (wasLow == IsLow) return;
            wasLow = IsLow;
            LowFuelChanged?.Invoke(wasLow);
        }
    }
}
