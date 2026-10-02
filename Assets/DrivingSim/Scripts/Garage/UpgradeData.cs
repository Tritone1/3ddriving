using UnityEngine;

namespace DrivingSim.Garage
{
    public enum UpgradeType
    {
        Engine,
        Tires,
        Brakes,
        Turbo,
        FuelTank
    }

    [CreateAssetMenu(menuName = "Driving Sim/Upgrade", fileName = "Upgrade_")]
    public sealed class UpgradeData : ScriptableObject
    {
        [SerializeField] private string id = "upgrade";
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private UpgradeType type;
        [SerializeField, Min(1)] private int maxLevel = 3;
        [SerializeField, Min(0)] private int basePrice = 1000;
        [SerializeField, Min(1f)] private float priceMultiplier = 1.7f;
        [Tooltip("Multiplier gain per level, except Fuel Tank where this is litres per level.")]
        [SerializeField, Min(0f)] private float valuePerLevel = 0.08f;

        public string Id => id;
        public string DisplayName => displayName;
        public UpgradeType Type => type;
        public int MaxLevel => maxLevel;
        public float ValuePerLevel => valuePerLevel;
        public int PriceForNextLevel(int currentLevel) => currentLevel >= maxLevel ? 0 : Mathf.RoundToInt(basePrice * Mathf.Pow(priceMultiplier, currentLevel));

        private void OnValidate() => id = string.IsNullOrWhiteSpace(id) ? name.ToLowerInvariant().Replace(' ', '-') : id.Trim();
    }
}
