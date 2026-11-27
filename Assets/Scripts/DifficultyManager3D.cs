using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor de Curva de Dificultad Progresiva en el Espacio Tridimensional.
/// Cumple con la instrucción 2 de la Actividad 4.2:
/// - Aumenta dinámicamente el desafío y tempo del juego en función del avance y puntuación.
/// - Modifica la velocidad del personaje principal y acelera obstáculos interactivos.
/// - Muestra la fase o nivel actual de dificultad en el HUD visual.
/// </summary>
public class DifficultyManager3D : MonoBehaviour
{
    public static DifficultyManager3D Instance { get; private set; }

    [System.Serializable]
    public struct DifficultyTier
    {
        public string tierName;
        public int scoreThreshold;
        public float speedMultiplier;
        public float obstacleSpeedMultiplier;
        public Color tierColor;
    }

    [Header("Configuración de Fases de Dificultad")]
    [Tooltip("Lista ordenada de fases de dificultad progresiva.")]
    [SerializeField] private List<DifficultyTier> difficultyTiers = new List<DifficultyTier>()
    {
        new DifficultyTier { tierName = "Nivel 1: Inicial", scoreThreshold = 0, speedMultiplier = 1.0f, obstacleSpeedMultiplier = 1.0f, tierColor = new Color(0.2f, 0.8f, 0.4f) },
        new DifficultyTier { tierName = "Nivel 2: Dinámico", scoreThreshold = 150, speedMultiplier = 1.25f, obstacleSpeedMultiplier = 1.3f, tierColor = new Color(0.1f, 0.7f, 0.9f) },
        new DifficultyTier { tierName = "Nivel 3: Avanzado", scoreThreshold = 350, speedMultiplier = 1.50f, obstacleSpeedMultiplier = 1.6f, tierColor = new Color(1.0f, 0.65f, 0.1f) },
        new DifficultyTier { tierName = "Nivel 4: Extremo", scoreThreshold = 650, speedMultiplier = 1.80f, obstacleSpeedMultiplier = 2.0f, tierColor = new Color(0.95f, 0.25f, 0.3f) }
    };

    [Header("Referencias")]
    [Tooltip("Controlador del jugador al cual se le ajustará la velocidad.")]
    [SerializeField] private PlayerController3D playerController;

    [Tooltip("Texto en la interfaz (UI Text) que muestra el nivel de dificultad actual.")]
    [SerializeField] private Text difficultyText;

    // Estado actual
    private int currentTierIndex = 0;
    private float currentSpeedMultiplier = 1.0f;
    private float currentObstacleSpeedMultiplier = 1.0f;

    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public float CurrentObstacleSpeedMultiplier => currentObstacleSpeedMultiplier;

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
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController3D>();
        }

        ApplyTier(0);
    }

    private void Update()
    {
        int currentScore = ScoreManager3D.Instance != null ? ScoreManager3D.Instance.CurrentScore : 0;

        // Evaluar si se debe ascender a la siguiente fase de dificultad
        for (int i = difficultyTiers.Count - 1; i >= 0; i--)
        {
            if (currentScore >= difficultyTiers[i].scoreThreshold)
            {
                if (currentTierIndex != i)
                {
                    ApplyTier(i);
                }
                break;
            }
        }
    }

    private void ApplyTier(int index)
    {
        currentTierIndex = index;
        DifficultyTier tier = difficultyTiers[index];

        currentSpeedMultiplier = tier.speedMultiplier;
        currentObstacleSpeedMultiplier = tier.obstacleSpeedMultiplier;

        // Aplicar la velocidad al controlador del jugador
        if (playerController != null)
        {
            playerController.SetSpeedMultiplier(currentSpeedMultiplier);
        }

        // Actualizar etiqueta visual en el HUD
        if (difficultyText != null)
        {
            difficultyText.text = $"Dificultad: {tier.tierName}";
            difficultyText.color = tier.tierColor;
        }

        Debug.Log($"[DifficultyManager3D] Subida de dificultad: {tier.tierName} | Multiplicador de velocidad: x{currentSpeedMultiplier}");
    }
}
