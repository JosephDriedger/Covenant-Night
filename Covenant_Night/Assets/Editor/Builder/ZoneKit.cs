using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Axis-aligned rectangle on the XZ plane.
public struct R
{
    public float x0, z0, x1, z1;
    public R(float x0, float z0, float x1, float z1) { this.x0 = x0; this.z0 = z0; this.x1 = x1; this.z1 = z1; }
    public float W => x1 - x0;
    public float D => z1 - z0;
    public Vector2 Center => new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
    public bool Contains(Vector2 p) => p.x >= x0 && p.x <= x1 && p.y >= z0 && p.y <= z1;
}

public struct Door
{
    public char side; public float c; public float w;
    public Door(char side, float c, float w) { this.side = side; this.c = c; this.w = w; }
}

// A small DSL for blocking out a zone. Zones are described by their walkable rectangles: everything else
// inside the floor extents becomes solid buildings (see Carve), so corridor widths are exactly as specified.
public class ZoneKit
{
    public string sceneName;
    public Scene scene;
    public Mats M;
    public Prefabs P;
    public AudioLib A;
    public MixerRefs MX;

    public Transform root, envRoot, propsRoot, lightsRoot, guardsRoot, pathsRoot, triggersRoot;
    public ZoneEntry entry;
    readonly List<Transform> _pots = new List<Transform>();

    int _count;

    public ZoneKit(string sceneName, Mats m, Prefabs p, AudioLib a, MixerRefs mx)
    {
        this.sceneName = sceneName; M = m; P = p; A = a; MX = mx;
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("Zone").transform;
        envRoot      = Child("Environment");
        propsRoot    = Child("Props");
        lightsRoot   = Child("Lighting");
        guardsRoot   = Child("Guards");
        pathsRoot    = Child("PatrolPaths");
        triggersRoot = Child("Triggers");
    }

    Transform Child(string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(root, false);
        return t;
    }

    // ── geometry primitives ─────────────────────────────────────────────────

    public GameObject Cube(string name, Transform parent, Vector3 center, Vector3 size, Material mat, int layer, bool collider = true, float tile = 2f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.layer = layer;
        var mesh = LowPoly.Box(size, tile);
        mesh.name = name;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        go.isStatic = true;
        return go;
    }

    public GameObject Block(R r, float h, Material mat, int layer = GameLayers.Walls, float y0 = 0f, string name = "Block")
    {
        if (r.W < 0.02f || r.D < 0.02f || h < 0.02f) return null;
        var c = r.Center;
        return Cube(name, envRoot, new Vector3(c.x, y0 + h * 0.5f, c.y), new Vector3(r.W, h, r.D), mat, layer);
    }

    public void Ground(R r, Material mat, int layer = GameLayers.GroundStone)
    {
        var c = r.Center;
        Cube("Ground", envRoot, new Vector3(c.x, -0.5f, c.y), new Vector3(r.W, 1f, r.D), mat, layer, true, 3f);
    }

    // Thin surface overlay to give a different footstep surface (dirt / wood)
    public void Patch(R r, Material mat, int layer, float th = 0.05f)
    {
        var c = r.Center;
        Cube("Patch", envRoot, new Vector3(c.x, th * 0.5f, c.y), new Vector3(r.W, th, r.D), mat, layer, true, 2.5f);
    }

    // ── carving ─────────────────────────────────────────────────────────────

    static List<R> Subtract(R a, R hole)
    {
        var res = new List<R>();
        if (hole.x1 <= a.x0 || hole.x0 >= a.x1 || hole.z1 <= a.z0 || hole.z0 >= a.z1) { res.Add(a); return res; }
        float hx0 = Mathf.Max(a.x0, hole.x0), hx1 = Mathf.Min(a.x1, hole.x1);
        float hz0 = Mathf.Max(a.z0, hole.z0), hz1 = Mathf.Min(a.z1, hole.z1);
        if (hx0 > a.x0) res.Add(new R(a.x0, a.z0, hx0, a.z1));
        if (hx1 < a.x1) res.Add(new R(hx1, a.z0, a.x1, a.z1));
        if (hz0 > a.z0) res.Add(new R(hx0, a.z0, hx1, hz0));
        if (hz1 < a.z1) res.Add(new R(hx0, hz1, hx1, a.z1));
        return res;
    }

    public static List<R> Carve(R floor, IEnumerable<R> walkable)
    {
        var solids = new List<R> { floor };
        foreach (var w in walkable)
        {
            var next = new List<R>();
            foreach (var s in solids) next.AddRange(Subtract(s, w));
            solids = next;
        }
        return solids;
    }

