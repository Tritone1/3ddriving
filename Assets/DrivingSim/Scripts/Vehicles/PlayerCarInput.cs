using DrivingSim.UI;
using UnityEngine;

namespace DrivingSim.Vehicles
{
    public sealed class PlayerCarInput : MonoBehaviour, ICarInputSource
    {
        [SerializeField] private MobileInputState mobileInput;
        [SerializeField] private bool allowKeyboardFallback = true;

        public CarInputState ReadInput()
        {
            float steering = mobileInput != null ? mobileInput.Steering : 0f;
            float throttle = mobileInput != null ? mobileInput.Throttle : 0f;
            float brake = mobileInput != null ? mobileInput.Brake : 0f;
            bool handbrake = mobileInput != null && mobileInput.Handbrake;
            bool park = mobileInput != null && mobileInput.SelectedGear == MobileInputState.TransmissionGear.Park;

#if ENABLE_LEGACY_INPUT_MANAGER
            // Device Simulator can emulate a mobile device while still running in the Editor.
            if (allowKeyboardFallback && (Application.isEditor || !Application.isMobilePlatform))
            {
                float keyboardSteering = Input.GetAxisRaw("Horizontal");
                float keyboardThrottle = Input.GetAxisRaw("Vertical");

                // Some Device Simulator versions do not forward the legacy axes reliably.
                // Read the physical keys too, and only override active touch input when a
                // keyboard direction is actually being pressed.
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboardSteering = -1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboardSteering = 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyboardThrottle = 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyboardThrottle = -1f;

                if (Mathf.Abs(keyboardSteering) > 0.01f) steering = keyboardSteering;
                if (Mathf.Abs(keyboardThrottle) > 0.01f) throttle = keyboardThrottle;
                brake = Input.GetKey(KeyCode.LeftShift) ? 1f : brake;
                handbrake |= Input.GetKey(KeyCode.Space);
            }
#endif
            return new CarInputState(steering, throttle, brake, handbrake, park);
        }

        public void Configure(MobileInputState input) => mobileInput = input;
    }
}
