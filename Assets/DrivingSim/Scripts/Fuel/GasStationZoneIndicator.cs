using UnityEngine;

namespace DrivingSim.Fuel
{
    [RequireComponent(typeof(BoxCollider), typeof(GasStation))]
    public sealed class GasStationZoneIndicator : MonoBehaviour
    {
        [SerializeField] private Color availableColor = new Color(1f, 0.67f, 0.05f, 0.95f);
        [SerializeField] private Color occupiedColor = new Color(0.12f, 1f, 0.4f, 1f);
        [SerializeField, Min(0.02f)] private float lineWidth = 0.16f;
        [SerializeField, Min(0f)] private float groundOffset = 0.08f;

        private GasStation station;
        private LineRenderer outline;
        private Material runtimeMaterial;

        private void Awake()
        {
            station = GetComponent<GasStation>();
            BuildOutline();
        }

        private void OnValidate()
        {
            if (Application.isPlaying || !isActiveAndEnabled) return;
            if (outline == null) outline = GetComponentInChildren<LineRenderer>(true);
            UpdateGeometry();
        }

        private void Update()
        {
            if (outline == null) return;
            Color target = station != null && station.ReadyToRefuel ? occupiedColor : availableColor;
            float pulse = 0.78f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.22f;
            target.a *= pulse;
            outline.startColor = target;
            outline.endColor = target;
            outline.widthMultiplier = lineWidth * (0.92f + pulse * 0.12f);
        }

        private void BuildOutline()
        {
            Transform existing = transform.Find("Refuel Zone Outline");
            GameObject outlineObject;
            if (existing != null)
            {
                outlineObject = existing.gameObject;
                outline = outlineObject.GetComponent<LineRenderer>();
            }
            else
            {
                outlineObject = new GameObject("Refuel Zone Outline");
                outlineObject.transform.SetParent(transform, false);
                outline = outlineObject.AddComponent<LineRenderer>();
            }

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            runtimeMaterial = new Material(shader) { name = "Runtime Refuel Zone" };
            outline.sharedMaterial = runtimeMaterial;
            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 4;
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.textureMode = LineTextureMode.Tile;
            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            if (outline == null) return;
            BoxCollider zone = GetComponent<BoxCollider>();
            Vector3 center = zone.center;
            Vector3 half = zone.size * 0.5f;
            float y = center.y - half.y + groundOffset;
            outline.SetPosition(0, new Vector3(center.x - half.x, y, center.z - half.z));
            outline.SetPosition(1, new Vector3(center.x - half.x, y, center.z + half.z));
            outline.SetPosition(2, new Vector3(center.x + half.x, y, center.z + half.z));
            outline.SetPosition(3, new Vector3(center.x + half.x, y, center.z - half.z));
            outline.widthMultiplier = lineWidth;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
