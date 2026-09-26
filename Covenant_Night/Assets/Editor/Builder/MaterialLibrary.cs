using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Flat-colour URP materials for the low-poly look (warm sandstone, deep charcoal/navy shadows).
public class Mats
{
    public Material sandA, sandB, sandC, limestone, palace, darkStone, floorStone, dirt, woodFloor, roofTile;
    public Material clothRed, clothBlue, clothOchre, clay, woodDark, crateWood, metal, stoneGrey, gold, banner;
    public Material flame, fireEmissive;
    public Material windowFrame, windowLit, windowDark, leafA, leafB, cypress, moon;
    public Material fireParticle, emberParticle, fireflyParticle, starParticle, glowParticle;
    public Material hairBlack, hairBrown, hairGrey, leather, straw;
    public Material civA, civB, civC, civD, civE, civF;
    public Material skin, jonRobe, jonSash, jonCloak, davRobe, davScarf, guardBody, guardSentry, cmdBody, helmet, spearWood, plume, dogFur;
    public Material hideDisc, waypointRing, waypointBeam, lockRed, cone, aimMarker;

    public Material Wall(int i)
    {
        switch (Mathf.Abs(i) % 4)
        {
            case 0:  return sandA;
            case 1:  return sandB;
            case 2:  return sandC;
            default: return limestone;
        }
    }
}

public static class MaterialLibrary
{
    const string Dir = "Assets/Materials";

    public static Mats Build()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Materials");

        TextureLib.Build();
        var m = new Mats();
        m.sandA      = Lit("Sandstone_A",  new Color(0.86f, 0.71f, 0.50f), 0.08f, null, 0f, TextureLib.Brick);
        m.sandB      = Lit("Sandstone_B",  new Color(0.78f, 0.64f, 0.46f), 0.08f, null, 0f, TextureLib.Brick);
        m.sandC      = Lit("Sandstone_C",  new Color(0.92f, 0.78f, 0.56f), 0.08f, null, 0f, TextureLib.Plaster);
        m.limestone  = Lit("Limestone",    new Color(0.84f, 0.80f, 0.72f), 0.1f, null, 0f, TextureLib.Plaster);
        m.palace     = Lit("PalaceStone",  new Color(0.78f, 0.70f, 0.62f), 0.2f, null, 0f, TextureLib.Plaster);
        m.darkStone  = Lit("DarkStone",    new Color(0.38f, 0.35f, 0.34f), 0.08f, null, 0f, TextureLib.Flagstone);
        m.floorStone = Lit("FloorStone",   new Color(0.52f, 0.48f, 0.43f), 0.08f, null, 0f, TextureLib.Flagstone);
        m.dirt       = Lit("Dirt",         new Color(0.46f, 0.34f, 0.23f), 0.02f, null, 0f, TextureLib.Dirt);
        m.woodFloor  = Lit("WoodFloor",    new Color(0.50f, 0.32f, 0.18f), 0.15f, null, 0f, TextureLib.Planks);
        m.roofTile   = Lit("RoofTile",     new Color(0.70f, 0.42f, 0.30f), 0.1f, null, 0f, TextureLib.Tiles);
        m.clothRed   = Lit("ClothRed",     new Color(0.72f, 0.16f, 0.13f), 0.05f, null, 0f, TextureLib.Stripes);
        m.clothBlue  = Lit("ClothBlue",    new Color(0.18f, 0.30f, 0.62f), 0.05f, null, 0f, TextureLib.Stripes);
        m.clothOchre = Lit("ClothOchre",   new Color(0.88f, 0.63f, 0.18f), 0.05f, null, 0f, TextureLib.Stripes);
        m.clay       = Lit("Clay",         new Color(0.68f, 0.36f, 0.21f), 0.1f, null, 0f, TextureLib.Plaster);
        m.woodDark   = Lit("WoodDark",     new Color(0.32f, 0.19f, 0.10f), 0.1f, null, 0f, TextureLib.Planks);
        m.crateWood  = Lit("CrateWood",    new Color(0.60f, 0.40f, 0.21f), 0.1f, null, 0f, TextureLib.Planks);
        m.metal      = Lit("Metal",        new Color(0.18f, 0.16f, 0.14f), 0.4f, null, 0.6f);
        m.stoneGrey  = Lit("StoneGrey",    new Color(0.62f, 0.59f, 0.55f), 0.1f, null, 0f, TextureLib.Flagstone);
        m.gold       = Lit("Gold",         new Color(0.85f, 0.65f, 0.15f), 0.5f, null, 0.7f);
        m.banner     = Lit("Banner",       new Color(0.50f, 0.10f, 0.10f), 0.05f);

        m.flame        = Lit("Flame",       new Color(1f, 0.6f, 0.15f), 0f, new Color(3.5f, 1.6f, 0.4f));
        m.fireEmissive = Lit("FirePit",     new Color(1f, 0.45f, 0.1f), 0f, new Color(3f, 1.0f, 0.2f));

