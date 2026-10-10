using UnityEngine;

namespace DrivingSim.Vehicles
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class EngineAudio : MonoBehaviour
    {
        [SerializeField] private CarController car;
        [SerializeField, Range(0.1f, 2f)] private float idlePitch = 0.65f;
        [SerializeField, Range(0.5f, 3f)] private float maximumPitch = 1.8f;
        [SerializeField, Range(0f, 1f)] private float throttleVolumeBoost = 0.25f;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            if (source.clip == null) source.clip = CreatePlaceholderEngineLoop();
            source.Play();
        }

        private void Update()
        {
            if (car == null || car.Data == null) return;
            if (!car.IsEngineRunning)
            {
                source.volume = Mathf.MoveTowards(source.volume, 0f, Time.unscaledDeltaTime * 3f);
                return;
            }
            float rpm = Mathf.InverseLerp(car.Data.IdleRpm, car.Data.MaxRpm, car.EngineRpm);
            source.pitch = Mathf.Lerp(idlePitch, maximumPitch, rpm);
            float targetVolume = Mathf.Clamp01(0.55f + Mathf.Abs(car.ThrottleInput) * throttleVolumeBoost);
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.unscaledDeltaTime * 3f);
        }

        public void Configure(CarController controller) => car = controller;

        // A tiny synthesized loop keeps the prototype self-contained. Replace it with a licensed engine recording for production.
        private static AudioClip CreatePlaceholderEngineLoop()
        {
            const int sampleRate = 22050;
            const int samples = 5512;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float phase = i / (float)sampleRate * Mathf.PI * 2f * 55f;
                data[i] = (Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 2f) * 0.25f + Mathf.Sin(phase * 0.5f) * 0.15f) * 0.15f;
            }
            AudioClip clip = AudioClip.Create("Procedural Engine Placeholder", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
