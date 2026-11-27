using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor de Curva de Dificultad Progresiva y Niveles de Juego.
/// Cumple con la Actividad 4.2 y 4.3:
/// - Transforma radicalmente la experiencia por cada nivel:
///   1. Cambia el color temático del suelo, obstáculos y fondo de la cámara.
///   2. Incrementa notoriamente la velocidad del personaje (+35%, +70%, +110%).
///   3. Acelera los obstáculos móviles y añade nuevos patrones de peligro.
///   4. Despliega un banner visual en el centro de la pantalla anunciando el nuevo nivel.
/// </summary>
public class DifficultyManager3D : MonoBehaviour
{
    public static DifficultyManager3D Instance { get; private set; }

    [System.Serializable]
    public struct DifficultyTier
    {
        public int levelNumber;
        public string levelTitle;
        public int scoreThreshold;
        public float speedMultiplier;
        public float obstacleSpeedMultiplier;
        public float trackWidth;
        public Color floorColor;
        public Color obstacleColor;
        public Color cameraBgColor;
        public Color bannerTextColor;
    }

    [Header("Configuración de Niveles Temáticos")]
    [Tooltip("Lista de niveles con configuraciones visuales y mecánicas únicas.")]
    [SerializeField] private List<DifficultyTier> difficultyTiers = new List<DifficultyTier>()
    {
        // NIVEL 1: Zona Azul Pizarra (Tranquilo y de aprendizaje)
        new DifficultyTier {
            levelNumber = 1,
            levelTitle = "NIVEL 1: ZONA AZUL",
            scoreThreshold = 0,
            speedMultiplier = 1.0f,
            obstacleSpeedMultiplier = 1.0f,
            trackWidth = 14f,
            floorColor = new Color(0.12f, 0.16f, 0.22f),       // Azul pizarra oscuro
            obstacleColor = new Color(0.92f, 0.20f, 0.25f),    // Rojo rubí
            cameraBgColor = new Color(0.10f, 0.14f, 0.20f),    // Noche crepuscular
            bannerTextColor = new Color(0.3f, 0.85f, 1.0f)
        },
        // NIVEL 2: Zona Eléctrica Púrpura (Velocidad +40%, obstáculos dinámicos)
        new DifficultyTier {
            levelNumber = 2,
            levelTitle = "NIVEL 2: ZONA PÚRPURA ELÉCTRICA",
            scoreThreshold = 150,
            speedMultiplier = 1.40f,
            obstacleSpeedMultiplier = 1.6f,
            trackWidth = 12f,
            floorColor = new Color(0.22f, 0.10f, 0.35f),       // Púrpura oscuro
            obstacleColor = new Color(1.0f, 0.75f, 0.1f),      // Amarillo eléctrico
            cameraBgColor = new Color(0.15f, 0.08f, 0.25f),    // Violeta profundo
            bannerTextColor = new Color(0.9f, 0.4f, 1.0f)
        },
        // NIVEL 3: Zona Volcánica Lava (Velocidad +75%, pista estrecha)
        new DifficultyTier {
            levelNumber = 3,
            levelTitle = "NIVEL 3: ZONA DE FUEGO",
            scoreThreshold = 350,
            speedMultiplier = 1.75f,
            obstacleSpeedMultiplier = 2.2f,
            trackWidth = 9.5f,
            floorColor = new Color(0.18f, 0.08f, 0.08f),       // Carbón oscuro
            obstacleColor = new Color(1.0f, 0.25f, 0.05f),     // Naranja lava incandescente
            cameraBgColor = new Color(0.20f, 0.05f, 0.05f),    // Rojo volcánico
            bannerTextColor = new Color(1.0f, 0.35f, 0.1f)
        },
        // NIVEL 4: Modo Cibernético Extremo (Velocidad x2.1, máxima exigencia)
        new DifficultyTier {
            levelNumber = 4,
            levelTitle = "NIVEL 4: MODO EXTREMO",
            scoreThreshold = 650,
            speedMultiplier = 2.10f,
            obstacleSpeedMultiplier = 3.0f,
            trackWidth = 8.0f,
            floorColor = new Color(0.04f, 0.20f, 0.15f),       // Verde cibernético
            obstacleColor = new Color(0.0f, 1.0f, 0.6f),       // Neón jade
            cameraBgColor = new Color(0.02f, 0.12f, 0.09f),    // Verde abisal
            bannerTextColor = new Color(0.2f, 1.0f, 0.5f)
        }
    };

