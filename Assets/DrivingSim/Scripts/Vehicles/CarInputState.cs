using UnityEngine;

namespace DrivingSim.Vehicles
{
    public readonly struct CarInputState
    {
        public CarInputState(float steering, float throttle, float brake, bool handbrake, bool park = false)
        {
            Steering = Mathf.Clamp(steering, -1f, 1f);
            Throttle = Mathf.Clamp(throttle, -1f, 1f);
            Brake = Mathf.Clamp01(brake);
            Handbrake = handbrake;
            Park = park;
        }

        public float Steering { get; }
        public float Throttle { get; }
        public float Brake { get; }
        public bool Handbrake { get; }
        public bool Park { get; }
    }

    public interface ICarInputSource
    {
        CarInputState ReadInput();
    }
}