    // Everything in `floor` that is not walkable becomes a building block.
    readonly List<(R r, float h)> _solids = new List<(R, float)>();
    readonly List<CombineInstance> _winFrames = new List<CombineInstance>(), _winLit = new List<CombineInstance>(), _winDark = new List<CombineInstance>();

    // Everything in floor that is not walkable becomes a building block (with plinth, cornice and lit windows).
    public void CarveBuildings(R floor, IEnumerable<R> walkable, Func<R, float> heightOf, int seed = 0)
    {
        int i = 0;
        var pieces = Carve(floor, walkable);
        foreach (var s in pieces) _solids.Add((s, heightOf(s)));
        foreach (var (s, h) in _solids)
        {
            Block(s, h, M.Wall(seed + i++), GameLayers.Walls, 0f, "Building");
            Decorate(s, h);
        }
    }

    // Plinth + cornice (visual only) and windows on faces that are open to the street.
    void Decorate(R r, float h)
    {
        if (h < 2.5f || r.W < 0.5f || r.D < 0.5f) return;
        var c = r.Center;
        Cube("Plinth", envRoot, new Vector3(c.x, 0.25f, c.y), new Vector3(r.W + 0.2f, 0.5f, r.D + 0.2f), M.stoneGrey, 0, false, 2f);
        Cube("Cornice", envRoot, new Vector3(c.x, h - 0.1f, c.y), new Vector3(r.W + 0.34f, 0.3f, r.D + 0.34f), M.limestone, 0, false, 2f);

        var rng = new System.Random(Mathf.RoundToInt(c.x * 73.1f + c.y * 19.7f + h));
        void FaceWindows(Vector3 origin, Vector3 along, float len, Vector3 normal)
        {
            if (len < 3.4f) return;
            int count = Mathf.Min(7, Mathf.FloorToInt(len / 4.2f));
            for (int k = 0; k < count; k++)
            {
                float t = (k + 0.5f) / count;
                var rowsY = h >= 6f ? new[] { 2.6f, 5.0f } : new[] { Mathf.Min(2.5f, h * 0.55f) };
                foreach (float y in rowsY)
                {
                    Vector3 p = origin + along * (t * len);
                    Vector3 probe = p + normal * 0.6f;
                    if (InsideOtherSolid(new Vector2(probe.x, probe.z), r)) continue;
                    p.y = y;
                    p += normal * 0.05f;
                    var q = Quaternion.LookRotation(normal);
                    AddBox(_winFrames, p, q, new Vector3(1.05f, 1.5f, 0.1f));
                    bool lit = rng.NextDouble() < 0.5;
                    AddBox(lit ? _winLit : _winDark, p + normal * 0.03f, q, new Vector3(0.75f, 1.15f, 0.06f));
                }
            }
        }
        FaceWindows(new Vector3(r.x0, 0, r.z1), Vector3.right, r.W, Vector3.forward);          // north face
        FaceWindows(new Vector3(r.x0, 0, r.z0), Vector3.right, r.W, Vector3.back);             // south face
        FaceWindows(new Vector3(r.x1, 0, r.z0), Vector3.forward, r.D, Vector3.right);          // east face
        FaceWindows(new Vector3(r.x0, 0, r.z0), Vector3.forward, r.D, Vector3.left);           // west face
    }

    public float floorMaxZ = 72f;

    bool InsideOtherSolid(Vector2 p, R self)
    {
        foreach (var (o, oh) in _solids)
        {
            if (o.x0 == self.x0 && o.z0 == self.z0 && o.x1 == self.x1 && o.z1 == self.z1) continue;
            if (o.Contains(p) && oh > 3f) return true;
        }
        // outside the zone floor there is only the boundary wall
        return p.x < -22f || p.x > 22f || p.y < 0f || p.y > floorMaxZ;
    }

    static void AddBox(List<CombineInstance> list, Vector3 pos, Quaternion rot, Vector3 size)
    {
        list.Add(new CombineInstance { mesh = LowPoly.Box(size, 1f), transform = Matrix4x4.TRS(pos, rot, Vector3.one) });
    }

    void FlushDecor()
    {
        void Emit(List<CombineInstance> list, string name, Material mat)
        {
            if (list.Count == 0) return;
            var go = new GameObject(name);
            go.transform.SetParent(envRoot, false);
            go.AddComponent<MeshFilter>().sharedMesh = LowPoly.Combine(list, name);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            list.Clear();
        }
        Emit(_winFrames, "Windows_Frames", M.windowFrame);
        Emit(_winLit, "Windows_Lit", M.windowLit);
        Emit(_winDark, "Windows_Dark", M.windowDark);
    }

    // ── walls / buildings ───────────────────────────────────────────────────

