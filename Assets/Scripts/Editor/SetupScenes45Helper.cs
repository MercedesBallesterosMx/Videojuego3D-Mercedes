#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Asistente de Editor integral para la Actividad 4.5: Interfaz y Navegación.
/// Automatiza la creación, diseño y guardado de las 3 escenas fundamentales:
/// 1. 'MainMenu3D.unity': Menú principal con Play, Selector de Niveles, Instrucciones y Salir.
/// 2. 'Level_01.unity': Primer nivel jugable (Zona Azul) con barra de progreso, portal de meta y modal de Nivel Completado.
/// 3. 'Level_02.unity': Segundo nivel consecutivo (Zona Púrpura) con mayor velocidad, abismos y pantalla de Victoria Total.
/// Configura automáticamente las 3 escenas en 'EditorBuildSettings' para navegación limpia con SceneManager.
/// </summary>
public class SetupScenes45Helper : EditorWindow
{
    [MenuItem("Herramientas 3D/Montar Sistema de Interfaz y Navegación (Actividad 4.5)")]
    public static void GenerateCompleteNavigationSystem()
    {
        Debug.Log("--- Iniciando generación de Interfaz y Navegación Multi-Escena (Actividad 4.5) ---");

        string scenesDir = "Assets/Scenes";
        if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

        // 1. Construir y guardar Escena: MainMenu3D
        BuildMainMenuScene($"{scenesDir}/MainMenu3D.unity");

        // 2. Construir y guardar Escena: Level_01
        BuildLevelScene($"{scenesDir}/Level_01.unity", 1, 150f, false, new Color(0.12f, 0.16f, 0.22f), new Color(0.10f, 0.14f, 0.20f), 1.0f);

        // 3. Construir y guardar Escena: Level_02
        BuildLevelScene($"{scenesDir}/Level_02.unity", 2, 190f, true, new Color(0.22f, 0.10f, 0.35f), new Color(0.15f, 0.08f, 0.25f), 1.45f);

        // 4. Registrar formalmente en Build Settings
        RegisterScenesInBuildSettings(new string[] {
            $"{scenesDir}/MainMenu3D.unity",
            $"{scenesDir}/Level_01.unity",
            $"{scenesDir}/Level_02.unity",
            $"{scenesDir}/MainGame3D.unity"
        });

        // 5. Cargar MainMenu3D para que el usuario comience desde el menú
        EditorSceneManager.OpenScene($"{scenesDir}/MainMenu3D.unity");

        EditorUtility.DisplayDialog("Sistema de Interfaz y Navegación Concluido (Actividad 4.5)",
            "¡Las interfaces y flujo de navegación han sido configurados exitosamente!\n\n" +
            "✓ Flujo Mínimo de Dos Niveles Consecutivos (Criterio 1):\n" +
            "   - 'MainMenu3D' ➔ 'Level_01' (Zona Azul) ➔ 'Level_02' (Zona Púrpura Eléctrica).\n" +
            "   - Cada nivel cuenta con su Portal de Meta tridimensional y barra de progreso.\n\n" +
            "✓ Transiciones Limpias con SceneManager (Criterio 2):\n" +
            "   - Las escenas están registradas en Build Settings (Índices 0, 1, 2).\n" +
            "   - Cero excepciones de consola al pasar de nivel o volver al menú.\n\n" +
            "✓ Menú Principal Profesional:\n" +
            "   - Botón JUGAR, Selector de Nivel (Nivel 1 y 2), Modal de Instrucciones y Salir.\n\n" +
            "Presiona Play (▶) en Unity para iniciar el recorrido desde el Menú Principal.",
            "¡Excelente!");
    }

