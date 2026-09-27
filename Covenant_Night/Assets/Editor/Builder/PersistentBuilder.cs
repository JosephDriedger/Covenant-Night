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
        var drone    = Mk("TensionDrone", a.drone, mx.music, true, 0f);
        var music    = Mk("MusicUnderscore", a.music, mx.music, true, 0.3f);
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
        BuildMenu();
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
        Plate(new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(470, 62));
        Plate(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -22), new Vector2(330, 104));
        Plate(new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 42), new Vector2(500, 112));

        hud.zoneText = MakeText(t, "ZoneText", "Zone 1 / 5", 34, Parchment, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 50), TextAlignmentOptions.TopLeft);
        hud.bestTimeText = MakeText(t, "BestTime", "Best: --:--", 26, new Color(0.75f, 0.85f, 0.75f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -66), new Vector2(600, 36), TextAlignmentOptions.TopLeft);
        hud.stoneCountText = MakeText(t, "StoneCount", "Stones: 3", 34, Parchment, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(500, 50), TextAlignmentOptions.TopRight);
        hud.decoyCountText = MakeText(t, "DecoyCount", "Decoys: 1", 30, new Color(0.85f, 0.8f, 0.65f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -66), new Vector2(500, 42), TextAlignmentOptions.TopRight);
        hud.davidModeText = MakeText(t, "DavidMode", "David: Follow", 32, new Color(0.7f, 0.9f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 95), new Vector2(600, 46), TextAlignmentOptions.BottomLeft);
        hud.harpText = MakeText(t, "HarpText", "Harp: Ready", 30, Gold, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 50), new Vector2(600, 42), TextAlignmentOptions.BottomLeft);

        // alarm banner
        var alarm = MakeImage(t, "AlarmTimer", new Color(0.35f, 0.02f, 0.02f, 0.75f));
        var art = alarm.rectTransform;
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 1); art.pivot = new Vector2(0.5f, 1);
        art.anchoredPosition = new Vector2(0, -28); art.sizeDelta = new Vector2(620, 96);
        hud.alarmTimerRoot = alarm.gameObject;
        hud.alarmTimerText = MakeText(alarm.transform, "AlarmText", "Hide: 30", 60, new Color(1f, 0.85f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 90), TextAlignmentOptions.Center, FontStyles.Bold);

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
        var prompt = MakeText(t, "ContinuePrompt", "Press Any Key or Button", 24, new Color(1, 1, 1, 0.55f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(600, 36), TextAlignmentOptions.Center);

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
        var canvas = MakeCanvas("FailPanel", 55);
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

    // ── Menus (title screen + pause menu) ───────────────────────────────────

    static readonly Color ButtonNormal = new Color(0.08f, 0.08f, 0.13f, 0.88f);

    static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, float fontSize = 36f)
    {
        var img = MakeImage(parent, name, Color.white);
        img.raycastTarget = true;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.ColorTint;
        var c = b.colors;
        c.normalColor = ButtonNormal;
        c.highlightedColor = new Color(0.42f, 0.33f, 0.12f, 0.96f);
        c.selectedColor = c.highlightedColor;
        c.pressedColor = new Color(0.62f, 0.48f, 0.16f, 1f);
        c.disabledColor = new Color(0.08f, 0.08f, 0.13f, 0.4f);
        c.colorMultiplier = 1f;
        c.fadeDuration = 0.06f;
        b.colors = c;

        MakeText(img.transform, "Label", label, fontSize, Parchment, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAlignmentOptions.Center);
        return b;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        var rt = Rt(root);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var bg = MakeImage(root.transform, "Background", new Color(0.08f, 0.08f, 0.13f, 0.92f));
        bg.raycastTarget = true;
        bg.rectTransform.anchorMin = new Vector2(0, 0.3f); bg.rectTransform.anchorMax = new Vector2(1, 0.7f);
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var far = Rt(fillArea);
        far.anchorMin = new Vector2(0, 0.3f); far.anchorMax = new Vector2(1, 0.7f);
        far.offsetMin = new Vector2(8, 0); far.offsetMax = new Vector2(-8, 0);
        var fill = MakeImage(fillArea.transform, "Fill", Gold);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        var har = Rt(handleArea);
        Stretch(har);
        har.offsetMin = new Vector2(14, 0); har.offsetMax = new Vector2(-14, 0);
        var handle = MakeImage(handleArea.transform, "Handle", Color.white);
        handle.raycastTarget = true;
        handle.rectTransform.anchorMin = new Vector2(0, 0); handle.rectTransform.anchorMax = new Vector2(0, 1);
        handle.rectTransform.sizeDelta = new Vector2(28, 0);

        var s = root.GetComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        var c = s.colors;
        c.normalColor = Parchment;
        c.highlightedColor = new Color(1f, 0.86f, 0.5f, 1f);
        c.selectedColor = c.highlightedColor;
        c.pressedColor = new Color(0.8f, 0.65f, 0.3f, 1f);
        c.fadeDuration = 0.06f;
        s.colors = c;
        return s;
    }

    static RectTransform MakePage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = Rt(go);
        Stretch(rt);
        return rt;
    }

    static TextMeshProUGUI MakeColumn(Transform parent, string name, string text, float size, Color color, float left, float top, float width, float height)
    {
        var t = MakeText(parent, name, text, size, color, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(left, top), new Vector2(width, height), TextAlignmentOptions.TopLeft);
        t.lineSpacing = 12;
        return t;
    }

    static void BuildMenu()
    {
        var canvas = MakeCanvas("Menu", 90);
        canvas.AddComponent<GraphicRaycaster>();
        var group = canvas.AddComponent<CanvasGroup>();
        var ctl = new GameObject("MenuSystem").AddComponent<MenuController>();
        ctl.canvasRoot = canvas;
        ctl.group = group;
        var t = canvas.transform;
        var mid = new Vector2(0.5f, 0.5f);

        // ── title ──
        var title = MakePage(t, "TitlePage");
        var art = MakeImage(title, "Illustration", Color.white);
        art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Story/ill_gate_approach.png");
        art.preserveAspect = false;
        art.color = art.sprite != null ? Color.white : new Color(0.02f, 0.02f, 0.045f, 1f);
        Stretch(art.rectTransform);
        var shade = MakeImage(title, "Shade", new Color(0.01f, 0.01f, 0.03f, 0.62f));
        Stretch(shade.rectTransform);
        MakeText(title, "Name", "COVENANT NIGHT", 140, Gold, mid, mid, new Vector2(0, 280), new Vector2(1700, 180), TextAlignmentOptions.Center, FontStyles.Bold);
        MakeText(title, "Verse", "1 Samuel 19 and 20", 40, Parchment, mid, mid, new Vector2(0, 170), new Vector2(1200, 60), TextAlignmentOptions.Center);
        var bSize = new Vector2(560, 72);
        ctl.playBtn          = MakeButton(title, "Play",     "Play",     new Vector2(0, 30),   bSize);
        ctl.titleControlsBtn = MakeButton(title, "Controls", "Controls", new Vector2(0, -58),  bSize);
        ctl.titleSettingsBtn = MakeButton(title, "Settings", "Settings", new Vector2(0, -146), bSize);
        ctl.titleQuitBtn     = MakeButton(title, "Quit",     "Quit",     new Vector2(0, -234), bSize);
        ctl.titlePage = title.gameObject;

        // ── difficulty ──
        var diff = MakePage(t, "DifficultyPage");
        var dArt = MakeImage(diff, "Illustration", art.color);
        dArt.sprite = art.sprite;
        Stretch(dArt.rectTransform);
        var dShade = MakeImage(diff, "Shade", new Color(0.01f, 0.01f, 0.03f, 0.78f));
        Stretch(dShade.rectTransform);
        MakeText(diff, "Heading", "Choose Your Night", 96, Gold, mid, mid, new Vector2(0, 390), new Vector2(1500, 130), TextAlignmentOptions.Center, FontStyles.Bold);
        MakeText(diff, "Note", "On Easy, Medium and Hard, a capture only restarts the current zone.", 34, Parchment, mid, mid, new Vector2(0, 300), new Vector2(1500, 50), TextAlignmentOptions.Center);
        var dSize = new Vector2(1100, 104);
        string Sub(string s) => "\n<size=62%><color=#C9C3B0>" + s + "</color></size>";
        ctl.easyBtn     = MakeButton(diff, "Easy",     "Easy" + Sub("Slower, near-sighted guards. More stones and more time to hide."), new Vector2(0, 170), dSize);
        ctl.mediumBtn   = MakeButton(diff, "Medium",   "Medium" + Sub("The intended experience."), new Vector2(0, 50), dSize);
        ctl.hardBtn     = MakeButton(diff, "Hard",     "Hard" + Sub("Keener guards, less time to hide and fewer stones."), new Vector2(0, -70), dSize);
        ctl.hardcoreBtn = MakeButton(diff, "Hardcore", "Hardcore" + Sub("One life. Any capture restarts the run. Faster guards that search hiding spots."), new Vector2(0, -190), dSize);
        ctl.difficultyBackBtn = MakeButton(diff, "Back", "Back", new Vector2(0, -330), new Vector2(400, 64), 32);
        ctl.mixedBtn = MakeButton(diff, "Mixed", "New Game+: Mixed Difficulty", new Vector2(0, -410), new Vector2(700, 56), 26);
        ctl.difficultyPage = diff.gameObject;

        // ── mixed difficulty (New Game+): pick Easy/Medium/Hard separately for each zone ──
        var mixedPage = MakePage(t, "MixedPage");
        var mixedArt = MakeImage(mixedPage, "Illustration", art.color);
        mixedArt.sprite = art.sprite;
        Stretch(mixedArt.rectTransform);
        var mixedShade = MakeImage(mixedPage, "Shade", new Color(0.01f, 0.01f, 0.03f, 0.82f));
        Stretch(mixedShade.rectTransform);
        MakeText(mixedPage, "Heading", "Choose Each Zone's Night", 76, Gold, mid, mid, new Vector2(0, 430), new Vector2(1600, 110), TextAlignmentOptions.Center, FontStyles.Bold);
        MakeText(mixedPage, "Note", "Hardcore is not available per zone; a capture restarts the current zone.", 30, Parchment, mid, mid, new Vector2(0, 350), new Vector2(1500, 46), TextAlignmentOptions.Center);
        string[] zoneNames = { "Zone 1 — Palace District", "Zone 2 — Market Quarter", "Zone 3 — Potter's Alley", "Zone 4 — Well Square", "Zone 5 — Eastern Gate" };
        for (int i = 0; i < 5; i++)
        {
            float rowY = 260 - i * 90;
            MakeText(mixedPage, $"ZoneLabel{i}", zoneNames[i], 32, Parchment, mid, new Vector2(0, 0.5f), new Vector2(-560, rowY), new Vector2(560, 60), TextAlignmentOptions.Left);
            ctl.zoneLevelBtns[i] = MakeButton(mixedPage, $"ZoneLevel{i}", "Medium", new Vector2(220, rowY), new Vector2(360, 64), 30);
        }
        ctl.mixedStartBtn = MakeButton(mixedPage, "Begin", "Begin", new Vector2(-160, -260), new Vector2(400, 72));
        ctl.mixedBackBtn = MakeButton(mixedPage, "Back", "Back", new Vector2(260, -260), new Vector2(400, 72));
        ctl.mixedPage = mixedPage.gameObject;

        // ── pause ──
        var pause = MakePage(t, "PausePage");
        var pauseShade = MakeImage(pause, "Shade", new Color(0.01f, 0.01f, 0.03f, 0.78f));
        Stretch(pauseShade.rectTransform);
        MakeText(pause, "Heading", "Paused", 96, Gold, mid, mid, new Vector2(0, 350), new Vector2(1200, 130), TextAlignmentOptions.Center, FontStyles.Bold);
        ctl.resumeBtn        = MakeButton(pause, "Resume",      "Resume",       new Vector2(0, 210),  bSize);
        ctl.restartBtn       = MakeButton(pause, "RestartZone", "Restart Zone", new Vector2(0, 122),  bSize);
        ctl.pauseControlsBtn = MakeButton(pause, "Controls",    "Controls",     new Vector2(0, 34),   bSize);
        ctl.pauseSettingsBtn = MakeButton(pause, "Settings",    "Settings",     new Vector2(0, -54),  bSize);
        ctl.mainMenuBtn      = MakeButton(pause, "MainMenu",    "Main Menu",    new Vector2(0, -142), bSize);
        ctl.pauseQuitBtn     = MakeButton(pause, "Quit",        "Quit Game",    new Vector2(0, -230), bSize);
        ctl.pausePage = pause.gameObject;

        // ── controls + field notes ──
        var controls = MakePage(t, "ControlsPage");
        var cShade = MakeImage(controls, "Shade", new Color(0.02f, 0.02f, 0.045f, 0.96f));
        Stretch(cShade.rectTransform);
        MakeText(controls, "Heading", "Controls", 80, Gold, mid, mid, new Vector2(0, 450), new Vector2(1200, 110), TextAlignmentOptions.Center, FontStyles.Bold);
        MakeColumn(controls, "KeyboardHeader", "Keyboard & Mouse", 30, Gold, -380, 375, 620, 44).fontStyle = FontStyles.Bold;
        MakeColumn(controls, "GamepadHeader", "Gamepad", 30, Gold, 250, 375, 620, 44).fontStyle = FontStyles.Bold;
        MakeColumn(controls, "Actions",
            "Move\nLook\nSneak (Silent)\nSprint (Loud)\nShadow-Step at a Wall\nThrow a Stone\nDrop a Decoy\nDavid: Follow, Wait, Run\nHush David\nHarp\nPause",
            23, Parchment, -880, 325, 480, 420);
        MakeColumn(controls, "Keyboard",
            "W A S D\nMouse\nHold Shift\nHold Ctrl or Cmd\nHold Space\nLeft Click\nG\n1, 2, 3 (E Toggles Follow and Wait)\nQ\nH\nEsc",
            23, Parchment, -380, 325, 620, 420);
        MakeColumn(controls, "Gamepad",
            "Left Stick\nRight Stick\nHold B / Circle\nClick L3\nHold A / Cross\nX / Square\nRight Shoulder\nD-Pad Left, Right, Down (Y Toggles)\nLeft Shoulder\nD-Pad Up\nStart",
            23, Parchment, 250, 325, 620, 420);
        MakeColumn(controls, "NotesHeader", "Field Notes", 32, Gold, -880, -85, 800, 44).fontStyle = FontStyles.Bold;
        MakeColumn(controls, "Notes",
            "Guard cones: green sees nothing, amber is suspicious, red is chasing you.\n" +
            "Sneak near guards and dogs. Sprinting and thrown stones can be heard from far off.\n" +
            "Torchlight gives you away. Walls and shadows hide you; a shadow-step makes you nearly invisible.\n" +
            "A thrown stone draws a guard from his post. The harp calms one who has only heard something.\n" +
            "David follows you. Leave him waiting in cover, then run him to the next golden marker.",
            27, Parchment, -880, -135, 1760, 300);
        ctl.controlsBackBtn = MakeButton(controls, "Back", "Back", new Vector2(0, -455), new Vector2(400, 64), 32);
        ctl.controlsPage = controls.gameObject;

        // ── settings ──
        var settings = MakePage(t, "SettingsPage");
        var sShade = MakeImage(settings, "Shade", new Color(0.02f, 0.02f, 0.045f, 0.96f));
        Stretch(sShade.rectTransform);
        MakeText(settings, "Heading", "Settings", 80, Gold, mid, mid, new Vector2(0, 330), new Vector2(1200, 110), TextAlignmentOptions.Center, FontStyles.Bold);
        ctl.volumeLabel = MakeText(settings, "VolumeLabel", "", 34, Parchment, mid, mid, new Vector2(-370, 120), new Vector2(480, 50), TextAlignmentOptions.Left);
        ctl.volumeSlider = MakeSlider(settings, "VolumeSlider", new Vector2(190, 120), new Vector2(560, 40));
        ctl.lookLabel = MakeText(settings, "LookLabel", "", 34, Parchment, mid, mid, new Vector2(-370, 30), new Vector2(480, 50), TextAlignmentOptions.Left);
        ctl.lookSlider = MakeSlider(settings, "LookSlider", new Vector2(190, 30), new Vector2(560, 40));
        ctl.invertBtn = MakeButton(settings, "InvertY", "", new Vector2(0, -70), new Vector2(640, 72));
        ctl.fullscreenBtn = MakeButton(settings, "Fullscreen", "", new Vector2(0, -158), new Vector2(640, 72));
        ctl.settingsBackBtn = MakeButton(settings, "Back", "Back", new Vector2(0, -280), new Vector2(400, 64), 32);
        ctl.settingsPage = settings.gameObject;

        canvas.SetActive(false);
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
            "A stealth game of loyalty, shadow, and sacrifice\nBased on 1 Samuel 19 and 20\n\n" +
            "Design, code, sound and illustration by one person.\n" +
            "See CREDITS.txt for attribution.\n\n" +
            "Thank you for playing.",
            32, new Color(0.85f, 0.83f, 0.78f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(1500, 420), TextAlignmentOptions.Top);
        MakeText(t, "Prompt", "Press Any Key to Play Again", 28, new Color(1, 1, 1, 0.55f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(900, 40), TextAlignmentOptions.Center);

        fail.winPanel = group;
        canvas.SetActive(false);
    }
}