    // Wall running along an axis ('x' = along X at z=fixedC, 'z' = along Z at x=fixedC) with door gaps + lintels.
    public void WallLine(char axis, float fixedC, float a0, float a1, float thick, float h, Door[] doors,
                         Material mat, int layer = GameLayers.Walls, float doorH = 2.8f, float y0 = 0f)
    {
        var list = new List<Door>(doors ?? new Door[0]);
        list.Sort((p, q) => p.c.CompareTo(q.c));

        R Seg(float s0, float s1) => axis == 'x'
            ? new R(s0, fixedC - thick * 0.5f, s1, fixedC + thick * 0.5f)
            : new R(fixedC - thick * 0.5f, s0, fixedC + thick * 0.5f, s1);

        float cursor = a0;
        foreach (var d in list)
        {
            float d0 = d.c - d.w * 0.5f, d1 = d.c + d.w * 0.5f;
            if (d0 > cursor) Block(Seg(cursor, d0), h, mat, layer, y0, "Wall");
            if (h > doorH) Block(Seg(d0, d1), h - doorH, mat, layer, y0 + doorH, "Lintel");
            cursor = d1;
        }
        if (cursor < a1) Block(Seg(cursor, a1), h, mat, layer, y0, "Wall");
    }

    // Hollow, roofed building with door openings. The roof sits on the Roof layer (blocks guards' sight from
    // rooftop sentries) and fades out when Jonathan is inside (RoofFader) so the camera can see in.
    public void Hollow(R box, float h, float t, Door[] doors, Material wallMat, int seed = 0)
    {
        Door[] On(char side)
        {
            var l = new List<Door>();
            foreach (var d in doors) if (d.side == side) l.Add(d);
            return l.ToArray();
        }
        WallLine('x', box.z0 + t * 0.5f, box.x0, box.x1, t, h, On('S'), wallMat);
        WallLine('x', box.z1 - t * 0.5f, box.x0, box.x1, t, h, On('N'), wallMat);
        WallLine('z', box.x0 + t * 0.5f, box.z0 + t, box.z1 - t, t, h, On('W'), wallMat);
        WallLine('z', box.x1 - t * 0.5f, box.z0 + t, box.z1 - t, t, h, On('E'), wallMat);

        var roof = Block(box, 0.4f, M.roofTile, GameLayers.Roof, h, "Roof");

        var c = box.Center;
        var trig = new GameObject("RoofTrigger");
        trig.transform.SetParent(triggersRoot, false);
        trig.transform.position = new Vector3(c.x, h * 0.5f, c.y);
        var bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(box.W - t * 2f, h, box.D - t * 2f);
        var rf = trig.AddComponent<RoofFader>();
        rf.roofRenderers = new[] { roof.GetComponent<Renderer>() };
    }

    public void LowCover(float x, float z, float w, float d, float h = 1.1f, Material mat = null, string name = "Cover")
    {
        var b = Block(new R(x - w * 0.5f, z - d * 0.5f, x + w * 0.5f, z + d * 0.5f), h, mat != null ? mat : M.crateWood, GameLayers.Walls, 0f, name);
        if (b != null) b.transform.SetParent(propsRoot, true);
    }

    public void Pillar(float x, float z, float h = 4f, float size = 1f, Material mat = null)
    {
        var b = Block(new R(x - size * 0.5f, z - size * 0.5f, x + size * 0.5f, z + size * 0.5f), h, mat != null ? mat : M.limestone, GameLayers.Walls, 0f, "Pillar");
        if (b != null) b.transform.SetParent(propsRoot, true);
    }

