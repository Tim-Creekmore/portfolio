using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WorldSceneSetup : EditorWindow
{
    [MenuItem("World Demo/Setup Scene")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("World Demo Setup",
            "This will create all materials, GameObjects, and configure the scene.\n\nContinue?",
            "Yes", "Cancel"))
            return;

        CreateMaterials();
        BuildHierarchy();
        ConfigureEnvironment();

        Debug.Log("World Demo scene setup complete. Press Play to test.");
    }

    [MenuItem("World Demo/Create Materials Only")]
    static void CreateMaterialsOnly()
    {
        CreateMaterials();
        Debug.Log("Materials created in Assets/Materials/");
    }

    static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        CreateMaterialAsset("TerrainMat", "Custom/Terrain");
        CreateMaterialAsset("WaterMat", "Custom/Water");
        CreateMaterialAsset("GrassImpostorMat", "Custom/GrassImpostor");

        var grassBlade = CreateMaterialAsset("GrassBladeMat", "Custom/GrassBlade");
        if (grassBlade != null) grassBlade.enableInstancing = true;

        CreateMaterialAsset("CanopyMat", "Custom/Canopy");
        CreateMaterialAsset("TrunkMat", "Custom/Trunk");

        var wildflower = CreateMaterialAsset("WildflowerMat", "Custom/Wildflower");
        if (wildflower != null) wildflower.enableInstancing = true;

        var rockMat = CreateMaterialAsset("RockMat", "Universal Render Pipeline/Lit");
        if (rockMat != null)
        {
            rockMat.SetColor("_BaseColor", new Color(0.38f, 0.34f, 0.30f));
            rockMat.SetFloat("_Smoothness", 0.04f);
            rockMat.SetFloat("_Metallic", 0f);
            rockMat.enableInstancing = true;
        }

        var skyboxMat = CreateMaterialAsset("DayNightSkyboxMat", "Custom/DayNightSkybox");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Material CreateMaterialAsset(string name, string shaderName)
    {
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            Debug.Log($"Material already exists: {name}");
            return existing;
        }

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"Shader not found: {shaderName} — skipping {name}");
            return null;
        }

        var mat = new Material(shader);
        mat.name = name;
        AssetDatabase.CreateAsset(mat, path);
        Debug.Log($"Created material: {name}");
        return mat;
    }

    static T LoadMat<T>(string name) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>($"Assets/Materials/{name}.mat");
    }

    static void BuildHierarchy()
    {
        var terrainMat       = LoadMat<Material>("TerrainMat");
        var waterMat         = LoadMat<Material>("WaterMat");
        var grassBladeMat    = LoadMat<Material>("GrassBladeMat");
        var grassImpostorMat = LoadMat<Material>("GrassImpostorMat");
        var canopyMat        = LoadMat<Material>("CanopyMat");
        var trunkMat         = LoadMat<Material>("TrunkMat");
        var rockMat          = LoadMat<Material>("RockMat");
        var wildflowerMat    = LoadMat<Material>("WildflowerMat");
        var skyboxMat        = LoadMat<Material>("DayNightSkyboxMat");

        // Root
        var world = new GameObject("World");
        var wc = world.AddComponent<WorldController>();

        // Terrain
        var terrainGO = new GameObject("Terrain");
        terrainGO.transform.SetParent(world.transform);
        terrainGO.AddComponent<MeshFilter>();
        terrainGO.AddComponent<MeshRenderer>();
        terrainGO.AddComponent<MeshCollider>();
        var tc = terrainGO.AddComponent<TerrainChunk>();
        SetField(tc, "terrainMaterial", terrainMat);
        SetField(tc, "waterMaterial", waterMat);

        // Foliage
        var foliageGO = new GameObject("Foliage");
        foliageGO.transform.SetParent(world.transform);
        var fp = foliageGO.AddComponent<FoliagePlacer>();
        SetField(fp, "grassBladeMaterial", grassBladeMat);
        SetField(fp, "grassImpostorMaterial", grassImpostorMat);
        SetField(fp, "canopyMaterial", canopyMat);
        SetField(fp, "trunkMaterial", trunkMat);
        SetField(fp, "rockMaterial", rockMat);
        SetField(fp, "wildflowerMaterial", wildflowerMat);

        // Perimeter Walls
        var wallsGO = new GameObject("PerimeterWalls");
        wallsGO.transform.SetParent(world.transform);
        wallsGO.AddComponent<PerimeterWalls>();

        // Directional Light + DayNight
        var lightGO = new GameObject("Directional Light");
        lightGO.transform.SetParent(world.transform);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.8f;
        var dn = lightGO.AddComponent<DayNight>();
        SetSerializedField(dn, "skyboxMaterial", skyboxMat);
        SetSerializedField(dn, "dayLengthSec", 720f);

        // Player
        var playerGO = CreatePlayerHierarchy();
        playerGO.transform.SetParent(world.transform);

        // Wire WorldController
        SetSerializedField(wc, "player", playerGO.GetComponent<PlayerController>());
        SetSerializedField(wc, "terrainChunk", tc);
        SetSerializedField(wc, "foliage", fp);

        // Global Volume
        var volumeGO = new GameObject("Global Volume");
        volumeGO.transform.SetParent(world.transform);
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");
        AssetDatabase.CreateAsset(profile, "Assets/Settings/WorldVolumeProfile.asset");

        var tonemapping = profile.Add<Tonemapping>();
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        var bloom = profile.Add<Bloom>();
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.8f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.0f;

        volume.profile = profile;
        EditorUtility.SetDirty(profile);

        // Crosshair Canvas
        CreateCrosshairUI(world.transform);

        Selection.activeGameObject = world;
        Debug.Log("Scene hierarchy created.");
    }

    static GameObject CreatePlayerHierarchy()
    {
        var playerGO = new GameObject("Player");
        playerGO.layer = LayerMask.NameToLayer("Default");

        var cc = playerGO.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        var pc = playerGO.AddComponent<PlayerController>();

        // Body visual (capsule, no collider)
        var bodyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyGO.name = "BodyVisual";
        Object.DestroyImmediate(bodyGO.GetComponent<Collider>());
        bodyGO.transform.SetParent(playerGO.transform);
        bodyGO.transform.localPosition = new Vector3(0, 0.9f, 0);
        bodyGO.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

        // Neck
        var neckGO = new GameObject("Neck");
        neckGO.transform.SetParent(playerGO.transform);
        neckGO.transform.localPosition = new Vector3(0, 1.6f, 0);

        // Camera FP
        var camFPGO = new GameObject("CameraFP");
        camFPGO.transform.SetParent(neckGO.transform);
        camFPGO.transform.localPosition = Vector3.zero;
        var camFP = camFPGO.AddComponent<Camera>();
        camFPGO.tag = "MainCamera";
        camFPGO.AddComponent<UniversalAdditionalCameraData>();

        // Spring arm + Camera TP
        var springArmGO = new GameObject("SpringArm");
        springArmGO.transform.SetParent(neckGO.transform);
        springArmGO.transform.localPosition = Vector3.zero;

        var camTPGO = new GameObject("CameraTP");
        camTPGO.transform.SetParent(springArmGO.transform);
        camTPGO.transform.localPosition = new Vector3(0, 1f, -4f);
        var camTP = camTPGO.AddComponent<Camera>();
        camTP.enabled = false;
        camTPGO.AddComponent<UniversalAdditionalCameraData>();

        // Wire references via SerializedObject
        var so = new SerializedObject(pc);
        so.FindProperty("neck").objectReferenceValue = neckGO.transform;
        so.FindProperty("camFP").objectReferenceValue = camFP;
        so.FindProperty("camTP").objectReferenceValue = camTP;
        so.FindProperty("springArm").objectReferenceValue = springArmGO.transform;
        so.FindProperty("bodyVisual").objectReferenceValue = bodyGO.GetComponent<MeshRenderer>();
        so.ApplyModifiedProperties();

        return playerGO;
    }

    static void CreateCrosshairUI(Transform parent)
    {
        var canvasGO = new GameObject("CrosshairCanvas");
        canvasGO.transform.SetParent(parent);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var crosshairGO = new GameObject("Crosshair");
        crosshairGO.transform.SetParent(canvasGO.transform);
        var img = crosshairGO.AddComponent<UnityEngine.UI.RawImage>();
        img.color = Color.white;

        var rt = crosshairGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(4, 4);
        rt.anchoredPosition = Vector2.zero;
    }

    static void ConfigureEnvironment()
    {
        var skyboxMat = LoadMat<Material>("DayNightSkyboxMat");
        if (skyboxMat != null)
            RenderSettings.skybox = skyboxMat;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.56f, 0.48f, 0.42f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 80f;
        RenderSettings.fogColor = new Color(0.68f, 0.62f, 0.52f);

        Debug.Log("Environment configured: skybox, fog, ambient.");
    }

    static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"Property '{fieldName}' not found on {target.GetType().Name}");
        }
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }

    static void SetSerializedField(Object target, string fieldName, float value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.floatValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
