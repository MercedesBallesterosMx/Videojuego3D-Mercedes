#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Asistente de Editor integral para Unity que automatiza el montaje de la escena para las Actividades 4.1, 4.2, 4.3 y 4.4.
/// Integra iluminación 3D avanzada, sombras suaves, niebla cinemática, sistema de skins personalizables para el jugador,
/// estela dinámica (TrailRenderer), efectos de partículas procedurales, bucle de juego, niveles procedimentales infinitos
/// y guardado automático de la escena en Assets/Scenes/MainGame3D.unity.
/// 
/// Accesible desde el menú superior:
/// 'Herramientas 3D > Montar Escenario Completo (Actividad 4.4 - Iluminación y Mejoras Visuales)'
/// </summary>
[InitializeOnLoad]
public class SetupScene3DHelper : EditorWindow
{
    static SetupScene3DHelper()
    {
        string pipeName = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "Built-in";
        Shader safeShader = MaterialHelper3D.GetSafeShader();
        Debug.Log($"[SetupScene3DHelper] Diagnóstico Gráfico -> Pipeline: {pipeName} | Shader Activo: {safeShader.name}");
    }

    [MenuItem("Herramientas 3D/Montar Escenario Completo (Actividad 4.4 - Iluminación y Mejoras Visuales)")]
    [MenuItem("Herramientas 3D/Montar Escenario Dinámico e Infinito (Actividad 4.3)")]
    public static void GenerateComplete3DScene()
    {
        Undo.SetCurrentGroupName("Montar Escena 3D Actividad 4.4");

        Debug.Log("--- Iniciando montaje completo: Iluminación 3D, Skins, Partículas y Guardado de Escena ---");

        // 1. Configuración de Standard Shaders nativos
        Material matFloor = CreateOrUpdateMaterial("Mat_Floor_DarkSlate", new Color(0.12f, 0.16f, 0.22f), 0.25f);
        Material matPlayer = CreateOrUpdateMaterial("Mat_Player_VibrantGold", new Color(1.0f, 0.72f, 0.05f), 0.85f);
        Material matPlayerBand = CreateOrUpdateMaterial("Mat_Player_Band_Dark", new Color(0.12f, 0.16f, 0.22f), 0.90f);
        Material matRamp = CreateOrUpdateMaterial("Mat_Ramp_ElectricCyan", new Color(0.02f, 0.75f, 0.88f), 0.50f);
        Material matPlatform = CreateOrUpdateMaterial("Mat_Platform_EmeraldGreen", new Color(0.08f, 0.75f, 0.45f), 0.50f);
        Material matObstacle = CreateOrUpdateMaterial("Mat_Obstacle_RubyRed", new Color(0.92f, 0.20f, 0.25f), 0.40f);

        // 2. Iluminación y atmósfera cinemática
        Light dirLight = SetupLighting();

        // 3. Entorno geométrico inicial
        GameObject groundParent = SetupEnvironment(matFloor, matRamp, matPlatform, matObstacle);

        // 4. Personaje físico con personalizador visual (Criterio 2)
        GameObject player = SetupPlayer(matPlayer, matPlayerBand);

        // 5. Cámara seguidora y atmósfera de fondo (Criterio 1)
        SetupCamera(player.transform);

        // 6. Luz de apoyo y halo luminoso sobre el jugador
        Light glowLight = SetupPlayerGlowLight(player.transform);

        // 7. Gestores del bucle de juego, puntuación, niveles e iluminación
        SetupGameManagers(player.GetComponent<PlayerController3D>(), dirLight, glowLight);

        // 8. Interfaz UI con botones táctiles, HUD, modal Game Over y selector interactivo de Skins
        SetupMobileUI(player.GetComponent<PlayerController3D>());

        // 9. Guardar la escena permanentemente en Assets/Scenes/MainGame3D.unity
        SaveAndRegisterScene();

        EditorUtility.DisplayDialog("Montaje de Actividad 4.4 Concluido",
            "¡El proyecto 3D ha sido configurado con éxito para la Actividad 4.4!\n\n" +
            "✓ Iluminación y Niebla 3D: Luz direccional calibrada con sombras suaves (Soft Shadows) y niebla volumétrica.\n" +
            "✓ Color de Fondo Dinámico (Criterio 1): Interpolación fluida (Color.Lerp) de cámara y niebla al avanzar de nivel.\n" +
            "✓ Personalización del Personaje (Criterio 2): 4 Skins temáticas (Dorado, Neón, Esmeralda, Cyan) con Trail 3D.\n" +
            "✓ Efectos de Partículas: Destello radial al pulsar SALTO y explosión al chocar con obstáculos.\n" +
            "✓ Botón de Skin en UI y Tecla 'C': Alterna el aspecto del personaje en tiempo real durante la partida.\n" +
            "✓ Shaders Standard: MaterialHelper3D garantiza compatibilidad nativa y colores vivos.\n" +
            "✓ Escena Guardada: La escena ha sido guardada en 'Assets/Scenes/MainGame3D.unity'.\n\n" +
            "Presiona Play (▶) para disfrutar la experiencia visual 3D.",
            "¡Excelente!");
    }

