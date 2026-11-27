using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador procedimental infinito y modular de niveles tridimensionales.
/// Cumple con los Criterios 1 y 2 de la Actividad 4.3:
/// - Generación continua e infinita de secciones mediante código en tiempo de ejecución.
/// - Parámetros completamente configurables desde el Inspector de Unity (modularidad).
/// - Optimización para móviles mediante descarte y destrucción de secciones rebasadas.
/// </summary>
public class LevelGenerator3D : MonoBehaviour
{
    public static LevelGenerator3D Instance { get; private set; }

    [Header("Referencias del Jugador")]
    [Tooltip("Transform del personaje principal para evaluar la distancia de generación.")]
    [SerializeField] private Transform playerTransform;

    [Header("Prefabs de Módulos (Configurables en Inspector)")]
    [Tooltip("Catálogo de módulos tridimensionales que se pueden generar aleatoriamente.")]
    [SerializeField] private List<GameObject> sectionPrefabs = new List<GameObject>();

    [Tooltip("Módulo inicial seguro sin obstáculos para el arranque de la partida.")]
    [SerializeField] private GameObject starterSectionPrefab;

    [Header("Configuración del Bucle Infinito")]
    [Tooltip("Cantidad de módulos activos precargados al iniciar la escena.")]
    [Range(3, 12)]
    [SerializeField] private int initialSectionsCount = 6;

    [Tooltip("Longitud predeterminada de cada sección en el eje Z (en unidades).")]
    [SerializeField] private float defaultSectionLength = 25f;

    [Tooltip("Distancia de visión por delante del jugador antes de generar el siguiente módulo.")]
    [SerializeField] private float spawnDistanceAhead = 75f;

    [Tooltip("Distancia que debe quedar atrás del jugador una sección para ser eliminada de memoria.")]
    [SerializeField] private float despawnDistanceBehind = 40f;

    [Header("Contenedor Jerárquico")]
    [Tooltip("Transform padre para organizar los módulos instanciados en la jerarquía.")]
    [SerializeField] private Transform trackParent;

    // Estado interno del generador
    private Vector3 nextSpawnPoint = Vector3.zero;
    private readonly Queue<GameObject> activeSections = new Queue<GameObject>();

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

        // Posición de inicio en Z
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

        // 2. Destruir módulos viejos que quedaron rezagados detrás del jugador (Ahorro de memoria en móviles)
        RecyclePassedSections();
    }

    /// <summary>
    /// Construye los primeros módulos al comenzar la partida.
    /// </summary>
    private void InitializeStartingTrack()
    {
        // Primer tramo seguro de arranque
        if (starterSectionPrefab != null)
        {
            SpawnSectionFromPrefab(starterSectionPrefab);
        }
        else if (sectionPrefabs.Count > 0)
        {
            SpawnSectionFromPrefab(sectionPrefabs[0]);
        }
        else
        {
            // Generador procedural procedural si no hay prefabs asignados
            SpawnProceduralFallbackSection(false);
        }

        // Generar el resto de secciones iniciales
        for (int i = 1; i < initialSectionsCount; i++)
        {
            SpawnNextSection();
        }
    }

    /// <summary>
    /// Selecciona e instancia la siguiente sección modular en función de la dificultad.
    /// </summary>
    public void SpawnNextSection()
    {
        if (sectionPrefabs != null && sectionPrefabs.Count > 0)
        {
            // Filtrar módulos compatibles con la dificultad actual
            int currentTier = DifficultyManager3D.Instance != null ? 
                Mathf.CeilToInt(DifficultyManager3D.Instance.CurrentSpeedMultiplier * 1.5f) : 1;

            GameObject selectedPrefab = SelectPrefabForDifficulty(currentTier);
            SpawnSectionFromPrefab(selectedPrefab);
        }
        else
        {
            // Fallback procedimental con obstáculos geométricos
            SpawnProceduralFallbackSection(true);
        }
    }

    private GameObject SelectPrefabForDifficulty(int tier)
    {
        List<GameObject> candidates = new List<GameObject>();

        foreach (GameObject prefab in sectionPrefabs)
        {
            if (prefab == null) continue;
            TrackSection3D sec = prefab.GetComponent<TrackSection3D>();
            if (sec == null || sec.RequiredDifficultyTier <= tier)
            {
                candidates.Add(prefab);
            }
        }

        if (candidates.Count == 0) candidates = sectionPrefabs;
        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

    private void SpawnSectionFromPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        GameObject newSection = Instantiate(prefab, nextSpawnPoint, Quaternion.identity, trackParent);
        activeSections.Enqueue(newSection);

        TrackSection3D trackComponent = newSection.GetComponent<TrackSection3D>();
        if (trackComponent != null)
        {
            nextSpawnPoint = trackComponent.GetConnectionPosition();
        }
        else
        {
            nextSpawnPoint += new Vector3(0f, 0f, defaultSectionLength);
        }
    }

    /// <summary>
    /// Crea dinámicamente un tramo de suelo con rampa u obstáculos si no existen prefabs en la escena.
    /// </summary>
    private void SpawnProceduralFallbackSection(bool withObstacles)
    {
        GameObject sectionObj = new GameObject($"ProceduralSection_{activeSections.Count + 1}");
        sectionObj.transform.position = nextSpawnPoint;
        sectionObj.transform.SetParent(trackParent);

        // Suelo principal del tramo
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(sectionObj.transform);
        floor.transform.localPosition = new Vector3(0f, -0.5f, defaultSectionLength * 0.5f);
        floor.transform.localScale = new Vector3(14f, 1f, defaultSectionLength);

        // Material del suelo si existe
        Material floorMat = Resources.Load<Material>("Mat_Floor_DarkSlate") 
                         ?? floor.GetComponent<Renderer>().sharedMaterial;
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // Obstáculo aleatorio si corresponde
        if (withObstacles && Random.value > 0.35f)
        {
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.name = "Obstacle_Procedural";
            obs.transform.SetParent(sectionObj.transform);
            float randomX = Random.Range(-3.5f, 3.5f);
            obs.transform.localPosition = new Vector3(randomX, 0.75f, defaultSectionLength * 0.5f);
            obs.transform.localScale = new Vector3(2.5f, 1.5f, 1.5f);

            // Añadir oscilación y peligro letal
            obs.AddComponent<MovingObstacle3D>();
            obs.AddComponent<HazardPart3D>();
        }

        activeSections.Enqueue(sectionObj);
        nextSpawnPoint += new Vector3(0f, 0f, defaultSectionLength);
    }

    /// <summary>
    /// Destruye secciones que ya quedaron muy por detrás del jugador.
    /// </summary>
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

            // Si la sección más antigua quedó detrás del margen de despawn
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

    /// <summary>
    /// Reinicia la pista para un nuevo intento del bucle de juego.
    /// </summary>
    public void ResetTrack()
    {
        while (activeSections.Count > 0)
        {
            GameObject sec = activeSections.Dequeue();
            if (sec != null) Destroy(sec);
        }

        nextSpawnPoint = Vector3.zero;
        InitializeStartingTrack();
    }
}
