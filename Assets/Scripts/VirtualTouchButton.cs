using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Componente de control táctil interactivo en pantalla para UI móvil.
/// Detecta eventos de pulsación continua (pointer down / up) para simular
/// botones direccionales analógicos y de acción (salto) sin depender de librerías externas.
/// 
/// Cumple con el Criterio 1 de la Actividad 4.1: UI adaptable a móviles con salto 100% responsivo.
/// </summary>
public class VirtualTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public enum ButtonDirection
    {
        Forward,  // Eje Z positivo
        Backward, // Eje Z negativo
        Left,     // Eje X negativo
        Right,    // Eje X positivo
        Jump      // Eje Y positivo (acción de salto)
    }

    [Header("Configuración del Botón Táctil")]
    [Tooltip("Acción o dirección que emite este botón virtual al ser presionado.")]
    [SerializeField] private ButtonDirection direction = ButtonDirection.Forward;

    [Tooltip("Referencia al controlador del personaje principal.")]
    [SerializeField] private PlayerController3D playerController;

    private bool isPressed = false;

    private void Awake()
    {
        FindController();
        SetupButtonClickListener();
    }

    private void Start()
    {
        FindController();
        SetupButtonClickListener();
    }

    private void SetupButtonClickListener()
    {
        Button btn = GetComponent<Button>();
        if (btn != null && direction == ButtonDirection.Jump)
        {
            btn.onClick.RemoveListener(ExecuteJumpAction);
            btn.onClick.AddListener(ExecuteJumpAction);
        }
    }

    private void FindController()
    {
        if (playerController == null)
        {
#if UNITY_6000_0_OR_NEWER
            playerController = Object.FindAnyObjectByType<PlayerController3D>();
#else
            playerController = Object.FindObjectOfType<PlayerController3D>();
#endif
        }
    }

    private void Update()
    {
        if (!isPressed) return;
        if (playerController == null) FindController();
        if (playerController == null) return;

        // Si es un botón direccional sostenido, emitir el vector correspondiente en cada frame
        switch (direction)
        {
            case ButtonDirection.Forward:
                playerController.SetMobileInput(new Vector2(0f, 1f));
                break;
            case ButtonDirection.Backward:
                playerController.SetMobileInput(new Vector2(0f, -1f));
                break;
            case ButtonDirection.Left:
                playerController.SetMobileInput(new Vector2(-1f, 0f));
                break;
            case ButtonDirection.Right:
                playerController.SetMobileInput(new Vector2(1f, 0f));
                break;
            case ButtonDirection.Jump:
                break;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        if (direction == ButtonDirection.Jump)
        {
            ExecuteJumpAction();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (direction == ButtonDirection.Jump)
        {
            ExecuteJumpAction();
        }
    }

    /// <summary>
    /// Método público para vincular directamente al evento OnClick del componente Button de Unity.
    /// </summary>
    public void OnButtonClick()
    {
        if (direction == ButtonDirection.Jump)
        {
            ExecuteJumpAction();
        }
    }

    private void ExecuteJumpAction()
    {
        if (playerController == null) FindController();
        if (playerController != null)
        {
            playerController.MobileJump();
        }
        else
        {
            Debug.LogWarning("[VirtualTouchButton] No se encontró PlayerController3D en la escena para saltar.");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (direction != ButtonDirection.Jump && playerController != null)
        {
            playerController.ResetMobileInput();
        }
    }

    private void OnDisable()
    {
        isPressed = false;
        if (playerController != null && direction != ButtonDirection.Jump)
        {
            playerController.ResetMobileInput();
        }
    }
}
