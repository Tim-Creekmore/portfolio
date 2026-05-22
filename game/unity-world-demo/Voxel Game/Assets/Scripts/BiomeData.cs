using UnityEngine;

/// <summary>
/// Data-driven biome definition — colors, densities, ambient mood.
/// Philosophy: map edges feel mysterious, not dreadful. No biome should be "ugly" —
/// even hostile zones keep visual warmth (BotW-style).
/// </summary>
[CreateAssetMenu(fileName = "NewBiome", menuName = "Voxel Kingdom/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Unnamed";
    [TextArea] public string moodDescription;

    [Header("Ground Palette")]
    public Color grassTop   = new Color(0.353f, 0.478f, 0.227f); // VB #5a7a3a
    public Color grassSide  = new Color(0.290f, 0.416f, 0.165f); // VB #4a6a2a
    public Color dirt       = new Color(0.478f, 0.361f, 0.227f); // VB #7a5c3a
    public Color stone      = new Color(0.416f, 0.396f, 0.376f); // VB #6a6560
    public Color stoneDark  = new Color(0.290f, 0.271f, 0.251f); // VB #4a4540

    [Header("Vegetation Palette")]
    public Color leafColor  = new Color(0.227f, 0.353f, 0.165f); // VB #3a5a2a
    public Color trunkColor = new Color(0.416f, 0.282f, 0.188f); // VB #6a4830

    [Header("Atmospheric Tint")]
    [Tooltip("Multiplicative color on ambient light — shapes mood (warm/cool/amber).")]
    public Color ambientTint = Color.white;
    [Tooltip("Optional fog color override. Alpha=0 means inherit global fog.")]
    public Color fogTint = new Color(0.784f, 0.722f, 0.596f, 0f);

    [Header("Vegetation Density (0-1)")]
    [Range(0f, 1f)] public float treeDensity  = 0.6f;
    [Range(0f, 1f)] public float grassDensity = 0.8f;

    public enum TreeStyleHint { Leafy, Pine, DeadTwisted, AutumnBurnt }
    public TreeStyleHint treeStyle = TreeStyleHint.Leafy;

    [Header("Audio")]
    [Tooltip("Identifier for the ambient audio zone manager to select clips.")]
    public string ambientProfileId = "default";
    [Range(0f, 1f)] public float ambientVolume = 0.2f;
}
