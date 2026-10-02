using DrivingSim.Garage;
using UnityEngine;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    public sealed class GarageView : MonoBehaviour
    {
        [SerializeField] private GarageController garage;
        [SerializeField] private Text carName;
        [SerializeField] private Text ownership;
        [SerializeField] private Text stats;
        [SerializeField] private Text message;

        private void OnEnable()
        {
            if (garage != null)
            {
                garage.SelectionChanged += Refresh;
                garage.TransactionFailed += ShowMessage;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (garage != null)
            {
                garage.SelectionChanged -= Refresh;
                garage.TransactionFailed -= ShowMessage;
            }
        }

        public void Next() => garage?.NextCar();
        public void Previous() => garage?.PreviousCar();
        public void BuyOrSelect()
        {
            if (garage == null) return;
            if (garage.IsOwned) garage.SelectCurrentCar(); else garage.BuyCurrentCar();
            Refresh();
        }
        public void BuyUpgrade(UpgradeData upgrade)
        {
            garage?.BuyUpgrade(upgrade);
            Refresh(upgrade);
        }
        public void SetPaintRed() => garage?.SetPaint(new Color(0.75f, 0.05f, 0.04f));
        public void SetPaintBlue() => garage?.SetPaint(new Color(0.04f, 0.18f, 0.75f));
        public void SetPaintWhite() => garage?.SetPaint(Color.white);

        private void Refresh() => Refresh(null);
        private void Refresh(UpgradeData comparison)
        {
            if (garage == null || garage.CurrentCar == null) return;
            if (carName != null) carName.text = garage.CurrentCar.DisplayName;
            if (ownership != null) ownership.text = garage.IsSelected ? "SELECTED" : garage.IsOwned ? "OWNED" : $"${garage.CurrentCar.Price:N0}";
            CarStatSnapshot now = garage.GetCurrentStats();
            CarStatSnapshot after = comparison == null ? now : garage.GetStatsAfter(comparison);
            if (stats != null) stats.text = $"Power {now.Power:0.00} → {after.Power:0.00}\nGrip {now.Grip:0.00} → {after.Grip:0.00}\nBrakes {now.Brakes:0.00} → {after.Brakes:0.00}\nTank {now.FuelCapacity:0} → {after.FuelCapacity:0} L";
        }

        private void ShowMessage(string value)
        {
            if (message != null) message.text = value;
        }
    }
}