    private static Material CreateOrUpdateMaterial(string matName, Color color, float smoothness = 0.5f)
    {
        string dirPath = "Assets/Materials";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string assetPath = $"{dirPath}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader safeShader = MaterialHelper3D.GetSafeShader();

        if (mat == null)
        {
            mat = new Material(safeShader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        else
        {
            mat.shader = safeShader;
        }

        MaterialHelper3D.ApplyColorToMaterial(mat, color);

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private static Light SetupLighting()
    {
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        dirLight.color = new Color(1.0f, 0.98f, 0.92f);
        dirLight.intensity = 1.20f;
        dirLight.shadows = LightShadows.Soft;
        dirLight.shadowStrength = 0.65f;
        dirLight.shadowBias = 0.04f;
        dirLight.shadowNormalBias = 0.4f;

        // Configuración de niebla cinemática global
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.10f, 0.14f, 0.20f);
        RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.42f);

        return dirLight;
    }

    private static Light SetupPlayerGlowLight(Transform playerTransform)
    {
        GameObject glowObj = GameObject.Find("PlayerGlowLight");
        if (glowObj == null)
        {
            glowObj = new GameObject("PlayerGlowLight");
        }

        Light ptLight = glowObj.GetComponent<Light>();
        if (ptLight == null) ptLight = glowObj.AddComponent<Light>();

        ptLight.type = LightType.Point;
        ptLight.range = 8.0f;
        ptLight.intensity = 1.35f;
        ptLight.color = new Color(1.0f, 0.85f, 0.30f);
        ptLight.shadows = LightShadows.None;

        glowObj.transform.position = playerTransform.position + new Vector3(0f, 1.2f, 0f);

        return ptLight;
    }

    private static GameObject SetupEnvironment(Material floorMat, Material rampMat, Material platMat, Material obsMat)
    {
        GameObject envParent = GameObject.Find("Environment_3D");
        if (envParent != null)
        {
            DestroyImmediate(envParent);
        }
        envParent = new GameObject("Environment_3D");

        // Tramo inicial seguro
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_StartingSection";
        floor.transform.SetParent(envParent.transform);
        floor.transform.position = new Vector3(0f, -0.5f, 20f);
        floor.transform.localScale = new Vector3(14f, 1f, 50f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // Rampa de elevación
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp_Elevation_Y";
        ramp.transform.SetParent(envParent.transform);
        ramp.transform.position = new Vector3(0f, 1.25f, 18f);
        ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp.transform.localScale = new Vector3(6f, 0.8f, 10f);
        ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

        // Obstáculo oscilante letal
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = "Hazard_OscillatingCube";
        obs.transform.SetParent(envParent.transform);
        obs.transform.position = new Vector3(0f, 0.75f, 32f);
        obs.transform.localScale = new Vector3(2.5f, 1.5f, 1.5f);
        obs.GetComponent<Renderer>().sharedMaterial = obsMat;
        obs.AddComponent<MovingObstacle3D>();
        obs.AddComponent<HazardPart3D>();

        return envParent;
    }

    private static GameObject SetupPlayer(Material playerMat, Material bandMat)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            DestroyImmediate(player);
        }

        player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        player.name = "Player_Ball3D";
        player.tag = "Player";

        player.transform.position = new Vector3(0f, 0.8f, 2.0f);
        player.transform.localScale = Vector3.one * 1.4f;
        player.GetComponent<Renderer>().sharedMaterial = playerMat;

        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Player_Band";
        band.transform.SetParent(player.transform, false);
        band.transform.localPosition = Vector3.zero;
        band.transform.localScale = new Vector3(1.02f, 0.08f, 1.02f);
        band.GetComponent<Renderer>().sharedMaterial = bandMat;
        DestroyImmediate(band.GetComponent<Collider>());

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        PlayerController3D controller = player.GetComponent<PlayerController3D>();
        if (controller == null) controller = player.AddComponent<PlayerController3D>();

        // Personalizador visual del personaje y partículas (Criterio 2)
        PlayerVisualCustomizer3D customizer = player.GetComponent<PlayerVisualCustomizer3D>();
        if (customizer == null) customizer = player.AddComponent<PlayerVisualCustomizer3D>();

        return player;
    }

