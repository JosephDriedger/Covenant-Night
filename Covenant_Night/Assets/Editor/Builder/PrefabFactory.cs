using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using TMPro;

// Builds every gameplay prefab from primitives (no custom modelling): Jonathan, David, three guard types,
// gate NPCs, stone, clay pot, torch, hiding spot, zone exit, run-waypoint marker.
public class Prefabs
{
    public GameObject jonathan, david, guardPatrol, guardSentry, guardCommander, gateNpc, saul;
    public GameObject stone, decoy, clayPot, loreScroll, torch, hidingSpot, zoneExit, runWaypoint, thrownSpear;
    public GameObject[] npc = new GameObject[6];
}

public static class PrefabFactory
{
    const string Dir = "Assets/Prefabs";

    // ── helpers ─────────────────────────────────────────────────────────────

    public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                                  Material mat, bool keepCollider = false, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    public static AudioSource AddSource(GameObject go, AudioClip clip, AudioMixerGroup group, bool loop, bool playOnAwake,
                                        float volume = 1f, float maxDistance = 15f, float spatial = 1f)
    {
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.outputAudioMixerGroup = group;
        s.loop = loop;
        s.playOnAwake = playOnAwake;
        s.volume = volume;
        s.spatialBlend = spatial;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1.5f;
        s.maxDistance = maxDistance;
        return s;
    }

    static GameObject Save(GameObject go, string name)
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        string path = $"{Dir}/{name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ── main entry ──────────────────────────────────────────────────────────

    public static Prefabs Build(Mats m, AudioLib a, MixerRefs mx)
    {
        var p = new Prefabs();
        p.stone       = BuildStone(m, a, mx);
        p.decoy       = BuildDecoy(m, a, mx);
        p.torch       = BuildTorch(m, a, mx);
        p.clayPot     = BuildClayPot(m, a, mx);
        p.loreScroll  = BuildLoreScroll(m, a);
        p.hidingSpot  = BuildHidingSpot(m);
        p.zoneExit    = BuildZoneExit(m);
        p.runWaypoint = BuildRunWaypoint(m);
        p.jonathan    = BuildJonathan(m, a, mx, p.stone, p.decoy);
        p.david       = BuildDavid(m, a, mx);
        p.guardPatrol    = BuildGuard(GuardType.Patrol,    "Guard_Patrol",    m);
        p.guardSentry    = BuildGuard(GuardType.Sentry,    "Guard_Sentry",    m);
        p.guardCommander = BuildGuard(GuardType.Commander, "Guard_Commander", m);
        p.gateNpc        = BuildGateNpc(m);
        p.saul           = BuildSaul(m);
        p.thrownSpear    = BuildThrownSpear(m);
        for (int i = 0; i < p.npc.Length; i++) p.npc[i] = BuildNpc(i, m);
        AssetDatabase.SaveAssets();
        return p;
    }

    // ── props ───────────────────────────────────────────────────────────────

