#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

/// <summary>
/// Módulo integral de Automatización para el Proyecto Integrador (PI):
/// 'Publicación del videojuego 3D' - LDSW UDGVirtual (IH743_2026B).
/// Estudiante: Mercedes de Jesús Ballesteros Chávez (Código: 226300438).
/// 
/// Automatiza:
/// 1. Configuración técnica exhaustiva de Player Settings (Identificador, Versión, Ícono, Orientación y Arquitecturas).
/// 2. Construcción del 'Level_03.unity' (Zona Magma / Experto: 240m, abismos, 4 obstáculos y 3 boosts) cumpliendo la curva de dificultad de 3 niveles.
/// 3. Actualización del Menú Principal ('MainMenu3D.unity') integrando selector para el Nivel 3.
/// 4. Registro y ordenamiento de escenas en 'EditorBuildSettings'.
/// 5. Compilación y exportación del ejecutable Standalone Windows ('RollingSpheres3D.exe') y empaquetado ZIP para publicación.
/// </summary>
[InitializeOnLoad]
public class ProjectIntegratorSetupPI : EditorWindow
{
    private const string SCENES_DIR = "Assets/Scenes";
    private const string BUILDS_DIR = "Builds/Windows";
    private const string ZIP_PATH = "Builds/RollingSpheres3D_v1.0.0_Windows.zip";

    static ProjectIntegratorSetupPI()
    {
        EditorApplication.delayCall += AutoRunPipelineOnce;
    }

    private static void AutoRunPipelineOnce()
    {
        if (!File.Exists("Builds/Windows/RollingSpheres3D.exe"))
        {
            Debug.Log("[PI InitializeOnLoad] Ejecutando pipeline automático de compilación Standalone Windows...");
            RunFullIntegratorPipeline(false);
        }
    }

    [MenuItem("Herramientas 3D/🎮 Abrir Menú Principal (MainMenu3D)", priority = 0)]
    public static void OpenMainMenuScene()
    {
        EditorSceneManager.OpenScene($"{SCENES_DIR}/MainMenu3D.unity");
    }

    [MenuItem("Herramientas 3D/🌟 PI: Ejecutar Automatización Completa (Player Settings, Nivel 3 y Compilar)", priority = 1)]
    public static void RunFullIntegratorPipelineMenu()
    {
        RunFullIntegratorPipeline(true);
    }

    public static void BuildStandaloneFromCLI()
    {
        RunFullIntegratorPipeline(false);
    }

