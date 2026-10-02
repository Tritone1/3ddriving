using UnityEngine;

namespace DrivingSim.Vehicles
{
    [CreateAssetMenu(menuName = "Driving Sim/Car", fileName = "Car_")]
    public sealed class CarData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "car";
        [SerializeField] private string displayName = "Car";
        [SerializeField, Min(0)] private int price;
        [SerializeField] private string unlockMissionId;
        [SerializeField] private GameObject prefab;

        [Header("Driving")]
        [SerializeField, Min(100f)] private float mass = 1350f;
        [SerializeField, Min(50f)] private float maxMotorTorque = 1450f;
        [SerializeField, Min(1f)] private float maxBrakeTorque = 3200f;
        [SerializeField, Range(5f, 50f)] private float maxSteerAngle = 32f;
        [SerializeField, Min(20f)] private float topSpeedKph = 180f;
        [SerializeField, Range(0.1f, 3f)] private float tireGrip = 1f;
        [SerializeField, Min(0f)] private float downforce = 25f;
        [SerializeField] private Vector3 centerOfMass = new Vector3(0f, -0.45f, 0.1f);

        [Header("Powertrain")]
        [SerializeField] private AnimationCurve torqueCurve = new AnimationCurve(
            new Keyframe(0f, 0.65f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.72f));
        [SerializeField] private float[] gearRatios = { 3.1f, 2.15f, 1.55f, 1.16f, 0.92f, 0.74f };
        [SerializeField, Min(500f)] private float idleRpm = 850f;
        [SerializeField, Min(1000f)] private float maxRpm = 7000f;
        [SerializeField, Min(0.1f)] private float finalDrive = 3.42f;

        [Header("Fuel")]
        [SerializeField, Min(1f)] private float fuelCapacityLitres = 55f;
        [Tooltip("Litres per 100 km at moderate load.")]
        [SerializeField, Min(0.1f)] private float consumptionLitresPer100Km = 9f;

        public string Id => id;
        public string DisplayName => displayName;
        public int Price => price;
        public string UnlockMissionId => unlockMissionId;
        public GameObject Prefab => prefab;
        public float Mass => mass;
        public float MaxMotorTorque => maxMotorTorque;
        public float MaxBrakeTorque => maxBrakeTorque;
        public float MaxSteerAngle => maxSteerAngle;
        public float TopSpeedKph => topSpeedKph;
        public float TireGrip => tireGrip;
        public float Downforce => downforce;
        public Vector3 CenterOfMass => centerOfMass;
        public AnimationCurve TorqueCurve => torqueCurve;
        public float[] GearRatios => gearRatios;
        public float IdleRpm => idleRpm;
        public float MaxRpm => maxRpm;
        public float FinalDrive => finalDrive;
        public float FuelCapacityLitres => fuelCapacityLitres;
        public float ConsumptionLitresPer100Km => consumptionLitresPer100Km;

        private void OnValidate()
        {
            id = string.IsNullOrWhiteSpace(id) ? name.ToLowerInvariant().Replace(' ', '-') : id.Trim();
            maxRpm = Mathf.Max(maxRpm, idleRpm + 500f);
        }
    }
}
