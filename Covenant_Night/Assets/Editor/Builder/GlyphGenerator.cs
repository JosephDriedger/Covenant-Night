using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Controller button icons for the Controls page, drawn from anti-aliased signed-distance shapes like the
// rest of the procedural art: a round face button, a stick, a bumper, the Menu and Options buttons, the
// four PlayStation face shapes and a D-pad with one direction lit. Button letters (A, RB, L3...) are not
// painted in; the UI lays a text label over the plain shapes. All icons are square with transparent
// padding so they share one size on screen.
public static class GlyphGenerator
{
    public const string Folder = "Assets/Art/UI/Glyphs";
    const int N = 128;

    static readonly Color Body = new Color(0.12f, 0.13f, 0.16f, 1f);
    static readonly Color Rim  = new Color(0.40f, 0.41f, 0.46f, 1f);
    static readonly Color Lit  = new Color(0.88f, 0.89f, 0.92f, 1f);

    delegate float Sdf(Vector2 p);

    class Img
    {
        public readonly Color[] px = new Color[N * N];

        public Img Layer(Sdf sdf, Color c)
        {
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var p = new Vector2(x + 0.5f - N * 0.5f, y + 0.5f - N * 0.5f);
                float a = Mathf.Clamp01(0.5f - sdf(p)) * c.a;
                if (a <= 0f) continue;
                int i = y * N + x;
                Color d = px[i];
                float outA = a + d.a * (1f - a);
                float k = 1f / Mathf.Max(outA, 1e-5f);
                px[i] = new Color((c.r * a + d.r * d.a * (1f - a)) * k,
                                  (c.g * a + d.g * d.a * (1f - a)) * k,
                                  (c.b * a + d.b * d.a * (1f - a)) * k, outA);
            }
            return this;
        }
    }

    // ── distance functions (pixels, origin at the icon centre, +y up) ──
    static float Circle(Vector2 p, float r) => p.magnitude - r;

    static float Box(Vector2 p, Vector2 c, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return (p - a - ab * t).magnitude;
    }

    // ── shapes ──
    static Img Disc() => new Img().Layer(p => Circle(p, 60f), Rim).Layer(p => Circle(p, 55f), Body);

    static Img Stick() => Disc().Layer(p => Mathf.Abs(Circle(p, 38f)) - 3f, new Color(0.26f, 0.27f, 0.31f));

    static Img Bumper() => new Img()
        .Layer(p => Box(p, Vector2.zero, new Vector2(62f, 38f), 22f), Rim)
        .Layer(p => Box(p, Vector2.zero, new Vector2(57f, 33f), 18f), Body);

    static Img Menu() => Disc()
        .Layer(p => Mathf.Min(Box(p, new Vector2(0, 15), new Vector2(23, 4), 4), Mathf.Min(Box(p, Vector2.zero, new Vector2(23, 4), 4), Box(p, new Vector2(0, -15), new Vector2(23, 4), 4))), Lit);

    static Img Options() => new Img()
        .Layer(p => Box(p, Vector2.zero, new Vector2(62f, 42f), 42f), Rim)
        .Layer(p => Box(p, Vector2.zero, new Vector2(57f, 37f), 37f), Body)
        .Layer(p => Mathf.Min(Box(p, new Vector2(0, 13), new Vector2(20, 3.5f), 3.5f), Mathf.Min(Box(p, Vector2.zero, new Vector2(20, 3.5f), 3.5f), Box(p, new Vector2(0, -13), new Vector2(20, 3.5f), 3.5f))), Lit);

    static Img DPad(Vector2 dir)
    {
        Sdf plus(float w, float l, float r) => p => Mathf.Min(Box(p, Vector2.zero, new Vector2(l, w), r), Box(p, Vector2.zero, new Vector2(w, l), r));
        // thin rim and a dark body so the one lit arm stands out even at icon size
        Vector2 armHalf = Mathf.Abs(dir.x) > 0f ? new Vector2(21f, 17f) : new Vector2(17f, 21f);
        return new Img()
            .Layer(plus(21f, 61f, 7f), new Color(0.30f, 0.31f, 0.35f))
            .Layer(plus(18.5f, 58.5f, 6f), new Color(0.09f, 0.10f, 0.12f))
            .Layer(p => Box(p, dir * 37f, armHalf, 4f), new Color(0.97f, 0.97f, 1f));
    }

    static Img PsCross() => Disc().Layer(p => Mathf.Min(Segment(p, new Vector2(-22, -22), new Vector2(22, 22)),
                                                        Segment(p, new Vector2(-22, 22), new Vector2(22, -22))) - 5.5f,
                                         new Color(0.47f, 0.67f, 0.96f));
    static Img PsCircle()   => Disc().Layer(p => Mathf.Abs(Circle(p, 25f)) - 5.5f, new Color(0.96f, 0.38f, 0.42f));
    static Img PsSquare()   => Disc().Layer(p => Mathf.Abs(Box(p, Vector2.zero, new Vector2(22f, 22f), 2f)) - 5.5f, new Color(0.93f, 0.55f, 0.84f));
    static Img PsTriangle()
    {
        Vector2 a = new Vector2(0, 27), b = new Vector2(-25, -17), c = new Vector2(25, -17);
        return Disc().Layer(p => Mathf.Min(Segment(p, a, b), Mathf.Min(Segment(p, b, c), Segment(p, c, a))) - 5.5f,
                            new Color(0.30f, 0.86f, 0.70f));
    }

    public static Dictionary<string, Sprite> Generate()
    {
        Directory.CreateDirectory(Folder);
        var shapes = new Dictionary<string, Img>
        {
            ["disc"] = Disc(), ["stick"] = Stick(), ["bumper"] = Bumper(), ["menu"] = Menu(), ["options"] = Options(),
            ["dpad_up"] = DPad(Vector2.up), ["dpad_down"] = DPad(Vector2.down),
            ["dpad_left"] = DPad(Vector2.left), ["dpad_right"] = DPad(Vector2.right),
            ["ps_cross"] = PsCross(), ["ps_circle"] = PsCircle(), ["ps_square"] = PsSquare(), ["ps_triangle"] = PsTriangle(),
        };

        var paths = new Dictionary<string, string>();
        foreach (var kv in shapes)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.SetPixels(kv.Value.px);
            tex.Apply();
            string path = $"{Folder}/glyph_{kv.Key}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            paths[kv.Key] = path;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var sprites = new Dictionary<string, Sprite>();
        foreach (var kv in paths)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(kv.Value);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            sprites[kv.Key] = AssetDatabase.LoadAssetAtPath<Sprite>(kv.Value);
        }
        return sprites;
    }
}
