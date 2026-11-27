using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestor de Puntuación (Score) y Persistencia de Récord Máximo (HighScore).
/// Cumple con los requerimientos de la Actividad 4.2:
/// - Contador visual en UI (UI Text) que incrementa uniformemente con el avance tridimensional.
/// - Sistema de guardado persistente entre sesiones mediante PlayerPrefs.
/// </summary>
public class ScoreManager3D : MonoBehaviour
{
    public static ScoreManager3D Instance { get; private set; }

    [Header("Referencias del Jugador")]
    [Tooltip("Transform del personaje principal para calcular la distancia de avance en el eje Z.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuración de Puntuación")]
    [Tooltip("Multiplicador de puntos por cada unidad recorrida en el eje Z.")]
    [SerializeField] private float pointsPerUnitZ = 10f;

    [Header("Elementos Visuales de Interfaz (UI)")]
    [Tooltip("Componente Text que muestra la puntuación actual en tiempo real.")]
    [SerializeField] private Text currentScoreText;

    [Tooltip("Componente Text que muestra el récord histórico guardado.")]
    [SerializeField] private Text highScoreText;

    [Tooltip("Prefijo para el texto de puntuación actual.")]
    [SerializeField] private string scorePrefix = "Puntos: ";

    [Tooltip("Prefijo para el texto de récord.")]
    [SerializeField] private string highScorePrefix = "Récord: ";

    // Clave para guardar en PlayerPrefs
    private const string HIGH_SCORE_KEY = "HighScore_3D_Mercedes";

    // Variables internas
    private float startZ = 0f;
    private int currentScore = 0;
    private int highScore = 0;
    private int bonusPoints = 0;
    private float maxZReached = 0f;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;

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

        // Cargar el récord guardado en almacenamiento persistente local
        LoadHighScore();
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

        if (playerTransform != null)
        {
            startZ = playerTransform.position.z;
            maxZReached = startZ;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Si el jugador avanzó más hacia el frente en el eje Z, incrementar la distancia máxima
        if (playerTransform.position.z > maxZReached)
        {
            maxZReached = playerTransform.position.z;
        }

        // Cálculo del puntaje uniforme por desplazamiento en el eje Z + bonus de interacción
        float distanceTraveled = Mathf.Max(0f, maxZReached - startZ);
        currentScore = Mathf.FloorToInt(distanceTraveled * pointsPerUnitZ) + bonusPoints;

        // Verificar si se supera el HighScore en tiempo real
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }

        UpdateUI();
    }

    /// <summary>
    /// Agrega puntos de bonus por interacción con coleccionables o hitos en el recorrido.
    /// </summary>
    public void AddBonusScore(int amount)
    {
        bonusPoints += amount;
        currentScore = Mathf.FloorToInt(Mathf.Max(0f, maxZReached - startZ) * pointsPerUnitZ) + bonusPoints;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }

        UpdateUI();
        Debug.Log($"[ScoreManager3D] +{amount} puntos de bonus agregados. Total: {currentScore}");
    }

    /// <summary>
    /// Actualiza los textos de la interfaz gráfica con el formato numérico adecuado.
    /// </summary>
    private void UpdateUI()
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = $"{scorePrefix}{currentScore:N0}";
        }

        if (highScoreText != null)
        {
            highScoreText.text = $"{highScorePrefix}{highScore:N0}";
        }
    }

    #region Persistencia con PlayerPrefs
    /// <summary>
    /// Guarda el nuevo récord en el almacenamiento persistente del dispositivo.
    /// </summary>
    private void SaveHighScore()
    {
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Carga el récord guardado en sesiones anteriores.
    /// </summary>
    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    /// <summary>
    /// Permite reiniciar el récord guardado para pruebas y auditoría de calidad.
    /// </summary>
    public void ResetHighScore()
    {
        highScore = 0;
        PlayerPrefs.DeleteKey(HIGH_SCORE_KEY);
        PlayerPrefs.Save();
        UpdateUI();
        Debug.Log("[ScoreManager3D] Récord histórico reiniciado a 0.");
    }
    #endregion
}
