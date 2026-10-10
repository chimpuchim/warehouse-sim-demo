using UnityEngine;
using UnityEngine.Rendering;

namespace Warehouse
{
    /// <summary>
    /// Procedural materials built from the render pipeline's default Lit material, so no shader
    /// is stripped from the build and the project needs no art files.
    /// </summary>
    public static class Mats
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int Smoothness = Shader.PropertyToID("_Smoothness");

        public static Material Solid(Color color, float smoothness = 0.25f)
        {
            var m = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            m.SetColor(BaseColor, color);
            m.SetFloat(Smoothness, smoothness);
            return m;
        }

        /// <summary>Two-tone checker, tiled so one cell is roughly one metre on the floor.</summary>
        public static Material Checker(Color a, Color b, float tilesPerMetre, Vector2 sizeMetres)
        {
            const int px = 64;
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var colors = new Color[px * px];
            for (int y = 0; y < px; y++)
            for (int x = 0; x < px; x++)
                colors[y * px + x] = ((x < px / 2) ^ (y < px / 2)) ? a : b;
            tex.SetPixels(colors);
            tex.Apply(true);
            var m = Solid(Color.white, 0.1f);
            m.SetTexture(BaseMap, tex);
            m.mainTextureScale = sizeMetres * tilesPerMetre * 0.5f;
            return m;
        }
    }
}
