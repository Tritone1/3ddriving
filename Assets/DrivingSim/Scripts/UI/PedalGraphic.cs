using UnityEngine;
using UnityEngine.UI;

namespace DrivingSim.UI
{
    /// <summary>
    /// Lightweight vector UI graphic used for mobile driving pedals. It avoids
    /// texture memory and stays sharp at every phone resolution.
    /// </summary>
    public sealed class PedalGraphic : MaskableGraphic
    {
        public enum PedalStyle
        {
            Accelerator,
            Brake,
            ReverseGear
        }

        [SerializeField] private PedalStyle style;

        public void Configure(PedalStyle newStyle, Color newColor)
        {
            style = newStyle;
            color = newColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Vector2[] shape = GetShape();
            Rect rect = GetPixelAdjustedRect();
            Color32 edgeColor = Multiply(color, 0.48f);
            Color32 faceColor = Multiply(color, 1.12f);

            AddPolygon(vertexHelper, shape, rect, 1f, edgeColor);
            AddPolygon(vertexHelper, shape, rect, 0.86f, faceColor);
        }

        private Vector2[] GetShape()
        {
            switch (style)
            {
                case PedalStyle.Accelerator:
                    return new[]
                    {
                        new Vector2(-0.28f, -0.50f), new Vector2(0.28f, -0.50f),
                        new Vector2(0.40f, -0.39f), new Vector2(0.32f, 0.42f),
                        new Vector2(0.22f, 0.50f), new Vector2(-0.22f, 0.50f),
                        new Vector2(-0.32f, 0.42f), new Vector2(-0.40f, -0.39f)
                    };
                case PedalStyle.Brake:
                    return new[]
                    {
                        new Vector2(-0.50f, -0.33f), new Vector2(-0.42f, -0.47f),
                        new Vector2(0.42f, -0.47f), new Vector2(0.50f, -0.33f),
                        new Vector2(0.46f, 0.34f), new Vector2(0.37f, 0.47f),
                        new Vector2(-0.37f, 0.47f), new Vector2(-0.46f, 0.34f)
                    };
                default:
                    return new[]
                    {
                        new Vector2(-0.38f, -0.50f), new Vector2(0.38f, -0.50f),
                        new Vector2(0.50f, -0.35f), new Vector2(0.50f, 0.35f),
                        new Vector2(0.38f, 0.50f), new Vector2(-0.38f, 0.50f),
                        new Vector2(-0.50f, 0.35f), new Vector2(-0.50f, -0.35f)
                    };
            }
        }

        private static void AddPolygon(VertexHelper helper, Vector2[] shape, Rect rect, float scale, Color32 tint)
        {
            int firstVertex = helper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = tint;
            vertex.position = rect.center;
            helper.AddVert(vertex);

            for (int i = 0; i < shape.Length; i++)
            {
                vertex.position = rect.center + Vector2.Scale(shape[i], rect.size) * scale;
                helper.AddVert(vertex);
            }

            for (int i = 0; i < shape.Length; i++)
                helper.AddTriangle(firstVertex, firstVertex + i + 1, firstVertex + ((i + 1) % shape.Length) + 1);
        }

        private static Color32 Multiply(Color source, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(source.r * multiplier),
                Mathf.Clamp01(source.g * multiplier),
                Mathf.Clamp01(source.b * multiplier),
                source.a);
        }
    }
}
