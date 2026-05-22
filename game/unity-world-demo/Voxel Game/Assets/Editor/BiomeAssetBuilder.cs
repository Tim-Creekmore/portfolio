using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// One-click builder for the Phase B starter biomes.
/// Menu: Voxel Kingdom / Build Starter Biomes.
/// Safe to re-run — it overwrites the three assets with fresh values.
/// </summary>
public static class BiomeAssetBuilder
{
    const string FolderPath = "Assets/Data/Biomes";

    [MenuItem("Voxel Kingdom/Build Starter Biomes")]
    public static void BuildAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(FolderPath))
            AssetDatabase.CreateFolder("Assets/Data", "Biomes");

        BuildTemperate();
        BuildMountain();
        BuildFrontier();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Starter biomes built: Temperate, Mountain, Frontier.");
    }

    static BiomeData GetOrCreate(string name)
    {
        string path = $"{FolderPath}/{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<BiomeData>(path);
        if (existing != null) return existing;

        var asset = ScriptableObject.CreateInstance<BiomeData>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    static void BuildTemperate()
    {
        var b = GetOrCreate("Temperate");
        b.displayName = "Temperate";
        b.moodDescription =
            "English countryside in golden hour. Safe, welcoming, soft. The player's home turf.\n" +
            "Reference: BotW Great Plateau / Hyrule Field.";

        // Visual bible base — unchanged
        b.grassTop  = Hex("#5A7A3A");
        b.grassSide = Hex("#4A6A2A");
        b.dirt      = Hex("#7A5C3A");
        b.stone     = Hex("#6A6560");
        b.stoneDark = Hex("#4A4540");
        b.leafColor = Hex("#3A5A2A");
        b.trunkColor = Hex("#6A4830");

        b.ambientTint = new Color(1.00f, 0.95f, 0.85f); // warm gold
        b.fogTint     = new Color(0.78f, 0.72f, 0.60f, 0f); // inherit global

        b.treeDensity  = 0.65f;
        b.grassDensity = 0.90f;
        b.treeStyle    = BiomeData.TreeStyleHint.Leafy;

        b.ambientProfileId = "temperate";
        b.ambientVolume = 0.22f;

        EditorUtility.SetDirty(b);
    }

    static void BuildMountain()
    {
        var b = GetOrCreate("Mountain");
        b.displayName = "Mountain";
        b.moodDescription =
            "BotW Hebra / Tabantha — cold but never bleak. Remote monastery on a peak.\n" +
            "Stones carry a faint amber undertone, snow is cream not pure white.";

        // Cooler, sparser grass + warm-amber stone (instead of cold grey)
        b.grassTop  = Hex("#5A6A4A");
        b.grassSide = Hex("#4A5A3A");
        b.dirt      = Hex("#6A5A4A");
        b.stone     = Hex("#7A6A5A"); // amber-tinted stone
        b.stoneDark = Hex("#4A3A2A");
        b.leafColor = Hex("#2A4A3A"); // darker pine green
        b.trunkColor = Hex("#4A3620"); // darker, weather-beaten wood

        b.ambientTint = new Color(0.92f, 0.94f, 1.00f); // gentle cool tilt, not bleak
        b.fogTint     = new Color(0.85f, 0.85f, 0.88f, 0f);

        b.treeDensity  = 0.25f;
        b.grassDensity = 0.15f;
        b.treeStyle    = BiomeData.TreeStyleHint.Pine;

        b.ambientProfileId = "mountain";
        b.ambientVolume = 0.28f;

        EditorUtility.SetDirty(b);
    }

    static void BuildFrontier()
    {
        var b = GetOrCreate("Frontier");
        b.displayName = "Frontier";
        b.moodDescription =
            "Autumn forest gone feral — burnt oranges, amber dirt, twisted but still living trees.\n" +
            "Signals edge-of-the-world and danger WITHOUT breaking cozy-medieval warmth.\n" +
            "Reference: BotW Akkala / Deep Akkala. Mystery, not dread.";

        // Autumn/feral palette
        b.grassTop  = Hex("#6A5A2A"); // burnt-ochre tufts
        b.grassSide = Hex("#5A4A1A");
        b.dirt      = Hex("#5A3A1A"); // dark amber
        b.stone     = Hex("#6A4A36"); // warm red-brown
        b.stoneDark = Hex("#4A3020");
        b.leafColor = Hex("#8A4A1A"); // burnt orange canopy
        b.trunkColor = Hex("#3A2A1A"); // near-black bark

        b.ambientTint = new Color(1.00f, 0.85f, 0.70f); // warm amber
        b.fogTint     = new Color(0.75f, 0.55f, 0.40f, 0f); // thicker, rust-tinged

        b.treeDensity  = 0.50f;
        b.grassDensity = 0.35f;
        b.treeStyle    = BiomeData.TreeStyleHint.AutumnBurnt;

        b.ambientProfileId = "frontier";
        b.ambientVolume = 0.18f;

        EditorUtility.SetDirty(b);
    }
}
