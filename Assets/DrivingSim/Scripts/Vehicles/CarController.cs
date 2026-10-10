using System;
using UnityEngine;

namespace DrivingSim.Vehicles
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarController : MonoBehaviour
    {
        [Serializable]
        public struct Axle
        {
            public WheelCollider leftCollider;
            public WheelCollider rightCollider;
            public Transform leftVisual;
            public Transform rightVisual;
            [HideInInspector] public Vector3 leftVisualPositionOffset;
            [HideInInspector] public Vector3 rightVisualPositionOffset;
            [HideInInspector] public Quaternion leftVisualRotationOffset;
            [HideInInspector] public Quaternion rightVisualRotationOffset;
            public bool steering;
            public bool powered;
            public bool handbrake;
        }

        [SerializeField] private CarData carData;
        [SerializeField] private MonoBehaviour inputSourceComponent;
        [SerializeField] private Axle[] axles = new Axle[2];
        [SerializeField, Min(100f)] private float antiRollForce = 6500f;
        [SerializeField, Min(0.1f)] private float shiftDuration = 0.22f;
        [SerializeField, Range(0.1f, 0.95f)] private float upshiftRpmRatio = 0.88f;
        [SerializeField, Range(0.05f, 0.8f)] private float downshiftRpmRatio = 0.38f;

        private Rigidbody body;
        private ICarInputSource inputSource;
        private CarInputState input;
        private int currentGear;
        private float shiftTimer;
        private float powerMultiplier = 1f;
        private float gripMultiplier = 1f;
        private float brakeMultiplier = 1f;
        private bool fuelAvailable = true;
        private bool engineRunning = true;

        public event Action<float> SpeedChanged;
        public event Action CollisionOccurred;
        public CarData Data => carData;
        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float EngineRpm { get; private set; }
        public int CurrentGear => currentGear + 1;
        public float ThrottleInput => input.Throttle;
        public bool IsEngineRunning => engineRunning && fuelAvailable;
        public bool HasCollidedRecently { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            inputSource = inputSourceComponent as ICarInputSource;
            ApplyData();
        }

        private void Update()
        {
            if (inputSource == null && inputSourceComponent != null)
                inputSource = inputSourceComponent as ICarInputSource;
            input = inputSource?.ReadInput() ?? default;
            SpeedChanged?.Invoke(SpeedKph);
        }

        private void FixedUpdate()
        {
            if (carData == null || axles == null || axles.Length == 0) return;

            shiftTimer = Mathf.Max(0f, shiftTimer - Time.fixedDeltaTime);
            UpdateEngineRpmAndGear();

            float speedRatio = Mathf.Clamp01(SpeedKph / Mathf.Max(1f, carData.TopSpeedKph));
            float steerAngle = input.Steering * carData.MaxSteerAngle * Mathf.Lerp(1f, 1f, speedRatio);
            float forwardSpeed = Vector3.Dot(body.linearVelocity, transform.forward) * 3.6f;
            bool reversing = input.Throttle < -0.05f && forwardSpeed < 4f;
            bool directionalBrake = input.Throttle < -0.05f && forwardSpeed > 4f;
            float throttle = IsEngineRunning && !directionalBrake ? (reversing ? input.Throttle * 0.55f : Mathf.Max(0f, input.Throttle)) : 0f;
            float torque = CalculateMotorTorque(throttle, speedRatio);
            float brake = Mathf.Max(input.Brake, directionalBrake ? -input.Throttle : 0f) * carData.MaxBrakeTorque * brakeMultiplier;

            int poweredWheels = CountPoweredWheels();
            foreach (Axle axle in axles)
            {
                if (axle.steering)
                {
                    axle.leftCollider.steerAngle = steerAngle;
                    axle.rightCollider.steerAngle = steerAngle;
                }
                if (axle.powered)
                {
                    axle.leftCollider.motorTorque = torque / poweredWheels;
                    axle.rightCollider.motorTorque = torque / poweredWheels;
                }

                float axleBrake = input.Park ? carData.MaxBrakeTorque * 1.5f : brake;
                if (axle.handbrake && input.Handbrake) axleBrake = Mathf.Max(axleBrake, carData.MaxBrakeTorque * 1.5f);
                axle.leftCollider.brakeTorque = axleBrake;
                axle.rightCollider.brakeTorque = axleBrake;
                ApplyAntiRoll(axle);
            }

            body.AddForce(-transform.up * carData.Downforce * body.linearVelocity.magnitude, ForceMode.Force);
        }

        private void LateUpdate()
        {
            if (axles == null) return;
            foreach (Axle axle in axles)
            {
                SyncWheel(axle.leftCollider, axle.leftVisual,
                    axle.leftVisualPositionOffset, axle.leftVisualRotationOffset);
                SyncWheel(axle.rightCollider, axle.rightVisual,
                    axle.rightVisualPositionOffset, axle.rightVisualRotationOffset);
            }
        }

        public void Configure(CarData data, MonoBehaviour source)
        {
            carData = data;
            inputSourceComponent = source;
            inputSource = source as ICarInputSource;
            ApplyData();
        }

        public void SetEngineEnabled(bool enabled)
        {
            fuelAvailable = enabled;
            if (!fuelAvailable) engineRunning = false;
            if (!IsEngineRunning) CutMotorTorque();
        }

        public bool TrySetEngineRunning(bool running)
        {
            if (running && !fuelAvailable) return false;
            engineRunning = running;
            if (!IsEngineRunning) CutMotorTorque();
            return true;
        }

        private void CutMotorTorque()
        {
            if (axles == null) return;
            foreach (Axle axle in axles)
            {
                if (axle.leftCollider != null) axle.leftCollider.motorTorque = 0f;
                if (axle.rightCollider != null) axle.rightCollider.motorTorque = 0f;
            }
        }

        public void ApplyRuntimeModifiers(float power, float grip, float brakes)
        {
            powerMultiplier = Mathf.Max(0.1f, power);
            gripMultiplier = Mathf.Max(0.1f, grip);
            brakeMultiplier = Mathf.Max(0.1f, brakes);
            ConfigureWheelFriction();
        }

        private void ApplyData()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (carData == null) return;
            body.mass = carData.Mass;
            body.centerOfMass = carData.CenterOfMass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            ConfigureWheelFriction();
            EngineRpm = carData.IdleRpm;
        }

        private void ConfigureWheelFriction()
        {
            if (carData == null || axles == null) return;
            foreach (Axle axle in axles)
            {
                ConfigureWheel(axle.leftCollider);
                ConfigureWheel(axle.rightCollider);
            }
        }

        private void ConfigureWheel(WheelCollider wheel)
        {
            if (wheel == null) return;
            WheelFrictionCurve forward = wheel.forwardFriction;
            forward.stiffness = carData.TireGrip * gripMultiplier;
            wheel.forwardFriction = forward;
            WheelFrictionCurve sideways = wheel.sidewaysFriction;
            sideways.stiffness = carData.TireGrip * gripMultiplier;
            wheel.sidewaysFriction = sideways;
        }

        private float CalculateMotorTorque(float throttle, float speedRatio)
        {
            if (Mathf.Abs(throttle) < 0.01f || shiftTimer > 0f || SpeedKph >= carData.TopSpeedKph) return 0f;
            float normalizedRpm = Mathf.InverseLerp(carData.IdleRpm, carData.MaxRpm, EngineRpm);
            float gearRatio = carData.GearRatios[Mathf.Clamp(currentGear, 0, carData.GearRatios.Length - 1)];
            float sign = Mathf.Sign(throttle);
            return sign * carData.MaxMotorTorque * powerMultiplier * carData.TorqueCurve.Evaluate(normalizedRpm) * gearRatio / carData.GearRatios[0] * Mathf.Lerp(1f, 0.2f, speedRatio);
        }

        private void UpdateEngineRpmAndGear()
        {
            if (!IsEngineRunning)
            {
                EngineRpm = Mathf.MoveTowards(EngineRpm, 0f, Time.fixedDeltaTime * 4500f);
                return;
            }
            if (carData.GearRatios == null || carData.GearRatios.Length == 0) return;
            float wheelRpm = 0f;
            int count = 0;
            foreach (Axle axle in axles)
            {
                if (!axle.powered) continue;
                wheelRpm += Mathf.Abs(axle.leftCollider.rpm) + Mathf.Abs(axle.rightCollider.rpm);
                count += 2;
            }
            wheelRpm /= Mathf.Max(1, count);
            float ratio = carData.GearRatios[currentGear] * carData.FinalDrive;
            float targetRpm = Mathf.Max(carData.IdleRpm, wheelRpm * ratio);
            EngineRpm = Mathf.Lerp(EngineRpm, Mathf.Min(targetRpm, carData.MaxRpm), Time.fixedDeltaTime * 8f);

            if (shiftTimer > 0f) return;
            float rpmRatio = EngineRpm / carData.MaxRpm;
            if (rpmRatio > upshiftRpmRatio && currentGear < carData.GearRatios.Length - 1) ShiftTo(currentGear + 1);
            else if (rpmRatio < downshiftRpmRatio && currentGear > 0) ShiftTo(currentGear - 1);
        }

        private void ShiftTo(int gear)
        {
            currentGear = Mathf.Clamp(gear, 0, carData.GearRatios.Length - 1);
            shiftTimer = shiftDuration;
        }

        private int CountPoweredWheels()
        {
            int count = 0;
            foreach (Axle axle in axles) if (axle.powered) count += 2;
            return Mathf.Max(1, count);
        }

        private void ApplyAntiRoll(Axle axle)
        {
            if (axle.leftCollider == null || axle.rightCollider == null) return;
            float leftTravel = 1f;
            float rightTravel = 1f;
            bool leftGrounded = axle.leftCollider.GetGroundHit(out WheelHit leftHit);
            bool rightGrounded = axle.rightCollider.GetGroundHit(out WheelHit rightHit);
            if (leftGrounded)
                leftTravel = (-axle.leftCollider.transform.InverseTransformPoint(leftHit.point).y - axle.leftCollider.radius) / axle.leftCollider.suspensionDistance;
            if (rightGrounded)
                rightTravel = (-axle.rightCollider.transform.InverseTransformPoint(rightHit.point).y - axle.rightCollider.radius) / axle.rightCollider.suspensionDistance;
            float force = (leftTravel - rightTravel) * antiRollForce;
            if (leftGrounded) body.AddForceAtPosition(axle.leftCollider.transform.up * -force, axle.leftCollider.transform.position);
            if (rightGrounded) body.AddForceAtPosition(axle.rightCollider.transform.up * force, axle.rightCollider.transform.position);
        }

        private static void SyncWheel(WheelCollider collider, Transform visual,
            Vector3 positionOffset, Quaternion rotationOffset)
        {
            if (collider == null || visual == null) return;
            collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
            // Imported vehicle wheels rarely use the same local axis as a
            // WheelCollider. Preserve the model's bind-pose offset so steering
            // and rolling do not turn the visible wheel sideways.
            float offsetLengthSquared = rotationOffset.x * rotationOffset.x +
                rotationOffset.y * rotationOffset.y + rotationOffset.z * rotationOffset.z +
                rotationOffset.w * rotationOffset.w;
            if (offsetLengthSquared < 0.5f) rotationOffset = Quaternion.identity;
            visual.SetPositionAndRotation(position + rotation * positionOffset,
                rotation * rotationOffset);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude < 9f) return;
            HasCollidedRecently = true;
            CollisionOccurred?.Invoke();
            CancelInvoke(nameof(ClearCollisionFlag));
            Invoke(nameof(ClearCollisionFlag), 1.5f);
        }

        private void ClearCollisionFlag() => HasCollidedRecently = false;
    }
}
