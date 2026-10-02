using UnityEngine;

namespace DrivingSim.Core
{
    public sealed class MobileQualityPreset : MonoBehaviour
    {
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
        [SerializeField, Min(10f)] private float lowShadowDistance = 25f;
        [SerializeField, Min(10f)] private float mediumShadowDistance = 50f;
        [SerializeField, Min(10f)] private float highShadowDistance = 80f;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Apply(QualitySettings.GetQualityLevel());
        }

        public void Apply(int level)
        {
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, QualitySettings.names.Length - 1), true);
            QualitySettings.shadowDistance = level <= 0 ? lowShadowDistance : level == 1 ? mediumShadowDistance : highShadowDistance;
            QualitySettings.lodBias = level <= 0 ? 0.65f : level == 1 ? 1f : 1.5f;
            QualitySettings.antiAliasing = level <= 0 ? 0 : level == 1 ? 2 : 4;
        }
    }
}
