using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Flat-shaded low-poly mesh toolkit (faceted look: every face has its own vertices and normal).
// Character/prop meshes are cached as assets under Assets/Art/Meshes so prefabs can reference them.
public static class LowPoly
{
    const string Dir = "Assets/Art/Meshes";

    class MB
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector3> n = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> t = new List<int>();

        // Adds a triangle; winds it so the geometric normal points along `outward`.
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward, Vector2 ua = default, Vector2 ub = default, Vector2 uc = default)
        {
            Vector3 nrm = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(nrm, outward) < 0f) { var tmp = b; b = c; c = tmp; var tu = ub; ub = uc; uc = tu; nrm = -nrm; }
            nrm.Normalize();
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(nrm); n.Add(nrm); n.Add(nrm);
            uv.Add(ua); uv.Add(ub); uv.Add(uc);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        public Mesh Build(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    // ── cache ───────────────────────────────────────────────────────────────

    static readonly Dictionary<string, Mesh> Session = new Dictionary<string, Mesh>();

    // Call at the start of a build so meshes are regenerated once per run (and shared within it).
    public static void ResetSession() => Session.Clear();

    public static Mesh Cached(string name, System.Func<Mesh> make)
    {
        if (Session.TryGetValue(name, out var done) && done != null) return done;
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Art", "Meshes");
        string path = $"{Dir}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);      // regenerate so edits to the generators always apply
        var m = make();
        m.name = name;
        AssetDatabase.CreateAsset(m, path);
        Session[name] = m;
        return m;
    }

    // ── shapes ──────────────────────────────────────────────────────────────

    // Tapered prism (cylinder / frustum / cone) along +Y starting at yStart. xz = non-uniform scale of the cross-section.
    public static Mesh Frustum(int sides, float rBottom, float rTop, float height, float yStart = 0f,
                               bool capTop = true, bool capBottom = true, float xs = 1f, float zs = 1f, float twistDeg = 0f)
    {
        var mb = new MB();
        float y0 = yStart, y1 = yStart + height;
        Vector3 P(int i, float r, float y, float tw)
        {
            float a = (i / (float)sides) * Mathf.PI * 2f + tw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * r * xs, y, Mathf.Sin(a) * r * zs);
        }
        for (int i = 0; i < sides; i++)
        {
            Vector3 b0 = P(i, rBottom, y0, 0), b1 = P(i + 1, rBottom, y0, 0);
            Vector3 t0 = P(i, rTop, y1, twistDeg), t1 = P(i + 1, rTop, y1, twistDeg);
            Vector3 mid = (b0 + b1 + t0 + t1) * 0.25f; mid.y = 0f;
            if (rTop > 0.0001f) { mb.Tri(b0, t0, t1, mid); mb.Tri(b0, t1, b1, mid); }
            else mb.Tri(b0, t0, b1, mid);           // cone: apex (t0 == t1)
            if (capTop && rTop > 0.0001f) mb.Tri(new Vector3(0, y1, 0), t0, t1, Vector3.up);
            if (capBottom && rBottom > 0.0001f) mb.Tri(new Vector3(0, y0, 0), b0, b1, Vector3.down);
        }
        return mb.Build("Frustum");
    }

    // Icosphere-ish blob (20 faces at level 0, 80 at level 1).
    public static Mesh Ico(float radius, int level = 0)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var verts = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        var faces = new List<int[]>
        {
            new[]{0,11,5}, new[]{0,5,1}, new[]{0,1,7}, new[]{0,7,10}, new[]{0,10,11},
            new[]{1,5,9}, new[]{5,11,4}, new[]{11,10,2}, new[]{10,7,6}, new[]{7,1,8},
            new[]{3,9,4}, new[]{3,4,2}, new[]{3,2,6}, new[]{3,6,8}, new[]{3,8,9},
            new[]{4,9,5}, new[]{2,4,11}, new[]{6,2,10}, new[]{8,6,7}, new[]{9,8,1},
        };
        var mb = new MB();
        void Sub(Vector3 a, Vector3 b, Vector3 c, int lv)
        {
            if (lv == 0) { mb.Tri(a * radius, b * radius, c * radius, (a + b + c)); return; }
            Vector3 ab = ((a + b) * 0.5f).normalized, bc = ((b + c) * 0.5f).normalized, ca = ((c + a) * 0.5f).normalized;
            Sub(a, ab, ca, lv - 1); Sub(b, bc, ab, lv - 1); Sub(c, ca, bc, lv - 1); Sub(ab, bc, ca, lv - 1);
        }
        foreach (var f in faces) Sub(verts[f[0]], verts[f[1]], verts[f[2]], level);
        return mb.Build("Ico");
    }

    // Box with UVs measured in world units / tile (so textures don't stretch on large blocks).
    public static Mesh Box(Vector3 size, float tile = 2f, Vector3 center = default)
    {
        var mb = new MB();
        Vector3 h = size * 0.5f;
        void Face(Vector3 origin, Vector3 uAxis, Vector3 vAxis, Vector3 normal)
        {
            Vector3 a = center + origin - uAxis * 0.5f - vAxis * 0.5f;
            Vector3 b = a + uAxis, c = a + uAxis + vAxis, d = a + vAxis;
            float uu = uAxis.magnitude / tile, vv = vAxis.magnitude / tile;
            mb.Tri(a, d, c, normal, new Vector2(0, 0), new Vector2(0, vv), new Vector2(uu, vv));
            mb.Tri(a, c, b, normal, new Vector2(0, 0), new Vector2(uu, vv), new Vector2(uu, 0));
        }
        Face(new Vector3(0, 0, h.z),  new Vector3(size.x, 0, 0), new Vector3(0, size.y, 0), Vector3.forward);
        Face(new Vector3(0, 0, -h.z), new Vector3(size.x, 0, 0), new Vector3(0, size.y, 0), Vector3.back);
        Face(new Vector3(h.x, 0, 0),  new Vector3(0, 0, size.z), new Vector3(0, size.y, 0), Vector3.right);
        Face(new Vector3(-h.x, 0, 0), new Vector3(0, 0, size.z), new Vector3(0, size.y, 0), Vector3.left);
        Face(new Vector3(0, h.y, 0),  new Vector3(size.x, 0, 0), new Vector3(0, 0, size.z), Vector3.up);
        Face(new Vector3(0, -h.y, 0), new Vector3(size.x, 0, 0), new Vector3(0, 0, size.z), Vector3.down);
        return mb.Build("Box");
    }

    // Merges many (mesh, matrix) pairs into one mesh (used for windows, decorations).
    public static Mesh Combine(List<CombineInstance> parts, string name)
    {
        var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.CombineMeshes(parts.ToArray(), true, true);
        m.RecalculateBounds();
        return m;
    }
}
