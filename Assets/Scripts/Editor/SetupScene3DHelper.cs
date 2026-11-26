#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

/// <summary>
/// Asistente de Editor para Unity que automatiza el montaje completo de la escena 3D.
/// Resuelve el contraste visual cromático (elimina el blanco uniforme) y garantiza
/// el funcionamiento responsivo del botón táctil de SALTO y controles móviles.
/// 
/// Accesible desde el menú superior de Unity: 'Herramientas 3D > Montar Escenario y Controles 3D (Actividad 4.1)'
/// </summary>
public class SetupScene3DHelper : EditorWindow
{
    [MenuItem("Herramientas 3D/Montar Escenario y Controles 3D (Actividad 4.1)")]
    public static void GenerateComplete3DScene()
    {
        Undo.SetCurrentGroupName("Montar Escena 3D Actividad 4.1");

        Debug.Log("--- Iniciando montaje optimizado de la escena 3D móvil ---");

        // 1. Crear paleta de materiales estilizados con alto contraste
        Material matFloor = GetOrCreateMaterial("Mat_Floor_DarkSlate", new Color(0.12f, 0.16f, 0.24f));       // Pista oscura elegante
        Material matPlayer = GetOrCreateMaterial("Mat_Player_NeonOrange", new Color(1.0f, 0.40f, 0.05f));     // Esfera naranja brillante visible
        Material matRamp = GetOrCreateMaterial("Mat_Ramp_Cyan", new Color(0.05f, 0.65f, 0.90f));             // Rampa celeste vivo (Eje Y)
        Material matPlatform = GetOrCreateMaterial("Mat_Platform_Emerald", new Color(0.08f, 0.72f, 0.45f));   // Plataforma elevada verde
        Material matObstacle = GetOrCreateMaterial("Mat_Obstacle_Crimson", new Color(0.92f, 0.22f, 0.26f));   // Obstáculos rojo carmín

        // 2. Configurar Iluminación y Cámara (fondo de contraste suave)
        SetupLighting();

        // 3. Construir Escenario 3D con materiales cromáticos
        GameObject groundParent = SetupEnvironment(matFloor, matRamp, matPlatform, matObstacle);

        // 4. Crear Personaje Principal con Físicas y color naranja de alta visibilidad
        GameObject player = SetupPlayer(matPlayer);

        // 5. Configurar Cámara Cinemática con Restricciones (Criterios 3 y 4)
        SetupCamera(player.transform);

        // 6. Construir Interfaz de Usuario (UI) Adaptada con Botón de Salto Responsivo (Criterio 1)
        SetupMobileUI(player.GetComponent<PlayerController3D>());

        EditorUtility.DisplayDialog("Escena 3D Actualizada con Éxito", 
            "¡El proyecto 3D ha sido configurado con éxito!\n\n" +
            "✓ Color y Visibilidad: La esfera ahora es de color NARANJA VIVO sobre pista AZUL OSCURA para máxima claridad.\n" +
            "✓ Salto Optimizado: El botón SALTO ahora responde con triple evento (Touch + Click + Buffer) en la esquina inferior derecha.\n" +
            "✓ Criterio 2: Físicas tridimensionales efectivas en X, Y, Z.\n" +
            "✓ Criterios 3 y 4: Cámara cinematográfica en X y Z con distancia Z constante.\n\n" +
            "Presiona Play (▶) para probar los controles y el salto.", 
            "¡Entendido!");
    }

    private static Material GetOrCreateMaterial(string matName, Color color)
    {
        string dirPath = "Assets/Materials";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        string assetPath = $"{dirPath}/{matName}.mat";
        Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existingMat != null)
        {
            ApplyColorToMaterial(existingMat, color);
            return existingMat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                     ?? Shader.Find("Standard") 
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                     ?? Shader.Find("Diffuse");

        Material newMat = new Material(shader);
        ApplyColorToMaterial(newMat, color);
        AssetDatabase.CreateAsset(newMat, assetPath);
        AssetDatabase.SaveAssets();

        return newMat;
    }