    private static void SetupCamera(Transform playerTransform)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.10f, 0.14f, 0.20f);

        CameraFollow3D followScript = mainCam.GetComponent<CameraFollow3D>();
        if (followScript == null) followScript = mainCam.gameObject.AddComponent<CameraFollow3D>();

        Vector3 cameraOffset = new Vector3(0f, 4.8f, -7.5f);

        SerializedObject so = new SerializedObject(followScript);
        so.FindProperty("target").objectReferenceValue = playerTransform;
        so.FindProperty("offset").vector3Value = cameraOffset;
        so.FindProperty("smoothTime").floatValue = 0.20f;
        so.FindProperty("lockFixedZDistance").boolValue = true;
        so.FindProperty("followYAxis").boolValue = false;
        so.FindProperty("lookAtTarget").boolValue = true;
        so.ApplyModifiedProperties();

        mainCam.transform.position = playerTransform.position + cameraOffset;
        mainCam.transform.LookAt(playerTransform.position + Vector3.up * 0.4f);
    }

    private static void SetupGameManagers(PlayerController3D controller, Light dirLight, Light glowLight)
    {
        GameObject gmObj = GameObject.Find("GameManagers_3D");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManagers_3D");
        }

        ScoreManager3D scoreManager = gmObj.GetComponent<ScoreManager3D>();
        if (scoreManager == null) scoreManager = gmObj.AddComponent<ScoreManager3D>();

        DifficultyManager3D diffManager = gmObj.GetComponent<DifficultyManager3D>();
        if (diffManager == null) diffManager = gmObj.AddComponent<DifficultyManager3D>();

        LevelGenerator3D levelGen = gmObj.GetComponent<LevelGenerator3D>();
        if (levelGen == null) levelGen = gmObj.AddComponent<LevelGenerator3D>();
        SerializedObject soLg = new SerializedObject(levelGen);
        soLg.FindProperty("playerTransform").objectReferenceValue = controller.transform;
        soLg.FindProperty("initialSectionsCount").intValue = 6;
        soLg.FindProperty("defaultSectionLength").floatValue = 28f;
        soLg.FindProperty("spawnDistanceAhead").floatValue = 85f;
        soLg.FindProperty("despawnDistanceBehind").floatValue = 45f;
        soLg.ApplyModifiedProperties();

        GameLoopManager3D gameLoop = gmObj.GetComponent<GameLoopManager3D>();
        if (gameLoop == null) gameLoop = gmObj.AddComponent<GameLoopManager3D>();
        SerializedObject soGl = new SerializedObject(gameLoop);
        soGl.FindProperty("playerTransform").objectReferenceValue = controller.transform;
        soGl.FindProperty("fallThresholdY").floatValue = -4.0f;
        soGl.ApplyModifiedProperties();

        // Gestor de Iluminación y Atmósfera (Criterio 1)
        LightingAtmosphereManager3D lightMgr = gmObj.GetComponent<LightingAtmosphereManager3D>();
        if (lightMgr == null) lightMgr = gmObj.AddComponent<LightingAtmosphereManager3D>();
        SerializedObject soLm = new SerializedObject(lightMgr);
        soLm.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        soLm.FindProperty("mainDirectionalLight").objectReferenceValue = dirLight;
        soLm.FindProperty("playerGlowLight").objectReferenceValue = glowLight;
        soLm.FindProperty("playerTransform").objectReferenceValue = controller.transform;
        soLm.ApplyModifiedProperties();
    }

    private static void SetupMobileUI(PlayerController3D controller)
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("MobileCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        MobileUIManager3D uiManager = canvasObj.GetComponent<MobileUIManager3D>();
        if (uiManager == null) uiManager = canvasObj.AddComponent<MobileUIManager3D>();

        // 1. Panel Superior de HUD
        Transform hudTopPanel = canvasObj.transform.Find("HUD_TopScorePanel");
        if (hudTopPanel != null) DestroyImmediate(hudTopPanel.gameObject);

        GameObject hudPanelObj = new GameObject("HUD_TopScorePanel", typeof(RectTransform));
        hudPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtHud = hudPanelObj.GetComponent<RectTransform>();
        rtHud.anchorMin = new Vector2(0f, 1f);
        rtHud.anchorMax = new Vector2(1f, 1f);
        rtHud.pivot = new Vector2(0.5f, 1f);
        rtHud.anchoredPosition = new Vector2(0f, -20f);
        rtHud.sizeDelta = new Vector2(-40f, 100f);

        Text txtScore = CreateHeaderText(hudPanelObj.transform, "Txt_CurrentScore", "Puntos: 0", new Vector2(200f, -30f), 36, Color.white, TextAnchor.MiddleLeft);
        Text txtHighScore = CreateHeaderText(hudPanelObj.transform, "Txt_HighScore", "Récord: 0", new Vector2(-30f, -30f), 30, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
        Text txtDiff = CreateHeaderText(hudPanelObj.transform, "Txt_Difficulty", "NIVEL 1: ZONA AZUL", new Vector2(-280f, -30f), 28, new Color(0.3f, 0.85f, 1.0f), TextAnchor.MiddleRight);

        // Botón interactivo para alternar Skin del personaje (Criterio 2)
        GameObject btnSkinObj = CreateSimpleButton(hudPanelObj.transform, "Btn_SkinSelector", "🎨 SKIN: DORADO (C)", new Vector2(0f, -80f), new Vector2(300f, 50f), new Color(0.95f, 0.65f, 0.05f, 0.90f));
        Button btnSkin = btnSkinObj.GetComponent<Button>();
        Text txtSkinBtn = btnSkinObj.GetComponentInChildren<Text>();
        txtSkinBtn.fontSize = 20;

        PlayerVisualCustomizer3D customizer = controller.GetComponent<PlayerVisualCustomizer3D>();
        if (customizer != null)
        {
            btnSkin.onClick.AddListener(customizer.CycleNextSkin);
            SerializedObject soC = new SerializedObject(customizer);
            soC.FindProperty("skinButtonText").objectReferenceValue = txtSkinBtn;
            soC.ApplyModifiedProperties();
        }

        // 2. Banner Central de Subida de Nivel
        Transform bannerPanel = canvasObj.transform.Find("Banner_LevelUpPanel");
        if (bannerPanel != null) DestroyImmediate(bannerPanel.gameObject);

        GameObject bannerObj = new GameObject("Banner_LevelUpPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bannerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtBanner = bannerObj.GetComponent<RectTransform>();
        rtBanner.sizeDelta = new Vector2(750f, 170f);
        rtBanner.anchoredPosition = new Vector2(0f, 140f);
        bannerObj.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.15f, 0.92f);

        Text txtBannerTitle = CreateHeaderText(bannerObj.transform, "Txt_BannerTitle", "★ ¡NIVEL 2: ZONA PÚRPURA! ★", new Vector2(0f, 30f), 38, Color.yellow, TextAnchor.MiddleCenter);
        Text txtBannerSubtitle = CreateHeaderText(bannerObj.transform, "Txt_BannerSubtitle", "¡Velocidad x1.40! Nueva atmósfera e iluminación.", new Vector2(0f, -30f), 24, Color.white, TextAnchor.MiddleCenter);

        bannerObj.SetActive(false);

        // 3. Panel Modal de Game Over
        Transform goPanel = canvasObj.transform.Find("Modal_GameOverPanel");
        if (goPanel != null) DestroyImmediate(goPanel.gameObject);

        GameObject goPanelObj = new GameObject("Modal_GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtGo = goPanelObj.GetComponent<RectTransform>();
        rtGo.anchorMin = Vector2.zero;
        rtGo.anchorMax = Vector2.one;
        rtGo.offsetMin = Vector2.zero;
        rtGo.offsetMax = Vector2.zero;
        Image imgGo = goPanelObj.GetComponent<Image>();
        imgGo.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);

        GameObject modalBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalBox.transform.SetParent(goPanelObj.transform, false);
        RectTransform rtBox = modalBox.GetComponent<RectTransform>();
        rtBox.sizeDelta = new Vector2(650f, 450f);
        rtBox.anchoredPosition = Vector2.zero;
        modalBox.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.25f, 0.95f);

        CreateHeaderText(modalBox.transform, "Title", "¡FIN DE LA PARTIDA!", new Vector2(0f, 150f), 42, new Color(0.95f, 0.25f, 0.3f), TextAnchor.MiddleCenter);
        Text txtFinalScore = CreateHeaderText(modalBox.transform, "FinalScore", "Puntuación: 0", new Vector2(0f, 70f), 32, Color.white, TextAnchor.MiddleCenter);
        Text txtFinalHighScore = CreateHeaderText(modalBox.transform, "FinalHighScore", "Mejor Récord: 0", new Vector2(0f, 10f), 28, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);

        GameObject btnRetryObj = CreateSimpleButton(modalBox.transform, "Btn_Retry", "REINTENTAR ⟳", new Vector2(0f, -80f), new Vector2(300f, 70f), new Color(0.10f, 0.65f, 0.40f));
        Button btnRetry = btnRetryObj.GetComponent<Button>();

        GameObject btnMenuObj = CreateSimpleButton(modalBox.transform, "Btn_Menu", "MENÚ PRINCIPAL", new Vector2(0f, -165f), new Vector2(300f, 60f), new Color(0.20f, 0.25f, 0.35f));
        Button btnMenu = btnMenuObj.GetComponent<Button>();

        goPanelObj.SetActive(false);

        // Conectar referencias en GameManagers_3D
        GameObject gmObj = GameObject.Find("GameManagers_3D");
        if (gmObj != null)
        {
            ScoreManager3D sm = gmObj.GetComponent<ScoreManager3D>();
            if (sm != null)
            {
                SerializedObject soSm = new SerializedObject(sm);
                soSm.FindProperty("currentScoreText").objectReferenceValue = txtScore;
                soSm.FindProperty("highScoreText").objectReferenceValue = txtHighScore;
                soSm.FindProperty("playerTransform").objectReferenceValue = controller.transform;
                soSm.ApplyModifiedProperties();
            }

            DifficultyManager3D dm = gmObj.GetComponent<DifficultyManager3D>();
            if (dm != null)
            {
                SerializedObject soDm = new SerializedObject(dm);
                soDm.FindProperty("playerController").objectReferenceValue = controller;
                soDm.FindProperty("difficultyText").objectReferenceValue = txtDiff;
                soDm.FindProperty("levelBannerPanel").objectReferenceValue = bannerObj;
                soDm.FindProperty("levelBannerTitleText").objectReferenceValue = txtBannerTitle;
                soDm.FindProperty("levelBannerSubtitleText").objectReferenceValue = txtBannerSubtitle;
                soDm.ApplyModifiedProperties();
            }

            GameLoopManager3D glm = gmObj.GetComponent<GameLoopManager3D>();
            if (glm != null)
            {
                SerializedObject soGlm = new SerializedObject(glm);
                soGlm.FindProperty("playerTransform").objectReferenceValue = controller.transform;
                soGlm.FindProperty("gameOverPanel").objectReferenceValue = goPanelObj;
                soGlm.FindProperty("finalScoreText").objectReferenceValue = txtFinalScore;
                soGlm.FindProperty("finalHighScoreText").objectReferenceValue = txtFinalHighScore;
                soGlm.FindProperty("retryButton").objectReferenceValue = btnRetry;
                soGlm.FindProperty("menuButton").objectReferenceValue = btnMenu;
                soGlm.ApplyModifiedProperties();
            }
        }

        // 4. Panel de Controles Móviles (D-Pad y Salto)
        Transform controlsPanel = canvasObj.transform.Find("MobileControlsPanel");
        if (controlsPanel != null) DestroyImmediate(controlsPanel.gameObject);

        GameObject cPanelObj = new GameObject("MobileControlsPanel", typeof(RectTransform));
        cPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = cPanelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        controlsPanel = cPanelObj.transform;

        Vector2 dpadCenter = new Vector2(200, 200);
        float dpadSpacing = 110f;
        Color dpadColor = new Color(0.12f, 0.30f, 0.55f, 0.80f);

        CreateTouchButton(controlsPanel, "Btn_Forward", "▲", dpadCenter + new Vector2(0, dpadSpacing), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Forward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Backward", "▼", dpadCenter + new Vector2(0, -dpadSpacing), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Backward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Left", "◀", dpadCenter + new Vector2(-dpadSpacing, 0), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Left, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Right", "▶", dpadCenter + new Vector2(dpadSpacing, 0), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Right, controller, new Vector2(100, 100), dpadColor);

        Color jumpBtnColor = new Color(0.08f, 0.72f, 0.45f, 0.90f);
        Vector2 jumpPos = new Vector2(-160, 160);
        CreateTouchButton(controlsPanel, "Btn_Jump", "SALTO\n▲", jumpPos, new Vector2(1, 0), new Vector2(1, 0), VirtualTouchButton.ButtonDirection.Jump, controller, new Vector2(170, 170), jumpBtnColor);

        Debug.Log("[SetupScene3DHelper] UI móvil generada con HUD, Selector de Skin, Banner de Nivel, Modal Game Over y Controles.");
    }

    private static void SaveAndRegisterScene()
    {
        string scenesDir = "Assets/Scenes";
        if (!Directory.Exists(scenesDir))
        {
            Directory.CreateDirectory(scenesDir);
        }

        string scenePath = $"{scenesDir}/MainGame3D.unity";
        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();

        EditorSceneManager.SaveScene(activeScene, scenePath);
        Debug.Log($"[SetupScene3DHelper] Escena guardada con éxito en: {scenePath}");

        // Registrar en EditorBuildSettings si no está añadida
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyAdded = false;
        foreach (var bs in buildScenes)
        {
            if (bs.path == scenePath)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[SetupScene3DHelper] Escena '{scenePath}' registrada en Build Settings.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreateSimpleButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

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
        txt.fontSize = 24;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.raycastTarget = false;

        return btnObj;
    }

    private static Text CreateHeaderText(Transform parent, string name, string initialText, Vector2 anchoredPos, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(650f, 60f);
        rt.anchoredPosition = anchoredPos;

        Text txt = textObj.GetComponent<Text>();
        txt.text = initialText;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = color;
        txt.alignment = alignment;
        txt.raycastTarget = false;

        return txt;
    }

    private static void CreateTouchButton(Transform parent, string name, string label, Vector2 anchoredPos, 
        Vector2 anchorMin, Vector2 anchorMax, VirtualTouchButton.ButtonDirection dir, PlayerController3D controller, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualTouchButton));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

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
}
#endif
