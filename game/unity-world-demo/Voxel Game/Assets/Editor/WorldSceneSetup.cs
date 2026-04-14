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
        var waterMat = CreateMaterialAsset("WaterMat", "Custom/Water");
        if (waterMat != null)
        {
            waterMat.SetColor("_ShallowColor", new Color(0.30f, 0.50f, 0.42f, 0.60f));
            waterMat.SetColor("_DeepColor",    new Color(0.08f, 0.18f, 0.22f, 0.88f));
            waterMat.SetFloat("_DepthFade", 3f);
            waterMat.SetColor("_EdgeColor", new Color(0.55f, 0.65f, 0.55f, 0.70f));
            waterMat.SetFloat("_EdgeWidth", 0.5f);
            waterMat.SetFloat("_WaveHeight", 0.08f);
            waterMat.SetFloat("_WaveSpeed", 0.8f);
            waterMat.SetFloat("_WaveScale", 0.6f);
            waterMat.SetFloat("_ShimmerStr", 0.12f);
            waterMat.SetFloat("_ShimmerScale", 3f);
            waterMat.SetFloat("_ShimmerSpeed", 0.4f);
            waterMat.SetFloat("_Smoothness", 0.55f);
            waterMat.SetFloat("_Specular", 0.10f);
        }
        var grassMat = CreateMaterialAsset("GrassMat", "Custom/Grass");
        if (grassMat != null)
        {
            grassMat.SetColor("_TopColor", new Color(0.32f, 0.48f, 0.18f));
            grassMat.SetColor("_BottomColor", new Color(0.08f, 0.15f, 0.04f));
            grassMat.SetFloat("_TranslucentGain", 0.5f);
            grassMat.SetFloat("_BladeWidth", 0.04f);
            grassMat.SetFloat("_BladeWidthRandom", 0.02f);
            grassMat.SetFloat("_BladeHeight", 0.5f);
            grassMat.SetFloat("_BladeHeightRandom", 0.3f);
            grassMat.SetFloat("_BladeForward", 0.38f);
            grassMat.SetFloat("_BladeCurve", 2f);
            grassMat.SetFloat("_BendRotationRandom", 0.2f);
            grassMat.SetFloat("_TessellationUniform", 3f);
            grassMat.SetFloat("_WindStrength", 0.3f);
            grassMat.SetVector("_WindFrequency", new Vector4(0.05f, 0.05f, 0, 0));
        }

        var canopyMat = CreateMaterialAsset("CanopyMat", "Custom/VoxelBlock");
        if (canopyMat != null)
        {
            canopyMat.SetFloat("_WindStrength", 0.06f);
            canopyMat.SetFloat("_WindSpeed", 1.0f);
        }
        var trunkMat = CreateMaterialAsset("TrunkMat", "Custom/VoxelBlock");
        if (trunkMat != null)
        {
            trunkMat.SetFloat("_WindStrength", 0.02f);
            trunkMat.SetFloat("_WindSpeed", 0.8f);
        }

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

        // Prop material for farm/village/ruin objects
        var propMat = CreateMaterialAsset("PropMat", "Universal Render Pipeline/Lit");
        if (propMat != null)
        {
            propMat.SetColor("_BaseColor", new Color(0.55f, 0.45f, 0.30f));
            propMat.SetFloat("_Smoothness", 0.1f);
            propMat.SetFloat("_Metallic", 0f);
            propMat.enableInstancing = true;
        }

        // Village building materials (URP Lit)
        var villageWoodMat = CreateMaterialAsset("VillageWoodMat", "Universal Render Pipeline/Lit");
        if (villageWoodMat != null)
        {
            villageWoodMat.SetColor("_BaseColor", new Color(0.50f, 0.35f, 0.20f));
            villageWoodMat.SetFloat("_Smoothness", 0.08f);
        }
        var villageStoneMat = CreateMaterialAsset("VillageStoneMat", "Universal Render Pipeline/Lit");
        if (villageStoneMat != null)
        {
            villageStoneMat.SetColor("_BaseColor", new Color(0.52f, 0.50f, 0.46f));
            villageStoneMat.SetFloat("_Smoothness", 0.05f);
        }
        var villageRoofMat = CreateMaterialAsset("VillageRoofMat", "Universal Render Pipeline/Lit");
        if (villageRoofMat != null)
        {
            villageRoofMat.SetColor("_BaseColor", new Color(0.55f, 0.25f, 0.18f));
            villageRoofMat.SetFloat("_Smoothness", 0.06f);
        }

        // Boundary line material (unlit white)
        var boundaryMat = CreateMaterialAsset("BoundaryLineMat", "Universal Render Pipeline/Unlit");
        if (boundaryMat != null)
        {
            boundaryMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.85f));
        }

        var skyboxMat = CreateMaterialAsset("DayNightSkyboxMat", "Custom/DayNightSkybox");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Material CreateMaterialAsset(string name, string shaderName)
    {
        string path = $"Assets/Materials/{name}.mat";
        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"Shader not found: {shaderName} — skipping {name}");
            return null;
        }

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.shader != shader)
            {
                existing.shader = shader;
                EditorUtility.SetDirty(existing);
                Debug.Log($"Updated material shader: {name} → {shaderName}");
            }
            return existing;
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
        var terrainMat    = LoadMat<Material>("TerrainMat");
        var waterMat      = LoadMat<Material>("WaterMat");
        var grassMat      = LoadMat<Material>("GrassMat");
        var canopyMat     = LoadMat<Material>("CanopyMat");
        var trunkMat      = LoadMat<Material>("TrunkMat");
        var rockMat       = LoadMat<Material>("RockMat");
        var wildflowerMat = LoadMat<Material>("WildflowerMat");
        var propMat       = LoadMat<Material>("PropMat");
        var villageWoodMat  = LoadMat<Material>("VillageWoodMat");
        var villageStoneMat = LoadMat<Material>("VillageStoneMat");
        var villageRoofMat  = LoadMat<Material>("VillageRoofMat");
        var boundaryMat   = LoadMat<Material>("BoundaryLineMat");
        var skyboxMat     = LoadMat<Material>("DayNightSkyboxMat");

        foreach (var existing in Object.FindObjectsOfType<WorldController>())
            Object.DestroyImmediate(existing.gameObject);

        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null && defaultCam.transform.parent == null)
            Object.DestroyImmediate(defaultCam);

        var defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null && defaultLight.transform.parent == null)
            Object.DestroyImmediate(defaultLight);

        var defaultVolume = GameObject.Find("Global Volume");
        if (defaultVolume != null && defaultVolume.transform.parent == null)
            Object.DestroyImmediate(defaultVolume);

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
        SetField(fp, "grassMaterial", grassMat);
        SetField(fp, "canopyMaterial", canopyMat);
        SetField(fp, "trunkMaterial", trunkMat);
        SetField(fp, "rockMaterial", rockMat);
        SetField(fp, "wildflowerMaterial", wildflowerMat);
        SetField(fp, "propMaterial", propMat);

        // Cobblestone Roads
        var roadGO = new GameObject("Roads");
        roadGO.transform.SetParent(world.transform);
        var rp = roadGO.AddComponent<RoadPlacer>();
        var roadFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/RockPath/RockPath_Round_Wide.fbx");
        if (roadFbx != null)
        {
            var meshFilter = roadFbx.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
                SetField(rp, "roadTileMesh", meshFilter.sharedMesh);
        }
        else
        {
            Debug.LogWarning("Road FBX not found at Assets/Models/RockPath/RockPath_Round_Wide.fbx");
        }
        SetField(rp, "roadMaterial", propMat);

        // Village Buildings
        var villageGO = new GameObject("Village");
        villageGO.transform.SetParent(world.transform);
        var vp = villageGO.AddComponent<VillagePlacer>();
        LoadVillagePrefab(vp, "house1Prefab",       "House_1.fbx");
        LoadVillagePrefab(vp, "house2Prefab",       "House_2.fbx");
        LoadVillagePrefab(vp, "house3Prefab",       "House_3.fbx");
        LoadVillagePrefab(vp, "house4Prefab",       "House_4.fbx");
        LoadVillagePrefab(vp, "innPrefab",          "Inn.fbx");
        LoadVillagePrefab(vp, "blacksmithPrefab",   "Blacksmith.fbx");
        LoadVillagePrefab(vp, "stablePrefab",       "Stable.fbx");
        LoadVillagePrefab(vp, "bellTowerPrefab",    "Bell_Tower.fbx");
        LoadVillagePrefab(vp, "wellPrefab",         "Well.fbx");
        LoadVillagePrefab(vp, "gazeboPrefab",       "Gazebo.fbx");
        LoadVillagePrefab(vp, "bonfirePrefab",      "Bonfire.fbx");
        LoadVillagePrefab(vp, "bench1Prefab",       "Bench_1.fbx");
        LoadVillagePrefab(vp, "marketStand1Prefab", "MarketStand_1.fbx");
        LoadVillagePrefab(vp, "marketStand2Prefab", "MarketStand_2.fbx");
        LoadVillagePrefab(vp, "cartPrefab",         "Cart.fbx");
        LoadVillagePrefab(vp, "barrelPrefab",       "Barrel.fbx");
        LoadVillagePrefab(vp, "cratePrefab",        "Crate.fbx");
        LoadVillagePrefab(vp, "fencePrefab",        "Fence.fbx");
        LoadVillagePrefab(vp, "hayPrefab",          "Hay.fbx");
        SetField(vp, "woodMaterial",  villageWoodMat);
        SetField(vp, "stoneMaterial", villageStoneMat);
        SetField(vp, "roofMaterial",  villageRoofMat);

        // Biome Boundaries
        var boundaryGO = new GameObject("BiomeBoundaries");
        boundaryGO.transform.SetParent(world.transform);
        var bb = boundaryGO.AddComponent<BiomeBoundary>();
        SetField(bb, "lineMaterial", boundaryMat);

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

        // Biome Toast (attached to player)
        var toast = playerGO.AddComponent<BiomeToast>();

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

        // Toast Canvas
        CreateToastUI(world.transform, toast);

        // Test Server
        var testServerGO = new GameObject("TestServer");
        testServerGO.transform.SetParent(world.transform);
        testServerGO.AddComponent<TestServer>();

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

        var bodyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyGO.name = "BodyVisual";
        Object.DestroyImmediate(bodyGO.GetComponent<Collider>());
        bodyGO.transform.SetParent(playerGO.transform);
        bodyGO.transform.localPosition = new Vector3(0, 0.9f, 0);
        bodyGO.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

        var neckGO = new GameObject("Neck");
        neckGO.transform.SetParent(playerGO.transform);
        neckGO.transform.localPosition = new Vector3(0, 1.6f, 0);

        var camFPGO = new GameObject("CameraFP");
        camFPGO.transform.SetParent(neckGO.transform);
        camFPGO.transform.localPosition = Vector3.zero;
        var camFP = camFPGO.AddComponent<Camera>();
        camFPGO.tag = "MainCamera";
        camFPGO.AddComponent<UniversalAdditionalCameraData>();

        var springArmGO = new GameObject("SpringArm");
        springArmGO.transform.SetParent(neckGO.transform);
        springArmGO.transform.localPosition = Vector3.zero;

        var camTPGO = new GameObject("CameraTP");
        camTPGO.transform.SetParent(springArmGO.transform);
        camTPGO.transform.localPosition = new Vector3(0, 1f, -4f);
        var camTP = camTPGO.AddComponent<Camera>();
        camTP.enabled = false;
        camTPGO.AddComponent<UniversalAdditionalCameraData>();

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

    static void CreateToastUI(Transform parent, BiomeToast toastComponent)
    {
        var canvasGO = new GameObject("ToastCanvas");
        canvasGO.transform.SetParent(parent);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Toast container — Image first so RectTransform is created
        var toastGO = new GameObject("ToastPanel");
        toastGO.transform.SetParent(canvasGO.transform);
        var bg = toastGO.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.6f);

        var cg = toastGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        var panelRT = toastGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.85f);
        panelRT.anchorMax = new Vector2(0.5f, 0.85f);
        panelRT.sizeDelta = new Vector2(320, 48);
        panelRT.anchoredPosition = Vector2.zero;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(toastGO.transform);
        var label = labelGO.AddComponent<UnityEngine.UI.Text>();
        label.text = "";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Italic;
        label.color = new Color(0.95f, 0.92f, 0.85f);
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        // Wire references
        var so = new SerializedObject(toastComponent);
        var groupProp = so.FindProperty("toastGroup");
        if (groupProp != null) groupProp.objectReferenceValue = cg;
        var labelProp = so.FindProperty("toastLabel");
        if (labelProp != null) labelProp.objectReferenceValue = label;
        so.ApplyModifiedProperties();
    }

    static void ConfigureEnvironment()
    {
        var skyboxMat = LoadMat<Material>("DayNightSkyboxMat");
        if (skyboxMat != null)
            RenderSettings.skybox = skyboxMat;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.45f, 0.35f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 50f;
        RenderSettings.fogEndDistance = 140f;
        RenderSettings.fogColor = new Color(0.78f, 0.74f, 0.62f);

        Debug.Log("Environment configured: skybox, fog, ambient.");
    }

    static void LoadVillagePrefab(VillagePlacer vp, string fieldName, string fbxName)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Village/" + fbxName);
        if (go != null)
            SetField(vp, fieldName, go);
        else
            Debug.LogWarning($"Village model not found: Assets/Models/Village/{fbxName}");
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