        m.hairBlack  = Lit("HairBlack",  new Color(0.06f, 0.05f, 0.05f), 0.05f);
        m.hairBrown  = Lit("HairBrown",  new Color(0.32f, 0.16f, 0.08f), 0.05f);
        m.hairGrey   = Lit("HairGrey",   new Color(0.62f, 0.60f, 0.58f), 0.05f);
        m.straw      = Lit("Straw",      new Color(0.78f, 0.64f, 0.30f), 0.05f);
        m.civA       = Lit("CivOchre",   new Color(0.72f, 0.54f, 0.30f), 0.05f);
        m.civB       = Lit("CivOlive",   new Color(0.36f, 0.44f, 0.27f), 0.05f);
        m.civC       = Lit("CivTerracotta", new Color(0.66f, 0.32f, 0.22f), 0.05f);
        m.civD       = Lit("CivFadedBlue", new Color(0.30f, 0.38f, 0.56f), 0.05f);
        m.civE       = Lit("CivCream",   new Color(0.82f, 0.75f, 0.60f), 0.05f);
        m.civF       = Lit("CivPlum",    new Color(0.46f, 0.26f, 0.42f), 0.05f);
        m.leather    = Lit("Leather",    new Color(0.28f, 0.17f, 0.10f), 0.08f);
        m.windowFrame = Lit("WindowFrame", new Color(0.22f, 0.13f, 0.07f), 0.1f);
        m.windowLit   = Lit("WindowLit",   new Color(1f, 0.7f, 0.3f), 0f, new Color(2.6f, 1.45f, 0.5f));
        m.windowDark  = Lit("WindowDark",  new Color(0.03f, 0.04f, 0.07f), 0.5f);
        m.leafA       = Lit("LeafA",       new Color(0.20f, 0.34f, 0.18f), 0.05f);
        m.leafB       = Lit("LeafB",       new Color(0.26f, 0.40f, 0.20f), 0.05f);
        m.cypress     = Lit("Cypress",     new Color(0.12f, 0.24f, 0.16f), 0.05f);
        m.moon        = Lit("Moon",        new Color(0.9f, 0.93f, 1f), 0f, new Color(3.2f, 3.4f, 4.2f));
        m.skin       = Lit("Skin",        new Color(0.75f, 0.55f, 0.40f), 0.15f);
        m.jonRobe    = Lit("JonathanRobe", new Color(0.12f, 0.20f, 0.45f), 0.1f);
        m.jonSash    = Lit("JonathanSash", new Color(0.85f, 0.65f, 0.15f), 0.3f);
        m.jonCloak   = Lit("JonathanCloak", new Color(0.07f, 0.11f, 0.28f), 0.05f);
        m.davRobe    = Lit("DavidRobe",   new Color(0.50f, 0.38f, 0.22f), 0.05f);
        m.davScarf   = Lit("DavidScarf",  new Color(0.60f, 0.15f, 0.10f), 0.05f);
        m.guardBody  = Lit("GuardBody",   new Color(0.45f, 0.14f, 0.10f), 0.05f);
        m.guardSentry = Lit("GuardSentry", new Color(0.22f, 0.26f, 0.32f), 0.05f);
        m.cmdBody    = Lit("CommanderBody", new Color(0.62f, 0.08f, 0.10f), 0.1f);
        m.helmet     = Lit("Helmet",      new Color(0.60f, 0.42f, 0.20f), 0.5f, null, 0.7f);
        m.spearWood  = Lit("SpearWood",   new Color(0.35f, 0.24f, 0.12f), 0.1f);
        m.plume      = Lit("Plume",       new Color(0.80f, 0.10f, 0.10f), 0.05f);
        m.dogFur     = Lit("DogFur",      new Color(0.38f, 0.26f, 0.15f), 0.05f);

        m.fireParticle    = ParticleAdditive("FireParticle",    TextureLib.Dot, new Color(1f, 1f, 1f, 1f));
        m.emberParticle   = ParticleAdditive("EmberParticle",   TextureLib.Dot, new Color(1f, 1f, 1f, 1f));
        m.fireflyParticle = ParticleAdditive("FireflyParticle", TextureLib.Dot, new Color(1f, 1f, 1f, 1f));
        m.starParticle    = ParticleAdditive("StarParticle",    TextureLib.Dot, new Color(1f, 1f, 1f, 1f));
        m.glowParticle    = ParticleAdditive("GlowParticle",    TextureLib.Dot, new Color(1f, 1f, 1f, 1f));
        m.hideDisc     = TransparentUnlit("HidingSpotDisc", new Color(0.3f, 0.5f, 1f, 0.28f));
        m.waypointRing = TransparentUnlit("WaypointRing",   new Color(1f, 0.85f, 0.3f, 0.45f));
        m.waypointBeam = TransparentUnlit("WaypointBeam",   new Color(1f, 0.85f, 0.3f, 0.25f));
        m.lockRed      = TransparentUnlit("ExitLocked",     new Color(1f, 0.1f, 0.05f, 0.45f));
        m.cone         = GetOrCreate("DetectionCone", "Sprites/Default");     // vertex-coloured, fades toward the far edge
        m.cone.shader  = Shader.Find("Sprites/Default");                          // (an older asset may still hold another shader)
        m.cone.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(m.cone);
        m.aimMarker    = TransparentUnlit("AimMarker",      new Color(0.4f, 0.9f, 1f, 0.6f));

        AssetDatabase.SaveAssets();
        return m;
    }

    static Material GetOrCreate(string name, string shaderName)
    {
        string path = $"{Dir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static Material Lit(string name, Color c, float smooth = 0.12f, Color? emission = null, float metallic = 0f, Texture2D tex = null)
    {
        var m = GetOrCreate(name, "Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ParticleAdditive(string name, Texture2D tex, Color c)
    {
        var m = GetOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 2f);                                       // additive
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material TransparentUnlit(string name, Color c)
    {
        var m = GetOrCreate(name, "Universal Render Pipeline/Unlit");
        m.SetFloat("_Surface", 1f);                                   // transparent
        m.SetFloat("_Blend", 0f);                                     // alpha
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(m);
        return m;
    }
}