    // Market cart: low cover box + wheels + shafts
    public void Cart(float x, float z, bool alongX = true)
    {
        float w = alongX ? 2.6f : 1.3f, d = alongX ? 1.3f : 2.6f;
        LowCover(x, z, w, d, 1.05f, M.woodDark, "Cart");
        Vector3 wa = alongX ? new Vector3(0, 0, 0.72f) : new Vector3(0.72f, 0, 0);
        Quaternion rot = alongX ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(0, 0, 90);
        for (int s = -1; s <= 1; s += 2)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(propsRoot, false);
            wheel.transform.position = new Vector3(x, 0.4f, z) + wa * s;
            wheel.transform.rotation = rot;
            wheel.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
            wheel.GetComponent<Renderer>().sharedMaterial = M.woodDark;
            UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());
        }
    }

    // Market stall: counter (low cover) + posts + cloth awning (visual only)
    public void Stall(float x, float z, float w, float d, Material cloth)
    {
        LowCover(x, z, w, d, 1.15f, M.crateWood, "Stall");
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Cube("StallPost", propsRoot, new Vector3(x + sx * (w * 0.5f - 0.1f), 1.3f, z + sz * (d * 0.5f - 0.1f)), new Vector3(0.12f, 2.6f, 0.12f), M.woodDark, 0, false);
        var awn = Cube("Awning", propsRoot, new Vector3(x, 2.65f, z), new Vector3(w + 0.6f, 0.1f, d + 0.8f), cloth, 0, false, 1.6f);
        awn.isStatic = false;
        var sw = awn.AddComponent<Sway>(); sw.amplitude = 1.2f; sw.speed = 0.9f; sw.axis = Vector3.forward;
        // wares
        Cube("Wares", propsRoot, new Vector3(x, 1.3f, z), new Vector3(w * 0.7f, 0.25f, d * 0.6f), M.clothOchre, 0, false);
    }

    public void Well(float x, float z)
    {
        Block(new R(x - 1.6f, z - 1.6f, x + 1.6f, z + 1.6f), 1.1f, M.stoneGrey, GameLayers.Walls, 0f, "Well");
        var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        water.name = "WellWater";
        water.transform.SetParent(propsRoot, false);
        water.transform.position = new Vector3(x, 1.11f, z);
        water.transform.localScale = new Vector3(2.4f, 0.01f, 2.4f);
        water.GetComponent<Renderer>().sharedMaterial = M.darkStone;
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
        Cube("WellPostL", propsRoot, new Vector3(x - 1.2f, 2.0f, z), new Vector3(0.15f, 2f, 0.15f), M.woodDark, 0, false);
        Cube("WellPostR", propsRoot, new Vector3(x + 1.2f, 2.0f, z), new Vector3(0.15f, 2f, 0.15f), M.woodDark, 0, false);
        Cube("WellBeam", propsRoot, new Vector3(x, 3.0f, z), new Vector3(2.7f, 0.15f, 0.15f), M.woodDark, 0, false);
    }

    // Ramp from `bottom` up to `top` (points on the walking surface), `width` wide. Walkable (Walls layer).
    public GameObject Ramp(Vector3 bottom, Vector3 top, float width, Material mat, string name = "Ramp")
    {
        Vector3 dir = top - bottom;
        float len = dir.magnitude + 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        const float th = 0.3f;
        Vector3 center = (bottom + top) * 0.5f + dir.normalized * 0.25f - (rot * Vector3.up) * (th * 0.5f);
        var go = Cube(name, envRoot, center, new Vector3(width, th, len), mat, GameLayers.Walls);
        go.transform.rotation = rot;
        return go;
    }

    // U-shaped booth (3 x 3 interior) that is also a hiding spot. `facing` = the open side.
    public void Alcove(float cx, float cz, char facing, float h = 3f, bool torch = false)
    {
        Material m = M.sandB;
        R back, s1, s2;
        switch (facing)
        {
            case 'E': back = new R(cx - 2f, cz - 2f, cx - 1.5f, cz + 2f); s1 = new R(cx - 1.5f, cz + 1.5f, cx + 1.5f, cz + 2f); s2 = new R(cx - 1.5f, cz - 2f, cx + 1.5f, cz - 1.5f); break;
            case 'W': back = new R(cx + 1.5f, cz - 2f, cx + 2f, cz + 2f); s1 = new R(cx - 1.5f, cz + 1.5f, cx + 1.5f, cz + 2f); s2 = new R(cx - 1.5f, cz - 2f, cx + 1.5f, cz - 1.5f); break;
            case 'N': back = new R(cx - 2f, cz - 2f, cx + 2f, cz - 1.5f); s1 = new R(cx + 1.5f, cz - 1.5f, cx + 2f, cz + 1.5f); s2 = new R(cx - 2f, cz - 1.5f, cx - 1.5f, cz + 1.5f); break;
            default:  back = new R(cx - 2f, cz + 1.5f, cx + 2f, cz + 2f); s1 = new R(cx + 1.5f, cz - 1.5f, cx + 2f, cz + 1.5f); s2 = new R(cx - 2f, cz - 1.5f, cx - 1.5f, cz + 1.5f); break;
        }
        Block(back, h, m, GameLayers.Walls, 0f, "AlcoveBack");
        Block(s1, h, m, GameLayers.Walls, 0f, "AlcoveSide");
        Block(s2, h, m, GameLayers.Walls, 0f, "AlcoveSide");
        HidingSpot(cx, cz);
        if (torch)
        {
            float tx = cx + (facing == 'E' ? 2.6f : facing == 'W' ? -2.6f : 0f);
            float tz = cz + (facing == 'N' ? 2.6f : facing == 'S' ? -2.6f : 0f);
            PlaceTorch(tx, tz);
        }
    }

    // ── placed prefabs ──────────────────────────────────────────────────────

    GameObject Inst(GameObject prefab, Transform parent, Vector3 pos, float yaw = 0f)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        return go;
    }

    public void PlaceTorch(float x, float z, bool shadows = false)
    {
        var go = Inst(P.torch, lightsRoot, new Vector3(x, 0, z));
        if (shadows)
        {
            var l = go.GetComponentInChildren<Light>();
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.9f;
        }
    }

    // Open fire pit: bright emissive fire + a big pool of light (the "environmental hazard" that illuminates the player).
    public void FirePit(float x, float z)
    {
        var go = new GameObject("FirePit");
        go.transform.SetParent(lightsRoot, false);
        go.transform.position = new Vector3(x, 0, z);
        PrefabFactory.Prim(PrimitiveType.Cylinder, "Ring", go.transform, new Vector3(0, 0.18f, 0), new Vector3(1.6f, 0.18f, 1.6f), M.stoneGrey);
        PrefabFactory.Prim(PrimitiveType.Cylinder, "Coals", go.transform, new Vector3(0, 0.34f, 0), new Vector3(1.2f, 0.02f, 1.2f), M.fireEmissive);
        var flame = PrefabFactory.Prim(PrimitiveType.Sphere, "Fire", go.transform, new Vector3(0, 0.8f, 0), new Vector3(0.8f, 1.1f, 0.8f), M.flame);
        var lg = new GameObject("FireLight");
        lg.transform.SetParent(go.transform, false);
        lg.transform.localPosition = new Vector3(0, 1.2f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.2f); l.range = 12f; l.intensity = 6f;
        var t = go.AddComponent<Torch>();
        t.torchLight = l; t.flame = flame.transform; t.lightRadius = 9f; t.flicker = 0.3f;
        PrefabFactory.AddFlameFx(go.transform, M, new Vector3(0, 0.55f, 0), 2.6f);
        PrefabFactory.AddSource(go, A.torchCrackle, MX.ambience, true, true, 0.7f, 16f);
        // the ring is solid (low cover, an obstacle to path around)
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(1.6f, 0.9f, 1.6f);
        col.center = new Vector3(0, 0.45f, 0);
        go.layer = GameLayers.Walls;
    }

    public void Pot(float x, float z, int stones = 1, float y = 0f)
    {
        var go = Inst(P.clayPot, propsRoot, new Vector3(x, y, z), UnityEngine.Random.Range(0f, 360f));
        go.GetComponent<StonePickup>().stoneCount = stones;
    }

    public void Scroll(float x, float z, StoryBeatData lore, float y = 0f)
    {
        var go = Inst(P.loreScroll, propsRoot, new Vector3(x, y, z), UnityEngine.Random.Range(0f, 360f));
        go.GetComponent<LoreScrollPickup>().lore = lore;
    }

    public void HidingSpot(float x, float z, float w = 3f, float d = 3f)
    {
        var go = Inst(P.hidingSpot, triggersRoot, new Vector3(x, 0, z));
        go.GetComponent<BoxCollider>().size = new Vector3(w, 2.2f, d);
        go.transform.Find("Marker").localScale = new Vector3(w - 0.4f, 0.01f, d - 0.4f);
    }

    public ZoneExit Exit(float x, float z, float width)
    {
        var go = Inst(P.zoneExit, triggersRoot, new Vector3(x, 0, z));
        go.GetComponent<BoxCollider>().size = new Vector3(width, 3f, 3f);
        float px = width * 0.5f + 0.6f;
        go.transform.Find("PillarL").localPosition = new Vector3(-px, 1.9f, 0);
        go.transform.Find("PillarR").localPosition = new Vector3(px, 1.9f, 0);
        var lintel = go.transform.Find("Lintel");
        lintel.localScale = new Vector3(width + 2.6f, 0.5f, 1f);
        go.transform.Find("LockBarrier").localScale = new Vector3(width, 3.2f, 0.12f);
        PlaceTorch(x - px - 1.2f, z - 1f);
        PlaceTorch(x + px + 1.2f, z - 1f);
        return go.GetComponent<ZoneExit>();
    }

    public ZoneEntry Entry(string display, string subtitle, Vector2 jonathanXZ, float yaw, Vector2[] runWaypoints, float difficulty = 1f, float ambient = 0.35f)
    {
        var go = new GameObject("ZoneEntry");
        go.transform.SetParent(root, false);
        entry = go.AddComponent<ZoneEntry>();
        entry.displayName = display;
        entry.subtitle = subtitle;
        entry.difficulty = difficulty;
        entry.ambientVisibility = ambient;

        Vector3 jp = new Vector3(jonathanXZ.x, 0.05f, jonathanXZ.y);
        Quaternion rot = Quaternion.Euler(0, yaw, 0);
        var j = new GameObject("JonathanSpawn").transform;
        j.SetParent(go.transform, false);
        j.SetPositionAndRotation(jp, rot);
        var d = new GameObject("DavidSpawn").transform;
        d.SetParent(go.transform, false);
        d.SetPositionAndRotation(jp - rot * Vector3.forward * 1.8f, rot);
        entry.jonathanSpawn = j;
        entry.davidSpawn = d;

        var wps = new List<Transform>();
        foreach (var w in runWaypoints)
        {
            var wp = Inst(P.runWaypoint, propsRoot, new Vector3(w.x, 0, w.y));
            wp.name = "RunWaypoint";
            wps.Add(wp.transform);
        }
        entry.davidRunWaypoints = wps.ToArray();
        return entry;
    }

    public GuardFSM Guard(GuardType type, Vector3 pos, float yaw, Vector2[] path = null, bool pingPong = false,
                          float gain = 1f, Action<GuardFSM> tweak = null)
    {
        var prefab = type == GuardType.Sentry ? P.guardSentry : type == GuardType.Commander ? P.guardCommander : P.guardPatrol;
        var go = Inst(prefab, guardsRoot, pos, yaw);
        go.name = $"{type}_{++_count}";
        var fsm = go.GetComponent<GuardFSM>();
        fsm.GetComponent<GuardVision>().gainMultiplier *= gain;

        if (path != null && path.Length > 0)
        {
            var pp = new GameObject($"Path_{go.name}").AddComponent<PatrolPath>();
            pp.transform.SetParent(pathsRoot, false);
            var wps = new Transform[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var wp = new GameObject($"WP{i}").transform;
                wp.SetParent(pp.transform, false);
                wp.position = new Vector3(path[i].x, 0f, path[i].y);
                wps[i] = wp;
            }
            pp.waypoints = wps;
            pp.pingPong = pingPong;
            fsm.patrolPath = pp;

            Vector3 start = new Vector3(path[0].x, pos.y, path[0].y);
            Vector3 look = path.Length > 1 ? new Vector3(path[1].x, pos.y, path[1].y) - start : Vector3.forward;
            go.transform.SetPositionAndRotation(start, Quaternion.LookRotation(new Vector3(look.x, 0, look.z)));
        }
        tweak?.Invoke(fsm);
        return fsm;
    }

    // A civilian. path (optional) makes a Wander NPC; otherwise it stands (Stand) or chats with its partner.
    public CivilianNPC Npc(int variant, Vector3 pos, float yaw, CivilianNPC.Mode mode = CivilianNPC.Mode.Stand,
                           Vector2[] path = null, bool pingPong = true)
    {
        var go = Inst(P.npc[Mathf.Abs(variant) % P.npc.Length], guardsRoot, pos, yaw);
        go.name = "Civilian_" + (++_count);
        var npc = go.GetComponent<CivilianNPC>();
        npc.mode = mode;
        if (path != null && path.Length > 1)
        {
            var holder = new GameObject("NpcPath_" + go.name).transform;
            holder.SetParent(pathsRoot, false);
            var wps = new Transform[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var wp = new GameObject("WP" + i).transform;
                wp.SetParent(holder, false);
                wp.position = new Vector3(path[i].x, 0f, path[i].y);
                wps[i] = wp;
            }
            npc.path = wps;
            npc.pingPong = pingPong;
            npc.mode = CivilianNPC.Mode.Wander;
            go.transform.position = new Vector3(path[0].x, pos.y, path[0].y);
        }
        return npc;
    }

    // Two townsfolk talking; each faces the other.
    public void NpcPair(int variantA, Vector2 a, int variantB, Vector2 b)
    {
        Vector3 pa = new Vector3(a.x, 0f, a.y), pb = new Vector3(b.x, 0f, b.y);
        var na = Npc(variantA, pa, Quaternion.LookRotation(pb - pa).eulerAngles.y, CivilianNPC.Mode.Chat);
        var nb = Npc(variantB, pb, Quaternion.LookRotation(pa - pb).eulerAngles.y, CivilianNPC.Mode.Chat);
        na.partner = nb; nb.partner = na;
    }

    public void DogPen(R niche, string label = "DogPen")
    {
        var c = niche.Center;
        var pen = new GameObject(label).transform;
        pen.SetParent(propsRoot, false);

        // fence (visual + collision)
        Block(new R(niche.x0, niche.z1 - 0.2f, niche.x1, niche.z1), 1.4f, M.woodDark, GameLayers.Walls, 0f, "FenceBack");
        // two dogs
        for (int i = 0; i < 2; i++)
        {
            var dog = new GameObject("Dog").transform;
            dog.SetParent(pen, false);
            dog.position = new Vector3(c.x + (i == 0 ? -0.7f : 0.8f), 0, c.y + 0.4f);
            dog.rotation = Quaternion.Euler(0, i == 0 ? 30 : -20, 0);
            PrefabFactory.Prim(PrimitiveType.Cube, "Body", dog, new Vector3(0, 0.4f, 0), new Vector3(0.36f, 0.32f, 0.8f), M.dogFur);
            PrefabFactory.Prim(PrimitiveType.Sphere, "Head", dog, new Vector3(0, 0.55f, 0.5f), Vector3.one * 0.3f, M.dogFur);
            PrefabFactory.Prim(PrimitiveType.Cube, "Tail", dog, new Vector3(0, 0.55f, -0.5f), new Vector3(0.08f, 0.08f, 0.3f), M.dogFur);
        }

        var trig = new GameObject("DogTrigger");
        trig.transform.SetParent(triggersRoot, false);
        trig.transform.position = new Vector3(c.x, 1f, c.y);
        var bc = trig.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(niche.W + 0.5f, 2f, niche.D + 1.5f);
        var dt = trig.AddComponent<DogTrigger>();
        dt.barkSource = PrefabFactory.AddSource(trig, null, MX.sfx, false, false, 1f, 50f);
        dt.barkClip = A.bark;
    }

    // ── trees, banners, fireflies ───────────────────────────────────────────

    public void Tree(float x, float z, bool cypress = false, float scale = 1f)
    {
        var root = new GameObject(cypress ? "Cypress" : "Tree").transform;
        root.SetParent(propsRoot, false);
        root.position = new Vector3(x, 0, z);
        root.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
        root.localScale = Vector3.one * scale;
        Mesh trunk = LowPoly.Cached("tree_trunk", () => LowPoly.Frustum(6, 0.24f, 0.14f, 2.3f));
        Mesh blob = LowPoly.Cached("tree_blob", () => LowPoly.Ico(1f, 1));
        Mesh cone = LowPoly.Cached("tree_cone", () => LowPoly.Frustum(7, 1.0f, 0f, 4.6f, 0f, true, true));
        void Part(string n, Mesh m, Material mat, Vector3 p, Vector3 sc)
        {
            var go = new GameObject(n);
            go.transform.SetParent(root, false);
            go.transform.localPosition = p; go.transform.localScale = sc;
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
        }
        if (cypress)
        {
            Part("Trunk", trunk, M.woodDark, Vector3.zero, new Vector3(0.7f, 0.5f, 0.7f));
            Part("Foliage", cone, M.cypress, new Vector3(0, 0.9f, 0), new Vector3(1f, 1f, 1f));
        }
        else
        {
            Part("Trunk", trunk, M.woodDark, Vector3.zero, Vector3.one);
            Part("CanopyA", blob, M.leafA, new Vector3(0, 3.0f, 0), new Vector3(1.7f, 1.25f, 1.7f));
            Part("CanopyB", blob, M.leafB, new Vector3(0.7f, 2.55f, 0.3f), new Vector3(1.05f, 0.85f, 1.05f));
            Part("CanopyC", blob, M.leafB, new Vector3(-0.6f, 2.7f, -0.4f), new Vector3(1.0f, 0.8f, 1.0f));
        }
        var col = root.gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.28f; col.height = 2.4f; col.center = new Vector3(0, 1.2f, 0);
        root.gameObject.layer = GameLayers.Walls;
    }

    // Hanging cloth banner on a wall face; sways gently. facing = outward normal of the wall.
    public void Banner(float x, float z, Vector3 facing, float topY, float width = 1.0f, float length = 2.6f)
    {
        var pivot = new GameObject("Banner").transform;
        pivot.SetParent(propsRoot, false);
        pivot.position = new Vector3(x, topY, z) + facing * 0.12f;
        pivot.rotation = Quaternion.LookRotation(facing);
        Cube("Cloth", pivot, pivot.position + Vector3.down * length * 0.5f, new Vector3(width, length, 0.04f), M.banner, 0, false, 2f).transform.SetParent(pivot, true);
        Cube("Trim", pivot, pivot.position + Vector3.down * (length - 0.05f), new Vector3(width, 0.12f, 0.06f), M.gold, 0, false, 1f).transform.SetParent(pivot, true);
        Cube("Emblem", pivot, pivot.position + Vector3.down * length * 0.42f + facing * 0.03f, new Vector3(width * 0.4f, width * 0.4f, 0.03f), M.gold, 0, false, 1f).transform.SetParent(pivot, true);
        Cube("Rod", pivot, pivot.position + Vector3.up * 0.05f, new Vector3(width + 0.3f, 0.08f, 0.08f), M.woodDark, 0, false, 1f).transform.SetParent(pivot, true);
        var sw = pivot.gameObject.AddComponent<Sway>();
        sw.amplitude = 3.5f; sw.speed = 1.1f; sw.axis = Vector3.right;
    }

    // Slow drifting fireflies (soft additive specks) over an area.
    public void Fireflies(R area, int count = 30)
    {
        var go = new GameObject("Fireflies");
        go.transform.SetParent(propsRoot, false);
        var c = area.Center;
        go.transform.position = new Vector3(c.x, 1.8f, c.y);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f, 1f), new Color(0.7f, 1f, 0.5f, 1f));
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = count / 8f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(area.W, 3f, area.D);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.25f; noise.scrollSpeed = 0.3f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
        col.color = g;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = M.fireflyParticle;
    }

    public void Ambience(Vector3[] positions)
    {
        var go = new GameObject("ZoneAmbience");
        go.transform.SetParent(root, false);
        var za = go.AddComponent<ZoneAmbience>();
        var srcs = new List<AudioSource>();
        foreach (var p in positions)
        {
            var e = new GameObject("Emitter");
            e.transform.SetParent(go.transform, false);
            e.transform.position = p;
            srcs.Add(PrefabFactory.AddSource(e, null, MX.ambience, false, false, 0.7f, 70f));
        }
        za.emitters = srcs.ToArray();
        za.clips = new[] { A.bark, A.clatter };
    }

    public void Moonlight(float intensity = 0.55f)
    {
        var go = new GameObject("Moonlight");
        go.transform.SetParent(lightsRoot, false);
        go.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.60f, 0.70f, 1.0f);       // cool moon
        l.intensity = intensity;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.85f;
    }

    // ── nav mesh + save ─────────────────────────────────────────────────────

    public void BakeNavMesh()
    {
        FlushDecor();
        var go = new GameObject("NavMesh");
        go.transform.SetParent(root, false);
        var s = go.AddComponent<NavMeshSurface>();
        s.collectObjects = CollectObjects.All;
        s.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        s.layerMask = GameLayers.Mask(GameLayers.Walls, GameLayers.GroundStone, GameLayers.GroundDirt, GameLayers.GroundWood);
        s.BuildNavMesh();

        string path = $"Assets/Scenes/{sceneName}_NavMesh.asset";
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(path) != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(s.navMeshData, path);
        EditorUtility.SetDirty(s);
    }

    // Logs and returns whether NavMesh paths exist between successive waypoints.
    public bool VerifyRoute(string label, params Vector2[] points)
    {
        bool ok = true;
        float total = 0f;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            Vector3 a = Snap(points[i]), b = Snap(points[i + 1]);
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path);
            if (!found || path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError($"[Builder] {sceneName}: route '{label}' leg {i} {points[i]} -> {points[i + 1]} has NO complete path ({path.status}).");
                ok = false;
                continue;
            }
            float len = 0f;
            for (int k = 0; k + 1 < path.corners.Length; k++) len += Vector3.Distance(path.corners[k], path.corners[k + 1]);
            total += len;
        }
        if (ok) Debug.Log($"[Builder] {sceneName}: route '{label}' OK ({total:F0} m).");
        return ok;
    }

    // Same as VerifyRoute but with explicit heights (rooftop / terrace routes).
    public bool VerifyRoute3D(string label, params Vector3[] points)
    {
        bool ok = true;
        float total = 0f;
        for (int i = 0; i + 1 < points.Length; i++)
        {
            Vector3 a = SnapV(points[i]), b = SnapV(points[i + 1]);
            var path = new NavMeshPath();
            bool found = NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path);
            if (!found || path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError($"[Builder] {sceneName}: route '{label}' leg {i} {points[i]} -> {points[i + 1]} has NO complete path ({path.status}).");
                ok = false;
                continue;
            }
            for (int q = 0; q + 1 < path.corners.Length; q++) total += Vector3.Distance(path.corners[q], path.corners[q + 1]);
        }
        if (ok) Debug.Log($"[Builder] {sceneName}: route '{label}' OK ({total:F0} m).");
        return ok;
    }

    static Vector3 SnapV(Vector3 v)
    {
        return NavMesh.SamplePosition(v, out NavMeshHit hit, 1.5f, NavMesh.AllAreas) ? hit.position : v;
    }

    static Vector3 Snap(Vector2 p)
    {
        var v = new Vector3(p.x, 0.1f, p.y);
        return NavMesh.SamplePosition(v, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : v;
    }

    public void Save()
    {
        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
    }
}
