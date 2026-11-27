using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Administrador del Bucle de Juego (Game Loop) y Estados de Partida.
/// Cumple con la instrucción 2 de la Actividad 4.3:
/// - Controla el flujo de juego (Playing, GameOver).
/// - Detecta caídas al vacío o impactos letales.
/// - Despliega el panel modal de Game Over con estadísticas de Score y HighScore.
/// - Permite reiniciar inmediatamente el flujo de juego con un botón táctil.
/// </summary>
public class GameLoopManager3D : MonoBehaviour
{
    public static GameLoopManager3D Instance { get; private set; }

    public enum GameState
    {
        Playing,
        GameOver
    }

    [Header("Referencias del Jugador y Caída")]
    [Tooltip("Transform del personaje principal para vigilar caídas al vacío.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Límite inferior en el eje Y que determina que el personaje cayó al vacío.")]
    [SerializeField] private float fallThresholdY = -4.0f;

    [Header("Panel Modal de Game Over (UI)")]
    [Tooltip("Ventana modal que se activa al perder la partida.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Texto para mostrar la puntuación alcanzada.")]
    [SerializeField] private Text finalScoreText;

    [Tooltip("Texto para mostrar el récord histórico guardado.")]
    [SerializeField] private Text finalHighScoreText;

    [Tooltip("Botón para reiniciar la partida de inmediato.")]
    [SerializeField] private Button retryButton;

    [Tooltip("Botón para volver al menú principal.")]
    [SerializeField] private Button menuButton;

    // Estado actual
    private GameState currentState = GameState.Playing;
    public GameState CurrentState => currentState;

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

        // Asegurar tiempo normal al inicio
        Time.timeScale = 1.0f;
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

        // Ocultar modal de Game Over al inicio
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Vincular eventos de botones
        if (retryButton != null) retryButton.onClick.AddListener(RestartGame);
        if (menuButton != null) menuButton.onClick.AddListener(LoadMainMenu);
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        // Detectar si el jugador cayó de la pista hacia el vacío (Eje Y < fallThresholdY)
        if (playerTransform != null && playerTransform.position.y < fallThresholdY)
        {
            TriggerGameOver("¡Caíste al vacío!");
        }
    }

    /// <summary>
    /// Se invoca al impactar con un obstáculo letal o caer de la plataforma.
    /// </summary>
    public void TriggerGameOver(string reason = "¡Colisión letal!")
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.GameOver;
        Debug.Log($"[GameLoopManager3D] Fin del juego: {reason}");

        // Desactivar controles físicos del jugador si es necesario
        if (playerTransform != null)
        {
            PlayerController3D controller = playerTransform.GetComponent<PlayerController3D>();
            if (controller != null) controller.enabled = false;
        }

        // Mostrar estadísticas en el panel de Game Over
        int currentScore = ScoreManager3D.Instance != null ? ScoreManager3D.Instance.CurrentScore : 0;
        int highScore = ScoreManager3D.Instance != null ? ScoreManager3D.Instance.HighScore : 0;

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Puntuación: {currentScore:N0}";
        }

        if (finalHighScoreText != null)
        {
            finalHighScoreText.text = $"Mejor Récord: {highScore:N0}";
        }

        // Mostrar el modal
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Pausar o ralentizar el tiempo ligeramente para dramatismo
        Time.timeScale = 0.2f;
    }

    /// <summary>
    /// Reinicia el bucle de juego recargando la escena actual.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1.0f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    /// <summary>
    /// Vuelve a la escena del menú principal.
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(0);
    }
}