    private static void ApplyColorToMaterial(Material mat, Color col)
    {
        mat.color = col;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.65f);
    }

    private static void SetupLighting()
    {
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightObj.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }
        dirLight.color = new Color(1.0f, 0.97f, 0.90f);
        dirLight.intensity = 1.10f;
        dirLight.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(0.25f, 0.30f, 0.40f);
    }

    private static GameObject SetupEnvironment(Material floorMat, Material rampMat, Material platMat, Material obsMat)
    {
        GameObject envParent = GameObject.Find("Environment_3D");
        if (envParent != null)
        {
            DestroyImmediate(envParent);
        }
        envParent = new GameObject("Environment_3D");

        // 1. Suelo principal (Ejes X y Z)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_Main";
        floor.transform.SetParent(envParent.transform);
        floor.transform.position = new Vector3(0f, -0.5f, 25f);
        floor.transform.localScale = new Vector3(14f, 1f, 60f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // 2. Rampa de prueba (Demuestra elevación en Eje Y)
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp_Elevation_Y";
        ramp.transform.SetParent(envParent.transform);
        ramp.transform.position = new Vector3(0f, 1.25f, 15f);
        ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp.transform.localScale = new Vector3(6f, 0.8f, 10f);
        ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

        // 3. Plataforma superior elevada (Eje Y)
        GameObject highPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highPlatform.name = "High_Platform_Y";
        highPlatform.transform.SetParent(envParent.transform);
        highPlatform.transform.position = new Vector3(0f, 2.75f, 28f);
        highPlatform.transform.localScale = new Vector3(8f, 1f, 14f);
        highPlatform.GetComponent<Renderer>().sharedMaterial = platMat;

        // 4. Obstáculos laterales (Eje X)
        for (int i = 0; i < 4; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_Cube_{i + 1}";
            obstacle.transform.SetParent(envParent.transform);
            float xOffset = (i % 2 == 0) ? -3.5f : 3.5f;
            obstacle.transform.position = new Vector3(xOffset, 0.75f, 8f + (i * 9f));
            obstacle.transform.localScale = new Vector3(2f, 1.5f, 2f);
            obstacle.GetComponent<Renderer>().sharedMaterial = obsMat;
        }

        return envParent;
    }

    private static GameObject SetupPlayer(Material playerMat)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player_Ball3D";
            player.tag = "Player";
        }

        player.transform.position = new Vector3(0f, 0.75f, 0f);
        player.transform.localScale = Vector3.one * 1.3f;
        player.GetComponent<Renderer>().sharedMaterial = playerMat;

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

        // Fondo elegante de contraste
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.15f, 0.19f, 0.27f);

        CameraFollow3D followScript = mainCam.GetComponent<CameraFollow3D>();
        if (followScript == null) followScript = mainCam.gameObject.AddComponent<CameraFollow3D>();

        SerializedObject so = new SerializedObject(followScript);
        so.FindProperty("target").objectReferenceValue = playerTransform;
        so.FindProperty("offset").vector3Value = new Vector3(0f, 6.0f, -9.5f);
        so.FindProperty("smoothTime").floatValue = 0.20f;
        so.FindProperty("lockFixedZDistance").boolValue = true;
        so.FindProperty("followYAxis").boolValue = false;
        so.FindProperty("lookAtTarget").boolValue = true;
        so.ApplyModifiedProperties();

        mainCam.transform.position = playerTransform.position + new Vector3(0f, 6.0f, -9.5f);
        mainCam.transform.LookAt(playerTransform.position + Vector3.up * 0.8f);
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
            scaler.referenceResolution = new Vector2(1920, 1080); // Compatible con landscape y portrait
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // 3. MobileUIManager3D
        MobileUIManager3D uiManager = canvasObj.GetComponent<MobileUIManager3D>();
        if (uiManager == null) uiManager = canvasObj.AddComponent<MobileUIManager3D>();

        // 4. Panel de Controles Móviles
        Transform controlsPanel = canvasObj.transform.Find("MobileControlsPanel");
        if (controlsPanel != null)
        {
            DestroyImmediate(controlsPanel.gameObject);
        }

        GameObject cPanelObj = new GameObject("MobileControlsPanel", typeof(RectTransform));
        cPanelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = cPanelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        controlsPanel = cPanelObj.transform;

        // --- D-PAD TÁCTIL (Anclado a Esquina Inferior Izquierda) ---
        Vector2 dpadCenter = new Vector2(180, 180);
        float dpadSpacing = 110f;
        Color dpadColor = new Color(0.15f, 0.35f, 0.65f, 0.75f);

        CreateTouchButton(controlsPanel, "Btn_Forward", "▲", dpadCenter + new Vector2(0, dpadSpacing), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Forward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Backward", "▼", dpadCenter + new Vector2(0, -dpadSpacing), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Backward, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Left", "◀", dpadCenter + new Vector2(-dpadSpacing, 0), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Left, controller, new Vector2(100, 100), dpadColor);
        CreateTouchButton(controlsPanel, "Btn_Right", "▶", dpadCenter + new Vector2(dpadSpacing, 0), new Vector2(0, 0), new Vector2(0, 0), VirtualTouchButton.ButtonDirection.Right, controller, new Vector2(100, 100), dpadColor);

        // --- BOTÓN DE SALTO (Anclado a Esquina Inferior Derecha para máxima comodidad táctil) ---
        Color jumpBtnColor = new Color(0.08f, 0.72f, 0.45f, 0.88f); // Verde esmeralda vibrante
        Vector2 jumpPos = new Vector2(-150, 150);
        CreateTouchButton(controlsPanel, "Btn_Jump", "SALTO\n▲", jumpPos, new Vector2(1, 0), new Vector2(1, 0), VirtualTouchButton.ButtonDirection.Jump, controller, new Vector2(170, 170), jumpBtnColor);

        Debug.Log("[SetupScene3DHelper] UI móvil generada con D-Pad y Botón de Salto anclado a la derecha.");
    }

    private static void CreateTouchButton(Transform parent, string name, string label, Vector2 anchoredPos, 
        Vector2 anchorMin, Vector2 anchorMax, VirtualTouchButton.ButtonDirection dir, PlayerController3D controller, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualTouchButton));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = (anchorMin == Vector2.zero) ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.5f);
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

        // Si es salto, vincular también al OnClick del Button para triple redundancia
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
        txt.raycastTarget = false; // Permite que el clic pase directamente al botón
    }
}
#endif
