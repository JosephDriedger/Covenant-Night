using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

// Builds the always-loaded Persistent scene: managers, camera rig, post-processing, audio bed, characters, all UI.
public static class PersistentBuilder
{
    static readonly Color Gold = new Color(0.96f, 0.80f, 0.42f);
    static readonly Color Parchment = new Color(0.93f, 0.90f, 0.82f);

    public static readonly string[] ZoneScenes =
    {
        "Zone1_PalaceDistrict", "Zone2_MarketQuarter", "Zone3_PottersAlley", "Zone4_WellSquare", "Zone5_EasternGate",
    };

    // ── UI helpers ──────────────────────────────────────────────────────────

    static GameObject MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        return go;
    }

    static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return t;
    }

    static CanvasGroup Group(GameObject go, float alpha)
    {
        var g = go.AddComponent<CanvasGroup>();
        g.alpha = alpha;
        g.interactable = false;
        g.blocksRaycasts = false;
        return g;
    }

    // ── main ────────────────────────────────────────────────────────────────

    public static void Build(Mats m, Prefabs p, AudioLib a, MixerRefs mx, StoryAssets story,
                             InputActionAsset input, CheckpointData[] checkpoints, VolumeProfile post)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Night atmosphere: flat navy ambient + dark exponential fog. (RenderSettings of the active scene apply
        // while zones are loaded additively, so they live here.)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.12f, 0.22f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.005f;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.09f);
        RenderSettings.skybox = null;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

        // ── characters ──
        var jonathan = (GameObject)PrefabUtility.InstantiatePrefab(p.jonathan, scene);
        jonathan.transform.position = new Vector3(0, 0.05f, 0);
        var david = (GameObject)PrefabUtility.InstantiatePrefab(p.david, scene);
        david.transform.position = new Vector3(0, 0.05f, -2f);

        var pc = jonathan.GetComponent<PlayerController>();
        var ab = jonathan.GetComponent<PlayerAbilities>();
        var dc = david.GetComponent<DavidCompanion>();
        dc.followTarget = jonathan.transform;
        ab.david = dc;

        // aim marker (where a thrown stone will land)
        var aim = PrefabFactory.Prim(PrimitiveType.Cylinder, "AimMarker", null, Vector3.zero, new Vector3(0.7f, 0.01f, 0.7f), m.aimMarker);
        aim.SetActive(false);
        ab.aimMarker = aim.transform;

        // ── camera ──
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.08f);
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 220f; cam.fieldOfView = 62f;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var tpc = camGo.AddComponent<ThirdPersonCamera>();
        tpc.target = pc.cameraTarget;
        tpc.collisionMask = GameLayers.CameraBlockers;
        camGo.transform.position = new Vector3(0, 3, -4);

        BuildSky(m);

        // ── post-processing (Bloom + Vignette + colour adjustments) ──
        var volGo = new GameObject("PostProcessing");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = post;
        volGo.AddComponent<PostFXController>();

        // ── managers ──
        var gmGo = new GameObject("GameManager");
        gmGo.AddComponent<GameManager>();
        var fail = gmGo.AddComponent<FailStateHandler>();
        gmGo.AddComponent<PlatformTuning>();

        new GameObject("AlarmSystem").AddComponent<AlarmSystem>();

        var inGo = new GameObject("InputReader");
        var reader = inGo.AddComponent<InputReader>();
        var so = new SerializedObject(reader);
        so.FindProperty("actionsAsset").objectReferenceValue = input;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ── audio bed ──
        var audioRoot = new GameObject("Audio");
        AudioSource Mk(string name, AudioClip clip, AudioMixerGroup g, bool loop, float vol2)
        {
            var go = new GameObject(name);
            go.transform.SetParent(audioRoot.transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.outputAudioMixerGroup = g; s.loop = loop; s.volume = vol2;
            s.playOnAwake = false; s.spatialBlend = 0f;
            return s;
        }
        var drone    = Mk("TensionDrone", a.drone, mx.music, true, 0.05f);
        var music    = Mk("MusicUnderscore", a.music, mx.music, true, 0.35f);
        var sting    = Mk("AlertSting", a.sting, mx.sfx, false, 0.9f);
        var epilogue = Mk("EpiloguePhrase", a.epilogue, mx.music, false, 0.8f);
        var wind     = Mk("NightWind", a.wind, mx.ambience, true, 0.45f); wind.playOnAwake = true;
        var crickets = Mk("Crickets", a.crickets, mx.ambience, true, 0.22f); crickets.playOnAwake = true;

        var tension = audioRoot.AddComponent<TensionAudioManager>();
        tension.droneSource = drone; tension.musicSource = music; tension.stingSource = sting; tension.epilogueSource = epilogue;
        tension.calmSnapshot = mx.calm; tension.suspiciousSnapshot = mx.suspicious; tension.alarmedSnapshot = mx.alarmed;

        // ── UI ──
        var hud = BuildHud();
        var (storyCtl, _) = BuildStory();
        BuildFail(fail);
        var cardCtl = BuildZoneCard();
        var fade = BuildFade();
        BuildCredits(fail);
        fail.jonathanCapturedBeats = story.jonathanCaptured;

        // ── zone manager ──
        var zmGo = new GameObject("ZoneManager");
        var zm = zmGo.AddComponent<ZoneManager>();
        zm.zoneSceneNames = ZoneScenes;
        zm.zoneCheckpoints = checkpoints;
        zm.jonathan = jonathan.transform;
        zm.david = david.transform;
        zm.abilities = ab;
        zm.davidCompanion = dc;
        zm.zoneEntryBeats = story.ZoneEntryBeats;
        zm.fadePanel = fade;
        zm.fadeDuration = 0.6f;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Persistent.unity");
    }

    // ── Sky: a low moon and a dome of stars that follow the camera ─────────

    static void BuildSky(Mats m)
    {
        var sky = new GameObject("SkyDecor");
        sky.AddComponent<FollowCamera>();

        Vector3 dir = new Vector3(0.28f, 0.19f, 0.94f).normalized;
        var moon = PrefabFactory.Prim(PrimitiveType.Sphere, "Moon", sky.transform, dir * 150f, Vector3.one * 11f, m.moon);
        moon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var glow = new GameObject("MoonGlow");
        glow.transform.SetParent(sky.transform, false);
        glow.transform.localPosition = dir * 148f;
        var gps = glow.AddComponent<ParticleSystem>();
        var gm = gps.main;
        gm.loop = false; gm.duration = 1f; gm.startLifetime = 1e6f; gm.startSpeed = 0f;
        gm.startSize = 80f; gm.startColor = new Color(0.45f, 0.55f, 1f, 0.32f); gm.maxParticles = 1;
        gm.simulationSpace = ParticleSystemSimulationSpace.Local;
        var gem = gps.emission; gem.rateOverTime = 0;
        gem.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        var gsh = gps.shape; gsh.enabled = false;
        glow.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.glowParticle;

        var stars = new GameObject("Stars");
        stars.transform.SetParent(sky.transform, false);
        stars.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);       // hemisphere faces up
        var sps = stars.AddComponent<ParticleSystem>();
        var sm = sps.main;
        sm.loop = false; sm.duration = 1f; sm.startLifetime = 1e6f; sm.startSpeed = 0f;
        sm.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
        sm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.85f, 1f, 1f), new Color(1f, 0.95f, 0.8f, 1f));
        sm.maxParticles = 800;
        sm.simulationSpace = ParticleSystemSimulationSpace.Local;
        var sem = sps.emission; sem.rateOverTime = 0;
        sem.SetBursts(new[] { new ParticleSystem.Burst(0f, 800) });
        var ssh = sps.shape;
        ssh.shapeType = ParticleSystemShapeType.Hemisphere;
        ssh.radius = 150f; ssh.radiusThickness = 0f;
        stars.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.starParticle;
    }

    // ── HUD ─────────────────────────────────────────────────────────────────

    static HUD BuildHud()
    {
        var canvas = MakeCanvas("HUD", 10);
        var hud = canvas.AddComponent<HUD>();
        var safe = new GameObject("SafeArea", typeof(RectTransform));
        safe.transform.SetParent(canvas.transform, false);
        Stretch(safe.GetComponent<RectTransform>());
        safe.AddComponent<SafeAreaFitter>();
        var t = safe.transform;

        void Plate(Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var p = MakeImage(t, "Plate", new Color(0.02f, 0.02f, 0.05f, 0.42f));
            var pr = p.rectTransform;
            pr.anchorMin = pr.anchorMax = anchor; pr.pivot = pivot; pr.anchoredPosition = pos; pr.sizeDelta = size;
        }
        Plate(new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(330, 62));
        Plate(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -22), new Vector2(330, 62));
        Plate(new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 42), new Vector2(500, 112));
        Plate(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(1500, 40));

        hud.zoneText = MakeText(t, "ZoneText", "Zone 1 / 5", 34, Parchment, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 50), TextAlignmentOptions.TopLeft);
        hud.stoneCountText = MakeText(t, "StoneCount", "Stones: 3", 34, Parchment, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(500, 50), TextAlignmentOptions.TopRight);
        hud.davidModeText = MakeText(t, "DavidMode", "David: Follow", 32, new Color(0.7f, 0.9f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 95), new Vector2(600, 46), TextAlignmentOptions.BottomLeft);
        hud.harpText = MakeText(t, "HarpText", "Harp: ready [H]", 30, Gold, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 50), new Vector2(600, 42), TextAlignmentOptions.BottomLeft);
        hud.controlsHint = MakeText(t, "ControlsHint", "", 19, new Color(1, 1, 1, 0.6f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(1480, 32), TextAlignmentOptions.Bottom);
        hud.controlsHint.textWrappingMode = TextWrappingModes.NoWrap;

        // alarm banner
        var alarm = MakeImage(t, "AlarmTimer", new Color(0.35f, 0.02f, 0.02f, 0.75f));
        var art = alarm.rectTransform;
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 1); art.pivot = new Vector2(0.5f, 1);
        art.anchoredPosition = new Vector2(0, -28); art.sizeDelta = new Vector2(620, 96);
        hud.alarmTimerRoot = alarm.gameObject;
        hud.alarmTimerText = MakeText(alarm.transform, "AlarmText", "HIDE: 30", 60, new Color(1f, 0.85f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 90), TextAlignmentOptions.Center, FontStyles.Bold);

        hud.messageText = MakeText(t, "Message", "", 36, Gold, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 210), new Vector2(1500, 60), TextAlignmentOptions.Center);

        var ret = MakeImage(t, "Reticle", new Color(1, 1, 1, 0.65f));
        var rr = ret.rectTransform;
        rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f); rr.pivot = new Vector2(0.5f, 0.5f);
        rr.sizeDelta = new Vector2(7, 7); rr.anchoredPosition = Vector2.zero;
        hud.reticle = ret.gameObject;
        return hud;
    }

    // ── Story panel ─────────────────────────────────────────────────────────

    static (StoryPanelController, CanvasGroup) BuildStory()
    {
        var canvas = MakeCanvas("StoryPanel", 60);
        var group = Group(canvas, 0f);
        var ctl = canvas.AddComponent<StoryPanelController>();
        var t = canvas.transform;

        var bg = MakeImage(t, "Background", new Color(0.02f, 0.02f, 0.045f, 1f));
        Stretch(bg.rectTransform);

        var ill = MakeImage(t, "Illustration", Color.white);
        ill.preserveAspect = true;
        var ir = ill.rectTransform;
        ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 1); ir.pivot = new Vector2(0.5f, 1);
        ir.anchoredPosition = new Vector2(0, -28); ir.sizeDelta = new Vector2(1024, 576);

        var heading = MakeText(t, "Heading", "", 32, Gold, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -618), new Vector2(1400, 46), TextAlignmentOptions.Center, FontStyles.Bold);
        var body = MakeText(t, "Body", "", 31, Parchment, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -672), new Vector2(1420, 300), TextAlignmentOptions.Top);
        body.lineSpacing = 6;
        var prompt = MakeText(t, "ContinuePrompt", "press any key or button  >", 24, new Color(1, 1, 1, 0.55f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(600, 36), TextAlignmentOptions.Center);

        ctl.panelGroup = group;
        ctl.headingText = heading;
        ctl.bodyText = body;
        ctl.continuePrompt = prompt;
        ctl.illustration = ill;
        return (ctl, group);
    }

    // ── Fail panel ──────────────────────────────────────────────────────────

    static void BuildFail(FailStateHandler fail)
    {
        var canvas = MakeCanvas("FailPanel", 65);
        var group = Group(canvas, 0f);
        var t = canvas.transform;
        var bg = MakeImage(t, "Tint", new Color(0.12f, 0.01f, 0.01f, 0.88f));
        Stretch(bg.rectTransform);
        fail.failTitle = MakeText(t, "Title", "Caught", 110, new Color(1f, 0.82f, 0.75f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1500, 140), TextAlignmentOptions.Center, FontStyles.Bold);
        fail.failReasonText = MakeText(t, "Reason", "", 40, Parchment, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(1400, 160), TextAlignmentOptions.Top);
        fail.failPanel = group;
        canvas.SetActive(false);
    }

    // ── Zone name card ──────────────────────────────────────────────────────

    static ZoneNameCard BuildZoneCard()
    {
        var canvas = MakeCanvas("ZoneNameCard", 70);
        var group = Group(canvas, 0f);
        var card = canvas.AddComponent<ZoneNameCard>();
        card.cardGroup = group;
        card.nameText = MakeText(canvas.transform, "ZoneName", "", 96, Gold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1700, 130), TextAlignmentOptions.Center, FontStyles.Bold);
        card.subtitleText = MakeText(canvas.transform, "ZoneSubtitle", "", 38, Parchment, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -45), new Vector2(1200, 60), TextAlignmentOptions.Center);
        return card;
    }

    static CanvasGroup BuildFade()
    {
        var canvas = MakeCanvas("Fade", 50);
        var group = Group(canvas, 1f);
        var img = MakeImage(canvas.transform, "Black", Color.black);
        Stretch(img.rectTransform);
        return group;
    }

    // ── Credits (win) ───────────────────────────────────────────────────────

    static void BuildCredits(FailStateHandler fail)
    {
        var canvas = MakeCanvas("CreditsPanel", 80);
        var group = Group(canvas, 0f);
        var t = canvas.transform;
        var bg = MakeImage(t, "Black", new Color(0.01f, 0.01f, 0.03f, 1f));
        Stretch(bg.rectTransform);

        MakeText(t, "Title", "COVENANT NIGHT", 120, Gold, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1700, 160), TextAlignmentOptions.Center, FontStyles.Bold);
        MakeText(t, "Sub", "David is beyond the gate. The covenant holds.", 40, Parchment, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(1500, 60), TextAlignmentOptions.Center);
        MakeText(t, "Body",
            "A stealth game of loyalty, shadow, and sacrifice\nBased on 1 Samuel 19-20\n\n" +
            "A one-person project: design, code, sound design and illustration\n" +
            "All audio and art in this build is original and procedurally generated.\n" +
            "See CREDITS.txt for the full attribution list.\n\n" +
            "Thank you for playing.",
            32, new Color(0.85f, 0.83f, 0.78f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(1500, 420), TextAlignmentOptions.Top);
        MakeText(t, "Prompt", "press any key to play again", 28, new Color(1, 1, 1, 0.55f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(900, 40), TextAlignmentOptions.Center);

        fail.winPanel = group;
        canvas.SetActive(false);
    }
}
