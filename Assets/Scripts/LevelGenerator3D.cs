using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador procedimental infinito y modular de niveles tridimensionales.
/// Cumple con la Actividad 4.3:
/// - Generación continua e infinita de secciones en tiempo de ejecución.
/// - Cambia dinámicamente la geometría (ancho de pista, abismos para saltar) y materiales
///   en función del nivel actual reportado por DifficultyManager3D.
/// - Reciclaje de memoria automático para mantener 60 FPS estables en móviles.
/// </summary>
public class LevelGenerator3D : MonoBehaviour
{
    public static LevelGenerator3D Instance { get; private set; }

    [Header("Referencias del Jugador")]
    [Tooltip("Transform del personaje principal para evaluar la distancia de generación.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuración del Bucle Infinito")]
    [Tooltip("Cantidad de módulos activos precargados al iniciar la escena.")]
    [Range(3, 12)]
    [SerializeField] private int initialSectionsCount = 6;

    [Tooltip("Longitud predeterminada de cada sección en el eje Z (en unidades).")]
    [SerializeField] private float defaultSectionLength = 28f;

    [Tooltip("Distancia de visión por delante del jugador antes de generar el siguiente módulo.")]
    [SerializeField] private float spawnDistanceAhead = 85f;

    [Tooltip("Distancia que debe quedar atrás del jugador una sección para ser eliminada de memoria.")]
    [SerializeField] private float despawnDistanceBehind = 45f;

    [Header("Contenedor Jerárquico")]
    [Tooltip("Transform padre para organizar los módulos instanciados en la jerarquía.")]
    [SerializeField] private Transform trackParent;

    // Estado interno del generador
    private Vector3 nextSpawnPoint = Vector3.zero;
    private readonly Queue<GameObject> activeSections = new Queue<GameObject>();
    private int generatedSectionsCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (trackParent == null)
        {
            GameObject parentObj = new GameObject("ProceduralTrack_Container");
            trackParent = parentObj.transform;
        }

        nextSpawnPoint = new Vector3(0f, 0f, 0f);

        // Generar secciones iniciales
        InitializeStartingTrack();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Generar nuevas secciones si el jugador se aproxima al final de la pista visible
        if (nextSpawnPoint.z - playerTransform.position.z < spawnDistanceAhead)
        {
            SpawnNextSection();
        }

