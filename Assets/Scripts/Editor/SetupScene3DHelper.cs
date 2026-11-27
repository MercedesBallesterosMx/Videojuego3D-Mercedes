#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.IO;

/// <summary>
/// Asistente de Editor integral para Unity que automatiza el montaje de la escena para las Actividades 4.1, 4.2 y 4.3.
/// Integra el entorno 3D, personaje físico, cámara cinematográfica, contadores UI,
/// curva de dificultad progresiva, generador procedimental infinito (LevelGenerator3D)
/// y el bucle de juego con panel modal de Game Over y reinicio (GameLoopManager3D).
/// 
/// Accesible desde el menú superior: 'Herramientas 3D > Montar Escenario Dinámico e Infinito (Actividad 4.3)'
/// </summary>
public class SetupScene3DHelper : EditorWindow
{
    [MenuItem("Herramientas 3D/Montar Escenario Dinámico e Infinito (Actividad 4.3)")]
    public static void GenerateComplete3DScene()
    {
        Undo.SetCurrentGroupName("Montar Escena 3D Actividad 4.3");

        Debug.Log("--- Iniciando montaje completo de la escena 4.3 con Generador Infinito y Game Loop ---");

        // 1. Detectar shader compatible (evita el color magenta/rosa)
        Shader universalShader = ResolveCompatibleShader();

        // 2. Crear materiales de alto contraste
        Material matFloor = CreateOrUpdateMaterial("Mat_Floor_DarkSlate", universalShader, new Color(0.12f, 0.16f, 0.22f), 0.2f);
        Material matPlayer = CreateOrUpdateMaterial("Mat_Player_VibrantGold", universalShader, new Color(1.0f, 0.72f, 0.0f), 0.8f);
        Material matPlayerBand = CreateOrUpdateMaterial("Mat_Player_Band_Dark", universalShader, new Color(0.06f, 0.09f, 0.16f), 0.9f);
        Material matRamp = CreateOrUpdateMaterial("Mat_Ramp_ElectricCyan", universalShader, new Color(0.02f, 0.75f, 0.88f), 0.5f);
        Material matPlatform = CreateOrUpdateMaterial("Mat_Platform_EmeraldGreen", universalShader, new Color(0.08f, 0.75f, 0.45f), 0.5f);
        Material matObstacle = CreateOrUpdateMaterial("Mat_Obstacle_RubyRed", universalShader, new Color(0.92f, 0.20f, 0.25f), 0.4f);

        // 3. Configurar Iluminación
        SetupLighting();

        // 4. Construir Escenario Inicial con Módulos
        GameObject groundParent = SetupEnvironment(matFloor, matRamp, matPlatform, matObstacle);

        // 5. Crear Personaje Principal con Físicas y color dorado
        GameObject player = SetupPlayer(matPlayer, matPlayerBand);

        // 6. Configurar Cámara Cinemática
        SetupCamera(player.transform);

        // 7. Configurar Gestores de Puntuación, Dificultad, Generador Infinito y Bucle de Juego
        SetupGameManagers(player.GetComponent<PlayerController3D>());

        // 8. Construir Interfaz de Usuario Móvil (Contadores, D-Pad, Botón Salto y Modal Game Over)
        SetupMobileUI(player.GetComponent<PlayerController3D>());

        EditorUtility.DisplayDialog("Montaje de Actividad 4.3 Concluido", 
            "¡El proyecto 3D ha sido configurado con éxito para la Actividad 4.3!\n\n" +
            "✓ Generación Infinita: LevelGenerator3D genera dinámicamente nuevos módulos al avanzar en Z y destruye los rebasados.\n" +
            "✓ Arquitectura Configurable: Todos los parámetros de longitud, distancia y prefabs son editables en el Inspector.\n" +
            "✓ Bucle de Juego (Game Loop): Detección de caída o impacto letal (HazardPart3D) con modal de Game Over y botón Reintentar.\n" +
            "✓ Persistencia y Dificultad: Score continuo en Z, HighScore con PlayerPrefs y fases progresivas de aceleración.\n\n" +
            "Presiona Play (▶) para probar el circuito infinito y el bucle de juego.", 
            "¡Excelente!");
    }

