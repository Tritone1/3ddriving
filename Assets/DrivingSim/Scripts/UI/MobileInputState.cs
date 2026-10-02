using UnityEngine;

namespace DrivingSim.UI
{
    public sealed class MobileInputState : MonoBehaviour
    {
        public enum SteeringMode { Wheel, Buttons, Tilt }
        public enum TransmissionGear { Park, Reverse, Neutral, Drive }

        [SerializeField] private SteeringMode mode = SteeringMode.Buttons;
        [SerializeField] private TransmissionGear selectedGear = TransmissionGear.Drive;
        [SerializeField, Range(0.1f, 4f)] private float tiltSensitivity = 1.5f;
        private float wheelSteering;
        private float buttonSteering;
        private bool acceleratorPressed;
        private bool handbrakePressed;

        public float Steering
        {
            get
            {
                if (mode == SteeringMode.Tilt)
                    return Mathf.Clamp(Input.acceleration.x * tiltSensitivity, -1f, 1f);
                if (mode == SteeringMode.Buttons) return buttonSteering;
                // The generated prototype exposes arrow buttons alongside the wheel mode.
                // Let those buttons act as a fallback until a wheel gesture is being used.
                return Mathf.Abs(buttonSteering) > 0.001f ? buttonSteering : wheelSteering;
            }
        }

        public float Throttle => acceleratorPressed ? GearDirection : 0f;
        public float Brake { get; private set; }
        public bool Handbrake => handbrakePressed || selectedGear == TransmissionGear.Park;
        public bool Refuel { get; private set; }
        public SteeringMode Mode => mode;
        public TransmissionGear SelectedGear => selectedGear;
        public float GearDirection => selectedGear == TransmissionGear.Drive ? 1f
            : selectedGear == TransmissionGear.Reverse ? -1f : 0f;

        public void SetMode(int value) => mode = (SteeringMode)Mathf.Clamp(value, 0, 2);
        public void SetWheelSteering(float value) => wheelSteering = Mathf.Clamp(value, -1f, 1f);
        public void SetLeft(bool pressed) => buttonSteering = pressed ? -1f : (buttonSteering < 0f ? 0f : buttonSteering);
        public void SetRight(bool pressed) => buttonSteering = pressed ? 1f : (buttonSteering > 0f ? 0f : buttonSteering);
        public void SetThrottle(bool pressed) => acceleratorPressed = pressed;

        // Compatibility for older generated scenes. The new UI uses SetGear.
        public void SetReverse(bool pressed)
        {
            selectedGear = TransmissionGear.Reverse;
            acceleratorPressed = pressed;
        }

        public void SetGear(int value)
        {
            selectedGear = (TransmissionGear)Mathf.Clamp(value, 0, 3);
            acceleratorPressed = false;
        }

        public void SetBrake(bool pressed) => Brake = pressed ? 1f : 0f;
        public void SetHandbrake(bool pressed) => handbrakePressed = pressed;
        public void SetRefuel(bool pressed) => Refuel = pressed;
    }
}
