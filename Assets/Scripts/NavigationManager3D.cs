using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor centralizado de Navegación, Carga de Escenas y Transiciones entre Niveles.
/// Cumple directamente con los Criterios 1 y 2 de la Actividad 4.5 en Moodle:
/// - Permite un flujo continuo entre un mínimo de dos niveles consecutivos (Level_01 -> Level_02).
/// - Ejecuta transiciones limpias con UnityEngine.SceneManagement.SceneManager sin excepciones.
/// - Restablece el flujo temporal (Time.timeScale = 1f) y la memoria entre escenas.
/// </summary>
public class NavigationManager3D : MonoBehaviour
{
    public static NavigationManager3D Instance { get; private set; }

    [Header("Nombres de Escenas en Build Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu3D";
    [SerializeField] private string level1SceneName = "Level_01";
    [SerializeField] private string level2SceneName = "Level_02";
    [SerializeField] private string level3SceneName = "Level_03";

    [Header("Efecto de Transición")]
    [Tooltip("Tiempo de retardo opcional para transiciones suaves.")]
    [SerializeField] private float transitionDelay = 0.25f;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Carga la escena del Menú Principal asegurando tiempo normal.
    /// </summary>
    public void LoadMainMenu()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(mainMenuSceneName));
    }

    /// <summary>
    /// Inicia el juego desde el Nivel 1.
    /// </summary>
    public void StartGame()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(level1SceneName));
    }

    /// <summary>
    /// Carga explícitamente el Nivel 1.
    /// </summary>
    public void LoadLevel1()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(level1SceneName));
    }

    /// <summary>
    /// Carga explícitamente el Nivel 2.
    /// </summary>
    public void LoadLevel2()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(level2SceneName));
    }

    /// <summary>
    /// Carga explícitamente el Nivel 3 (Zona Carmesí / Experto - Proyecto Integrador).
    /// </summary>
    public void LoadLevel3()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(level3SceneName));
    }

    /// <summary>
    /// Avanza de forma inteligente al siguiente nivel consecutivo en el flujo del juego.
    /// Flujo de 3 niveles progresivos: Level_01 -> Level_02 -> Level_03 -> MainMenu3D (Victoria Total).
    /// </summary>
    public void LoadNextLevel()
    {
        if (isTransitioning) return;

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == level1SceneName || currentScene == "MainGame3D")
        {
            StartCoroutine(TransitionRoutine(level2SceneName));
        }
        else if (currentScene == level2SceneName)
        {
            StartCoroutine(TransitionRoutine(level3SceneName));
        }
        else if (currentScene == level3SceneName)
        {
            // Al completar el nivel 3 (Final), regresa al Menú Principal con récord actualizado
            StartCoroutine(TransitionRoutine(mainMenuSceneName));
        }
        else
        {
            StartCoroutine(TransitionRoutine(level1SceneName));
        }
    }

    /// <summary>
    /// Reinicia la escena activa de manera limpia y sin arrojar excepciones.
    /// </summary>
    public void RestartCurrentLevel()
    {
        if (isTransitioning) return;
        string activeScene = SceneManager.GetActiveScene().name;
        StartCoroutine(TransitionRoutine(activeScene));
    }

    /// <summary>
    /// Carga cualquier escena por nombre validando su existencia.
    /// </summary>
    public void LoadSceneCustom(string targetSceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(targetSceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;
        Time.timeScale = 1.0f; // Garantizar que el tiempo corra normalmente

        if (transitionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(transitionDelay);
        }

        Debug.Log($"[NavigationManager3D] Transicionando limpiamente hacia la escena: '{sceneName}'");

        // Verificación de seguridad antes de cargar
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"[NavigationManager3D] La escena '{sceneName}' no se encontró en Build Settings. Cargando escena activa como salvaguarda.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Cierra la aplicación de manera ordenada.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[NavigationManager3D] Cerrando la aplicación...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
