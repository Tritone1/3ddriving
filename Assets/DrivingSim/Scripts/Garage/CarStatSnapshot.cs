using System;

namespace DrivingSim.Garage
{
    [Serializable]
    public readonly struct CarStatSnapshot
    {
        public CarStatSnapshot(float power, float grip, float brakes, float fuelCapacity)
        {
            Power = power;
            Grip = grip;
            Brakes = brakes;
            FuelCapacity = fuelCapacity;
        }

        public float Power { get; }
        public float Grip { get; }
        public float Brakes { get; }
        public float FuelCapacity { get; }
    }
}
