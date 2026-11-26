#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.IO;

/// <summary>
/// Asistente de Editor para Unity que automatiza el montaje completo de la escena 3D.
/// Resuelve definitivamente el problema de compatibilidad de shaders (evita el magenta/rosa),
/// genera materiales Standard compatibles con el render pipeline activo y asegura que
/// la esfera del jugador sea brillante, nítida y perfectamente visible con rotación 3D.
/// 
/// Accesible desde el menú superior de Unity: 'Herramientas 3D > Montar Escenario y Controles 3D (Actividad 4.1)'
/// </summary>
public class SetupScene3DHelper : EditorWindow
{
    [MenuItem("Herramientas 3D/Montar Escenario y Controles 3D (Actividad 4.1)")]
    public static void GenerateComplete3DScene()
    {
        Undo.SetCurrentGroupName("Montar Escena 3D Actividad 4.1");

        Debug.Log("--- Iniciando montaje con shaders Standard compatibles ---");

        // 1. Detectar el shader correcto según el pipeline activo (evita el color magenta/rosa)
        Shader universalShader = ResolveCompatibleShader();

        // 2. Crear materiales con colores estilizados de alto contraste
        Material matFloor = CreateOrUpdateMaterial("Mat_Floor_DarkSlate", universalShader, new Color(0.12f, 0.16f, 0.22f), 0.2f);
        Material matPlayer = CreateOrUpdateMaterial("Mat_Player_VibrantGold", universalShader, new Color(1.0f, 0.72f, 0.0f), 0.8f);
        Material matPlayerBand = CreateOrUpdateMaterial("Mat_Player_Band_Dark", universalShader, new Color(0.06f, 0.09f, 0.16f), 0.9f);
        Material matRamp = CreateOrUpdateMaterial("Mat_Ramp_ElectricCyan", universalShader, new Color(0.02f, 0.75f, 0.88f), 0.5f);
        Material matPlatform = CreateOrUpdateMaterial("Mat_Platform_EmeraldGreen", universalShader, new Color(0.08f, 0.75f, 0.45f), 0.5f);
        Material matObstacle = CreateOrUpdateMaterial("Mat_Obstacle_RubyRed", universalShader, new Color(0.92f, 0.20f, 0.25f), 0.4f);

        // 3. Configurar Iluminación y Cámara
        SetupLighting();

        // 4. Construir Escenario 3D
        GameObject groundParent = SetupEnvironment(matFloor, matRamp, matPlatform, matObstacle);

        // 5. Crear Personaje Principal con Físicas y Esfera Nítida con Banda de Rotación
        GameObject player = SetupPlayer(matPlayer, matPlayerBand);

        // 6. Configurar Cámara Cinemática orientada a la esfera (Criterios 3 y 4)
        SetupCamera(player.transform);

        // 7. Construir Interfaz de Usuario (UI) Móvil y Botón de Salto (Criterio 1)
        SetupMobileUI(player.GetComponent<PlayerController3D>());

        EditorUtility.DisplayDialog("Escena 3D Configurada con Éxito", 
            "¡El proyecto 3D ha sido corregido y configurado con éxito!\n\n" +
            "✓ Shaders Corregidos: Se eliminó el color magenta/rosa. Ahora todo el escenario tiene iluminación y sombras reales.\n" +
            "✓ Esfera Visible: El jugador es una esfera DORADA BRILLANTE con banda oscura de giro sobre pista AZUL OSCURA.\n" +
            "✓ Botón de Salto: Ubicado en la esquina inferior derecha con respuesta inmediata (táctil y teclado).\n" +
            "✓ Cámara Cinemática: Sigue a la esfera en X y Z con distancia Z constante.\n\n" +
            "Presiona el botón de Play (▶) para probar el movimiento y el salto.", 
            "¡Excelente!");
    }

    private static Shader ResolveCompatibleShader()
    {
        // Si el proyecto usa Scriptable Render Pipeline (URP/HDRP)
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpShader != null) return urpShader;
        }

        // Si el proyecto usa Built-in Render Pipeline (Standard Shader nativo)
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null) return standardShader;

        Shader diffuseShader = Shader.Find("Mobile/Diffuse") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
        return diffuseShader;
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

        // 1. Suelo principal (Ejes X y Z)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_Main";
        floor.transform.SetParent(envParent.transform);
        floor.transform.position = new Vector3(0f, -0.5f, 25f);
        floor.transform.localScale = new Vector3(14f, 1f, 60f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // 2. Rampa de elevación (Demuestra desplazamiento en Eje Y)
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

        // Posición inicial clara en el suelo (z = 2 para quedar centrado en la cámara)
        player.transform.position = new Vector3(0f, 0.8f, 2.0f);
        player.transform.localScale = Vector3.one * 1.4f;
        player.GetComponent<Renderer>().sharedMaterial = playerMat;

        // Banda ecuatorial de contraste para visualizar la rotación 3D del orbe
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = "Ball_RotationBand";
        band.transform.SetParent(player.transform, false);
        band.transform.localPosition = Vector3.zero;
        band.transform.localScale = new Vector3(1.02f, 0.08f, 1.02f);
        band.GetComponent<Renderer>().sharedMaterial = bandMat;
        DestroyImmediate(band.GetComponent<Collider>()); // Sin colisión propia

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

        // Fondo crepuscular elegante
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

        Debug.Log("[SetupScene3DHelper] UI móvil generada con éxito.");
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