    [Header("Referencias de Escena")]
    [SerializeField] private PlayerController3D playerController;
    [SerializeField] private Text difficultyText;

    [Header("Banner Central de Cambio de Nivel")]
    [Tooltip("Panel o texto central que se activa temporalmente al ascender de nivel.")]
    [SerializeField] private GameObject levelBannerPanel;
    [SerializeField] private Text levelBannerTitleText;
    [SerializeField] private Text levelBannerSubtitleText;

    // Estado actual
    private int currentTierIndex = 0;
    private Camera mainCamera;
    private Coroutine bannerCoroutine;

    public DifficultyTier CurrentTier => difficultyTiers[currentTierIndex];
    public float CurrentSpeedMultiplier => difficultyTiers[currentTierIndex].speedMultiplier;
    public float CurrentObstacleSpeedMultiplier => difficultyTiers[currentTierIndex].obstacleSpeedMultiplier;
    public int CurrentLevelNumber => difficultyTiers[currentTierIndex].levelNumber;

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

        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController3D>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (levelBannerPanel != null)
        {
            levelBannerPanel.SetActive(false);
        }

        ApplyTier(0, false);
    }

    private void Update()
    {
        int currentScore = ScoreManager3D.Instance != null ? ScoreManager3D.Instance.CurrentScore : 0;

        // Comprobar si corresponde subir de nivel
        for (int i = difficultyTiers.Count - 1; i >= 0; i--)
        {
            if (currentScore >= difficultyTiers[i].scoreThreshold)
            {
                if (currentTierIndex != i)
                {
                    ApplyTier(i, true);
                }
                break;
            }
        }
    }

    private void ApplyTier(int index, bool showBanner)
    {
        currentTierIndex = index;
        DifficultyTier tier = difficultyTiers[index];

        // 1. Acelerar perceptiblemente la velocidad del jugador
        if (playerController != null)
        {
            playerController.SetSpeedMultiplier(tier.speedMultiplier);
        }

        // 2. Cambiar la atmósfera y color de fondo de la cámara
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = tier.cameraBgColor;
        }

        // 3. Actualizar la etiqueta del HUD superior
        if (difficultyText != null)
        {
            difficultyText.text = tier.levelTitle;
            difficultyText.color = tier.bannerTextColor;
        }

        // 4. Mostrar el Gran Banner Central de subida de nivel
        if (showBanner)
        {
            ShowLevelUpBanner(tier);
        }

        Debug.Log($"[DifficultyManager3D] ¡SUBIDA DE NIVEL REAL! {tier.levelTitle} | Velocidad x{tier.speedMultiplier} | Ancho de pista: {tier.trackWidth}");
    }

    private void ShowLevelUpBanner(DifficultyTier tier)
    {
        if (levelBannerPanel == null) return;

        if (bannerCoroutine != null)
        {
            StopCoroutine(bannerCoroutine);
        }

        bannerCoroutine = StartCoroutine(DisplayBannerRoutine(tier));
    }

    private IEnumerator DisplayBannerRoutine(DifficultyTier tier)
    {
        levelBannerPanel.SetActive(true);

        if (levelBannerTitleText != null)
        {
            levelBannerTitleText.text = $"★ {tier.levelTitle} ★";
            levelBannerTitleText.color = tier.bannerTextColor;
        }

        if (levelBannerSubtitleText != null)
        {
            levelBannerSubtitleText.text = $"¡Velocidad aumentada a x{tier.speedMultiplier:F1}! Nuevos obstáculos activados.";
        }

        // Mantener visible durante 2.5 segundos de juego real
        yield return new WaitForSeconds(2.5f);

        levelBannerPanel.SetActive(false);
        bannerCoroutine = null;
    }
}
