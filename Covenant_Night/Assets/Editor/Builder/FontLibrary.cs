using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// TextMesh Pro font assets built from the OFL fonts in Assets/Fonts (see CREDITS.txt):
//   Display - Cinzel, inscriptional capitals for titles, headings and buttons
//   Text    - Cardo, a scholarly serif for story text and readouts
// Saved under Resources so rich text can switch with <font="Cardo SDF">. Atlases are filled with the game's
// character set and made static, so playing never rewrites the font assets.
public static class FontLibrary
{
    public const string DisplayName = "Cinzel SDF";
    public const string TextName    = "Cardo SDF";
    const string OutDir = "Assets/Resources/Fonts & Materials";
    const string Charset =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "—–‘’“”…·•é";

    public static TMP_FontAsset Display { get; private set; }
    public static TMP_FontAsset Text    { get; private set; }

    public static void Build()
    {
        Display = Make("Assets/Fonts/Cinzel.ttf", DisplayName);
        Text    = Make("Assets/Fonts/Cardo-Regular.ttf", TextName);
    }

    static TMP_FontAsset Make(string ttfPath, string name)
    {
        string path = $"{OutDir}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogWarning($"[Builder] Font file missing at {ttfPath}; using the TMP default font.");
            return TMP_Settings.defaultFontAsset;
        }

        Directory.CreateDirectory(OutDir);
        var fa = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        fa.name = name;
        fa.TryAddCharacters(Charset, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.Log($"[Builder] {name}: no glyphs for \"{missing}\".");
        fa.atlasPopulationMode = AtlasPopulationMode.Static;

        AssetDatabase.CreateAsset(fa, path);
        fa.atlasTextures[0].name = name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }
}