    private static Shader ResolveCompatibleShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpShader != null) return urpShader;
        }

        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null) return standardShader;

        return Shader.Find("Mobile/Diffuse") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
    }

    private static Material CreateOrUpdateMaterial(string matName, Shader shader, Color color, float smoothness = 0.5f)
    {
        string dirPath = "Assets/Materials";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string assetPath = $"{dirPath}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        else
        {
            mat.shader = shader;
        }

        mat.color = color;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private static void SetupLighting()
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
        dirLight.intensity = 1.15f;
        dirLight.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.42f);
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

        // Banda oscura para visualización de giro
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Ball_RotationBand";
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

    private static void SetupGameManagers(PlayerController3D controller)
    {
        GameObject gmObj = GameObject.Find("GameManagers_3D");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManagers_3D");
        }

        // 1. ScoreManager3D
        ScoreManager3D scoreManager = gmObj.GetComponent<ScoreManager3D>();
        if (scoreManager == null) scoreManager = gmObj.AddComponent<ScoreManager3D>();

        // 2. DifficultyManager3D
        DifficultyManager3D diffManager = gmObj.GetComponent<DifficultyManager3D>();
        if (diffManager == null) diffManager = gmObj.AddComponent<DifficultyManager3D>();

        // 3. LevelGenerator3D (Generador Procedimental Infinito)
        LevelGenerator3D levelGen = gmObj.GetComponent<LevelGenerator3D>();
        if (levelGen == null) levelGen = gmObj.AddComponent<LevelGenerator3D>();
        SerializedObject soLg = new SerializedObject(levelGen);
        soLg.FindProperty("playerTransform").objectReferenceValue = controller.transform;
        soLg.FindProperty("initialSectionsCount").intValue = 6;
        soLg.FindProperty("defaultSectionLength").floatValue = 25f;
        soLg.FindProperty("spawnDistanceAhead").floatValue = 75f;
        soLg.FindProperty("despawnDistanceBehind").floatValue = 40f;
        soLg.ApplyModifiedProperties();

        // 4. GameLoopManager3D (Control del Bucle de Juego)
        GameLoopManager3D gameLoop = gmObj.GetComponent<GameLoopManager3D>();
        if (gameLoop == null) gameLoop = gmObj.AddComponent<GameLoopManager3D>();
        SerializedObject soGl = new SerializedObject(gameLoop);
        soGl.FindProperty("playerTransform").objectReferenceValue = controller.transform;
        soGl.FindProperty("fallThresholdY").floatValue = -4.0f;
        soGl.ApplyModifiedProperties();
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

        Text txtScore = CreateHeaderText(hudPanelObj.transform, "Txt_CurrentScore", "Puntos: 0", new Vector2(200f, -30f), 38, Color.white, TextAnchor.MiddleLeft);
        Text txtHighScore = CreateHeaderText(hudPanelObj.transform, "Txt_HighScore", "Récord: 0", new Vector2(0f, -30f), 32, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
        Text txtDiff = CreateHeaderText(hudPanelObj.transform, "Txt_Difficulty", "Dificultad: Nivel 1", new Vector2(-220f, -30f), 30, new Color(0.2f, 0.85f, 0.5f), TextAnchor.MiddleRight);

        // 2. Panel Modal de Game Over (Bucle de Juego)
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

        // Cuadro central de diálogo Game Over
        GameObject modalBox = new GameObject("DialogBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalBox.transform.SetParent(goPanelObj.transform, false);
        RectTransform rtBox = modalBox.GetComponent<RectTransform>();
        rtBox.sizeDelta = new Vector2(650f, 450f);
        rtBox.anchoredPosition = Vector2.zero;
        modalBox.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.25f, 0.95f);

        CreateHeaderText(modalBox.transform, "Title", "¡FIN DE LA PARTIDA!", new Vector2(0f, 150f), 42, new Color(0.95f, 0.25f, 0.3f), TextAnchor.MiddleCenter);
        Text txtFinalScore = CreateHeaderText(modalBox.transform, "FinalScore", "Puntuación: 0", new Vector2(0f, 70f), 32, Color.white, TextAnchor.MiddleCenter);
        Text txtFinalHighScore = CreateHeaderText(modalBox.transform, "FinalHighScore", "Mejor Récord: 0", new Vector2(0f, 10f), 28, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);

        // Botón Reintentar Partida
        GameObject btnRetryObj = CreateSimpleButton(modalBox.transform, "Btn_Retry", "REINTENTAR ⟳", new Vector2(0f, -80f), new Vector2(300f, 70f), new Color(0.10f, 0.65f, 0.40f));
        Button btnRetry = btnRetryObj.GetComponent<Button>();

        // Botón Menú Principal
        GameObject btnMenuObj = CreateSimpleButton(modalBox.transform, "Btn_Menu", "MENÚ PRINCIPAL", new Vector2(0f, -165f), new Vector2(300f, 60f), new Color(0.20f, 0.25f, 0.35f));
        Button btnMenu = btnMenuObj.GetComponent<Button>();

        goPanelObj.SetActive(false); // Oculto al inicio

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

        // 3. Panel de Controles Móviles (D-Pad y Salto)
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

        Debug.Log("[SetupScene3DHelper] UI móvil generada con HUD, Modal Game Over y Controles.");
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
        txt.fontSize = 26;
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
        rt.sizeDelta = new Vector2(500f, 60f);
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
