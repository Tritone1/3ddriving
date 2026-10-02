using DrivingSim.Core;
using DrivingSim.UI;
using UnityEngine;

namespace DrivingSim.Fuel
{
    [RequireComponent(typeof(Collider))]
    public sealed class GasStation : MonoBehaviour
    {
        [SerializeField, Min(1)] private int pricePerLitre = 3;
        [SerializeField, Min(0.1f)] private float litresPerSecond = 8f;
        [SerializeField] private WalletBehaviour wallet;
        [SerializeField] private MobileInputState mobileInput;
        private FuelSystem vehicle;
        private float purchaseAccumulator;

        public bool CanRefuel => vehicle != null && wallet != null && vehicle.CurrentLitres < vehicle.CapacityLitres && wallet.Balance >= pricePerLitre;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Update()
        {
            bool requested = mobileInput != null && mobileInput.Refuel;
#if ENABLE_LEGACY_INPUT_MANAGER
            requested |= Input.GetKey(KeyCode.R);
#endif
            if (!requested || !CanRefuel)
            {
                purchaseAccumulator = 0f;
                return;
            }

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
        }

        public void Configure(WalletBehaviour source, MobileInputState input)
        {
            wallet = source;
            mobileInput = input;
        }

        private void OnTriggerEnter(Collider other)
        {
            FuelSystem found = other.GetComponentInParent<FuelSystem>();
            if (found != null) vehicle = found;
        }

        private void OnTriggerExit(Collider other)
        {
            FuelSystem found = other.GetComponentInParent<FuelSystem>();
            if (found == vehicle) vehicle = null;
        }
    }
}