        // 2. Destruir módulos viejos que quedaron rezagados detrás del jugador (Ahorro de RAM)
        RecyclePassedSections();
    }

    private void InitializeStartingTrack()
    {
        // Primer tramo seguro de arranque (Nivel 1)
        SpawnProceduralSection(false, 1);

        // Generar el resto de secciones iniciales
        for (int i = 1; i < initialSectionsCount; i++)
        {
            SpawnNextSection();
        }
    }

    public void SpawnNextSection()
    {
        int currentLevel = DifficultyManager3D.Instance != null ? DifficultyManager3D.Instance.CurrentLevelNumber : 1;
        SpawnProceduralSection(true, currentLevel);
    }

    /// <summary>
    /// Construye procedimentalmente una sección de pista adaptada a las características del nivel actual.
    /// </summary>
    private void SpawnProceduralSection(bool withObstacles, int level)
    {
        generatedSectionsCount++;
        GameObject sectionObj = new GameObject($"Section_{generatedSectionsCount}_Lvl{level}");
        sectionObj.transform.position = nextSpawnPoint;
        sectionObj.transform.SetParent(trackParent);

        // Obtener la configuración temática del nivel actual
        DifficultyManager3D.DifficultyTier tier = DifficultyManager3D.Instance != null ? 
            DifficultyManager3D.Instance.CurrentTier : default;

        float trackWidth = tier.trackWidth > 0 ? tier.trackWidth : 14f;
        Color floorColor = tier.floorColor.a > 0 ? tier.floorColor : new Color(0.12f, 0.16f, 0.22f);
        Color obsColor = tier.obstacleColor.a > 0 ? tier.obstacleColor : new Color(0.92f, 0.20f, 0.25f);

        // Crear material temporal o compartido para el suelo del nivel
        Material floorMat = CreateDynamicMaterial($"Mat_Floor_Lvl{level}", floorColor);
        Material obsMat = CreateDynamicMaterial($"Mat_Obs_Lvl{level}", obsColor);

        // --- TIPO DE GEOMETRÍA SEGÚN EL NIVEL ---
        // En niveles avanzados (3 y 4) se intercalan tramos con abismos para saltar o rampas
        bool isJumpSection = (level >= 3) && (generatedSectionsCount % 3 == 0);

        if (isJumpSection)
        {
            // Tramo con rampa y salto al vacío (Exige usar el botón de SALTO)
            BuildJumpSection(sectionObj.transform, trackWidth, floorMat, obsMat);
        }
        else
        {
            // Tramo continuo estándar con el ancho y color del nivel
            BuildStandardSection(sectionObj.transform, trackWidth, floorMat, withObstacles, obsMat, level);
        }

        activeSections.Enqueue(sectionObj);
        nextSpawnPoint += new Vector3(0f, 0f, defaultSectionLength);
    }

    private void BuildStandardSection(Transform parent, float width, Material floorMat, bool withObstacles, Material obsMat, int level)
    {
        // 1. Suelo del tramo con el ancho del nivel
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(parent);
        floor.transform.localPosition = new Vector3(0f, -0.5f, defaultSectionLength * 0.5f);
        floor.transform.localScale = new Vector3(width, 1f, defaultSectionLength);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // 2. Obstáculos dinámicos acordes al nivel
        if (withObstacles)
        {
            int obstacleCount = level >= 3 ? 2 : 1;
            for (int i = 0; i < obstacleCount; i++)
            {
                GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.name = $"DynamicHazard_Lvl{level}_{i + 1}";
                obs.transform.SetParent(parent);

                float zOffset = defaultSectionLength * (0.35f + (i * 0.35f));
                obs.transform.localPosition = new Vector3(0f, 0.75f, zOffset);
                obs.transform.localScale = new Vector3(2.4f, 1.5f, 1.5f);
                obs.GetComponent<Renderer>().sharedMaterial = obsMat;

                // Dinámica tridimensional: oscilación horizontal acelerada
                MovingObstacle3D mover = obs.AddComponent<MovingObstacle3D>();
                HazardPart3D hazard = obs.AddComponent<HazardPart3D>();
            }
        }
    }

    private void BuildJumpSection(Transform parent, float width, Material floorMat, Material obsMat)
    {
        // Primer tramo antes del salto
        float halfLength = defaultSectionLength * 0.35f;
        GameObject floor1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor1.name = "Floor_BeforeGap";
        floor1.transform.SetParent(parent);
        floor1.transform.localPosition = new Vector3(0f, -0.5f, halfLength * 0.5f);
        floor1.transform.localScale = new Vector3(width, 1f, halfLength);
        floor1.GetComponent<Renderer>().sharedMaterial = floorMat;

        // Rampa de despegue
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp_Jump";
        ramp.transform.SetParent(parent);
        ramp.transform.localPosition = new Vector3(0f, 0.6f, halfLength + 3f);
        ramp.transform.rotation = Quaternion.Euler(-18f, 0f, 0f);
        ramp.transform.localScale = new Vector3(width * 0.7f, 0.8f, 7f);
        ramp.GetComponent<Renderer>().sharedMaterial = floorMat;

        // Segundo tramo de aterrizaje después del abismo (Requiere haber saltado)
        float landingZ = defaultSectionLength * 0.75f;
        float landingLength = defaultSectionLength * 0.25f;
        GameObject floor2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor2.name = "Floor_AfterLanding";
        floor2.transform.SetParent(parent);
        floor2.transform.localPosition = new Vector3(0f, 1.25f, landingZ);
        floor2.transform.localScale = new Vector3(width, 1f, landingLength);
        floor2.GetComponent<Renderer>().sharedMaterial = floorMat;
    }

    private Material CreateDynamicMaterial(string name, Color col)
    {
        return MaterialHelper3D.CreateSafeMaterial(name, col, 0.35f);
    }

    private void RecyclePassedSections()
    {
        while (activeSections.Count > 0)
        {
            GameObject oldestSection = activeSections.Peek();
            if (oldestSection == null)
            {
                activeSections.Dequeue();
                continue;
            }

            if (playerTransform.position.z - oldestSection.transform.position.z > despawnDistanceBehind)
            {
                activeSections.Dequeue();
                Destroy(oldestSection);
            }
            else
            {
                break;
            }
        }
    }

    public void ResetTrack()
    {
        while (activeSections.Count > 0)
        {
            GameObject sec = activeSections.Dequeue();
            if (sec != null) Destroy(sec);
        }

        nextSpawnPoint = Vector3.zero;
        generatedSectionsCount = 0;
        InitializeStartingTrack();
    }
}
