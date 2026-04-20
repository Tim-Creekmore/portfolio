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
        var roadTileFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/RockPath/RockPath_Round_Wide.fbx");
        if (roadTileFbx != null) SetField(vp, "roadTilePrefab", roadTileFbx);
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

        // Directional Light — visual bible: warm amber #ffcc88, 35° angle, soft shadows
        var lightGO = new GameObject("Directional Light");
        lightGO.transform.SetParent(world.transform);
        lightGO.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.8f, 0.533f); // #ffcc88
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.75f;
        light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
        var dn = lightGO.AddComponent<DayNight>();
        SetSerializedField(dn, "skyboxMaterial", skyboxMat);
        SetSerializedField(dn, "dayLengthSec", 720f);

        // Player
        var playerGO = CreatePlayerHierarchy();
        playerGO.transform.SetParent(world.transform);

        // Screen Fade overlay
        var fadeCanvasGO = new GameObject("FadeCanvas");
        fadeCanvasGO.transform.SetParent(world.transform);
        var fadeCanvas = fadeCanvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 200;
        fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        var fadeImgGO = new GameObject("FadeImage");
        fadeImgGO.transform.SetParent(fadeCanvasGO.transform);
        var fadeImg = fadeImgGO.AddComponent<UnityEngine.UI.Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f);
        fadeImg.raycastTarget = false;
        var fadeRT = fadeImgGO.GetComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero;
        fadeRT.offsetMax = Vector2.zero;

        var screenFade = fadeCanvasGO.AddComponent<ScreenFade>();
        SetField(screenFade, "fadeImage", fadeImg);

        // Death System
        var deathGO = new GameObject("DeathSystem");
        deathGO.transform.SetParent(world.transform);
        var ds = deathGO.AddComponent<DeathSystem>();
        SetField(ds, "playerHealth", playerGO.GetComponent<PlayerHealth>());
        SetField(ds, "playerStamina", playerGO.GetComponent<PlayerStamina>());
        SetField(ds, "playerTransform", playerGO.transform);
        SetField(ds, "screenFade", screenFade);
        SetField(ds, "characterController", playerGO.GetComponent<CharacterController>());
        SetField(ds, "cameraStateMachine", playerGO.GetComponent<CameraStateMachine>());

        // Interactor (R to pick up)
        var interactor = playerGO.AddComponent<Interactor>();
        SetField(interactor, "deathSystem", ds);
        SetField(interactor, "cameraStateMachine", playerGO.GetComponent<CameraStateMachine>());

        // Biome Toast (attached to player)
        var toast = playerGO.AddComponent<BiomeToast>();

        // Combat HUD
        var pickupText = CreateCombatHUD(world.transform, playerGO.GetComponent<PlayerHealth>(), playerGO.GetComponent<PlayerStamina>());
        SetField(interactor, "pickupText", pickupText);

        // Wire WorldController
        SetSerializedField(wc, "player", playerGO.GetComponent<PlayerController>());
        SetSerializedField(wc, "terrainChunk", tc);
        SetSerializedField(wc, "foliage", fp);

        // Combat System + Weapon
        SetupCombatSystem(playerGO, world.transform);

        // Wire CombatSystem to HUD (must happen after both exist)
        var combatHUD = Object.FindObjectOfType<CombatHUD>();
        if (combatHUD != null)
            SetField(combatHUD, "combatSystem", playerGO.GetComponent<CombatSystem>());

        // Test Dummy (passive, for aiming practice)
        CreateTestDummy(world.transform);

        // Attack Dummy (swings at you, for blocking practice)
        CreateAttackDummy(world.transform);

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

        // A5.3 — SSAO via URP Renderer Feature
        EnableSSAO();

        // A5.3 — Shadow settings (visual bible: soft, distance 80u, 4 cascades)
        QualitySettings.shadows = UnityEngine.ShadowQuality.All;
        QualitySettings.shadowDistance = 80f;
        QualitySettings.shadowCascades = 4;

        // Rendering crispness: anti-aliasing + frame rate
        QualitySettings.antiAliasing = 4; // 4x MSAA
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;

        // Enable MSAA on the active URP pipeline asset
        var rpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (rpAsset != null)
        {
            rpAsset.msaaSampleCount = 4;
            EditorUtility.SetDirty(rpAsset);
        }

        // A5.3 — Ambient audio
        SetupAmbientAudio(world.transform);

        // Crosshair Canvas
        CreateCrosshairUI(world.transform);

        // Toast Canvas
        CreateToastUI(world.transform, toast);

        // Squad System
        SetupSquadSystem(playerGO, world.transform);

        // Wire SquadManager to HUD (must happen after squad system exists)
        var combatHUDAfterSquad = Object.FindObjectOfType<CombatHUD>();
        var squadMgrRef = playerGO.GetComponent<SquadManager>();
        if (combatHUDAfterSquad != null && squadMgrRef != null)
            SetField(combatHUDAfterSquad, "squadManager", squadMgrRef);

        // Arena props (A5.1)
        SetupArena(world.transform);

        // A5.4 — Save/Load system (F5 save, F9 load)
        var saveSystem = playerGO.AddComponent<SaveSystem>();
        var saveSO = new SerializedObject(saveSystem);
        saveSO.FindProperty("playerHealth").objectReferenceValue = playerGO.GetComponent<PlayerHealth>();
        saveSO.FindProperty("playerStamina").objectReferenceValue = playerGO.GetComponent<PlayerStamina>();
        saveSO.FindProperty("squadManager").objectReferenceValue = playerGO.GetComponent<SquadManager>();
        saveSO.FindProperty("unitSpawner").objectReferenceValue = Object.FindObjectOfType<UnitSpawner>();
        saveSO.ApplyModifiedProperties();

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
        var csm = playerGO.AddComponent<CameraStateMachine>();
        playerGO.AddComponent<PlayerHealth>();
        playerGO.AddComponent<PlayerStamina>();

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
        camFPGO.AddComponent<AudioListener>();

        var springArmGO = new GameObject("SpringArm");
        springArmGO.transform.SetParent(neckGO.transform);
        springArmGO.transform.localPosition = Vector3.zero;

        var camTPGO = new GameObject("CameraTP");
        camTPGO.transform.SetParent(springArmGO.transform);
        camTPGO.transform.localPosition = new Vector3(0, 1f, -4f);
        var camTP = camTPGO.AddComponent<Camera>();
        camTP.enabled = false;
        camTPGO.AddComponent<UniversalAdditionalCameraData>();

        // Wire CameraStateMachine
        var csmSO = new SerializedObject(csm);
        csmSO.FindProperty("heroCam").objectReferenceValue = camFP;
        csmSO.FindProperty("thirdPersonCam").objectReferenceValue = camTP;
        csmSO.FindProperty("springArm").objectReferenceValue = springArmGO.transform;
        csmSO.FindProperty("playerBody").objectReferenceValue = playerGO.transform;
        csmSO.FindProperty("bodyVisual").objectReferenceValue = bodyGO.GetComponent<MeshRenderer>();
        csmSO.ApplyModifiedProperties();

        // Wire PlayerController
        var pcSO = new SerializedObject(pc);
        pcSO.FindProperty("cameraStateMachine").objectReferenceValue = csm;
        pcSO.FindProperty("camFP").objectReferenceValue = camFP;
        pcSO.FindProperty("playerStamina").objectReferenceValue = playerGO.GetComponent<PlayerStamina>();
        pcSO.ApplyModifiedProperties();

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

    static UnityEngine.UI.Text CreateCombatHUD(Transform parent, PlayerHealth playerHealth, PlayerStamina playerStamina)
    {
        var canvasGO = new GameObject("CombatHUDCanvas");
        canvasGO.transform.SetParent(parent);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Health bar background
        var hpBgGO = new GameObject("HealthBarBG");
        hpBgGO.transform.SetParent(canvasGO.transform);
        var hpBg = hpBgGO.AddComponent<UnityEngine.UI.Image>();
        hpBg.color = new Color(0.15f, 0.08f, 0.08f, 0.8f);
        var hpBgRT = hpBgGO.GetComponent<RectTransform>();
        hpBgRT.anchorMin = new Vector2(0f, 0f);
        hpBgRT.anchorMax = new Vector2(0f, 0f);
        hpBgRT.pivot = new Vector2(0f, 0f);
        hpBgRT.anchoredPosition = new Vector2(30f, 30f);
        hpBgRT.sizeDelta = new Vector2(260f, 22f);

        // Health bar fill — uses anchor stretching (anchorMax.x driven by CombatHUD)
        var hpFillGO = new GameObject("HealthBarFill");
        hpFillGO.transform.SetParent(hpBgGO.transform);
        var hpFill = hpFillGO.AddComponent<UnityEngine.UI.Image>();
        hpFill.color = new Color(0.7f, 0.15f, 0.12f, 1f);
        var hpFillRT = hpFillGO.GetComponent<RectTransform>();
        hpFillRT.anchorMin = new Vector2(0f, 0f);
        hpFillRT.anchorMax = new Vector2(1f, 1f);
        hpFillRT.offsetMin = new Vector2(2f, 2f);
        hpFillRT.offsetMax = new Vector2(-2f, -2f);

        // HP label
        var hpLabelGO = new GameObject("HealthLabel");
        hpLabelGO.transform.SetParent(hpBgGO.transform);
        var hpLabel = hpLabelGO.AddComponent<UnityEngine.UI.Text>();
        hpLabel.text = "HP";
        hpLabel.fontSize = 14;
        hpLabel.alignment = TextAnchor.MiddleCenter;
        hpLabel.color = new Color(0.95f, 0.9f, 0.85f, 0.9f);
        hpLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var hpLabelRT = hpLabelGO.GetComponent<RectTransform>();
        hpLabelRT.anchorMin = Vector2.zero;
        hpLabelRT.anchorMax = Vector2.one;
        hpLabelRT.offsetMin = Vector2.zero;
        hpLabelRT.offsetMax = Vector2.zero;

        // Stamina bar background
        var stBgGO = new GameObject("StaminaBarBG");
        stBgGO.transform.SetParent(canvasGO.transform);
        var stBg = stBgGO.AddComponent<UnityEngine.UI.Image>();
        stBg.color = new Color(0.08f, 0.12f, 0.08f, 0.8f);
        var stBgRT = stBgGO.GetComponent<RectTransform>();
        stBgRT.anchorMin = new Vector2(0f, 0f);
        stBgRT.anchorMax = new Vector2(0f, 0f);
        stBgRT.pivot = new Vector2(0f, 0f);
        stBgRT.anchoredPosition = new Vector2(30f, 58f);
        stBgRT.sizeDelta = new Vector2(200f, 16f);

        // Stamina bar fill — anchor-based like HP
        var stFillGO = new GameObject("StaminaBarFill");
        stFillGO.transform.SetParent(stBgGO.transform);
        var stFill = stFillGO.AddComponent<UnityEngine.UI.Image>();
        stFill.color = new Color(0.85f, 0.7f, 0.15f, 1f);
        var stFillRT = stFillGO.GetComponent<RectTransform>();
        stFillRT.anchorMin = new Vector2(0f, 0f);
        stFillRT.anchorMax = new Vector2(1f, 1f);
        stFillRT.offsetMin = new Vector2(2f, 2f);
        stFillRT.offsetMax = new Vector2(-2f, -2f);

        // Damage feedback text
        var dmgTextGO = new GameObject("DamageText");
        dmgTextGO.transform.SetParent(canvasGO.transform);
        var dmgText = dmgTextGO.AddComponent<UnityEngine.UI.Text>();
        dmgText.text = "";
        dmgText.fontSize = 16;
        dmgText.alignment = TextAnchor.MiddleLeft;
        dmgText.color = new Color(1f, 0.4f, 0.3f, 1f);
        dmgText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var dmgRT = dmgTextGO.GetComponent<RectTransform>();
        dmgRT.anchorMin = new Vector2(0f, 0f);
        dmgRT.anchorMax = new Vector2(0f, 0f);
        dmgRT.pivot = new Vector2(0f, 0f);
        dmgRT.anchoredPosition = new Vector2(300f, 32f);
        dmgRT.sizeDelta = new Vector2(200f, 20f);

        // Squad count — top left
        var squadGO = new GameObject("SquadCount");
        squadGO.transform.SetParent(canvasGO.transform);
        var squadText = squadGO.AddComponent<UnityEngine.UI.Text>();
        squadText.text = "Squad: 0/0";
        squadText.fontSize = 16;
        squadText.alignment = TextAnchor.UpperLeft;
        squadText.color = new Color(0.85f, 0.9f, 1f, 0.9f);
        squadText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var squadRT = squadGO.GetComponent<RectTransform>();
        squadRT.anchorMin = new Vector2(0f, 1f);
        squadRT.anchorMax = new Vector2(0f, 1f);
        squadRT.pivot = new Vector2(0f, 1f);
        squadRT.anchoredPosition = new Vector2(30f, -20f);
        squadRT.sizeDelta = new Vector2(160f, 24f);

        // Outgoing damage toast — centered, gentle fade
        var toastGO = new GameObject("OutgoingToast");
        toastGO.transform.SetParent(canvasGO.transform);
        var toastText = toastGO.AddComponent<UnityEngine.UI.Text>();
        toastText.text = "";
        toastText.fontSize = 18;
        toastText.alignment = TextAnchor.MiddleCenter;
        toastText.color = new Color(1f, 0.95f, 0.8f, 1f);
        toastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var toastRT = toastGO.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0.5f, 0.45f);
        toastRT.anchorMax = new Vector2(0.5f, 0.45f);
        toastRT.sizeDelta = new Vector2(200f, 28f);
        toastRT.anchoredPosition = Vector2.zero;
        toastGO.AddComponent<CanvasGroup>();

        // Hit flash overlay — full-screen red, fades on damage (placed under HUD elements)
        var hitFlashGO = new GameObject("HitFlash");
        hitFlashGO.transform.SetParent(canvasGO.transform);
        hitFlashGO.transform.SetAsFirstSibling(); // behind UI text
        var hitFlashImg = hitFlashGO.AddComponent<UnityEngine.UI.Image>();
        hitFlashImg.color = new Color(0.85f, 0.05f, 0.05f, 0f);
        hitFlashImg.raycastTarget = false;
        var hitFlashRT = hitFlashGO.GetComponent<RectTransform>();
        hitFlashRT.anchorMin = Vector2.zero;
        hitFlashRT.anchorMax = Vector2.one;
        hitFlashRT.offsetMin = Vector2.zero;
        hitFlashRT.offsetMax = Vector2.zero;

        // Block flash overlay — soft cyan/white, fades on full block
        var blockFlashGO = new GameObject("BlockFlash");
        blockFlashGO.transform.SetParent(canvasGO.transform);
        blockFlashGO.transform.SetAsFirstSibling();
        var blockFlashImg = blockFlashGO.AddComponent<UnityEngine.UI.Image>();
        blockFlashImg.color = new Color(0.7f, 0.85f, 1f, 0f);
        blockFlashImg.raycastTarget = false;
        var blockFlashRT = blockFlashGO.GetComponent<RectTransform>();
        blockFlashRT.anchorMin = Vector2.zero;
        blockFlashRT.anchorMax = Vector2.one;
        blockFlashRT.offsetMin = Vector2.zero;
        blockFlashRT.offsetMax = Vector2.zero;

        // Wire CombatHUD component
        var hud = canvasGO.AddComponent<CombatHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        so.FindProperty("playerStamina").objectReferenceValue = playerStamina;
        so.FindProperty("healthFillRT").objectReferenceValue = hpFillRT;
        so.FindProperty("healthFillImage").objectReferenceValue = hpFill;
        so.FindProperty("staminaFillRT").objectReferenceValue = stFillRT;
        so.FindProperty("staminaFillImage").objectReferenceValue = stFill;
        so.FindProperty("damageText").objectReferenceValue = dmgText;
        so.FindProperty("outgoingToast").objectReferenceValue = toastText;
        so.FindProperty("squadCountText").objectReferenceValue = squadText;
        so.FindProperty("hitFlash").objectReferenceValue = hitFlashImg;
        so.FindProperty("blockFlash").objectReferenceValue = blockFlashImg;
        so.ApplyModifiedProperties();

        // Pickup feedback text (centered, used by Interactor)
        var pickupGO = new GameObject("PickupText");
        pickupGO.transform.SetParent(canvasGO.transform);
        var pickupText = pickupGO.AddComponent<UnityEngine.UI.Text>();
        pickupText.text = "";
        pickupText.fontSize = 20;
        pickupText.alignment = TextAnchor.MiddleCenter;
        pickupText.color = new Color(0.9f, 0.85f, 0.6f, 1f);
        pickupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var pickupRT = pickupGO.GetComponent<RectTransform>();
        pickupRT.anchorMin = new Vector2(0.5f, 0.25f);
        pickupRT.anchorMax = new Vector2(0.5f, 0.25f);
        pickupRT.sizeDelta = new Vector2(300f, 30f);
        pickupRT.anchoredPosition = Vector2.zero;

        return pickupText;
    }

    static void SetupCombatSystem(GameObject playerGO, Transform worldRoot)
    {
        // Create Iron Sword ScriptableObject
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        var swordPath = "Assets/Data/IronSword.asset";
        var sword = AssetDatabase.LoadAssetAtPath<WeaponData>(swordPath);
        if (sword == null)
        {
            sword = ScriptableObject.CreateInstance<WeaponData>();
            sword.weaponName = "Iron Sword";
            sword.baseDamage = 20f;
            sword.windUpTime = 0.25f;
            sword.swingTime = 0.15f;
            sword.recoveryTime = 0.35f;
            sword.range = 1.8f;
            sword.hitRadius = 0.3f;
            sword.staminaCostAttack = 15f;
            sword.staminaCostBlock = 10f;
            AssetDatabase.CreateAsset(sword, swordPath);
        }

        // Weapon visual — simple elongated cube as placeholder sword
        var neckGO = playerGO.transform.Find("Neck");
        var camFPGO = neckGO != null ? neckGO.Find("CameraFP") : null;

        GameObject weaponPivot = new GameObject("WeaponPivot");
        if (camFPGO != null)
            weaponPivot.transform.SetParent(camFPGO, false);
        else
            weaponPivot.transform.SetParent(playerGO.transform, false);

        weaponPivot.transform.localPosition = new Vector3(0.3f, -0.25f, 0.4f);
        weaponPivot.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);

        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "SwordBlade";
        Object.DestroyImmediate(blade.GetComponent<Collider>());
        blade.transform.SetParent(weaponPivot.transform, false);
        blade.transform.localScale = new Vector3(0.05f, 0.7f, 0.04f);
        blade.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        var bladeMR = blade.GetComponent<MeshRenderer>();
        bladeMR.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bladeMR.sharedMaterial.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.72f));
        bladeMR.sharedMaterial.SetFloat("_Smoothness", 0.6f);
        bladeMR.sharedMaterial.SetFloat("_Metallic", 0.8f);

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "SwordHandle";
        Object.DestroyImmediate(handle.GetComponent<Collider>());
        handle.transform.SetParent(weaponPivot.transform, false);
        handle.transform.localScale = new Vector3(0.04f, 0.18f, 0.04f);
        handle.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        var handleMR = handle.GetComponent<MeshRenderer>();
        handleMR.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        handleMR.sharedMaterial.SetColor("_BaseColor", new Color(0.45f, 0.30f, 0.15f));
        handleMR.sharedMaterial.SetFloat("_Smoothness", 0.15f);

        var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "SwordGuard";
        Object.DestroyImmediate(guard.GetComponent<Collider>());
        guard.transform.SetParent(weaponPivot.transform, false);
        guard.transform.localScale = new Vector3(0.15f, 0.025f, 0.05f);
        guard.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        var guardMR = guard.GetComponent<MeshRenderer>();
        guardMR.sharedMaterial = bladeMR.sharedMaterial;

        // Add CombatSystem
        var combat = playerGO.AddComponent<CombatSystem>();
        var cso = new SerializedObject(combat);
        cso.FindProperty("equippedWeapon").objectReferenceValue = sword;
        cso.FindProperty("playerHealth").objectReferenceValue = playerGO.GetComponent<PlayerHealth>();
        cso.FindProperty("playerStamina").objectReferenceValue = playerGO.GetComponent<PlayerStamina>();
        cso.FindProperty("cameraStateMachine").objectReferenceValue = playerGO.GetComponent<CameraStateMachine>();

        cso.FindProperty("characterController").objectReferenceValue = playerGO.GetComponent<CharacterController>();
        var heroCam = camFPGO != null ? camFPGO.GetComponent<Camera>() : null;
        cso.FindProperty("heroCam").objectReferenceValue = heroCam;
        cso.FindProperty("attackOrigin").objectReferenceValue = camFPGO;
        cso.FindProperty("weaponVisual").objectReferenceValue = weaponPivot.transform;

        // Shield visual — flat box on left arm, hidden by default
        var shieldPivot = new GameObject("ShieldPivot");
        if (camFPGO != null)
            shieldPivot.transform.SetParent(camFPGO, false);
        else
            shieldPivot.transform.SetParent(playerGO.transform, false);

        shieldPivot.transform.localPosition = new Vector3(-0.35f, -0.2f, 0.35f);
        shieldPivot.transform.localRotation = Quaternion.Euler(5f, 10f, 0f);

        var shieldFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shieldFace.name = "ShieldFace";
        Object.DestroyImmediate(shieldFace.GetComponent<Collider>());
        shieldFace.transform.SetParent(shieldPivot.transform, false);
        shieldFace.transform.localScale = new Vector3(0.45f, 0.55f, 0.04f);
        shieldFace.transform.localPosition = Vector3.zero;
        var shieldMR = shieldFace.GetComponent<MeshRenderer>();
        shieldMR.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        shieldMR.sharedMaterial.SetColor("_BaseColor", new Color(0.45f, 0.30f, 0.15f));
        shieldMR.sharedMaterial.SetFloat("_Smoothness", 0.12f);

        var shieldRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shieldRim.name = "ShieldRim";
        Object.DestroyImmediate(shieldRim.GetComponent<Collider>());
        shieldRim.transform.SetParent(shieldPivot.transform, false);
        shieldRim.transform.localScale = new Vector3(0.50f, 0.60f, 0.02f);
        shieldRim.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        var rimMR = shieldRim.GetComponent<MeshRenderer>();
        rimMR.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rimMR.sharedMaterial.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.52f));
        rimMR.sharedMaterial.SetFloat("_Metallic", 0.6f);
        rimMR.sharedMaterial.SetFloat("_Smoothness", 0.4f);

        shieldPivot.SetActive(false);

        cso.FindProperty("shieldVisual").objectReferenceValue = shieldPivot.transform;
        cso.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
    }

    static void CreateTestDummy(Transform worldRoot)
    {
        Vector3 spawnPos = WorldData.GetSpawnPosition();
        float dummyX = spawnPos.x + 4f;
        float dummyZ = spawnPos.z;
        float dummyY = WorldData.HeightSmooth(dummyX, dummyZ);

        var dummyGO = new GameObject("TestDummy");
        dummyGO.transform.SetParent(worldRoot);
        dummyGO.transform.position = new Vector3(dummyX, dummyY, dummyZ);

        var litShader = Shader.Find("Universal Render Pipeline/Lit");

        // Zone colors (distinct so you can tell them apart)
        var headMat = new Material(litShader);
        headMat.SetColor("_BaseColor", new Color(0.85f, 0.65f, 0.25f)); // gold/yellow
        var leftMat = new Material(litShader);
        leftMat.SetColor("_BaseColor", new Color(0.3f, 0.55f, 0.7f));   // blue
        var rightMat = new Material(litShader);
        rightMat.SetColor("_BaseColor", new Color(0.7f, 0.35f, 0.55f)); // purple/pink
        var legsMat = new Material(litShader);
        legsMat.SetColor("_BaseColor", new Color(0.45f, 0.6f, 0.35f));  // green
        var postMat = new Material(litShader);
        postMat.SetColor("_BaseColor", new Color(0.45f, 0.32f, 0.18f)); // wood brown

        // Head zone — small box at top (Overhead)
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Zone_Head";
        head.transform.SetParent(dummyGO.transform, false);
        head.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        head.transform.localScale = new Vector3(0.45f, 0.35f, 0.38f);
        head.GetComponent<MeshRenderer>().sharedMaterial = headMat;

        // Left torso zone — box on left side (Left slash)
        var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = "Zone_Left";
        left.transform.SetParent(dummyGO.transform, false);
        left.transform.localPosition = new Vector3(-0.25f, 1.45f, 0f);
        left.transform.localScale = new Vector3(0.32f, 0.7f, 0.38f);
        left.GetComponent<MeshRenderer>().sharedMaterial = leftMat;

        // Right torso zone — box on right side (Right slash)
        var right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = "Zone_Right";
        right.transform.SetParent(dummyGO.transform, false);
        right.transform.localPosition = new Vector3(0.25f, 1.45f, 0f);
        right.transform.localScale = new Vector3(0.32f, 0.7f, 0.38f);
        right.GetComponent<MeshRenderer>().sharedMaterial = rightMat;

        // Legs zone — box below torso (Thrust)
        var legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legs.name = "Zone_Legs";
        legs.transform.SetParent(dummyGO.transform, false);
        legs.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        legs.transform.localScale = new Vector3(0.5f, 0.8f, 0.38f);
        legs.GetComponent<MeshRenderer>().sharedMaterial = legsMat;

        // Wooden post behind the dummy
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        Object.DestroyImmediate(post.GetComponent<Collider>());
        post.transform.SetParent(dummyGO.transform, false);
        post.transform.localPosition = new Vector3(0f, 0.6f, -0.25f);
        post.transform.localScale = new Vector3(0.1f, 1.1f, 0.1f);
        post.GetComponent<MeshRenderer>().sharedMaterial = postMat;

        // Components
        var targetHP = dummyGO.AddComponent<TargetHealth>();
        var thSO = new SerializedObject(targetHP);
        thSO.FindProperty("maxHP").floatValue = 999999f;
        thSO.FindProperty("height").floatValue = 2.2f;
        thSO.ApplyModifiedProperties();

        var dummy = dummyGO.AddComponent<TestDummy>();
        // Wire zone renderers after play mode starts — use serialized call
        dummy.AssignZoneRenderers(
            head.GetComponent<MeshRenderer>(),
            left.GetComponent<MeshRenderer>(),
            right.GetComponent<MeshRenderer>(),
            legs.GetComponent<MeshRenderer>());
    }

    static void CreateAttackDummy(Transform worldRoot)
    {
        Vector3 spawnPos = WorldData.GetSpawnPosition();
        float dx = spawnPos.x - 4f;
        float dz = spawnPos.z;
        float dy = WorldData.HeightSmooth(dx, dz);

        var dummyGO = new GameObject("AttackDummy");
        dummyGO.transform.SetParent(worldRoot);
        dummyGO.transform.position = new Vector3(dx, dy, dz);

        var litShader = Shader.Find("Universal Render Pipeline/Lit");

        var bodyMat = new Material(litShader);
        bodyMat.SetColor("_BaseColor", new Color(0.6f, 0.25f, 0.2f));

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(dummyGO.transform, false);
        body.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        body.transform.localScale = new Vector3(0.5f, 0.8f, 0.35f);
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Head
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        Object.DestroyImmediate(head.GetComponent<Collider>());
        head.transform.SetParent(dummyGO.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        head.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Legs
        var legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legs.name = "Legs";
        Object.DestroyImmediate(legs.GetComponent<Collider>());
        legs.transform.SetParent(dummyGO.transform, false);
        legs.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        legs.transform.localScale = new Vector3(0.4f, 0.7f, 0.3f);
        legs.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Sword arm pivot
        var armPivot = new GameObject("SwordArm");
        armPivot.transform.SetParent(dummyGO.transform, false);
        armPivot.transform.localPosition = new Vector3(0.35f, 1.3f, 0f);
        armPivot.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);

        var swordBlade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        swordBlade.name = "Blade";
        Object.DestroyImmediate(swordBlade.GetComponent<Collider>());
        swordBlade.transform.SetParent(armPivot.transform, false);
        swordBlade.transform.localScale = new Vector3(0.08f, 0.85f, 0.05f);
        swordBlade.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        var bladeMat = new Material(litShader);
        bladeMat.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.75f));
        bladeMat.SetFloat("_Metallic", 0.8f);
        bladeMat.SetFloat("_Smoothness", 0.6f);
        swordBlade.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;

        // Crossguard
        var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Guard";
        Object.DestroyImmediate(guard.GetComponent<Collider>());
        guard.transform.SetParent(armPivot.transform, false);
        guard.transform.localScale = new Vector3(0.22f, 0.04f, 0.06f);
        guard.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        guard.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;

        var swordHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        swordHandle.name = "Handle";
        Object.DestroyImmediate(swordHandle.GetComponent<Collider>());
        swordHandle.transform.SetParent(armPivot.transform, false);
        swordHandle.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);
        swordHandle.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        var handleMat = new Material(litShader);
        handleMat.SetColor("_BaseColor", new Color(0.4f, 0.28f, 0.15f));
        swordHandle.GetComponent<MeshRenderer>().sharedMaterial = handleMat;

        // Wooden post
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        Object.DestroyImmediate(post.GetComponent<Collider>());
        post.transform.SetParent(dummyGO.transform, false);
        post.transform.localPosition = new Vector3(0f, 0.5f, -0.22f);
        post.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);
        var postMat = new Material(litShader);
        postMat.SetColor("_BaseColor", new Color(0.45f, 0.32f, 0.18f));
        post.GetComponent<MeshRenderer>().sharedMaterial = postMat;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(dummyGO.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        var tm = labelGO.AddComponent<TextMesh>();
        tm.text = "ATTACKER";
        tm.characterSize = 0.12f;
        tm.fontSize = 48;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(0.9f, 0.3f, 0.2f);
        labelGO.AddComponent<Billboard>();

        // Component
        var ad = dummyGO.AddComponent<AttackDummy>();
        ad.SetSwordArm(armPivot.transform);
    }

    static void SetupArena(Transform worldRoot)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        Vector3 spawn = WorldData.GetSpawnPosition();

        // Arena is 50x50, centered at world center (60, 60), spawn at south end
        float cx = 60f, cz = 60f;
        float halfSize = 25f;

        // ── Wooden fence walls ──────────────────────────────────────────
        var woodMat = new Material(litShader);
        woodMat.SetColor("_BaseColor", new Color(0.541f, 0.376f, 0.251f)); // #8a6040
        woodMat.SetFloat("_Smoothness", 0.1f);

        var woodDarkMat = new Material(litShader);
        woodDarkMat.SetColor("_BaseColor", new Color(0.416f, 0.282f, 0.188f)); // #6a4830
        woodDarkMat.SetFloat("_Smoothness", 0.08f);

        var arenaRoot = new GameObject("Arena");
        arenaRoot.transform.SetParent(worldRoot);

        // Fence posts + rails along 4 sides
        float postSpacing = 3f;
        float postHeight = 2.2f;
        float railHeight = 1.4f;

        // North, South, East, West walls
        Vector3[] wallStarts = {
            new Vector3(cx - halfSize, 0, cz + halfSize), // North (left to right)
            new Vector3(cx - halfSize, 0, cz - halfSize), // South
            new Vector3(cx + halfSize, 0, cz - halfSize), // East (south to north)
            new Vector3(cx - halfSize, 0, cz - halfSize), // West
        };
        Vector3[] wallDirs = {
            Vector3.right,  // North
            Vector3.right,  // South
            Vector3.forward, // East
            Vector3.forward, // West
        };
        float[] wallLengths = { 50f, 50f, 50f, 50f };

        for (int w = 0; w < 4; w++)
        {
            int postCount = Mathf.FloorToInt(wallLengths[w] / postSpacing) + 1;
            for (int p = 0; p < postCount; p++)
            {
                Vector3 pos = wallStarts[w] + wallDirs[w] * (p * postSpacing);
                float y = WorldData.HeightSmooth(pos.x, pos.z);

                // Post
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"FencePost_{w}_{p}";
                post.transform.SetParent(arenaRoot.transform, false);
                post.transform.position = new Vector3(pos.x, y + postHeight * 0.5f, pos.z);
                post.transform.localScale = new Vector3(0.15f, postHeight, 0.15f);
                post.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;

                // Rail between this post and next (keep colliders so player can't walk through)
                if (p < postCount - 1)
                {
                    var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rail.name = $"FenceRail_{w}_{p}";
                    rail.transform.SetParent(arenaRoot.transform, false);

                    Vector3 nextPos = wallStarts[w] + wallDirs[w] * ((p + 1) * postSpacing);
                    float nextY = WorldData.HeightSmooth(nextPos.x, nextPos.z);
                    float midY = (y + nextY) * 0.5f + railHeight;
                    Vector3 mid = (pos + nextPos) * 0.5f;

                    // Thicken collision footprint slightly (visual stays slim)
                    float lengthAxis = postSpacing;
                    float crossAxis = 0.18f;
                    rail.transform.position = new Vector3(mid.x, midY, mid.z);
                    rail.transform.localScale = new Vector3(
                        wallDirs[w] == Vector3.right ? lengthAxis : crossAxis,
                        0.18f,
                        wallDirs[w] == Vector3.forward ? lengthAxis : crossAxis);
                    rail.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

                    // Second rail lower
                    var rail2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rail2.name = $"FenceRail2_{w}_{p}";
                    rail2.transform.SetParent(arenaRoot.transform, false);
                    rail2.transform.position = new Vector3(mid.x, midY - 0.5f, mid.z);
                    rail2.transform.localScale = rail.transform.localScale;
                    rail2.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                }
            }
        }

        // ── Rock props for cover ────────────────────────────────────────
        var stoneMat = new Material(litShader);
        stoneMat.SetColor("_BaseColor", new Color(0.416f, 0.396f, 0.376f)); // #6a6560
        stoneMat.SetFloat("_Smoothness", 0.15f);

        var stoneDarkMat = new Material(litShader);
        stoneDarkMat.SetColor("_BaseColor", new Color(0.29f, 0.271f, 0.251f)); // #4a4540
        stoneDarkMat.SetFloat("_Smoothness", 0.12f);

        // Large boulders for cover — tall enough to hide behind (player is 1.8u)
        // Each entry: x offset from center, z offset, scaleX, scaleY, scaleZ, rotation
        float[][] boulders = {
            // Big central boulder cluster
            new float[]{ -2f,  6f,  3.5f, 2.4f, 3.0f,  15f },
            new float[]{ -4f,  7f,  2.0f, 1.8f, 2.2f,  50f },
            // Left flank cover
            new float[]{-14f,  2f,  4.0f, 2.8f, 3.5f, -20f },
            new float[]{-12f,  0f,  2.2f, 1.6f, 2.0f,  70f },
            // Right flank cover
            new float[]{ 12f, -4f,  3.8f, 2.5f, 3.2f,  35f },
            new float[]{ 14f, -2f,  1.8f, 2.0f, 1.6f, 110f },
            // Mid-field obstacles
            new float[]{  5f, 12f,  3.0f, 2.2f, 2.8f, -45f },
            new float[]{ -8f,-10f,  2.8f, 2.0f, 3.0f,  80f },
            // Near spawn (player side)
            new float[]{  8f, -15f, 3.2f, 2.6f, 2.5f,  25f },
            new float[]{ -6f, -18f, 2.5f, 1.9f, 2.8f, -60f },
            // Near enemy side
            new float[]{  0f, 18f,  4.2f, 3.0f, 3.8f,   0f },
            new float[]{  9f, 16f,  2.0f, 1.5f, 2.2f,  90f },
        };

        for (int i = 0; i < boulders.Length; i++)
        {
            float[] b = boulders[i];
            float bx = cx + b[0];
            float bz = cz + b[1];
            float by = WorldData.HeightSmooth(bx, bz);

            var boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = $"Boulder_{i}";
            boulder.transform.SetParent(arenaRoot.transform, false);
            boulder.transform.position = new Vector3(bx, by + b[3] * 0.35f, bz);
            boulder.transform.localScale = new Vector3(b[2], b[3], b[4]);
            boulder.transform.rotation = Quaternion.Euler(
                (i % 3) * 5f - 5f, b[5], (i % 2) * 3f);
            boulder.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? stoneMat : stoneDarkMat;

            // Accent rock at the base
            var accent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            accent.name = $"BoulderAccent_{i}";
            Object.DestroyImmediate(accent.GetComponent<Collider>());
            accent.transform.SetParent(arenaRoot.transform, false);
            float aOff = (i % 2 == 0) ? 1f : -1f;
            accent.transform.position = new Vector3(bx + aOff, by + 0.3f, bz + aOff * 0.6f);
            accent.transform.localScale = new Vector3(b[2] * 0.35f, b[3] * 0.4f, b[4] * 0.35f);
            accent.transform.rotation = Quaternion.Euler(8f, b[5] + 40f, 0f);
            accent.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? stoneDarkMat : stoneMat;
        }

        // ── Torch lights (warm point lights) ────────────────────────────
        Vector3[] torchPositions = {
            new Vector3(cx - 10f, 0f, cz + 10f),
            new Vector3(cx + 10f, 0f, cz - 10f),
        };

        for (int t = 0; t < torchPositions.Length; t++)
        {
            float tx = torchPositions[t].x;
            float tz = torchPositions[t].z;
            float ty = WorldData.HeightSmooth(tx, tz);

            // Torch post
            var torchPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchPost.name = $"TorchPost_{t}";
            Object.DestroyImmediate(torchPost.GetComponent<Collider>());
            torchPost.transform.SetParent(arenaRoot.transform, false);
            torchPost.transform.position = new Vector3(tx, ty + 1.2f, tz);
            torchPost.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            torchPost.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;

            // Torch head (fire cube)
            var torchHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torchHead.name = $"TorchHead_{t}";
            Object.DestroyImmediate(torchHead.GetComponent<Collider>());
            torchHead.transform.SetParent(arenaRoot.transform, false);
            torchHead.transform.position = new Vector3(tx, ty + 2.5f, tz);
            torchHead.transform.localScale = new Vector3(0.15f, 0.2f, 0.15f);
            var fireMat = new Material(litShader);
            fireMat.SetColor("_BaseColor", new Color(1f, 0.6f, 0.15f));
            fireMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 3f);
            fireMat.EnableKeyword("_EMISSION");
            torchHead.GetComponent<MeshRenderer>().sharedMaterial = fireMat;

            // Point light
            var lightGO = new GameObject($"TorchLight_{t}");
            lightGO.transform.SetParent(arenaRoot.transform, false);
            lightGO.transform.position = new Vector3(tx, ty + 2.8f, tz);
            var pointLight = lightGO.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.7f, 0.3f);
            pointLight.intensity = 2.5f;
            pointLight.range = 12f;
            pointLight.shadows = LightShadows.Soft;
        }

        Debug.Log("Arena setup: fences, rocks, torches placed.");
    }

    static void ConfigureEnvironment()
    {
        var skyboxMat = LoadMat<Material>("DayNightSkyboxMat");
        if (skyboxMat != null)
            RenderSettings.skybox = skyboxMat;

        // Visual bible: ambient warm grey #886644 at 0.4 intensity
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.533f, 0.4f, 0.267f) * 0.4f;

        // Visual bible: fog #c8b898, start 40u, end 120u
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 120f;
        RenderSettings.fogColor = new Color(0.784f, 0.722f, 0.596f); // #c8b898

        Debug.Log("Environment configured: visual bible lighting, fog, ambient.");
    }

    static void EnableSSAO()
    {
        var rpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (rpAsset == null)
        {
            Debug.LogWarning("No URP pipeline asset found — SSAO not configured.");
            return;
        }

        var dataField = rpAsset.GetType().GetField("m_RendererDataList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField == null) return;

        var dataList = dataField.GetValue(rpAsset) as ScriptableRendererData[];
        if (dataList == null || dataList.Length == 0) return;

        var rendererData = dataList[0] as UniversalRendererData;
        if (rendererData == null) return;

        bool hasSSAO = false;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
            {
                feature.SetActive(true);
                hasSSAO = true;

                var so = new SerializedObject(feature);
                var settings = so.FindProperty("m_Settings");
                if (settings != null)
                {
                    var intensity = settings.FindPropertyRelative("Intensity");
                    if (intensity != null) intensity.floatValue = 1.0f;
                    var radius = settings.FindPropertyRelative("Radius");
                    if (radius != null) radius.floatValue = 0.4f;
                    so.ApplyModifiedProperties();
                }
                break;
            }
        }

        if (!hasSSAO)
        {
            // ScreenSpaceAmbientOcclusion is internal in URP 14.x — use reflection
            var urpAssembly = typeof(UniversalRenderPipelineAsset).Assembly;
            var ssaoType = urpAssembly.GetType(
                "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
            if (ssaoType == null)
            {
                Debug.LogWarning("SSAO type not found in URP assembly. Add it manually via the Renderer Data inspector.");
                return;
            }

            var ssaoFeature = ScriptableObject.CreateInstance(ssaoType) as ScriptableRendererFeature;
            if (ssaoFeature == null) return;
            ssaoFeature.name = "ScreenSpaceAmbientOcclusion";

            var so = new SerializedObject(ssaoFeature);
            var settings = so.FindProperty("m_Settings");
            if (settings != null)
            {
                var intensity = settings.FindPropertyRelative("Intensity");
                if (intensity != null) intensity.floatValue = 1.0f;
                var radius = settings.FindPropertyRelative("Radius");
                if (radius != null) radius.floatValue = 0.4f;
                so.ApplyModifiedProperties();
            }

            var featuresField = rendererData.GetType().GetField("m_RendererFeatures",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (featuresField != null)
            {
                var list = featuresField.GetValue(rendererData) as System.Collections.Generic.List<ScriptableRendererFeature>;
                if (list != null)
                {
                    AssetDatabase.AddObjectToAsset(ssaoFeature, rendererData);
                    list.Add(ssaoFeature);
                    featuresField.SetValue(rendererData, list);
                    EditorUtility.SetDirty(rendererData);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        EditorUtility.SetDirty(rendererData);
        Debug.Log("SSAO renderer feature enabled (radius 0.4, intensity 1.0).");
    }

    static void SetupAmbientAudio(Transform worldRoot)
    {
        var audioRoot = new GameObject("AmbientAudio");
        audioRoot.transform.SetParent(worldRoot, false);

        var windGO = new GameObject("WindLoop");
        windGO.transform.SetParent(audioRoot.transform, false);
        var windSource = windGO.AddComponent<AudioSource>();
        windSource.loop = true;
        windSource.playOnAwake = true;
        windSource.spatialBlend = 0f;
        windSource.volume = 0.15f;
        windSource.priority = 200;

        var birdsGO = new GameObject("BirdsLoop");
        birdsGO.transform.SetParent(audioRoot.transform, false);
        var birdsSource = birdsGO.AddComponent<AudioSource>();
        birdsSource.loop = true;
        birdsSource.playOnAwake = true;
        birdsSource.spatialBlend = 0f;
        birdsSource.volume = 0.08f;
        birdsSource.priority = 200;

        audioRoot.AddComponent<AmbientAudio>();

        Debug.Log("Ambient audio setup: WindLoop + BirdsLoop AudioSources ready.");
    }

    static void LoadVillagePrefab(VillagePlacer vp, string fieldName, string fbxName)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Village/" + fbxName);
        if (go != null)
            SetField(vp, fieldName, go);
        else
            Debug.LogWarning($"Village model not found: Assets/Models/Village/{fbxName}");
    }

    static void SetupSquadSystem(GameObject playerGO, Transform worldRoot)
    {
        var litShader = Shader.Find("Universal Render Pipeline/Lit");

        // Create Militia UnitData asset
        string dataPath = "Assets/Data/Militia.asset";
        var militia = AssetDatabase.LoadAssetAtPath<UnitData>(dataPath);
        if (militia == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            militia = ScriptableObject.CreateInstance<UnitData>();
            militia.unitName = "Militia";
            militia.maxHP = 60f;
            militia.damage = 10f;
            militia.attackInterval = 1.5f;
            militia.attackRange = 1.8f;
            militia.moveSpeed = 3.5f;
            militia.followDistance = 2.5f;
            militia.threatDetectionRange = 15f;
            militia.moraleThreshold = 30f;
            AssetDatabase.CreateAsset(militia, dataPath);
        }

        // Squad Manager on player
        var squadMgr = playerGO.AddComponent<SquadManager>();
        SetField(squadMgr, "followTarget", playerGO.transform);

        // Commander Input on player
        var cmdInput = playerGO.AddComponent<CommanderInput>();
        SetField(cmdInput, "cameraStateMachine", playerGO.GetComponent<CameraStateMachine>());
        SetField(cmdInput, "squadManager", squadMgr);

        Vector3 spawn = WorldData.GetSpawnPosition();

        // Friendly unit material
        var friendlyMat = new Material(litShader);
        friendlyMat.SetColor("_BaseColor", new Color(0.25f, 0.45f, 0.7f));

        var friendlyArmorMat = new Material(litShader);
        friendlyArmorMat.SetColor("_BaseColor", new Color(0.35f, 0.55f, 0.8f));

        // Spawn 4 friendly units in a circle around spawn
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            float radius = 3f;
            float ux = spawn.x + Mathf.Sin(angle) * radius;
            float uz = spawn.z + Mathf.Cos(angle) * radius;
            float uy = WorldData.HeightSmooth(ux, uz);

            var unitGO = CreateUnitVisual($"FriendlyUnit_{i}", new Vector3(ux, uy, uz),
                worldRoot, friendlyMat, friendlyArmorMat, "MILITIA");

            unitGO.AddComponent<UnitHealth>();
            var ai = unitGO.AddComponent<UnitAI>();
            var aiSO = new SerializedObject(ai);
            aiSO.FindProperty("unitData").objectReferenceValue = militia;
            aiSO.FindProperty("followTarget").objectReferenceValue = playerGO.transform;
            aiSO.FindProperty("formationIndex").intValue = i;
            aiSO.ApplyModifiedProperties();
        }

        // 8 enemy units spread across north half of arena
        var enemyMat = new Material(litShader);
        enemyMat.SetColor("_BaseColor", new Color(0.7f, 0.2f, 0.15f));

        var enemyArmorMat = new Material(litShader);
        enemyArmorMat.SetColor("_BaseColor", new Color(0.8f, 0.3f, 0.2f));

        Vector2[] enemyPositions = {
            new Vector2(60f, 78f),  new Vector2(54f, 75f),
            new Vector2(66f, 76f),  new Vector2(48f, 68f),
            new Vector2(72f, 66f),  new Vector2(58f, 70f),
            new Vector2(65f, 72f),  new Vector2(60f, 82f),
        };

        for (int i = 0; i < enemyPositions.Length; i++)
        {
            float ex = enemyPositions[i].x;
            float ez = enemyPositions[i].y;
            float ey = WorldData.HeightSmooth(ex, ez);

            var enemyGO = CreateUnitVisual($"EnemyUnit_{i}", new Vector3(ex, ey, ez),
                worldRoot, enemyMat, enemyArmorMat, "ENEMY");

            enemyGO.AddComponent<EnemyTag>();
            enemyGO.AddComponent<UnitHealth>();
            var eAI = enemyGO.AddComponent<UnitAI>();
            var eaiSO = new SerializedObject(eAI);
            eaiSO.FindProperty("unitData").objectReferenceValue = militia;
            eaiSO.FindProperty("formationIndex").intValue = i;
            eaiSO.FindProperty("initialState").enumValueIndex = (int)UnitAI.UnitState.HoldPosition;
            eaiSO.ApplyModifiedProperties();
        }

        // UnitSpawner for runtime respawning
        var spawnerGO = new GameObject("UnitSpawner");
        spawnerGO.transform.SetParent(worldRoot);
        var spawner = spawnerGO.AddComponent<UnitSpawner>();
        SetField(spawner, "militiaData", militia);
        SetField(spawner, "playerTransform", playerGO.transform);
        SetField(spawner, "squadManager", squadMgr);

        // Wire UnitSpawner to DeathSystem
        var deathSys = Object.FindObjectOfType<DeathSystem>();
        if (deathSys != null)
            SetField(deathSys, "unitSpawner", spawner);
    }

    static GameObject CreateUnitVisual(string name, Vector3 position, Transform parent,
        Material bodyMat, Material armorMat, string label)
    {
        var unitGO = new GameObject(name);
        unitGO.transform.SetParent(parent);
        unitGO.transform.position = position;

        var cc = unitGO.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.25f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.3f;

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(unitGO.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.4f, 0.5f, 0.3f);
        body.GetComponent<MeshRenderer>().sharedMaterial = armorMat;

        // Head
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        Object.DestroyImmediate(head.GetComponent<Collider>());
        head.transform.SetParent(unitGO.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        head.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
        head.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Legs
        var leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLeg.name = "LeftLeg";
        Object.DestroyImmediate(leftLeg.GetComponent<Collider>());
        leftLeg.transform.SetParent(unitGO.transform, false);
        leftLeg.transform.localPosition = new Vector3(-0.1f, 0.25f, 0f);
        leftLeg.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
        leftLeg.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        var rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLeg.name = "RightLeg";
        Object.DestroyImmediate(rightLeg.GetComponent<Collider>());
        rightLeg.transform.SetParent(unitGO.transform, false);
        rightLeg.transform.localPosition = new Vector3(0.1f, 0.25f, 0f);
        rightLeg.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
        rightLeg.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

        // Sword
        var litShader = Shader.Find("Universal Render Pipeline/Lit");
        var swordMat = new Material(litShader);
        swordMat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.65f));
        swordMat.SetFloat("_Metallic", 0.7f);

        var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sword.name = "Sword";
        Object.DestroyImmediate(sword.GetComponent<Collider>());
        sword.transform.SetParent(unitGO.transform, false);
        sword.transform.localPosition = new Vector3(0.3f, 0.9f, 0f);
        sword.transform.localScale = new Vector3(0.04f, 0.6f, 0.03f);
        sword.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        sword.GetComponent<MeshRenderer>().sharedMaterial = swordMat;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(unitGO.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        var tm = labelGO.AddComponent<TextMesh>();
        tm.text = label;
        tm.characterSize = 0.08f;
        tm.fontSize = 48;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = bodyMat.GetColor("_BaseColor");
        labelGO.AddComponent<Billboard>();

        return unitGO;
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
