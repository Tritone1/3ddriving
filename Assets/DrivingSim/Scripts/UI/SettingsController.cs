using DrivingSim.Save;
using UnityEngine;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private SaveService saveService;
        [SerializeField] private MobileInputState mobileInput;
        [SerializeField] private Dropdown steeringDropdown;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Slider volumeSlider;

        private void Start()
        {
            if (saveService == null || saveService.Profile == null) return;
            SettingsRecord settings = saveService.Profile.settings;
            if (steeringDropdown != null) steeringDropdown.SetValueWithoutNotify(settings.steeringMode);
            if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(settings.qualityLevel);
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(settings.masterVolume);
            Apply(settings.steeringMode, settings.qualityLevel, settings.masterVolume);
        }

        public void SetSteeringMode(int value)
        {
            mobileInput?.SetMode(value);
            if (saveService?.Profile != null) saveService.Profile.settings.steeringMode = value;
            saveService?.Save();
        }
        public void SetQuality(int value)
        {
            int level = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualitySettings.SetQualityLevel(level, true);
            if (saveService?.Profile != null) saveService.Profile.settings.qualityLevel = level;
            saveService?.Save();
        }
        public void SetVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            if (saveService?.Profile != null) saveService.Profile.settings.masterVolume = AudioListener.volume;
            saveService?.Save();
        }
        private void Apply(int steering, int quality, float volume)
        {
            mobileInput?.SetMode(steering);
            QualitySettings.SetQualityLevel(Mathf.Clamp(quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1)), true);
            AudioListener.volume = Mathf.Clamp01(volume);
        }
    }
}