    static GameObject BuildStone(Mats m, AudioLib a, MixerRefs mx)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Stone";
        go.transform.localScale = Vector3.one * 0.18f;
        go.GetComponent<Renderer>().sharedMaterial = m.stoneGrey;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var stone = go.AddComponent<Stone>();
        stone.audioSource = AddSource(go, null, mx.sfx, false, false, 1f, 25f);
        stone.clatter = a.clatter;
        return Save(go, "Stone");
    }

    static GameObject BuildDecoy(Mats m, AudioLib a, MixerRefs mx)
    {
        var root = new GameObject("Decoy");
        Prim(PrimitiveType.Sphere, "Pouch", root.transform, new Vector3(0, 0.09f, 0), new Vector3(0.22f, 0.16f, 0.22f), m.leather);
        Prim(PrimitiveType.Cylinder, "Tie", root.transform, new Vector3(0, 0.19f, 0), new Vector3(0.05f, 0.03f, 0.05f), m.straw);

        var decoy = root.AddComponent<Decoy>();
        decoy.audioSource = AddSource(root, null, mx.sfx, false, false, 0.9f, 20f);
        decoy.whistle = a.whistle;
        return Save(root, "Decoy");
    }

    static GameObject BuildTorch(Mats m, AudioLib a, MixerRefs mx)
    {
        var root = new GameObject("Torch");
        Prim(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0, 1.05f, 0), new Vector3(0.12f, 1.05f, 0.12f), m.woodDark);
        Prim(PrimitiveType.Cylinder, "Bowl", root.transform, new Vector3(0, 2.15f, 0), new Vector3(0.36f, 0.08f, 0.36f), m.metal);
        var flame = Prim(PrimitiveType.Sphere, "Flame", root.transform, new Vector3(0, 2.4f, 0), new Vector3(0.22f, 0.34f, 0.22f), m.flame);

        var lightGo = new GameObject("Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 2.5f, 0);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.62f, 0.28f);      // ~2200K amber
        l.intensity = 4f;
        l.range = 9f;
        l.shadows = LightShadows.None;

        var t = root.AddComponent<Torch>();
        t.torchLight = l;
        t.flame = flame.transform;
        t.lightRadius = 8f;

        AddFlameFx(root.transform, m, new Vector3(0, 2.42f, 0), 1f);
        AddSource(root, a.torchCrackle, mx.ambience, true, true, 0.45f, 12f);   // spatial blend 1.0
        return Save(root, "Torch");
    }

    static GameObject BuildClayPot(Mats m, AudioLib a, MixerRefs mx)
    {
        var root = new GameObject("ClayPot");
        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        Prim(PrimitiveType.Sphere, "Body", vis.transform, new Vector3(0, 0.35f, 0), new Vector3(0.6f, 0.55f, 0.6f), m.clay);
        Prim(PrimitiveType.Cylinder, "Neck", vis.transform, new Vector3(0, 0.66f, 0), new Vector3(0.3f, 0.07f, 0.3f), m.clay);
        Prim(PrimitiveType.Sphere, "S1", vis.transform, new Vector3(0.05f, 0.78f, 0.03f), Vector3.one * 0.12f, m.stoneGrey);
        Prim(PrimitiveType.Sphere, "S2", vis.transform, new Vector3(-0.06f, 0.77f, -0.02f), Vector3.one * 0.1f, m.stoneGrey);

        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.9f;
        col.center = new Vector3(0, 0.5f, 0);

        var pick = root.AddComponent<StonePickup>();
        pick.stoneCount = 1;
        pick.bobTarget = vis.transform;
        pick.pickupClip = a.pickup;
        pick.audioSource = null;
        return Save(root, "ClayPot");
    }

    static GameObject BuildLoreScroll(Mats m, AudioLib a)
    {
        var root = new GameObject("LoreScroll");
        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        Prim(PrimitiveType.Cylinder, "Scroll", vis.transform, new Vector3(0, 0.14f, 0), new Vector3(0.09f, 0.16f, 0.09f), m.straw, false, new Vector3(0, 0, 90));
        Prim(PrimitiveType.Cylinder, "Tie", vis.transform, new Vector3(0, 0.14f, 0), new Vector3(0.1f, 0.02f, 0.1f), m.leather, false, new Vector3(0, 0, 90));

        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.7f;
        col.center = new Vector3(0, 0.2f, 0);

        var pick = root.AddComponent<LoreScrollPickup>();
        pick.bobTarget = vis.transform;
        pick.pickupClip = a.pickup;
        pick.audioSource = null;
        return Save(root, "LoreScroll");
    }

    static GameObject BuildHidingSpot(Mats m)
    {
        var root = new GameObject("HidingSpot");
        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.8f, 2.2f, 2.8f);
        box.center = new Vector3(0, 1.1f, 0);

        Prim(PrimitiveType.Cylinder, "Marker", root.transform, new Vector3(0, 0.03f, 0), new Vector3(2.4f, 0.01f, 2.4f), m.hideDisc);

        var lg = new GameObject("MarkerLight");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 0.5f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.35f, 0.55f, 1f);
        l.range = 3.5f;
        l.intensity = 0.4f;

        var hs = root.AddComponent<HidingSpot>();
        hs.markerLight = l;
        return Save(root, "HidingSpot");
    }

    static GameObject BuildZoneExit(Mats m)
    {
        var root = new GameObject("ZoneExit");
        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(8f, 3f, 3f);
        box.center = new Vector3(0, 1.5f, 0);

        Prim(PrimitiveType.Cube, "PillarL", root.transform, new Vector3(-4.6f, 1.9f, 0), new Vector3(0.9f, 3.8f, 0.9f), m.limestone);
        Prim(PrimitiveType.Cube, "PillarR", root.transform, new Vector3(4.6f, 1.9f, 0), new Vector3(0.9f, 3.8f, 0.9f), m.limestone);
        Prim(PrimitiveType.Cube, "Lintel",  root.transform, new Vector3(0, 4.0f, 0), new Vector3(10f, 0.5f, 1.0f), m.limestone);

        var lockGo = Prim(PrimitiveType.Cube, "LockBarrier", root.transform, new Vector3(0, 1.6f, 0.4f), new Vector3(8f, 3.2f, 0.12f), m.lockRed);
        lockGo.SetActive(false);

        // Wayfinding: a pale shaft of light rising from the gateway, visible over the rooftops from anywhere in
        // the zone, and a glowing threshold across the opening itself.
        var beacon = Prim(PrimitiveType.Cylinder, "Beacon", root.transform, new Vector3(0, 15f, 0.6f), new Vector3(1.8f, 15f, 1.8f), m.exitBeam);
        var core   = Prim(PrimitiveType.Cylinder, "BeaconCore", root.transform, new Vector3(0, 15f, 0.6f), new Vector3(0.7f, 15f, 0.7f), m.exitBeam);
        var sill   = Prim(PrimitiveType.Cube, "Threshold", root.transform, new Vector3(0, 0.03f, 0), new Vector3(8f, 0.02f, 1.6f), m.exitGlow);
        foreach (var r in new[] { beacon, core, sill })
        {
            var rend = r.GetComponent<Renderer>();
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        var lg = new GameObject("ExitGlow");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 3.3f, -0.5f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(0.85f, 0.93f, 1f); l.range = 11f; l.intensity = 2.2f;

        var ze = root.AddComponent<ZoneExit>();
        ze.lockVisual = lockGo;
        return Save(root, "ZoneExit");
    }

    static GameObject BuildRunWaypoint(Mats m)
    {
        var root = new GameObject("RunWaypoint");
        Prim(PrimitiveType.Cylinder, "Ring", root.transform, new Vector3(0, 0.04f, 0), new Vector3(1.4f, 0.01f, 1.4f), m.waypointRing);
        Prim(PrimitiveType.Cylinder, "Beam", root.transform, new Vector3(0, 1.1f, 0), new Vector3(0.1f, 1.1f, 0.1f), m.waypointBeam);
        var lg = new GameObject("Glow");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 0.6f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.85f, 0.4f); l.range = 3f; l.intensity = 0.6f;
        return Save(root, "RunWaypoint");
    }

    // ── characters ──────────────────────────────────────────────────────────

    static GameObject BuildJonathan(Mats m, AudioLib a, MixerRefs mx, GameObject stonePrefab, GameObject decoyPrefab)
    {
        var root = new GameObject("Jonathan");
        root.tag = "Player";
        root.layer = GameLayers.Characters;

        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.4f; cc.slopeLimit = 50f; cc.skinWidth = 0.05f; cc.minMoveDistance = 0f;

        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        var v = vis.transform;
        HumanoidBuilder.Build(v, Role.Jonathan, m);

        var cam = new GameObject("CameraTarget");
        cam.transform.SetParent(root.transform, false);
        cam.transform.localPosition = new Vector3(0, 1.53f, 0);
        var origin = new GameObject("ThrowOrigin");
        origin.transform.SetParent(root.transform, false);
        origin.transform.localPosition = new Vector3(0.25f, 1.3f, 0.45f);

        var pc = root.AddComponent<PlayerController>();
        pc.cameraTarget = cam.transform;
        pc.footstepSource = AddSource(root, null, mx.sfx, false, false, 0.7f, 14f);

        var fs = root.AddComponent<FootstepAudio>();
        fs.audioSource = pc.footstepSource;
        fs.defaultClips = a.stepStone;
        fs.surfaces = new[]
        {
            new FootstepAudio.SurfaceAudio { label = "Dirt",  layer = GameLayers.Mask(GameLayers.GroundDirt),  clips = a.stepDirt  },
            new FootstepAudio.SurfaceAudio { label = "Wood",  layer = GameLayers.Mask(GameLayers.GroundWood),  clips = a.stepWood  },
            new FootstepAudio.SurfaceAudio { label = "Stone", layer = GameLayers.Mask(GameLayers.GroundStone, GameLayers.Walls), clips = a.stepStone },
        };

        var ab = root.AddComponent<PlayerAbilities>();
        ab.controller = pc;
        ab.stonePrefab = stonePrefab;
        ab.decoyPrefab = decoyPrefab;
        ab.throwOrigin = origin.transform;
        ab.wallLayer = GameLayers.StaticSight;

        var anim = root.AddComponent<ProceduralCharacterAnim>();
        anim.visual = v;
        return Save(root, "Jonathan");
    }

    static GameObject BuildDavid(Mats m, AudioLib a, MixerRefs mx)
    {
        var root = new GameObject("David");
        root.tag = GameLayers.DavidTag;
        root.layer = GameLayers.Characters;

        var cap = root.AddComponent<CapsuleCollider>();
        cap.height = 1.75f; cap.radius = 0.35f; cap.center = new Vector3(0, 0.875f, 0);
        var rb = root.AddComponent<Rigidbody>();      // kinematic: lets trigger volumes (hiding spots, exits) see him
        rb.isKinematic = true;

        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        var v = vis.transform;
        HumanoidBuilder.Build(v, Role.David, m);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f; agent.height = 1.8f;
        agent.speed = 2.8f; agent.acceleration = 12f; agent.angularSpeed = 300f;
        agent.stoppingDistance = 0.3f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.enabled = false;        // enabled by DavidCompanion.Teleport() once the zone's NavMesh exists

        var dc = root.AddComponent<DavidCompanion>();
        dc.harpSource = AddSource(root, null, mx.sfx, false, false, 0.9f, 40f, 0.7f);
        dc.harpClip = a.harp;

        var anim = root.AddComponent<ProceduralCharacterAnim>();
        anim.visual = v;
        return Save(root, "David");
    }

    static GameObject BuildGuard(GuardType type, string name, Mats m)
    {
        var root = new GameObject(name);
        root.layer = GameLayers.Guards;

        var cap = root.AddComponent<CapsuleCollider>();
        cap.height = 1.8f; cap.radius = 0.35f; cap.center = new Vector3(0, 0.9f, 0);

        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        HumanoidBuilder.Build(vis.transform, type == GuardType.Commander ? Role.Commander : type == GuardType.Sentry ? Role.GuardSentry : Role.GuardPatrol, m);

        var eye = new GameObject("EyePoint");
        eye.transform.SetParent(root.transform, false);
        eye.transform.localPosition = new Vector3(0, 1.6f, 0.12f);

        var lantern = new GameObject("Lantern");
        lantern.transform.SetParent(root.transform, false);
        lantern.transform.localPosition = new Vector3(0.35f, 1.15f, 0.35f);
        var light = lantern.AddComponent<Light>();
        light.type = LightType.Point; light.color = new Color(1f, 0.78f, 0.45f); light.range = 4.5f; light.intensity = 1.6f;
        Prim(PrimitiveType.Sphere, "LanternBody", lantern.transform, Vector3.zero, Vector3.one * 0.12f, m.flame);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f; agent.height = 1.8f; agent.speed = 2f;
        agent.acceleration = 14f; agent.angularSpeed = 220f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.enabled = false;

        var vision = root.AddComponent<GuardVision>();
        vision.eyePoint = eye.transform;
        vision.obstacleMask = GameLayers.SightBlockers;
        vision.targetMask = GameLayers.Mask(GameLayers.Characters);
        root.AddComponent<GuardHearing>();

        var fsm = root.AddComponent<GuardFSM>();
        fsm.guardType = type;
        switch (type)
        {
            case GuardType.Patrol:
                vision.coneAngle = 70f; vision.maxRange = 11f; vision.gainMultiplier = 1f;
                fsm.patrolSpeed = 2.0f;
                break;
            case GuardType.Sentry:
                // widest cone of the three types; stationary, rotates on an arc
                vision.coneAngle = 110f; vision.maxRange = 14f; vision.gainMultiplier = 1.1f;
                fsm.sentryArcDegrees = 100f; fsm.sentryRotateSpeed = 25f;
                break;
            case GuardType.Commander:
                // confirms sightings very quickly (instant alarm), higher speed, wider cone than a patrol
                vision.coneAngle = 80f; vision.maxRange = 13f; vision.gainMultiplier = 3f;
                fsm.patrolSpeed = 2.4f; fsm.investigateSpeed = 4f; fsm.alarmSpeed = 5.6f;
                break;
        }

        // Ground cone
        var coneGo = new GameObject("DetectionCone");
        coneGo.transform.SetParent(root.transform, false);
        coneGo.AddComponent<MeshFilter>();
        var mr = coneGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = m.cone;
        var cone = coneGo.AddComponent<GuardConeVisual>();
        cone.vision = vision;

        // "?" / "!" icon
        var iconGo = new GameObject("StateIcon");
        iconGo.transform.SetParent(root.transform, false);
        iconGo.transform.localPosition = new Vector3(0, 2.75f, 0);
        var icon = iconGo.AddComponent<TextMeshPro>();
        icon.font = TMP_Settings.defaultFontAsset;
        icon.text = "";
        icon.fontSize = 7f;
        icon.fontStyle = FontStyles.Bold;
        icon.alignment = TextAlignmentOptions.Center;
        icon.color = new Color(1, 1, 1, 0);
        icon.rectTransform.sizeDelta = new Vector2(3, 2);

        var ind = root.AddComponent<DetectionIndicator>();
        ind.indicatorLight = light;
        ind.coneVisual = cone;
        ind.icon = icon;
        fsm.indicator = ind;

        var anim = root.AddComponent<ProceduralCharacterAnim>();
        anim.visual = vis.transform;
        return Save(root, name);
    }

    // Flickering flame + rising embers (additive soft particles).
    public static void AddFlameFx(Transform parent, Mats m, Vector3 pos, float scale)
    {
        var go = new GameObject("FlameFx");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f * scale, 0.55f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f * scale, 0.38f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.82f, 0.4f, 0.9f), new Color(1f, 0.45f, 0.12f, 0.9f));
        main.gravityModifier = -0.15f;
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 30f * scale;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 7f; sh.radius = 0.06f * scale;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.15f), 0.5f), new GradientColorKey(new Color(0.8f, 0.15f, 0.05f), 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.15f)));
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.fireParticle;

        var em2 = new GameObject("Embers");
        em2.transform.SetParent(go.transform, false);
        var eps = em2.AddComponent<ParticleSystem>();
        var emain = eps.main;
        emain.loop = true;
        emain.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
        emain.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * scale, 1.0f * scale);
        emain.startSize = new ParticleSystem.MinMaxCurve(0.03f * scale, 0.06f * scale);
        emain.startColor = new Color(1f, 0.65f, 0.2f, 1f);
        emain.gravityModifier = -0.05f;
        emain.maxParticles = 25;
        emain.simulationSpace = ParticleSystemSimulationSpace.World;
        var eem = eps.emission; eem.rateOverTime = 5f * scale;
        var esh = eps.shape; esh.shapeType = ParticleSystemShapeType.Cone; esh.angle = 25f; esh.radius = 0.05f * scale;
        var enoise = eps.noise; enoise.enabled = true; enoise.strength = 0.4f; enoise.frequency = 0.8f;
        var ecol = eps.colorOverLifetime; ecol.enabled = true;
        var eg = new Gradient();
        eg.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f) },
                   new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        ecol.color = eg;
        em2.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.emberParticle;
    }

    // Townsfolk: solid collider on the Civilians layer, NavMesh agent (wanderers) or carving obstacle (standing).
    static GameObject BuildNpc(int variant, Mats m)
    {
        var root = new GameObject("NPC_" + variant);
        root.layer = GameLayers.Civilians;

        var cap = root.AddComponent<CapsuleCollider>();
        cap.height = 1.75f; cap.radius = 0.34f; cap.center = new Vector3(0, 0.875f, 0);

        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        HumanoidBuilder.Build(vis.transform, Role.Civilian, m, variant);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f; agent.height = 1.75f; agent.speed = 1.1f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
        agent.avoidancePriority = 90;
        agent.enabled = false;

        var obs = root.AddComponent<NavMeshObstacle>();
        obs.shape = NavMeshObstacleShape.Capsule;
        obs.radius = 0.4f; obs.height = 1.8f; obs.center = new Vector3(0, 0.9f, 0);
        obs.carving = true;
        obs.enabled = false;

        root.AddComponent<CivilianNPC>();
        var anim = root.AddComponent<ProceduralCharacterAnim>();
        anim.visual = vis.transform;
        return Save(root, "NPC_" + variant);
    }

    static GameObject BuildGateNpc(Mats m)
    {
        var root = new GameObject("GateNPC");
        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        HumanoidBuilder.Build(vis.transform, Role.Commander, m);
        return Save(root, "GateNPC");
    }

    // King Saul, for the intro cutscene only (no AI; positioned and animated by IntroCutscene).
    static GameObject BuildSaul(Mats m)
    {
        var root = new GameObject("Saul");
        var vis = new GameObject("Visual");
        vis.transform.SetParent(root.transform, false);
        HumanoidBuilder.Build(vis.transform, Role.Saul, m);
        var anim = root.AddComponent<ProceduralCharacterAnim>();
        anim.visual = vis.transform;
        return Save(root, "Saul");
    }

    // A standalone spear prop, tweened (not physics-driven) from Saul's hand to where it lands.
    static GameObject BuildThrownSpear(Mats m)
    {
        var root = new GameObject("ThrownSpear");
        // a full-length spear: 2 m shaft from the root along +z, leaf-shaped iron head ending at z = 2.25
        Prim(PrimitiveType.Cylinder, "Shaft", root.transform, new Vector3(0, 0, 1.0f), new Vector3(0.05f, 1.0f, 0.05f), m.spearWood, false, new Vector3(90, 0, 0));
        Prim(PrimitiveType.Sphere, "Tip", root.transform, new Vector3(0, 0, 2.1f), new Vector3(0.1f, 0.06f, 0.32f), m.metal);
        Prim(PrimitiveType.Cylinder, "Binding", root.transform, new Vector3(0, 0, 1.93f), new Vector3(0.065f, 0.05f, 0.065f), m.leather, false, new Vector3(90, 0, 0));
        return Save(root, "ThrownSpear");
    }
}