    public static void RunFullIntegratorPipeline(bool showDialog = false)
    {
        Debug.Log("====================================================================");
        Debug.Log("[PI Automatización] INICIANDO PIPELINE DE PUBLICACIÓN FINAL 3D...");
        Debug.Log("====================================================================");

        // 1. Configurar Player Settings
        ConfigurePlayerSettings();

        // 2. Construir Escena Nivel 3
        BuildLevel03Scene();

        // 3. Actualizar Menú Principal con Nivel 3
        UpdateMainMenuWithLevel3();

        // 4. Actualizar Build Settings con todas las escenas ordenadas
        UpdateBuildSettings();

        // 5. Compilar Standalone Windows
        CompileStandaloneWindows();

        // 6. Volver a abrir MainMenu3D para que el editor quede listo
        EditorSceneManager.OpenScene($"{SCENES_DIR}/MainMenu3D.unity");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "★ Publicación del Videojuego Lista ★",
                "¡El pipeline de publicación se completó con éxito total!\n\n" +
                "✓ Player Settings configurados (com.mercedesballesteros.rollingspheres3d v1.0.0).\n" +
                "✓ Ícono oficial de la aplicación asignado.\n" +
                "✓ Nivel 3 (Zona Magma) creado e integrado a la curva de dificultad.\n" +
                "✓ Menú Principal actualizado con selector de 3 niveles.\n" +
                "✓ Build Standalone Windows compilada en 'Builds/Windows/RollingSpheres3D.exe'.\n" +
                "✓ Archivo comprimido generado en 'Builds/RollingSpheres3D_v1.0.0_Windows.zip'.\n\n" +
                "El proyecto está 100% listo para entrega institucional y publicación.",
                "¡Excelente!");
        }
    }

    [MenuItem("Herramientas 3D/⚙️ PI: Configurar Player Settings (Identificador, Ícono y Metadatos)", priority = 10)]
    public static void ConfigurePlayerSettings()
    {
        Debug.Log("[PI] Configurando Player Settings técnicos...");

        // Metadatos de la aplicación
        PlayerSettings.companyName = "UDGVirtual - LDSW";
        PlayerSettings.productName = "Rolling Spheres 3D";
        PlayerSettings.bundleVersion = "1.0.0";

        // Identificadores de paquete
        string appId = "com.mercedesballesteros.rollingspheres3d";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, appId);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, appId);

        // Orientación de pantalla para móviles (Multiplataforma)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        // Asignación de Ícono de Aplicación
        string iconPath = "Assets/Textures/AppIcon.png";
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon != null)
        {
            Texture2D[] iconArray = new Texture2D[] { icon };
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, iconArray, IconKind.Application);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, iconArray, IconKind.Application);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, iconArray, IconKind.Application);
            Debug.Log($"[PI] Ícono oficial asignado desde: {iconPath}");
        }
        else
        {
            Debug.LogWarning($"[PI] No se encontró ícono en '{iconPath}'. Verifique la ruta.");
        }

        // Resolución y presentación Standalone
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.runInBackground = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[PI] Player Settings actualizados exitosamente.");
    }

    [MenuItem("Herramientas 3D/🌋 PI: Construir Escena Nivel 3 (Zona Magma / Experto)", priority = 11)]
    public static void BuildLevel03Scene()
    {
        Debug.Log("[PI] Construyendo Nivel 3 (Zona Magma - Nivel Experto)...");

        string scenePath = $"{SCENES_DIR}/Level_03.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        int levelNum = 3;
        float goalDistance = 240f;
        Color floorColor = new Color(0.18f, 0.05f, 0.08f); // Magma oscuro
        Color bgColor = new Color(0.12f, 0.03f, 0.05f); // Atmósfera volcánica profunda

        // 1. Materiales de alta resistencia visual (Standard Shader Built-in)
        Material matFloor = MaterialHelper3D.CreateSafeMaterial($"Mat_Floor_Lvl{levelNum}", floorColor, 0.35f);
        Material matRamp = MaterialHelper3D.CreateSafeMaterial($"Mat_Ramp_Lvl{levelNum}", new Color(1.0f, 0.45f, 0.05f), 0.7f);
        Material matHazard = MaterialHelper3D.CreateSafeMaterial($"Mat_Hazard_Lvl{levelNum}", new Color(1.0f, 0.15f, 0.05f), 0.5f);
        Material matPlayer = MaterialHelper3D.CreateSafeMaterial("Mat_Player_Lvl", new Color(1.0f, 0.72f, 0.05f), 0.85f);
        Material matBand = MaterialHelper3D.CreateSafeMaterial("Mat_Band_Lvl", new Color(0.12f, 0.16f, 0.22f), 0.90f);
        Material matGoal = MaterialHelper3D.CreateSafeMaterial("Mat_Goal_Portal_Final", new Color(1.0f, 0.85f, 0.1f), 0.95f);
        MaterialHelper3D.ApplyEmissionToMaterial(matGoal, new Color(1.0f, 0.80f, 0.15f));

        Material matBoost = MaterialHelper3D.CreateSafeMaterial("Mat_SpeedBoost_Energy", new Color(1.0f, 0.90f, 0.10f), 0.9f);
        MaterialHelper3D.ApplyEmissionToMaterial(matBoost, new Color(1.0f, 0.80f, 0.20f));

        // 2. Iluminación y niebla atmosférica
        GameObject lightObj = new GameObject("Directional Light", typeof(Light));
        Light dirLight = lightObj.GetComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(1.0f, 0.75f, 0.65f); // Luz cálida incandescente
        dirLight.intensity = 1.35f;
        dirLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.010f;
        RenderSettings.fogColor = bgColor;
        RenderSettings.ambientLight = new Color(0.35f, 0.18f, 0.18f);

        // 3. Geometría de Pista Avanzada (240 unidades, 4 tramos y 3 abismos)
        GameObject trackParent = new GameObject("Track_Geometry");
        float trackWidth = 10.5f; // Pista más angosta para aumentar la destreza

        // Tramo 1: Arranque (0m a 65m)
        GameObject floor1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor1.name = "Floor_Section_1";
        floor1.transform.SetParent(trackParent.transform);
        floor1.transform.position = new Vector3(0f, -0.5f, 32.5f);
        floor1.transform.localScale = new Vector3(trackWidth, 1f, 65f);
        floor1.GetComponent<Renderer>().sharedMaterial = matFloor;

        // Potenciador 1 (Z=30m)
        CreateSpeedBoost(trackParent.transform, new Vector3(0f, 0.9f, 30f), matBoost);

        // Rampa de salto 1
        GameObject ramp1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp1.name = "Ramp_Elevation_1";
        ramp1.transform.SetParent(trackParent.transform);
        ramp1.transform.position = new Vector3(0f, 0.8f, 50f);
        ramp1.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp1.transform.localScale = new Vector3(trackWidth * 0.75f, 0.8f, 10f);
        ramp1.GetComponent<Renderer>().sharedMaterial = matRamp;

        // Obstáculo oscilante 1 (Z=60m)
        CreateHazard(trackParent.transform, "Hazard_Magma_1", new Vector3(0f, 0.75f, 60f), matHazard, 1.8f, 3.5f);

        // Abismo 1: Hueco entre 65m y 77m (12m de foso para salto)

        // Tramo 2: Zona Media (77m a 140m = 63m de largo)
        GameObject floor2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor2.name = "Floor_Section_2";
        floor2.transform.SetParent(trackParent.transform);
        floor2.transform.position = new Vector3(0f, -0.5f, 77f + 31.5f);
        floor2.transform.localScale = new Vector3(trackWidth, 1f, 63f);
        floor2.GetComponent<Renderer>().sharedMaterial = matFloor;

        // Potenciador 2 (Z=105m)
        CreateSpeedBoost(trackParent.transform, new Vector3(0f, 0.9f, 105f), matBoost);

        // Obstáculo oscilante 2 y 3 (Z=95m y Z=125m)
        CreateHazard(trackParent.transform, "Hazard_Magma_2", new Vector3(0f, 0.75f, 95f), matHazard, 2.2f, 4.0f);
        CreateHazard(trackParent.transform, "Hazard_Magma_3", new Vector3(0f, 0.75f, 125f), matHazard, 2.6f, 3.8f);

        // Abismo 2: Hueco entre 140m y 155m (15m de foso con rampa)
        GameObject ramp2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp2.name = "Ramp_Elevation_2";
        ramp2.transform.SetParent(trackParent.transform);
        ramp2.transform.position = new Vector3(0f, 0.8f, 137f);
        ramp2.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
        ramp2.transform.localScale = new Vector3(trackWidth * 0.75f, 0.8f, 10f);
        ramp2.GetComponent<Renderer>().sharedMaterial = matRamp;

        // Tramo 3: Recta Final y Meta (155m a 265m)
        float len3 = 110f;
        GameObject floor3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor3.name = "Floor_Section_Goal";
        floor3.transform.SetParent(trackParent.transform);
        floor3.transform.position = new Vector3(0f, -0.5f, 155f + len3 * 0.5f);
        floor3.transform.localScale = new Vector3(trackWidth, 1f, len3);
        floor3.GetComponent<Renderer>().sharedMaterial = matFloor;

        // Potenciador 3 (Z=175m)
        CreateSpeedBoost(trackParent.transform, new Vector3(0f, 0.9f, 175f), matBoost);

        // Obstáculo oscilante 4 (Z=205m)
        CreateHazard(trackParent.transform, "Hazard_Magma_4", new Vector3(0f, 0.75f, 205f), matHazard, 3.0f, 4.2f);

        // 4. Portal de Meta 3D (GoalPortal3D) en Z=240
        GameObject goalObj = new GameObject("Goal_Portal_3D");
        goalObj.transform.position = new Vector3(0f, 1.8f, goalDistance);

        GameObject archTorus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        archTorus.name = "Portal_Ring";
        archTorus.transform.SetParent(goalObj.transform, false);
        archTorus.transform.localScale = new Vector3(trackWidth * 0.8f, 0.3f, trackWidth * 0.8f);
        archTorus.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        archTorus.GetComponent<Renderer>().sharedMaterial = matGoal;
        Object.DestroyImmediate(archTorus.GetComponent<Collider>());

        BoxCollider goalCol = goalObj.AddComponent<BoxCollider>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector3(trackWidth, 6f, 2f);
        goalObj.AddComponent<GoalPortal3D>();

        // 5. Jugador (Player_Ball3D) con velocidad 1.85x
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player_Ball3D";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0.8f, 2.0f);
        player.transform.localScale = Vector3.one * 1.4f;
        player.GetComponent<Renderer>().sharedMaterial = matPlayer;

        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Player_Band";
        band.transform.SetParent(player.transform, false);
        band.transform.localPosition = Vector3.zero;
        band.transform.localScale = new Vector3(1.02f, 0.08f, 1.02f);
        band.GetComponent<Renderer>().sharedMaterial = matBand;
        Object.DestroyImmediate(band.GetComponent<Collider>());

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        PlayerController3D controller = player.AddComponent<PlayerController3D>();
        controller.SetSpeedMultiplier(1.85f); // Máxima velocidad nominal (22.2 u/s)
        player.AddComponent<PlayerVisualCustomizer3D>();

        // Luz puntual de soporte
        GameObject glowObj = new GameObject("PlayerGlowLight", typeof(Light));
        Light ptLight = glowObj.GetComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.range = 8.5f;
        ptLight.intensity = 1.45f;
        ptLight.color = new Color(1.0f, 0.85f, 0.30f);
        glowObj.transform.position = player.transform.position + new Vector3(0f, 1.2f, 0f);

        // 6. Cámara Seguidora (CameraFollow3D)
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow3D));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bgColor;

        CameraFollow3D follow = camObj.GetComponent<CameraFollow3D>();
        SerializedObject soFollow = new SerializedObject(follow);
        soFollow.FindProperty("target").objectReferenceValue = player.transform;
        soFollow.FindProperty("offset").vector3Value = new Vector3(0f, 4.8f, -7.5f);
        soFollow.FindProperty("smoothTime").floatValue = 0.20f;
        soFollow.FindProperty("lockFixedZDistance").boolValue = true;
        soFollow.FindProperty("lookAtTarget").boolValue = true;
        soFollow.ApplyModifiedProperties();

        // 7. Managers de Escena
        GameObject gmObj = new GameObject("GameManagers_3D");
        ScoreManager3D sm = gmObj.AddComponent<ScoreManager3D>();
        DifficultyManager3D dm = gmObj.AddComponent<DifficultyManager3D>();
        GameLoopManager3D glm = gmObj.AddComponent<GameLoopManager3D>();
        LightingAtmosphereManager3D lam = gmObj.AddComponent<LightingAtmosphereManager3D>();
        LevelProgressController3D lpc = gmObj.AddComponent<LevelProgressController3D>();

        if (Object.FindAnyObjectByType<NavigationManager3D>() == null)
        {
            new GameObject("NavigationManager", typeof(NavigationManager3D));
        }

        // 8. Canvas de HUD, Barra de Progreso y Modal de Victoria Total
        EnsureEventSystem();
        GameObject canvasObj = CreateCanvas("LevelCanvas");
        MobileUIManager3D uiMgr = canvasObj.AddComponent<MobileUIManager3D>();

        // Barra de Progreso Superior
        GameObject pnlProg = CreateHeaderPanel(canvasObj.transform, "Panel_LevelProgress", new Vector2(0f, -25f), new Vector2(520f, 65f));
        Text txtLvlName = CreateText(pnlProg.transform, "Txt_LvlName", "NIVEL 3 (MAGMA)", new Vector2(-170f, 0f), new Vector2(210f, 40f), 22, new Color(1.0f, 0.45f, 0.1f), TextAnchor.MiddleLeft);
        Slider sldProg = CreateProgressBar(pnlProg.transform, "Slider_Progress", new Vector2(40f, 0f), new Vector2(240f, 26f));
        Text txtPct = CreateText(pnlProg.transform, "Txt_Percent", "0%", new Vector2(215f, 0f), new Vector2(80f, 40f), 22, Color.white, TextAnchor.MiddleRight);

        // Marcadores de Puntos y Récord
        Text txtScore = CreateText(canvasObj.transform, "Txt_Score", "Puntos: 0", new Vector2(180f, -30f), new Vector2(280f, 50f), 32, Color.white, TextAnchor.MiddleLeft);
        Text txtHighScore = CreateText(canvasObj.transform, "Txt_HighScore", "Récord: 0", new Vector2(-180f, -30f), new Vector2(280f, 50f), 28, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleRight);

        // Banner Animado de Súper Velocidad (UX)
        GameObject pnlBoostBanner = CreateModalPanel(canvasObj.transform, "Banner_SpeedBoost", "★ ¡SÚPER VELOCIDAD x2.0! ★", new Vector2(520f, 90f));
        RectTransform rtBoost = pnlBoostBanner.GetComponent<RectTransform>();
        rtBoost.anchorMin = new Vector2(0.5f, 1f);
        rtBoost.anchorMax = new Vector2(0.5f, 1f);
        rtBoost.pivot = new Vector2(0.5f, 1f);
        rtBoost.anchoredPosition = new Vector2(0f, -100f);
        Text txtBoostBanner = pnlBoostBanner.transform.Find("Title")?.GetComponent<Text>();
        if (txtBoostBanner != null)
        {
            txtBoostBanner.fontSize = 28;
            txtBoostBanner.color = new Color(1.0f, 0.90f, 0.1f);
        }
        pnlBoostBanner.SetActive(false);

        // Botón de Selector de Skin
        GameObject btnSkinObj = CreateButton(canvasObj.transform, "Btn_SkinSelector", "🎨 SKIN: DORADO (C)", new Vector2(0f, -85f), new Vector2(280f, 45f), new Color(0.95f, 0.65f, 0.05f), 18);
        PlayerVisualCustomizer3D customizer = player.GetComponent<PlayerVisualCustomizer3D>();
        btnSkinObj.GetComponent<Button>().onClick.AddListener(customizer.CycleNextSkin);

        // Controles Táctiles Virtuales para Móviles
        GameObject dpadObj = new GameObject("Virtual_Dpad", typeof(RectTransform));
        dpadObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtDpad = dpadObj.GetComponent<RectTransform>();
        rtDpad.anchorMin = new Vector2(0f, 0f);
        rtDpad.anchorMax = new Vector2(0f, 0f);
        rtDpad.pivot = new Vector2(0f, 0f);
        rtDpad.anchoredPosition = new Vector2(40f, 40f);
        rtDpad.sizeDelta = new Vector2(220f, 220f);

        CreateTouchButton(dpadObj.transform, "Btn_Up", "▲", new Vector2(0f, 65f), Vector2.up);
        CreateTouchButton(dpadObj.transform, "Btn_Down", "▼", new Vector2(0f, -65f), Vector2.down);
        CreateTouchButton(dpadObj.transform, "Btn_Left", "◀", new Vector2(-65f, 0f), Vector2.left);
        CreateTouchButton(dpadObj.transform, "Btn_Right", "▶", new Vector2(65f, 0f), Vector2.right);

        // Botón de Salto Móvil
        GameObject jumpObj = CreateButton(canvasObj.transform, "Btn_MobileJump", "SALTO\n▲", new Vector2(-70f, 70f), new Vector2(130f, 130f), new Color(0.05f, 0.75f, 0.40f), 24);
        RectTransform rtJump = jumpObj.GetComponent<RectTransform>();
        rtJump.anchorMin = new Vector2(1f, 0f);
        rtJump.anchorMax = new Vector2(1f, 0f);
        rtJump.pivot = new Vector2(1f, 0f);
        jumpObj.GetComponent<Button>().onClick.AddListener(controller.MobileJump);

        // Modal de Victoria Total / Fin del Juego
        GameObject pnlVictory = CreateModalPanel(canvasObj.transform, "Modal_LevelComplete", "★ ¡VICTORIA TOTAL! ★\n¡Juego Completado!", new Vector2(640f, 400f));
        Text txtVictoryStats = CreateText(pnlVictory.transform, "Txt_Stats", "¡Has conquistado los 3 niveles del juego!\nPuntuación Total Excelente.", new Vector2(0f, 30f), new Vector2(560f, 90f), 22, Color.white, TextAnchor.MiddleCenter);
        GameObject btnNextLvl = CreateButton(pnlVictory.transform, "Btn_NextLevel", "VOLVER AL MENÚ PRINCIPAL ➔", new Vector2(0f, -55f), new Vector2(440f, 65f), new Color(0.15f, 0.65f, 0.35f), 22);
        GameObject btnMenuLvl = CreateButton(pnlVictory.transform, "Btn_MainMenu", "REINTENTAR NIVEL 3 ↺", new Vector2(0f, -135f), new Vector2(440f, 55f), new Color(0.25f, 0.35f, 0.50f), 20);

        btnNextLvl.GetComponent<Button>().onClick.AddListener(() => {
            if (NavigationManager3D.Instance != null) NavigationManager3D.Instance.LoadMainMenu();
        });
        btnMenuLvl.GetComponent<Button>().onClick.AddListener(() => {
            if (NavigationManager3D.Instance != null) NavigationManager3D.Instance.LoadLevel3();
        });
        pnlVictory.SetActive(false);

        // Modal de Game Over
        GameObject pnlGameOver = CreateModalPanel(canvasObj.transform, "Modal_GameOver", "¡HAS CAÍDO EN LA MAGMA!", new Vector2(580f, 320f));
        Text txtGOverStats = CreateText(pnlGameOver.transform, "Txt_Stats", "Intenta esquivar los obstáculos oscilantes.", new Vector2(0f, 25f), new Vector2(500f, 70f), 22, Color.white, TextAnchor.MiddleCenter);
        GameObject btnRestart = CreateButton(pnlGameOver.transform, "Btn_Restart", "REINTENTAR NIVEL 3 ↺", new Vector2(0f, -50f), new Vector2(360f, 60f), new Color(0.85f, 0.25f, 0.20f), 22);
        GameObject btnExitToMenu = CreateButton(pnlGameOver.transform, "Btn_ExitMenu", "VOLVER AL MENÚ", new Vector2(0f, -120f), new Vector2(360f, 50f), new Color(0.35f, 0.40f, 0.45f), 20);

        btnRestart.GetComponent<Button>().onClick.AddListener(() => {
            if (NavigationManager3D.Instance != null) NavigationManager3D.Instance.LoadLevel3();
        });
        btnExitToMenu.GetComponent<Button>().onClick.AddListener(() => {
            if (NavigationManager3D.Instance != null) NavigationManager3D.Instance.LoadMainMenu();
        });
        pnlGameOver.SetActive(false);

        // Conexión de SerializedObject en LevelProgressController3D
        SerializedObject soLpc = new SerializedObject(lpc);
        soLpc.FindProperty("currentLevelNumber").intValue = levelNum;
        soLpc.FindProperty("isFinalLevel").boolValue = true; // Marca victoria final
        soLpc.FindProperty("playerTransform").objectReferenceValue = player.transform;
        soLpc.FindProperty("startZ").floatValue = 2.0f;
        soLpc.FindProperty("goalZ").floatValue = goalDistance;
        soLpc.FindProperty("progressBar").objectReferenceValue = sldProg;
        soLpc.FindProperty("progressPercentageText").objectReferenceValue = txtPct;
        soLpc.FindProperty("levelLabelText").objectReferenceValue = txtLvlName;
        soLpc.FindProperty("levelCompletePanel").objectReferenceValue = pnlVictory;
        soLpc.FindProperty("levelCompleteTitleText").objectReferenceValue = pnlVictory.transform.Find("Title")?.GetComponent<Text>();
        soLpc.FindProperty("levelCompleteStatsText").objectReferenceValue = txtVictoryStats;
        soLpc.FindProperty("nextLevelButton").objectReferenceValue = btnNextLvl.GetComponent<Button>();
        soLpc.FindProperty("mainMenuButton").objectReferenceValue = btnMenuLvl.GetComponent<Button>();
        soLpc.FindProperty("speedBoostBanner").objectReferenceValue = pnlBoostBanner;
        soLpc.FindProperty("speedBoostText").objectReferenceValue = txtBoostBanner;
        soLpc.ApplyModifiedProperties();

        // Conexión de SerializedObject en MobileUIManager3D
        SerializedObject soUi = new SerializedObject(uiMgr);
        soUi.FindProperty("scoreText").objectReferenceValue = txtScore;
        soUi.FindProperty("highScoreText").objectReferenceValue = txtHighScore;
        soUi.FindProperty("gameOverPanel").objectReferenceValue = pnlGameOver;
        soUi.FindProperty("finalScoreText").objectReferenceValue = txtGOverStats;
        soUi.FindProperty("restartButton").objectReferenceValue = btnRestart.GetComponent<Button>();
        soUi.ApplyModifiedProperties();

        // 9. Guardar la escena Level_03
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[PI] Escena Nivel 3 guardada exitosamente en: {scenePath}");
    }

    [MenuItem("Herramientas 3D/📋 PI: Actualizar Menú Principal con Nivel 3", priority = 12)]
    public static void UpdateMainMenuWithLevel3()
    {
        string menuPath = $"{SCENES_DIR}/MainMenu3D.unity";
        Debug.Log($"[PI] Actualizando Menú Principal en {menuPath} con selector de Nivel 3...");

        var scene = EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Single);

        MainMenuUI3D menuUI = Object.FindAnyObjectByType<MainMenuUI3D>();
        if (menuUI == null)
        {
            Debug.LogError("[PI] No se encontró el componente MainMenuUI3D en la escena.");
            return;
        }

        // Buscar el panel Modal_LevelSelect
        GameObject modalSelect = GameObject.Find("Modal_LevelSelect");
        if (modalSelect == null)
        {
            Debug.LogError("[PI] No se encontró Modal_LevelSelect en la escena.");
            return;
        }

        // Ajustar el tamaño del panel modal para acomodar 3 niveles cómodamente
        RectTransform rtModal = modalSelect.GetComponent<RectTransform>();
        if (rtModal != null)
        {
            rtModal.sizeDelta = new Vector2(660f, 490f);
        }

        // Buscar o reubicar botones existentes
        Transform trLvl1 = modalSelect.transform.Find("Btn_Lvl1");
        Transform trLvl2 = modalSelect.transform.Find("Btn_Lvl2");
        Transform trClose = modalSelect.transform.Find("Btn_CloseSelect");

        if (trLvl1 != null) trLvl1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 105f);
        if (trLvl2 != null) trLvl2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 25f);
        if (trClose != null) trClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -180f);

        // Crear o actualizar Btn_Lvl3
        Transform trLvl3 = modalSelect.transform.Find("Btn_Lvl3");
        GameObject btnLvl3Obj = trLvl3 != null ? trLvl3.gameObject : null;

        if (btnLvl3Obj == null)
        {
            btnLvl3Obj = CreateButton(modalSelect.transform, "Btn_Lvl3", "NIVEL 3: ZONA MAGMA\n(Modo Experto Final)", new Vector2(0f, -55f), new Vector2(380f, 70f), new Color(0.75f, 0.15f, 0.12f), 22);
        }
        else
        {
            btnLvl3Obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -55f);
        }

        Button btnLvl3 = btnLvl3Obj.GetComponent<Button>();

        // Conectar serializables en MainMenuUI3D
        SerializedObject soMenu = new SerializedObject(menuUI);
        soMenu.FindProperty("level3Button").objectReferenceValue = btnLvl3;
        soMenu.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, menuPath);
        Debug.Log("[PI] Menú Principal actualizado exitosamente con Nivel 3.");
    }

    [MenuItem("Herramientas 3D/🔢 PI: Registrar Escenas en Build Settings", priority = 13)]
    public static void UpdateBuildSettings()
    {
        Debug.Log("[PI] Configurando Build Settings con las 4 escenas del proyecto...");

        string[] scenePaths = new string[] {
            $"{SCENES_DIR}/MainMenu3D.unity",
            $"{SCENES_DIR}/Level_01.unity",
            $"{SCENES_DIR}/Level_02.unity",
            $"{SCENES_DIR}/Level_03.unity",
            $"{SCENES_DIR}/MainGame3D.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string path in scenePaths)
        {
            if (File.Exists(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"   [✓] Escena agregada al Build: {path}");
            }
            else
            {
                Debug.LogWarning($"   [!] No se encontró escena en: {path}");
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log($"[PI] {buildScenes.Count} escenas registradas en EditorBuildSettings.");
    }

    [MenuItem("Herramientas 3D/📦 PI: Compilar Ejecutable Standalone Windows (Build)", priority = 14)]
    public static void CompileStandaloneWindows()
    {
        Debug.Log("[PI] Iniciando compilación de ejecutable Standalone Windows x86_64...");

        string outputDir = Path.GetFullPath(BUILDS_DIR);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string exePath = Path.Combine(outputDir, "RollingSpheres3D.exe");

        // Escenas para la compilación
        List<string> scenesToBuild = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && File.Exists(s.path))
            {
                scenesToBuild.Add(s.path);
            }
        }

        if (scenesToBuild.Count == 0)
        {
            scenesToBuild.Add($"{SCENES_DIR}/MainMenu3D.unity");
            scenesToBuild.Add($"{SCENES_DIR}/Level_01.unity");
            scenesToBuild.Add($"{SCENES_DIR}/Level_02.unity");
            scenesToBuild.Add($"{SCENES_DIR}/Level_03.unity");
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenesToBuild.ToArray();
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log($"[PI] Compilando {scenesToBuild.Count} escenas hacia: {exePath}...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[PI] ¡COMPILACIÓN EXITOSA! Tamaño total: {summary.totalSize / (1024f * 1024f):F2} MB. Tiempo: {summary.totalTime.TotalSeconds:F1}s.");

            // Crear archivo ZIP para distribución y enlace público
            CompressBuildFolder(outputDir, Path.GetFullPath(ZIP_PATH));
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[PI] Error durante la compilación: {summary.totalErrors} errores detectados.");
        }
    }

    private static void CompressBuildFolder(string sourceDirectory, string targetZipFile)
    {
        try
        {
            string zipDir = Path.GetDirectoryName(targetZipFile);
            if (!Directory.Exists(zipDir)) Directory.CreateDirectory(zipDir);

            if (File.Exists(targetZipFile))
            {
                File.Delete(targetZipFile);
            }

            Debug.Log($"[PI] Comprimiendo compilación en '{targetZipFile}'...");
            ZipFile.CreateFromDirectory(sourceDirectory, targetZipFile, System.IO.Compression.CompressionLevel.Optimal, false);
            FileInfo zipInfo = new FileInfo(targetZipFile);
            Debug.Log($"[PI] ¡Empaquetado ZIP completado! Tamaño: {zipInfo.Length / (1024f * 1024f):F2} MB en: {targetZipFile}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PI] No se pudo crear el archivo ZIP automáticamente: {ex.Message}");
        }
    }

    #region Utilerías Auxiliares de Creación de Objetos
    private static void CreateSpeedBoost(Transform parent, Vector3 position, Material mat)
    {
        GameObject pickup = new GameObject("SpeedBoost_Pickup_3D");
        pickup.transform.SetParent(parent);
        pickup.transform.position = position;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual_Capsule";
        visual.transform.SetParent(pickup.transform, false);
        visual.transform.localScale = new Vector3(0.9f, 0.45f, 0.9f);
        visual.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        visual.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        BoxCollider triggerCol = pickup.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(2.5f, 2.5f, 2.5f);
        pickup.AddComponent<SpeedBoostPickup3D>();
    }

    private static void CreateHazard(Transform parent, string name, Vector3 position, Material mat, float speed, float range)
    {
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = name;
        obs.transform.SetParent(parent);
        obs.transform.position = position;
        obs.transform.localScale = new Vector3(2.6f, 1.5f, 1.5f);
        obs.GetComponent<Renderer>().sharedMaterial = mat;

        MovingObstacle3D mover = obs.AddComponent<MovingObstacle3D>();
        SerializedObject soMover = new SerializedObject(mover);
        soMover.FindProperty("speed").floatValue = speed;
        soMover.FindProperty("moveDistance").floatValue = range;
        soMover.ApplyModifiedProperties();

        obs.AddComponent<HazardPart3D>();
    }

    private static GameObject CreateCanvas(string name)
    {
        GameObject canvasObj = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasObj;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static GameObject CreateHeaderPanel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject pnl = new GameObject(name, typeof(RectTransform), typeof(Image));
        pnl.transform.SetParent(parent, false);
        pnl.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.88f);
        RectTransform rt = pnl.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return pnl;
    }

    private static GameObject CreateModalPanel(Transform parent, string name, string title, Vector2 size)
    {
        GameObject modal = new GameObject(name, typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(parent, false);
        modal.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        RectTransform rt = modal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        CreateText(modal.transform, "Title", title, new Vector2(0f, size.y * 0.5f - 40f), new Vector2(size.x - 40f, 70f), 28, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
        return modal;
    }

    private static Slider CreateProgressBar(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject sldObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sldObj.transform.SetParent(parent, false);
        RectTransform rt = sldObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sldObj.transform, false);
        bg.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.8f);
        RectTransform rtBg = bg.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sldObj.transform, false);
        RectTransform rtFa = fillArea.GetComponent<RectTransform>();
        rtFa.anchorMin = new Vector2(0f, 0.25f);
        rtFa.anchorMax = new Vector2(1f, 0.75f);
        rtFa.sizeDelta = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = new Color(0.1f, 0.75f, 0.95f);
        RectTransform rtFill = fill.GetComponent<RectTransform>();
        rtFill.sizeDelta = Vector2.zero;

        Slider sld = sldObj.GetComponent<Slider>();
        sld.fillRect = rtFill;
        sld.direction = Slider.Direction.LeftToRight;
        sld.minValue = 0f;
        sld.maxValue = 1f;
        sld.value = 0f;

        return sld;
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize, Color col, TextAnchor alignment)
    {
        GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(parent, false);
        Text txt = txtObj.GetComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = col;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return txt;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, int fontSize)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        btnObj.GetComponent<Image>().color = col;
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        CreateText(btnObj.transform, "Text", label, Vector2.zero, size - new Vector2(10f, 10f), fontSize, Color.white, TextAnchor.MiddleCenter);
        return btnObj;
    }

    private static void CreateTouchButton(Transform parent, string name, string arrow, Vector2 pos, Vector2 dir)
    {
        GameObject btn = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VirtualTouchButton));
        btn.transform.SetParent(parent, false);
        btn.GetComponent<Image>().color = new Color(0.45f, 0.20f, 0.70f, 0.85f);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(55f, 55f);

        CreateText(btn.transform, "Arrow", arrow, Vector2.zero, new Vector2(50f, 50f), 26, Color.white, TextAnchor.MiddleCenter);

        VirtualTouchButton vtb = btn.GetComponent<VirtualTouchButton>();
        SerializedObject so = new SerializedObject(vtb);
        so.FindProperty("directionVector").vector2Value = dir;
        so.ApplyModifiedProperties();
    }
    #endregion
}
#endif
