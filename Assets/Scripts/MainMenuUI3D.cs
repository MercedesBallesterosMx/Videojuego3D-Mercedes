using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador de Interfaz de Usuario para la escena del Menú Principal (MainMenu3D).
/// Inspirado en el Cap. 10 de Lucas Canary ("Main Menu - Trail").
/// Ofrece opciones de navegación para iniciar la partida, selección de niveles,
/// visualización de instrucciones y cierre de la aplicación.
/// </summary>
public class MainMenuUI3D : MonoBehaviour
{
    [Header("Botones Principales")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button instructionsButton;
    [SerializeField] private Button quitButton;

    [Header("Paneles Secundarios")]
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject instructionsPanel;

    [Header("Botones de Selección de Nivel")]
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    [SerializeField] private Button closeLevelSelectButton;

    [Header("Botones de Instrucciones")]
    [SerializeField] private Button closeInstructionsButton;

    [Header("Etiquetas de Texto")]
    [SerializeField] private Text highScoreText;

    private void Start()
    {
        Time.timeScale = 1.0f;

        // Mostrar el récord guardado
        int highScore = PlayerPrefs.GetInt("HighScore3D", 0);
        if (highScoreText != null)
        {
            highScoreText.text = $"Mejor Récord: {highScore:N0} pts";
        }

        // Ocultar paneles secundarios
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);

        // Vincular eventos de botones
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.StartGame();
                }
            });
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.RemoveAllListeners();
            levelSelectButton.onClick.AddListener(() => {
                if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
            });
        }

        if (instructionsButton != null)
        {
            instructionsButton.onClick.RemoveAllListeners();
            instructionsButton.onClick.AddListener(() => {
                if (instructionsPanel != null) instructionsPanel.SetActive(true);
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.QuitGame();
                }
            });
        }

        if (level1Button != null)
        {
            level1Button.onClick.RemoveAllListeners();
            level1Button.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.LoadLevel1();
                }
            });
        }

        if (level2Button != null)
        {
            level2Button.onClick.RemoveAllListeners();
            level2Button.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.LoadLevel2();
                }
            });
        }

        if (level3Button != null)
        {
            level3Button.onClick.RemoveAllListeners();
            level3Button.onClick.AddListener(() => {
                if (NavigationManager3D.Instance != null)
                {
                    NavigationManager3D.Instance.LoadLevel3();
                }
            });
        }

        if (closeLevelSelectButton != null)
        {
            closeLevelSelectButton.onClick.RemoveAllListeners();
            closeLevelSelectButton.onClick.AddListener(() => {
                if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            });
        }

        if (closeInstructionsButton != null)
        {
            closeInstructionsButton.onClick.RemoveAllListeners();
            closeInstructionsButton.onClick.AddListener(() => {
                if (instructionsPanel != null) instructionsPanel.SetActive(false);
            });
        }
    }
}
