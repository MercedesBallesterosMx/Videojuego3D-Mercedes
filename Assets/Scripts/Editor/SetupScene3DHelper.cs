#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Asistente de Editor para Unity que automatiza el montaje completo de la escena 3D.
/// Permite generar de forma instantánea el entorno tridimensional, el personaje con físicas,
/// la cámara cinematográfica con restricciones y la interfaz adaptada a dispositivos móviles.
/// 
/// Accesible desde el menú superior de Unity: 'Herramientas 3D > Montar Escenario y Controles 3D (Actividad 4.1)'
/// </summary>
public class SetupScene3DHelper : EditorWindow
{
    [MenuItem("Herramientas 3D/Montar Escenario y Controles 3D (Actividad 4.1)")]
    public static void GenerateComplete3DScene()
    {
        Undo.SetCurrentGroupName("Montar Escena 3D Actividad 4.1");

        Debug.Log("--- Iniciando montaje de la escena 3D móvil (Actividad 4.1) ---");

        // 1. Configurar Iluminación Básica
        SetupLighting();

        // 2. Construir Escenario 3D (Suelo, Rampas en Y, Plataformas Elevadas y Obstáculos)
        GameObject groundParent = SetupEnvironment();

        // 3. Crear Personaje Principal con Físicas y Controlador 3D
        GameObject player = SetupPlayer();

        // 4. Configurar Cámara Cinemática con Restricciones (Criterios 3 y 4)
        SetupCamera(player.transform);

        // 5. Construir Interfaz de Usuario (UI) Adaptada a Móviles (Criterio 1)
        SetupMobileUI(player.GetComponent<PlayerController3D>());

        EditorUtility.DisplayDialog("Montaje 3D Completo", 
            "¡La escena 3D para móviles ha sido configurada con éxito!\n\n" +
            "✓ Criterio 1: UI Móvil con botones táctiles y menú de pausa.\n" +
            "✓ Criterio 2: Personaje 3D con físicas en 3 ejes (X, Y, Z).\n" +
            "✓ Criterio 3: Cámara con seguimiento cinemático en X y Z.\n" +
            "✓ Criterio 4: Cámara con posición Z constante y fija respecto al jugador.", 
            "Entendido");
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
        dirLight.color = new Color(1.0f, 0.96f, 0.90f);
        dirLight.intensity = 1.15f;
        dirLight.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(0.35f, 0.40f, 0.50f);
    }

    private static GameObject SetupEnvironment()
    {
        GameObject envParent = new GameObject("Environment_3D");

        // Suelo principal (Ejes X y Z)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_Main";
        floor.transform.SetParent(envParent.transform);
        floor.transform.position = new Vector3(0f, -0.5f, 25f);
        floor.transform.localScale = new Vector3(14f, 1f, 60f);

        // Rampa de prueba (Demuestra desplazamiento en Eje Y)
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp_Elevation_Y";
        ramp.transform.SetParent(envParent.transform);
        ramp.transform.position = new Vector3(0f, 1.2f, 15f);
        ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp.transform.localScale = new Vector3(6f, 0.8f, 10f);

        // Plataforma superior elevada (Eje Y)
        GameObject highPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highPlatform.name = "High_Platform_Y";
        highPlatform.transform.SetParent(envParent.transform);
        highPlatform.transform.position = new Vector3(0f, 2.75f, 28f);
        highPlatform.transform.localScale = new Vector3(8f, 1f, 14f);

        // Obstáculos laterales (Eje X)
        for (int i = 0; i < 4; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_Cube_{i + 1}";
            obstacle.transform.SetParent(envParent.transform);
            float xOffset = (i % 2 == 0) ? -3.5f : 3.5f;
            obstacle.transform.position = new Vector3(xOffset, 0.75f, 8f + (i * 9f));
            obstacle.transform.localScale = new Vector3(2f, 1.5f, 2f);
        }

        return envParent;
    }

    private static GameObject SetupPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player_Ball3D";
            player.tag = "Player";
        }

        player.transform.position = new Vector3(0f, 1.0f, 0f);
        player.transform.localScale = Vector3.one * 1.2f;

        // Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.AddComponent<Rigidbody>();
        rb.mass = 1.2f;
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

        CameraFollow3D followScript = mainCam.GetComponent<CameraFollow3D>();
        if (followScript == null) followScript = mainCam.gameObject.AddComponent<CameraFollow3D>();

        SerializedObject so = new SerializedObject(followScript);
        so.FindProperty("target").objectReferenceValue = playerTransform;
        so.FindProperty("offset").vector3Value = new Vector3(0f, 5.5f, -9.0f);
        so.FindProperty("smoothTime").floatValue = 0.22f;
        so.FindProperty("lockFixedZDistance").boolValue = true;
        so.FindProperty("followYAxis").boolValue = false;
        so.FindProperty("lookAtTarget").boolValue = true;
        so.ApplyModifiedProperties();

        mainCam.transform.position = playerTransform.position + new Vector3(0f, 5.5f, -9.0f);
        mainCam.transform.LookAt(playerTransform.position + Vector3.up);
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
            scaler.referenceResolution = new Vector2(1080, 1920);
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
        if (controlsPanel == null)
        {
            GameObject cPanelObj = new GameObject("MobileControlsPanel", typeof(RectTransform));
            cPanelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = cPanelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            controlsPanel = cPanelObj.transform;

            // Botones D-Pad Móviles (Izquierda, Derecha, Adelante, Atrás)
            CreateTouchButton(controlsPanel, "Btn_Forward", "▲", new Vector2(200, 320), VirtualTouchButton.ButtonDirection.Forward, controller);
            CreateTouchButton(controlsPanel, "Btn_Backward", "▼", new Vector2(200, 120), VirtualTouchButton.ButtonDirection.Backward, controller);
            CreateTouchButton(controlsPanel, "Btn_Left", "◀", new Vector2(100, 220), VirtualTouchButton.ButtonDirection.Left, controller);
            CreateTouchButton(controlsPanel, "Btn_Right", "▶", new Vector2(300, 220), VirtualTouchButton.ButtonDirection.Right, controller);

            // Botón de Salto Móvil (Derecha de la pantalla)
            CreateTouchButton(controlsPanel, "Btn_Jump", "SALTO", new Vector2(880, 220), VirtualTouchButton.ButtonDirection.Jump, controller, new Vector2(180, 180));
        }

        Debug.Log("[SetupScene3DHelper] Interfaz móvil y controles táctiles generados con éxito.");
    }

    private static void CreateTouchButton(Transform parent, string name, string label, Vector2 anchoredPos, 
        VirtualTouchButton.ButtonDirection dir, PlayerController3D controller, Vector2? size = null)
    {
        Vector2 buttonSize = size ?? new Vector2(120, 120);

        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualTouchButton));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.sizeDelta = buttonSize;
        rt.anchoredPosition = anchoredPos;

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.40f, 0.70f, 0.65f);

        VirtualTouchButton vtb = btnObj.GetComponent<VirtualTouchButton>();
        SerializedObject so = new SerializedObject(vtb);
        so.FindProperty("direction").enumValueIndex = (int)dir;
        so.FindProperty("playerController").objectReferenceValue = controller;
        so.ApplyModifiedProperties();

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
        txt.fontSize = (dir == VirtualTouchButton.ButtonDirection.Jump) ? 32 : 48;
        txt.color = Color.white;
    }
}
#endif
