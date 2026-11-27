#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.IO;

/// <summary>
/// Asistente de Editor para Unity que automatiza el montaje de la escena para las Actividades 4.1 y 4.2.
/// Genera el entorno 3D, el personaje físico, la cámara cinematográfica,
/// el contador visual de puntuación (Score), la persistencia (HighScore con PlayerPrefs),
/// la curva de dificultad progresiva y los obstáculos móviles dinámicos.
/// 
/// Accesible desde el menú superior: 'Herramientas 3D > Montar Escenario 3D Completo (Actividad 4.1 y 4.2)'
/// </summary>
public class SetupScene3DHelper : EditorWindow
{
    [MenuItem("Herramientas 3D/Montar Escenario 3D Completo (Actividad 4.1 y 4.2)")]
    public static void GenerateComplete3DScene()
    {
        Undo.SetCurrentGroupName("Montar Escena 3D Actividad 4.2");

        Debug.Log("--- Iniciando montaje completo de la escena 3D con Score y Dificultad (Actividad 4.2) ---");

        // 1. Detectar shader compatible
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

        // 4. Construir Escenario 3D con Obstáculos Dinámicos
        GameObject groundParent = SetupEnvironment(matFloor, matRamp, matPlatform, matObstacle);

        // 5. Crear Personaje Principal con Físicas
        GameObject player = SetupPlayer(matPlayer, matPlayerBand);

        // 6. Configurar Cámara Cinemática
        SetupCamera(player.transform);

        // 7. Configurar Gestores de Puntuación y Dificultad
        SetupGameManagers(player.GetComponent<PlayerController3D>());

        // 8. Construir Interfaz de Usuario Móvil con Contadores (Score y HighScore)
        SetupMobileUI(player.GetComponent<PlayerController3D>());

        EditorUtility.DisplayDialog("Montaje de Actividad 4.2 Concluido", 
            "¡El proyecto 3D ha sido configurado con éxito para la Actividad 4.2!\n\n" +
            "✓ Contador Visual UI: Puntuación (Score) que incrementa uniformemente con el avance en Z.\n" +
            "✓ Persistencia de Datos: Guardado de Récord Histórico (HighScore) con PlayerPrefs.\n" +
            "✓ Curva de Dificultad: Fases progresivas que aceleran al personaje y a los obstáculos.\n" +
            "✓ Dinámicas 3D: Obstáculos cinemáticos móviles en X y rotatorios en Y.\n" +
            "✓ Controles Móviles: D-Pad y Botón SALTO ergonómico en la esquina derecha.\n\n" +
            "Presiona Play (▶) para probar el avance tridimensional y el contador de puntos.", 
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

        // 1. Suelo principal continuo en el plano XZ (60 unidades de recorrido)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_Main";
        floor.transform.SetParent(envParent.transform);
        floor.transform.position = new Vector3(0f, -0.5f, 35f);
        floor.transform.localScale = new Vector3(14f, 1f, 80f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // 2. Rampa de prueba (Eje Y)
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp_Elevation_Y";
        ramp.transform.SetParent(envParent.transform);
        ramp.transform.position = new Vector3(0f, 1.25f, 18f);
        ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp.transform.localScale = new Vector3(6f, 0.8f, 10f);
        ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

        // 3. Plataforma superior elevada (Eje Y)
        GameObject highPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highPlatform.name = "High_Platform_Y";
        highPlatform.transform.SetParent(envParent.transform);
        highPlatform.transform.position = new Vector3(0f, 2.75f, 32f);
        highPlatform.transform.localScale = new Vector3(8f, 1f, 16f);
        highPlatform.GetComponent<Renderer>().sharedMaterial = platMat;

        // 4. Obstáculos dinámicos móviles (Curva de dificultad)
        for (int i = 0; i < 5; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Dynamic_Obstacle_{i + 1}";
            obstacle.transform.SetParent(envParent.transform);
            float zPos = 12f + (i * 12f);
            obstacle.transform.position = new Vector3(0f, 0.75f, zPos);
            obstacle.transform.localScale = new Vector3(2.5f, 1.5f, 1.5f);
            obstacle.GetComponent<Renderer>().sharedMaterial = obsMat;

            // Agregar script de oscilación tridimensional
            MovingObstacle3D mover = obstacle.AddComponent<MovingObstacle3D>();
            SerializedObject soMover = new SerializedObject(mover);
            soMover.FindProperty("amplitude").floatValue = 3.5f;
            soMover.FindProperty("baseSpeed").floatValue = 1.8f + (i * 0.4f);
            soMover.ApplyModifiedProperties();
        }

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

        // Banda ecuatorial de contraste para ver el giro 3D
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Ball_RotationBand";
        band.transform.SetParent(player.transform, false);
        band.transform.localPosition = Vector3.zero;
        band.transform.localScale = new Vector3(1.02f, 0.08f, 1.02f);
        band.GetComponent<Renderer>().sharedMaterial = bandMat;
        DestroyImmediate(band.GetComponent<Collider>());

        // Rigidbody físico
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // PlayerController3D
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

        ScoreManager3D scoreManager = gmObj.GetComponent<ScoreManager3D>();
        if (scoreManager == null) scoreManager = gmObj.AddComponent<ScoreManager3D>();

        DifficultyManager3D diffManager = gmObj.GetComponent<DifficultyManager3D>();
        if (diffManager == null) diffManager = gmObj.AddComponent<DifficultyManager3D>();
    }

    private static void SetupMobileUI(PlayerController3D controller)
    {
        // 1. EventSystem
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // 2. Canvas Adaptativo para Móviles
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

        // 3. Panel Superior de HUD: Contadores de Score, HighScore y Dificultad (Criterio 1 y 3)
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

        // Contador de Score Actual (Izquierda superior)
        Text txtScore = CreateHeaderText(hudPanelObj.transform, "Txt_CurrentScore", "Puntos: 0", new Vector2(200f, -30f), 38, Color.white, TextAnchor.MiddleLeft);
        
        // Contador de HighScore Persistente (Centro superior)
        Text txtHighScore = CreateHeaderText(hudPanelObj.transform, "Txt_HighScore", "Récord: 0", new Vector2(0f, -30f), 32, new Color(1.0f, 0.85f, 0.2f), TextAnchor.MiddleCenter);

        // Etiqueta de Nivel de Dificultad (Derecha superior)
        Text txtDiff = CreateHeaderText(hudPanelObj.transform, "Txt_Difficulty", "Dificultad: Nivel 1", new Vector2(-220f, -30f), 30, new Color(0.2f, 0.85f, 0.5f), TextAnchor.MiddleRight);

        // Conectar referencias en ScoreManager3D y DifficultyManager3D
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

        // D-PAD TÁCTIL (Esquina inferior izquierda)
        Vector2 dpadCenter = new Vector2(200, 200);
        float dpadSpacing = 110f;
        Color dpadColor = new Color(0.12f, 0.30f, 0.55f, 0.80f);

        CreateTouchButton(controlsPanel, "Btn_Forward", "▲", dpadCenter + new Vector2(0, dpadSpacing), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Forward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Backward", "▼", dpadCenter + new Vector2(0, -dpadSpacing), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Backward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Left", "◀", dpadCenter + new Vector2(-dpadSpacing, 0), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Left, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Right", "▶", dpadCenter + new Vector2(dpadSpacing, 0), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Right, controller, new Vector2(100, 100), dpadColor);

        // BOTÓN DE SALTO (Esquina inferior derecha)
        Color jumpBtnColor = new Color(0.08f, 0.72f, 0.45f, 0.90f);
        Vector2 jumpPos = new Vector2(-160, 160);
        CreateTouchButton(controlsPanel, "Btn_Jump", "SALTO\n▲", jumpPos, new Vector2(1, 0), new Vector2(1, 0), VirtualTouchButton.ButtonDirection.Jump, controller, new Vector2(170, 170), jumpBtnColor);

        Debug.Log("[SetupScene3DHelper] UI móvil generada con Contadores y Controles táctiles.");
    }

    private static Text CreateHeaderText(Transform parent, string name, string initialText, Vector2 anchoredPos, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 60f);
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

        // Texto del botón
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
