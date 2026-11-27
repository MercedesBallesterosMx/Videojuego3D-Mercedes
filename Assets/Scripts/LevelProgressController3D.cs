using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de Progreso de Nivel, Barra de Navegación y Finalización de Nivel.
/// Cumple directamente con los Criterios 1 y 2 de la Actividad 4.5 en Moodle:
/// - Rastrea la distancia del jugador hacia la meta y actualiza un Slider horizontal de progreso (0% a 100%).
/// - Despliega el modal de '¡Nivel Completado!' al alcanzar la meta.
/// - Permite avanzar de forma interactiva y sin errores al Nivel 2 o retornar al Menú Principal.
/// </summary>
public class LevelProgressController3D : MonoBehaviour
{
    public static LevelProgressController3D Instance { get; private set; }

    [Header("Identificación del Nivel")]
    [SerializeField] private int currentLevelNumber = 1;
    [SerializeField] private bool isFinalLevel = false;

    [Header("Puntos de Control de Distancia")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float startZ = 2.0f;
    [SerializeField] private float goalZ = 160.0f;

    [Header("Elementos de HUD de Progreso")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text progressPercentageText;
    [SerializeField] private Text levelLabelText;

    [Header("Modal de Nivel Completado (Victoria)")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private Text levelCompleteTitleText;
    [SerializeField] private Text levelCompleteStatsText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Feedback de Súper Velocidad (UX / Actividad 4.6)")]
    [SerializeField] private GameObject speedBoostBanner;
    [SerializeField] private Text speedBoostText;
    private Coroutine boostBannerCoroutine;

    private bool levelCompleted = false;

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
        Time.timeScale = 1.0f;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
        }

        if (levelLabelText != null)
        {
            levelLabelText.text = $"NIVEL {currentLevelNumber}";
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        // Vincular botones del modal
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.LoadNextLevel();
                }
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.LoadMainMenu();
                }
            });
        }
    }

    private void Update()
    {
        if (levelCompleted || playerTransform == null) return;

        // Calcular porcentaje de avance hacia la meta
        float currentZ = playerTransform.position.z;
        float totalDist = Mathf.Max(1f, goalZ - startZ);
        float progress = Mathf.Clamp01((currentZ - startZ) / totalDist);

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressPercentageText != null)
        {
            progressPercentageText.text = $"{Mathf.FloorToInt(progress * 100f)}%";
        }

        // Detección de respaldo si el jugador sobrepasa la coordenada Z de la meta
        if (currentZ >= goalZ && !levelCompleted)
        {
            OnGoalReached();
        }
    }

    /// <summary>
    /// Se invoca cuando el jugador cruza el GoalPortal3D o rebasa la meta del circuito.
    /// </summary>
    public void OnGoalReached()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        Debug.Log($"[LevelProgressController3D] ¡Nivel {currentLevelNumber} Completado Exitosamente!");

        // Pausa suave para festejo
        Time.timeScale = 0.25f;

        // Desactivar controles físicos del jugador
        if (playerTransform != null)
        {
            PlayerController3D controller = playerTransform.GetComponent<PlayerController3D>();
            if (controller != null) controller.enabled = false;
        }

        // Configurar y mostrar el modal de victoria
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);

            int score = ScoreManager3D.Instance != null ? ScoreManager3D.Instance.CurrentScore : 0;

            if (levelCompleteTitleText != null)
            {
                levelCompleteTitleText.text = isFinalLevel ? "★ ¡VICTORIA TOTAL! ★\n¡Juego Completado!" : $"★ ¡NIVEL {currentLevelNumber} COMPLETADO! ★";
            }

            if (levelCompleteStatsText != null)
            {
                levelCompleteStatsText.text = $"Puntuación alcanzada: {score:N0} pts\n¡Meta alcanzada con éxito!";
            }

            if (nextLevelButton != null)
            {
                Text btnText = nextLevelButton.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = isFinalLevel ? "VOLVER AL MENÚ" : "SIGUIENTE NIVEL ➔";
                }
            }
        }
    }

    #region Feedback de Súper Velocidad (UX - Actividad 4.6)
    /// <summary>
    /// Despliega una notificación visual de Súper Velocidad en la interfaz móvil durante la duración del boost (máx 3s).
    /// </summary>
    public void ShowSpeedBoostBanner(float duration, float multiplier)
    {
        if (speedBoostBanner == null && speedBoostText == null) return;

        if (boostBannerCoroutine != null)
        {
            StopCoroutine(boostBannerCoroutine);
        }
        boostBannerCoroutine = StartCoroutine(SpeedBoostBannerRoutine(duration, multiplier));
    }

    private System.Collections.IEnumerator SpeedBoostBannerRoutine(float duration, float multiplier)
    {
        if (speedBoostBanner != null) speedBoostBanner.SetActive(true);
        if (speedBoostText != null) speedBoostText.text = $"★ ¡SÚPER VELOCIDAD x{multiplier:F1}! ★";

        yield return new WaitForSeconds(duration);

        if (speedBoostBanner != null) speedBoostBanner.SetActive(false);
        boostBannerCoroutine = null;
    }
    #endregion
}
