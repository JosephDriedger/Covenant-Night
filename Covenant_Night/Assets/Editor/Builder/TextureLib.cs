using System.IO;
using UnityEditor;
using UnityEngine;

// Procedural, seamlessly tiling greyscale textures (tinted by each material's base colour) for the hand-painted
// low-poly look: mud-brick, stucco, flagstone, dirt, planks, roof tiles, awning stripes, plus a soft particle dot.
public static class TextureLib
{
    const string Dir = "Assets/Art/Textures";
    const int N = 256;

    public static Texture2D Brick, Plaster, Flagstone, Dirt, Planks, Tiles, Stripes, Dot;

    // ── periodic value noise ────────────────────────────────────────────────

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    static float Noise(float u, float v, int cells, int seed)          // u,v in 0..1, wraps
    {
        float x = u * cells, y = v * cells;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(Mod(x0, cells), Mod(y0, cells), seed), b = Hash(Mod(x0 + 1, cells), Mod(y0, cells), seed);
        float c = Hash(Mod(x0, cells), Mod(y0 + 1, cells), seed), d = Hash(Mod(x0 + 1, cells), Mod(y0 + 1, cells), seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    static int Mod(int a, int m) => ((a % m) + m) % m;

    static float Fbm(float u, float v, int seed) =>
        Noise(u, v, 4, seed) * 0.5f + Noise(u, v, 8, seed + 1) * 0.3f + Noise(u, v, 16, seed + 2) * 0.2f;

    // ── generators ──────────────────────────────────────────────────────────

    static Color G(float g) { g = Mathf.Clamp01(g); return new Color(g, g, g, 1f); }

    static Texture2D Make(string name, System.Func<float, float, Color> f, bool alpha = false)
    {
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                px[y * N + x] = f(x / (float)N, y / (float)N);
        tex.SetPixels(px);
        tex.Apply();
        Directory.CreateDirectory(Dir);
        string path = $"{Dir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.mipmapEnabled = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.anisoLevel = 4;
        imp.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        imp.alphaIsTransparency = alpha;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Art", "Textures");

        // mud-brick courses, 4 x 8 bricks per tile, alternate rows offset
        Brick = Make("tex_brick", (u, v) =>
        {
            int rows = 8, cols = 4;
            float rv = v * rows; int row = Mathf.FloorToInt(rv); float fy = rv - row;
            float ru = u * cols + (row % 2 == 0 ? 0f : 0.5f); int col = Mathf.FloorToInt(ru); float fx = ru - col;
            float mortar = (fx < 0.05f || fy < 0.09f) ? 1f : 0f;
            float tone = 0.78f + 0.22f * Hash(Mod(col, cols), row, 7);
            float g = tone * (0.85f + 0.15f * Fbm(u, v, 3));
            g *= mortar > 0 ? 0.62f : 1f;
            return G(g);
        });

        // stucco: soft blotches and faint cracks
        Plaster = Make("tex_plaster", (u, v) =>
        {
            float g = 0.82f + 0.18f * Fbm(u, v, 11);
            float crack = Mathf.Abs(Noise(u, v, 6, 21) - 0.5f);
            if (crack < 0.012f) g *= 0.78f;
            return G(g);
        });

        // irregular flagstones (periodic Voronoi)
        Flagstone = Make("tex_flagstone", (u, v) =>
        {
            const int cells = 6;
            float x = u * cells, y = v * cells; int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float d1 = 9f, d2 = 9f; int bestId = 0;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j;
                    float px = gx + Hash(Mod(gx, cells), Mod(gy, cells), 31), py = gy + Hash(Mod(gx, cells), Mod(gy, cells), 32);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < d1) { d2 = d1; d1 = d; bestId = Mod(gx, cells) * 31 + Mod(gy, cells); }
                    else if (d < d2) d2 = d;
                }
            float edge = d2 - d1;
            float tone = 0.7f + 0.3f * Hash(bestId, 3, 5);
            float g = tone * (0.88f + 0.12f * Fbm(u, v, 5));
            if (edge < 0.09f) g *= 0.55f;
            return G(g);
        });

        Dirt = Make("tex_dirt", (u, v) => G(0.72f + 0.28f * Fbm(u, v, 41) - (Hash((int)(u * N), (int)(v * N), 9) > 0.985f ? 0.25f : 0f)));

        // vertical planks with grain
        Planks = Make("tex_planks", (u, v) =>
        {
            int planks = 8; float pu = u * planks; int p = Mathf.FloorToInt(pu); float fx = pu - p;
            float tone = 0.7f + 0.3f * Hash(p, 1, 3);
            float grain = 0.9f + 0.1f * Mathf.Sin((v * 14f + Hash(p, 2, 4) * 6f + Noise(u, v, 8, 5) * 2f) * Mathf.PI * 2f);
            float g = tone * grain;
            if (fx < 0.06f || fx > 0.96f) g *= 0.5f;
            return G(g);
        });

        // roof tiles: overlapping rows with a shaded lower edge
        Tiles = Make("tex_tiles", (u, v) =>
        {
            int rows = 8, cols = 6;
            float rv = v * rows; int row = Mathf.FloorToInt(rv); float fy = rv - row;
            float ru = u * cols + (row % 2 == 0 ? 0f : 0.5f); int col = Mathf.FloorToInt(ru); float fx = ru - col;
            float tone = 0.75f + 0.25f * Hash(Mod(col, cols), row, 11);
            float shade = Mathf.Lerp(1.05f, 0.65f, fy);
            float g = tone * shade;
            if (fx < 0.05f) g *= 0.6f;
            return G(g);
        });

        // awning stripes (white / mid grey bands)
        Stripes = Make("tex_stripes", (u, v) =>
        {
            int band = Mathf.FloorToInt(u * 8f);
            float g = band % 2 == 0 ? 1f : 0.62f;
            return G(g * (0.94f + 0.06f * Fbm(u, v, 61)));
        });

        // soft radial dot for particles (alpha)
        Dot = Make("tex_softdot", (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float a = Mathf.Clamp01(1f - d); a *= a;
            return new Color(1f, 1f, 1f, a);
        }, alpha: true);
        AssetDatabase.SaveAssets();
    }
}
