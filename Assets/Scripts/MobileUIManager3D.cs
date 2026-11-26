using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor integral de Interfaz de Usuario (UI) adaptada a dispositivos móviles.
/// Cumple con el Criterio 1 de la Actividad 4.1: Navegación fluida entre menús y escenas principales.
/// 
/// Administra el HUD táctil en partida, el menú de pausa con congelamiento temporal (Time.timeScale = 0),
/// reinicio de nivel, retorno al menú principal y salida de la aplicación.
/// </summary>
public class MobileUIManager3D : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    [Tooltip("Panel contenedor de los controles táctiles virtuales en pantalla.")]
    [SerializeField] private GameObject mobileControlsPanel;

    [Tooltip("Ventana modal del menú de pausa dentro de la partida.")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Tooltip("Ventana modal con instrucciones de juego y controles.")]
    [SerializeField] private GameObject instructionsPanel;

    [Header("Elementos Interactivos de HUD")]
    [Tooltip("Botón para pausar la partida.")]
    [SerializeField] private Button pauseButton;

    [Tooltip("Botón para reanudar el juego desde la pausa.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Botón para reiniciar el nivel actual.")]
    [SerializeField] private Button restartButton;

    [Tooltip("Botón para volver a la escena del Menú Principal.")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("Botón para cerrar la aplicación.")]
    [SerializeField] private Button quitButton;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del Menú Principal.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu3D";

    [Tooltip("Nombre de la escena de juego 3D principal.")]
    [SerializeField] private string gameSceneName = "MainScene3D";

    private bool isPaused = false;

    private void Awake()
    {
        // Asegurar que el tiempo comience corriendo normalmente
        Time.timeScale = 1.0f;
    }

    private void Start()
    {
        // Vincular eventos de botones mediante código para robustez
        if (pauseButton != null) pauseButton.onClick.AddListener(PauseGame);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartCurrentLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // Ocultar paneles modales al iniciar
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (mobileControlsPanel != null) mobileControlsPanel.SetActive(true);
    }

    private void Update()
    {
        // Atajo de teclado 'Escape' para pausar en pruebas de editor / PC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    #region Navegación y Control de Estados
    /// <summary>
    /// Pausa el juego congelando la física y desplegando el menú modal.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0.0f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (mobileControlsPanel != null) mobileControlsPanel.SetActive(false);
    }

    /// <summary>
    /// Reanuda el curso normal de la partida reactivando el tiempo.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1.0f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (mobileControlsPanel != null) mobileControlsPanel.SetActive(true);
    }

    /// <summary>
    /// Recarga la escena activa para reintentar la fase.
    /// </summary>
    public void RestartCurrentLevel()
    {
        Time.timeScale = 1.0f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Navega de regreso al Menú Principal del videojuego.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1.0f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    /// <summary>
    /// Inicia la partida cargando la escena de juego 3D principal.
    /// </summary>
    public void StartGame()
    {
        Time.timeScale = 1.0f;
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }

    /// <summary>
    /// Muestra u oculta la ventana con la guía de controles móviles.
    /// </summary>
    public void ToggleInstructions(bool show)
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(show);
        }
    }

    /// <summary>
    /// Cierra el programa de manera limpia.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MobileUIManager3D] Cerrando la aplicación...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion
}
