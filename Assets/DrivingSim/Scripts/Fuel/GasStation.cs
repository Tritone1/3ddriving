using System.Collections.Generic;
using DrivingSim.Core;
using DrivingSim.Save;
using DrivingSim.UI;
using DrivingSim.Vehicles;
using UnityEngine;

namespace DrivingSim.Fuel
{
    [RequireComponent(typeof(Collider))]
    public sealed class GasStation : MonoBehaviour
    {
        [SerializeField, Min(1)] private int pricePerLitre = 3;
        [SerializeField, Min(0.1f)] private float litresPerSecond = 8f;
        [SerializeField, Min(0f)] private float maximumParkingSpeedKph = 0.35f;
        [SerializeField, Min(0f)] private float parkingMargin = 0.12f;
        [SerializeField] private WalletBehaviour wallet;
        [SerializeField] private MobileInputState mobileInput;
        [SerializeField] private SaveService saveService;
        private readonly HashSet<Collider> vehicleColliders = new HashSet<Collider>();
        private FuelSystem configuredVehicle;
        private FuelSystem vehicle;
        private float purchaseAccumulator;
        private bool wasRefuelRequested;
        private uint lastRefuelRequestVersion;

        public bool ReadyToRefuel => IsVehicleParkedForRefuel(vehicle);
        public bool CanRefuel => ReadyToRefuel && wallet != null && vehicle.CurrentLitres < vehicle.CapacityLitres && wallet.Balance >= pricePerLitre;
        public bool HasVehicle => vehicle != null && vehicleColliders.Count > 0;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Update()
        {
            uint requestVersion = mobileInput != null ? mobileInput.RefuelRequestVersion : 0u;
            bool tapped = requestVersion != lastRefuelRequestVersion;
            lastRefuelRequestVersion = requestVersion;
            bool requested = mobileInput != null && (mobileInput.Refuel || tapped);
#if ENABLE_LEGACY_INPUT_MANAGER
            requested |= Input.GetKey(KeyCode.R) || Input.GetKeyDown(KeyCode.R);
#endif
            if (!requested || !CanRefuel)
            {
                purchaseAccumulator = 0f;
                wasRefuelRequested = false;
                return;
            }

            // A tap must have an immediate result; holding continues at the
            // configured litres-per-second rate.
            if (!wasRefuelRequested) purchaseAccumulator = Mathf.Max(1f, purchaseAccumulator);
            wasRefuelRequested = true;
            purchaseAccumulator += litresPerSecond * Time.deltaTime;
            int wholeLitres = Mathf.FloorToInt(purchaseAccumulator);
            if (wholeLitres <= 0) return;
            int affordable = Mathf.Min(wholeLitres, wallet.Balance / pricePerLitre);
            float room = vehicle.CapacityLitres - vehicle.CurrentLitres;
            int toBuy = Mathf.Min(affordable, Mathf.CeilToInt(room));
            if (toBuy <= 0 || !wallet.TrySpend(toBuy * pricePerLitre)) return;
            float accepted = vehicle.AddFuel(toBuy);
            purchaseAccumulator -= accepted;
            int refundLitres = Mathf.Max(0, toBuy - Mathf.CeilToInt(accepted));
            if (refundLitres > 0) wallet.Add(refundLitres * pricePerLitre);
            saveService?.Save();
        }

        public void Configure(WalletBehaviour source, MobileInputState input, SaveService saves = null,
            FuelSystem playerFuel = null)
        {
            wallet = source;
            mobileInput = input;
            saveService = saves;
            configuredVehicle = playerFuel;
            lastRefuelRequestVersion = input != null ? input.RefuelRequestVersion : 0u;
            if (vehicle != null && configuredVehicle != null && vehicle != configuredVehicle)
            {
                vehicleColliders.Clear();
                vehicle = null;
            }
        }

        public bool IsVehicleInRange(FuelSystem fuel)
        {
            vehicleColliders.RemoveWhere(item => item == null);
            if (vehicleColliders.Count == 0) vehicle = null;
            return fuel != null && vehicle == fuel && IsVehicleParkedForRefuel(fuel);
        }

        private bool IsVehicleParkedForRefuel(FuelSystem fuel)
        {
            if (fuel == null || vehicle != fuel || vehicleColliders.Count == 0) return false;
            CarController car = fuel.GetComponent<CarController>();
            if (car == null || car.SpeedKph > maximumParkingSpeedKph || car.IsEngineRunning) return false;
            if (mobileInput == null || mobileInput.SelectedGear != MobileInputState.TransmissionGear.Park) return false;
            return IsEntireVehicleInsideZone(fuel);
        }

        private bool IsEntireVehicleInsideZone(FuelSystem fuel)
        {
            BoxCollider zone = GetComponent<BoxCollider>();
            if (zone == null) return false;

            bool foundBodyCollider = false;
            Vector3 half = zone.size * 0.5f;
            float minimumX = zone.center.x - half.x + parkingMargin;
            float maximumX = zone.center.x + half.x - parkingMargin;
            float minimumZ = zone.center.z - half.z + parkingMargin;
            float maximumZ = zone.center.z + half.z - parkingMargin;

            foreach (Collider bodyCollider in fuel.GetComponentsInChildren<Collider>())
            {
                if (bodyCollider == null || !bodyCollider.enabled || bodyCollider.isTrigger || bodyCollider is WheelCollider)
                    continue;

                foundBodyCollider = true;
                Bounds bounds = bodyCollider.bounds;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center + new Vector3(bounds.extents.x * x, 0f, bounds.extents.z * z);
                        Vector3 localCorner = transform.InverseTransformPoint(corner);
                        if (localCorner.x < minimumX || localCorner.x > maximumX ||
                            localCorner.z < minimumZ || localCorner.z > maximumZ)
                            return false;
                    }
                }
            }

            return foundBodyCollider;
        }

        private void OnTriggerEnter(Collider other) => TrackVehicleCollider(other);

        private void OnTriggerStay(Collider other) => TrackVehicleCollider(other);

        private void TrackVehicleCollider(Collider other)
        {
            FuelSystem found = other.GetComponentInParent<FuelSystem>();
            if (found == null) return;
            if (configuredVehicle != null && found != configuredVehicle) return;
            if (vehicle != null && vehicle != found) vehicleColliders.Clear();
            vehicle = found;
            vehicleColliders.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            vehicleColliders.Remove(other);
            vehicleColliders.RemoveWhere(item => item == null);
            if (vehicleColliders.Count == 0) vehicle = null;
        }

        private void OnDisable()
        {
            vehicleColliders.Clear();
            configuredVehicle = null;
            vehicle = null;
            purchaseAccumulator = 0f;
            wasRefuelRequested = false;
        }
    }
}
