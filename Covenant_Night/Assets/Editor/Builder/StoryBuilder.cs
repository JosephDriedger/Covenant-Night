using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Creates the StoryBeatData assets: intro (Beats 1–2), zone intros (Beat 3), the gate bluff (Beat 4),
// the farewell (Beat 5), the epilogue (1 Samuel 20:42) and the fail cutscene.
public class StoryAssets
{
    public StoryBeatData intro, zone2, zone3, zone4, zone5;
    public StoryBeatData gateBluff, farewell, epilogue, jonathanCaptured;
    public StoryBeatData[] ZoneEntryBeats => new[] { intro, zone2, zone3, zone4, zone5 };
}

public static class StoryBuilder
{
    const string Dir = "Assets/ScriptableObjects/Story";
    const string ArtDir = "Assets/Art/Story";

    static Sprite Spr(string name)
    {
        string path = $"{ArtDir}/{name}.png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static StoryBeat B(string heading, string text, Sprite s) => new StoryBeat { heading = heading, text = text, sprite = s };

    static StoryBeatData Make(string name, params StoryBeat[] beats)
    {
        string path = $"{Dir}/{name}.asset";
        var so = AssetDatabase.LoadAssetAtPath<StoryBeatData>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<StoryBeatData>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.beats = beats;
        EditorUtility.SetDirty(so);
        return so;
    }

    public static StoryAssets Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Story");

        var warning  = Spr("ill_warning");
        var wake     = Spr("ill_wake_david");
        var market   = Spr("ill_city_market");
        var alley    = Spr("ill_city_alley");
        var well     = Spr("ill_city_well");
        var approach = Spr("ill_gate_approach");
        var bluff    = Spr("ill_gate_bluff");
        var farewell = Spr("ill_farewell");
        var empty    = Spr("ill_empty_gate");
        var throne   = Spr("ill_throne");

        var s = new StoryAssets();

        s.intro = Make("Story_Intro",
            B("COVENANT NIGHT  -  Beat 1: The Warning",
              "A servant shakes you from sleep. \"My lord - the king's men are gathering. By morning they will be at David's door.\"\n\n" +
              "Your father Saul has already tried to kill David, twice, in open court. Tonight he sends assassins. You cannot obey your king and protect your friend.\n\n" +
              "You choose the covenant.", warning),
            B("Beat 2: Wake David",
              "You slip into David's quarters and wake him. There is no time for long words.\n\n" +
              "\"The eastern gate,\" you whisper. \"We reach it before dawn.\"\n\n" +
              "David - anointed by Samuel as Israel's future king - takes up his harp and follows. If a guard sees him, it is over.", wake),
            B("How to slip through Gibeah",
              "MOVE  WASD          LOOK  Mouse          SPRINT  Shift (loud)          CREEP  hold C (silent)\n" +
              "SHADOW STEP  hold Space beside a wall - nearly invisible while still\n" +
              "THROW A STONE  Left mouse - draws guards to the sound\n" +
              "DAVID  1 Follow    2 Wait    3 Run to the next golden marker    E toggle Follow/Wait\n" +
              "HARP  H - calms Suspicious guards who cannot see David (once per zone)\n\n" +
              "GAMEPAD  left stick move, right stick look, A/Cross shadow-step (hold), X/Square throw, B/Circle creep, L3 sprint, D-pad Left/Right/Down/Up = Follow/Wait/Run/Harp\n\n" +
              "Guards' cones show what they see: green, amber, red. Torchlight makes you easier to spot. No combat - only patience.", null));

        s.zone2 = Make("Story_Zone2",
            B("Beat 3: The Escape  -  The Market Quarter",
              "You have drilled with these guards; you know their rounds. Beyond the palace walls the plaza opens wide, lit by torch and fire.\n\n" +
              "Cover is scarce. Use the stalls and carts, and the dark gaps between the torches. The lane to the west is quiet - but its fires burn bright.", market));
        s.zone3 = Make("Story_Zone3",
            B("Beat 3: The Escape  -  Potter's Alley",
              "Narrow lanes, kilns still warm, dogs asleep in their pens - for now. Stray from the path and they will wake the whole quarter.\n\n" +
              "There is a way across the rooftops, but the roofs are open to the sky and to watching eyes.", alley));
        s.zone4 = Make("Story_Zone4",
            B("Beat 3: The Escape  -  Well Square",
              "Sentries keep watch from the rooftops, their sight lines long and unbroken.\n\n" +
              "Slip beneath their gaze: through the houses, where roofs give cover, or along the raised terrace behind its low wall.", well));
        s.zone5 = Make("Story_Zone5",
            B("Beat 4: The Gate",
              "The Eastern Gate. Beyond it: the dark hills, and freedom for David.\n\n" +
              "The gate commander is Saul's man, and no stealth will carry you past him. There is only one thing he will not dare to refuse - your father's own seal.", approach));

        s.gateBluff = Make("Story_GateBluff",
            B("The Bluff",
              "\"By order of the king, no one leaves Gibeah tonight,\" the commander says, blocking the way.\n\n" +
              "Jonathan steps into the torchlight and lifts his hand. On his finger, his father's signet ring catches the flame.", bluff),
            B("The Bluff",
              "\"I carry the king's seal. Saul sends this man beyond the walls before dawn - and does not wish it spoken of.\"\n\n" +
              "The commander studies the ring, then the prince's face. Slowly, he steps aside. The great gate groans open.", bluff));

        s.farewell = Make("Story_Farewell",
            B("Beat 5: The Farewell",
              "David passes through the gate into the dark hills. He turns once. No words are enough for what lies between them.\n\n" +
              "Jonathan watches from the threshold, knowing he will not see his friend again for years.", farewell));

        s.epilogue = Make("Story_Epilogue",
            B("Epilogue",
              "\"Go in peace. We have sworn friendship with each other in the name of the Lord. The Lord is witness between you and me forever.\"\n\n" +
              "- 1 Samuel 20:42", empty));

        s.jonathanCaptured = Make("Story_JonathanCaptured",
            B("Before the King",
              "Saul's guards drag Jonathan into the hall and throw him before the throne.\n\n" +
              "\"Where is the son of Jesse?\" the king demands.", throne),
            B("Before the King",
              "Jonathan lifts his chin and says nothing. Not a word of the covenant. Not a word of the gate.\n\n" +
              "In the uproar that follows, the night is given a second chance...", throne));

        AssetDatabase.SaveAssets();
        return s;
    }
}