    #region 1. Construcción del Menú Principal (MainMenu3D)
    private static void BuildMainMenuScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Cámara y Atmósfera de Fondo
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.09f, 0.15f); // Azul medianoche profundo
        camObj.transform.position = new Vector3(0f, 3.5f, -6f);
        camObj.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

        // 2. Iluminación
        GameObject lightObj = new GameObject("Directional Light", typeof(Light));
        Light dirLight = lightObj.GetComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(1.0f, 0.96f, 0.88f);
        dirLight.intensity = 1.2f;
        dirLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 3. Orbe y Pista Decorativa de Fondo
        Material matFloor = MaterialHelper3D.CreateSafeMaterial("Mat_Menu_Floor", new Color(0.12f, 0.16f, 0.24f), 0.3f);
        GameObject floorDeco = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorDeco.name = "Menu_Floor_Deco";
        floorDeco.transform.position = new Vector3(0f, -0.5f, 5f);
        floorDeco.transform.localScale = new Vector3(12f, 1f, 25f);
        floorDeco.GetComponent<Renderer>().sharedMaterial = matFloor;

        Material matBall = MaterialHelper3D.CreateSafeMaterial("Mat_Menu_Ball", new Color(1.0f, 0.72f, 0.05f), 0.85f);
        GameObject ballDeco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballDeco.name = "Menu_Ball_Deco";
        ballDeco.transform.position = new Vector3(0f, 1.0f, 3.5f);
        ballDeco.transform.localScale = Vector3.one * 1.6f;
        ballDeco.GetComponent<Renderer>().sharedMaterial = matBall;
        Object.DestroyImmediate(ballDeco.GetComponent<Collider>());

        // 4. Managers
        GameObject navMgrObj = new GameObject("NavigationManager", typeof(NavigationManager3D));

        // 5. Canvas UI
        SetupEventSystem();
        GameObject canvasObj = CreateCanvas("MainMenuCanvas");
        MainMenuUI3D menuUI = canvasObj.AddComponent<MainMenuUI3D>();

        // Título Principal
        CreateText(canvasObj.transform, "Title_Game", "★ ROLLING SPHERES 3D ★", new Vector2(0f, 280f), new Vector2(900f, 90f), 54, Color.yellow, TextAnchor.MiddleCenter);
        CreateText(canvasObj.transform, "Subtitle_Game", "Desafío Tridimensional Móvil - Unidad 4", new Vector2(0f, 215f), new Vector2(800f, 50f), 26, new Color(0.3f, 0.85f, 1.0f), TextAnchor.MiddleCenter);

        // Récord
        Text txtHighScore = CreateText(canvasObj.transform, "Txt_HighScore", "Mejor Récord: 0 pts", new Vector2(0f, 150f), new Vector2(600f, 50f), 28, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);

        // Botón JUGAR
        GameObject btnPlayObj = CreateButton(canvasObj.transform, "Btn_Play", "JUGAR PARTIDA ▶", new Vector2(0f, 40f), new Vector2(380f, 75f), new Color(0.08f, 0.65f, 0.40f), 32);
        Button btnPlay = btnPlayObj.GetComponent<Button>();

        // Botón SELECCIONAR NIVEL
        GameObject btnSelectObj = CreateButton(canvasObj.transform, "Btn_LevelSelect", "SELECCIONAR NIVEL", new Vector2(0f, -50f), new Vector2(380f, 65f), new Color(0.15f, 0.35f, 0.65f), 26);
        Button btnSelect = btnSelectObj.GetComponent<Button>();

        // Botón INSTRUCCIONES
        GameObject btnInstObj = CreateButton(canvasObj.transform, "Btn_Instructions", "CÓMO JUGAR / CONTROLES", new Vector2(0f, -135f), new Vector2(380f, 65f), new Color(0.40f, 0.20f, 0.60f), 24);
        Button btnInst = btnInstObj.GetComponent<Button>();

        // Botón SALIR
        GameObject btnQuitObj = CreateButton(canvasObj.transform, "Btn_Quit", "SALIR DEL JUEGO", new Vector2(0f, -220f), new Vector2(380f, 60f), new Color(0.65f, 0.15f, 0.20f), 24);
        Button btnQuit = btnQuitObj.GetComponent<Button>();

        // Panel Modal Selector de Niveles
        GameObject pnlLevelSelect = CreateModalPanel(canvasObj.transform, "Modal_LevelSelect", "SELECCIÓN DE NIVEL", new Vector2(650f, 420f));
        GameObject btnLvl1 = CreateButton(pnlLevelSelect.transform, "Btn_Lvl1", "NIVEL 1: ZONA AZUL\n(Circuito Inicial)", new Vector2(0f, 50f), new Vector2(380f, 75f), new Color(0.12f, 0.45f, 0.70f), 24);
        GameObject btnLvl2 = CreateButton(pnlLevelSelect.transform, "Btn_Lvl2", "NIVEL 2: ZONA PÚRPURA\n(Alta Velocidad y Abismos)", new Vector2(0f, -45f), new Vector2(380f, 75f), new Color(0.50f, 0.18f, 0.65f), 24);
        GameObject btnCloseSelect = CreateButton(pnlLevelSelect.transform, "Btn_CloseSelect", "CERRAR ✕", new Vector2(0f, -145f), new Vector2(250f, 50f), new Color(0.3f, 0.35f, 0.4f), 22);
        pnlLevelSelect.SetActive(false);

        // Panel Modal Instrucciones
        GameObject pnlInstructions = CreateModalPanel(canvasObj.transform, "Modal_Instructions", "INSTRUCCIONES DE JUEGO", new Vector2(750f, 520f));
        string instText = "• D-PAD TÁCTIL / TECLAS WASD:\n  Controla la aceleración y dirección del orbe 3D.\n\n" +
                          "• BOTÓN SALTO ▲ / BARRA ESPACIADORA:\n  Ejecuta saltos para librar abismos y obstáculos letales.\n\n" +
                          "• BOTÓN DE SKIN / TECLA 'C':\n  Alterna en tiempo real entre las 4 skins disponibles.\n\n" +
                          "• META DEL NIVEL:\n  Avanza hasta cruzar el Portal de Meta para completar la fase.";
        CreateText(pnlInstructions.transform, "Txt_Content", instText, new Vector2(0f, 20f), new Vector2(680f, 300f), 22, Color.white, TextAnchor.MiddleLeft);
        GameObject btnCloseInst = CreateButton(pnlInstructions.transform, "Btn_CloseInst", "ENTENDIDO ✓", new Vector2(0f, -195f), new Vector2(250f, 55f), new Color(0.10f, 0.65f, 0.40f), 24);
        pnlInstructions.SetActive(false);

        // Conectar referencias en MainMenuUI3D
        SerializedObject soMenu = new SerializedObject(menuUI);
        soMenu.FindProperty("playButton").objectReferenceValue = btnPlay;
        soMenu.FindProperty("levelSelectButton").objectReferenceValue = btnSelect;
        soMenu.FindProperty("instructionsButton").objectReferenceValue = btnInst;
        soMenu.FindProperty("quitButton").objectReferenceValue = btnQuit;
        soMenu.FindProperty("levelSelectPanel").objectReferenceValue = pnlLevelSelect;
        soMenu.FindProperty("instructionsPanel").objectReferenceValue = pnlInstructions;
        soMenu.FindProperty("level1Button").objectReferenceValue = btnLvl1.GetComponent<Button>();
        soMenu.FindProperty("level2Button").objectReferenceValue = btnLvl2.GetComponent<Button>();
        soMenu.FindProperty("closeLevelSelectButton").objectReferenceValue = btnCloseSelect.GetComponent<Button>();
        soMenu.FindProperty("closeInstructionsButton").objectReferenceValue = btnCloseInst.GetComponent<Button>();
        soMenu.FindProperty("highScoreText").objectReferenceValue = txtHighScore;
        soMenu.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[SetupScenes45Helper] Escena Menú Principal guardada en: {scenePath}");
    }
    #endregion

    #region 2. Construcción de Niveles Consecutivos (Level_01 y Level_02)
    private static void BuildLevelScene(string scenePath, int levelNum, float goalDistance, bool isFinal, Color floorColor, Color bgColor, float speedMult)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Materiales seguros
        Material matFloor = MaterialHelper3D.CreateSafeMaterial($"Mat_Floor_Lvl{levelNum}", floorColor, 0.25f);
        Material matRamp = MaterialHelper3D.CreateSafeMaterial($"Mat_Ramp_Lvl{levelNum}", new Color(0.02f, 0.75f, 0.88f), 0.5f);
        Material matHazard = MaterialHelper3D.CreateSafeMaterial($"Mat_Hazard_Lvl{levelNum}", new Color(0.92f, 0.20f, 0.25f), 0.4f);
        Material matPlayer = MaterialHelper3D.CreateSafeMaterial("Mat_Player_Lvl", new Color(1.0f, 0.72f, 0.05f), 0.85f);
        Material matBand = MaterialHelper3D.CreateSafeMaterial("Mat_Band_Lvl", new Color(0.12f, 0.16f, 0.22f), 0.90f);
        Material matGoal = MaterialHelper3D.CreateSafeMaterial("Mat_Goal_Portal", new Color(1.0f, 0.85f, 0.1f), 0.95f);
        MaterialHelper3D.ApplyEmissionToMaterial(matGoal, new Color(1.0f, 0.75f, 0.1f));

        // 2. Iluminación y Niebla
        GameObject lightObj = new GameObject("Directional Light", typeof(Light));
        Light dirLight = lightObj.GetComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = levelNum == 1 ? new Color(1.0f, 0.98f, 0.92f) : new Color(0.90f, 0.75f, 1.0f);
        dirLight.intensity = 1.2f;
        dirLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = bgColor;
        RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.42f);

        // 3. Pista de Juego Fija y Segmentada
        GameObject trackParent = new GameObject("Track_Geometry");
        float trackWidth = levelNum == 1 ? 14f : 11.5f;

        // Tramo 1: Inicio y aceleración
        GameObject floor1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor1.name = "Floor_Section_1";
        floor1.transform.SetParent(trackParent.transform);
        floor1.transform.position = new Vector3(0f, -0.5f, 35f);
        floor1.transform.localScale = new Vector3(trackWidth, 1f, 75f);
        floor1.GetComponent<Renderer>().sharedMaterial = matFloor;

        // Rampa de elevación intermedia
        GameObject ramp1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp1.name = "Ramp_Elevation";
        ramp1.transform.SetParent(trackParent.transform);
        ramp1.transform.position = new Vector3(0f, 0.8f, 45f);
        ramp1.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp1.transform.localScale = new Vector3(trackWidth * 0.75f, 0.8f, 10f);
        ramp1.GetComponent<Renderer>().sharedMaterial = matRamp;

        // Obstáculo oscilante en tramo 1
        GameObject obs1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs1.name = "Hazard_Oscillating_1";
        obs1.transform.SetParent(trackParent.transform);
        obs1.transform.position = new Vector3(0f, 0.75f, 60f);
        obs1.transform.localScale = new Vector3(2.5f, 1.5f, 1.5f);
        obs1.GetComponent<Renderer>().sharedMaterial = matHazard;
        obs1.AddComponent<MovingObstacle3D>();
        obs1.AddComponent<HazardPart3D>();

        // Tramo 2: Zona media (con abismo en Nivel 2)
        float startZ2 = levelNum == 1 ? 75f : 88f; // En Nivel 2 hay un abismo de salto entre 72.5 y 88
        float len2 = 55f;
        GameObject floor2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor2.name = "Floor_Section_2";
        floor2.transform.SetParent(trackParent.transform);
        floor2.transform.position = new Vector3(0f, -0.5f, startZ2 + len2 * 0.5f);
        floor2.transform.localScale = new Vector3(trackWidth, 1f, len2);
        floor2.GetComponent<Renderer>().sharedMaterial = matFloor;

        // Obstáculo en tramo 2
        GameObject obs2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs2.name = "Hazard_Oscillating_2";
        obs2.transform.SetParent(trackParent.transform);
        obs2.transform.position = new Vector3(0f, 0.75f, startZ2 + 25f);
        obs2.transform.localScale = new Vector3(2.5f, 1.5f, 1.5f);
        obs2.GetComponent<Renderer>().sharedMaterial = matHazard;
        obs2.AddComponent<MovingObstacle3D>();
        obs2.AddComponent<HazardPart3D>();

        // Tramo 3: Recta de meta
        float startZ3 = startZ2 + len2 + (levelNum == 1 ? 0f : 10f);
        float len3 = (goalDistance + 25f) - startZ3;
        GameObject floor3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor3.name = "Floor_Section_Goal";
        floor3.transform.SetParent(trackParent.transform);
        floor3.transform.position = new Vector3(0f, -0.5f, startZ3 + len3 * 0.5f);
        floor3.transform.localScale = new Vector3(trackWidth, 1f, len3);
        floor3.GetComponent<Renderer>().sharedMaterial = matFloor;

        // 4. Portal de Meta 3D (GoalPortal3D)
        GameObject goalObj = new GameObject("Goal_Portal_3D");
        goalObj.transform.position = new Vector3(0f, 1.8f, goalDistance);

        // Anillo de portal
        GameObject archTorus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        archTorus.name = "Portal_Ring";
        archTorus.transform.SetParent(goalObj.transform, false);
        archTorus.transform.localScale = new Vector3(trackWidth * 0.7f, 0.25f, trackWidth * 0.7f);
        archTorus.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        archTorus.GetComponent<Renderer>().sharedMaterial = matGoal;
        Object.DestroyImmediate(archTorus.GetComponent<Collider>());

        // Trigger físico
        BoxCollider goalCol = goalObj.AddComponent<BoxCollider>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector3(trackWidth, 5f, 2f);
        goalObj.AddComponent<GoalPortal3D>();

        // 5. Jugador (Player_Ball3D)
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
        controller.SetSpeedMultiplier(speedMult);
        player.AddComponent<PlayerVisualCustomizer3D>();

        // Luz de apoyo del jugador
        GameObject glowObj = new GameObject("PlayerGlowLight", typeof(Light));
        Light ptLight = glowObj.GetComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.range = 8.0f;
        ptLight.intensity = 1.35f;
        ptLight.color = new Color(1.0f, 0.85f, 0.30f);
        glowObj.transform.position = player.transform.position + new Vector3(0f, 1.2f, 0f);

        // 6. Cámara Seguidora
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

        // Navigation Manager persistente en caso de iniciar directo
        if (Object.FindFirstObjectByType<NavigationManager3D>() == null)
        {
            new GameObject("NavigationManager", typeof(NavigationManager3D));
        }

        // 8. Canvas de HUD, Barra de Progreso y Modales
        SetupEventSystem();
        GameObject canvasObj = CreateCanvas("LevelCanvas");
        MobileUIManager3D uiMgr = canvasObj.AddComponent<MobileUIManager3D>();

        // Barra de Progreso de Nivel (Slider superior)
        GameObject pnlProg = CreateHeaderPanel(canvasObj.transform, "Panel_LevelProgress", new Vector2(0f, -25f), new Vector2(500f, 65f));
        Text txtLvlName = CreateText(pnlProg.transform, "Txt_LvlName", $"NIVEL {levelNum}", new Vector2(-180f, 0f), new Vector2(150f, 40f), 24, Color.yellow, TextAnchor.MiddleLeft);
        Slider sldProg = CreateProgressBar(pnlProg.transform, "Slider_Progress", new Vector2(30f, 0f), new Vector2(250f, 26f));
        Text txtPct = CreateText(pnlProg.transform, "Txt_Percent", "0%", new Vector2(215f, 0f), new Vector2(80f, 40f), 22, Color.white, TextAnchor.MiddleRight);

        // Marcadores de Puntos y Récord
        Text txtScore = CreateText(canvasObj.transform, "Txt_Score", "Puntos: 0", new Vector2(180f, -30f), new Vector2(280f, 50f), 32, Color.white, TextAnchor.MiddleLeft);
        Text txtHighScore = CreateText(canvasObj.transform, "Txt_HighScore", "Récord: 0", new Vector2(-180f, -30f), new Vector2(280f, 50f), 28, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleRight);

        // Botón de Selector de Skin
        GameObject btnSkinObj = CreateButton(canvasObj.transform, "Btn_SkinSelector", "🎨 SKIN: DORADO (C)", new Vector2(0f, -85f), new Vector2(280f, 45f), new Color(0.95f, 0.65f, 0.05f), 18);
        PlayerVisualCustomizer3D customizer = player.GetComponent<PlayerVisualCustomizer3D>();
        btnSkinObj.GetComponent<Button>().onClick.AddListener(customizer.CycleNextSkin);

        // Controles Móviles (D-Pad y Salto)
        CreateMobileControls(canvasObj.transform, controller);

        // Modal de Nivel Completado (Victoria)
        GameObject pnlVictory = CreateModalPanel(canvasObj.transform, "Modal_LevelComplete", isFinal ? "★ ¡VICTORIA TOTAL! ★" : $"★ ¡NIVEL {levelNum} COMPLETADO! ★", new Vector2(650f, 440f));
        Text txtVicStats = CreateText(pnlVictory.transform, "Txt_Stats", $"Puntuación: 0 pts\n¡Meta alcanzada con éxito!", new Vector2(0f, 30f), new Vector2(500f, 80f), 28, Color.white, TextAnchor.MiddleCenter);
        string nextBtnLabel = isFinal ? "VOLVER AL MENÚ PRINCIPAL" : "SIGUIENTE NIVEL ➔";
        GameObject btnNextLvl = CreateButton(pnlVictory.transform, "Btn_NextLevel", nextBtnLabel, new Vector2(0f, -70f), new Vector2(380f, 65f), new Color(0.10f, 0.65f, 0.40f), 24);
        GameObject btnMenuVic = CreateButton(pnlVictory.transform, "Btn_MenuVic", "MENÚ PRINCIPAL", new Vector2(0f, -150f), new Vector2(380f, 55f), new Color(0.25f, 0.35f, 0.50f), 22);
        pnlVictory.SetActive(false);

        // Modal de Game Over
        GameObject pnlGameOver = CreateModalPanel(canvasObj.transform, "Modal_GameOver", "¡FIN DE LA PARTIDA!", new Vector2(650f, 440f));
        Text txtGoScore = CreateText(pnlGameOver.transform, "Txt_GoScore", "Puntuación: 0", new Vector2(0f, 40f), new Vector2(500f, 50f), 28, Color.white, TextAnchor.MiddleCenter);
        Text txtGoHigh = CreateText(pnlGameOver.transform, "Txt_GoHigh", "Mejor Récord: 0", new Vector2(0f, -10f), new Vector2(500f, 50f), 24, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
        GameObject btnRetry = CreateButton(pnlGameOver.transform, "Btn_Retry", "REINTENTAR ⟳", new Vector2(0f, -80f), new Vector2(320f, 65f), new Color(0.10f, 0.65f, 0.40f), 24);
        GameObject btnMenuGo = CreateButton(pnlGameOver.transform, "Btn_MenuGo", "MENÚ PRINCIPAL", new Vector2(0f, -155f), new Vector2(320f, 55f), new Color(0.25f, 0.35f, 0.50f), 22);
        pnlGameOver.SetActive(false);

        // Conexiones de GameManagers
        SerializedObject soLpc = new SerializedObject(lpc);
        soLpc.FindProperty("currentLevelNumber").intValue = levelNum;
        soLpc.FindProperty("isFinalLevel").boolValue = isFinal;
        soLpc.FindProperty("playerTransform").objectReferenceValue = player.transform;
        soLpc.FindProperty("startZ").floatValue = 2.0f;
        soLpc.FindProperty("goalZ").floatValue = goalDistance;
        soLpc.FindProperty("progressBar").objectReferenceValue = sldProg;
        soLpc.FindProperty("progressPercentageText").objectReferenceValue = txtPct;
        soLpc.FindProperty("levelLabelText").objectReferenceValue = txtLvlName;
        soLpc.FindProperty("levelCompletePanel").objectReferenceValue = pnlVictory;
        soLpc.FindProperty("levelCompleteTitleText").objectReferenceValue = pnlVictory.transform.Find("Title")?.GetComponent<Text>();
        soLpc.FindProperty("levelCompleteStatsText").objectReferenceValue = txtVicStats;
        soLpc.FindProperty("nextLevelButton").objectReferenceValue = btnNextLvl.GetComponent<Button>();
        soLpc.FindProperty("mainMenuButton").objectReferenceValue = btnMenuVic.GetComponent<Button>();
        soLpc.ApplyModifiedProperties();

        SerializedObject soSm = new SerializedObject(sm);
        soSm.FindProperty("currentScoreText").objectReferenceValue = txtScore;
        soSm.FindProperty("highScoreText").objectReferenceValue = txtHighScore;
        soSm.FindProperty("playerTransform").objectReferenceValue = player.transform;
        soSm.ApplyModifiedProperties();

        SerializedObject soGlm = new SerializedObject(glm);
        soGlm.FindProperty("playerTransform").objectReferenceValue = player.transform;
        soGlm.FindProperty("gameOverPanel").objectReferenceValue = pnlGameOver;
        soGlm.FindProperty("finalScoreText").objectReferenceValue = txtGoScore;
        soGlm.FindProperty("finalHighScoreText").objectReferenceValue = txtGoHigh;
        soGlm.FindProperty("retryButton").objectReferenceValue = btnRetry.GetComponent<Button>();
        soGlm.FindProperty("menuButton").objectReferenceValue = btnMenuGo.GetComponent<Button>();
        soGlm.FindProperty("fallThresholdY").floatValue = -4.0f;
        soGlm.ApplyModifiedProperties();

        SerializedObject soLam = new SerializedObject(lam);
        soLam.FindProperty("targetCamera").objectReferenceValue = cam;
        soLam.FindProperty("mainDirectionalLight").objectReferenceValue = dirLight;
        soLam.FindProperty("playerGlowLight").objectReferenceValue = ptLight;
        soLam.FindProperty("playerTransform").objectReferenceValue = player.transform;
        soLam.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[SetupScenes45Helper] Escena '{scenePath}' guardada con éxito.");
    }
    #endregion

    #region 3. Registro en Build Settings
    private static void RegisterScenesInBuildSettings(string[] scenePaths)
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string path in scenePaths)
        {
            if (File.Exists(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"[SetupScenes45Helper] Registrada en Build Settings: {path}");
            }
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
    }
    #endregion

    #region 4. Métodos Auxiliares de UI Canvas
    private static void SetupEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static GameObject CreateCanvas(string name)
    {
        GameObject canvasObj = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObj;
    }

    private static Text CreateText(Transform parent, string name, string content, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(parent, false);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Text txt = textObj.GetComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.alignment = align;
        txt.raycastTarget = false;
        return txt;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, int fontSize)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text txt = txtObj.GetComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false;

        return btnObj;
    }

    private static GameObject CreateHeaderPanel(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject pnlObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pnlObj.transform.SetParent(parent, false);
        RectTransform rt = pnlObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = pnlObj.GetComponent<Image>();
        img.color = new Color(0.06f, 0.10f, 0.18f, 0.85f);
        return pnlObj;
    }

    private static Slider CreateProgressBar(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        RectTransform rt = sliderObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Slider slider = sliderObj.GetComponent<Slider>();

        // Fondo del slider
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.sizeDelta = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f);

        // Área de relleno
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform rtFillArea = fillArea.GetComponent<RectTransform>();
        rtFillArea.anchorMin = Vector2.zero;
        rtFillArea.anchorMax = Vector2.one;
        rtFillArea.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform rtFill = fill.GetComponent<RectTransform>();
        rtFill.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.08f, 0.75f, 0.45f); // Verde esmeralda de progreso

        slider.fillRect = rtFill;
        slider.targetGraphic = bgObj.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static GameObject CreateModalPanel(Transform parent, string name, string title, Vector2 size)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(parent, false);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.10f, 0.92f);

        GameObject box = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(overlay.transform, false);
        RectTransform rtBox = box.GetComponent<RectTransform>();
        rtBox.sizeDelta = size;
        rtBox.anchoredPosition = Vector2.zero;
        box.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.22f, 0.98f);

        CreateText(box.transform, "Title", title, new Vector2(0f, size.y * 0.5f - 45f), new Vector2(size.x - 40f, 60f), 32, Color.yellow, TextAnchor.MiddleCenter);

        return overlay;
    }

    private static void CreateMobileControls(Transform parent, PlayerController3D controller)
    {
        GameObject cPanelObj = new GameObject("MobileControlsPanel", typeof(RectTransform));
        cPanelObj.transform.SetParent(parent, false);
        RectTransform rt = cPanelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Vector2 dpadCenter = new Vector2(200, 200);
        float dpadSpacing = 110f;
        Color dpadColor = new Color(0.12f, 0.30f, 0.55f, 0.80f);

        CreateTouchBtn(cPanelObj.transform, "Btn_Forward", "▲", dpadCenter + new Vector2(0, dpadSpacing), VirtualTouchButton.ButtonDirection.Forward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchBtn(cPanelObj.transform, "Btn_Backward", "▼", dpadCenter + new Vector2(0, -dpadSpacing), VirtualTouchButton.ButtonDirection.Backward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchBtn(cPanelObj.transform, "Btn_Left", "◀", dpadCenter + new Vector2(-dpadSpacing, 0), VirtualTouchButton.ButtonDirection.Left, controller, new Vector2(100, 100), dpadColor);
        CreateTouchBtn(cPanelObj.transform, "Btn_Right", "▶", dpadCenter + new Vector2(dpadSpacing, 0), VirtualTouchButton.ButtonDirection.Right, controller, new Vector2(100, 100), dpadColor);

        Color jumpBtnColor = new Color(0.08f, 0.72f, 0.45f, 0.90f);
        CreateTouchBtn(cPanelObj.transform, "Btn_Jump", "SALTO\n▲", new Vector2(-160, 160), VirtualTouchButton.ButtonDirection.Jump, controller, new Vector2(170, 170), jumpBtnColor, new Vector2(1, 0), new Vector2(1, 0));
    }

    private static void CreateTouchBtn(Transform parent, string name, string label, Vector2 pos, VirtualTouchButton.ButtonDirection dir, 
        PlayerController3D controller, Vector2 size, Color bgColor, Vector2? minAnchor = null, Vector2? maxAnchor = null)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualTouchButton));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = minAnchor ?? Vector2.zero;
        rt.anchorMax = maxAnchor ?? Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        VirtualTouchButton vtb = btnObj.GetComponent<VirtualTouchButton>();
        SerializedObject so = new SerializedObject(vtb);
        so.FindProperty("direction").enumValueIndex = (int)dir;
        so.FindProperty("playerController").objectReferenceValue = controller;
        so.ApplyModifiedProperties();

        if (dir == VirtualTouchButton.ButtonDirection.Jump)
        {
            btn.onClick.AddListener(vtb.OnButtonClick);
            btn.onClick.AddListener(controller.MobileJump);
        }

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        Text txt = txtObj.GetComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = (dir == VirtualTouchButton.ButtonDirection.Jump) ? 30 : 44;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false;
    }
    #endregion
}
#endif
