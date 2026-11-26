using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Componente de control táctil interactivo en pantalla para UI móvil.
/// Detecta eventos de pulsación continua (pointer down / up) para simular
/// botones direccionales analógicos y de acción (salto) sin depender de librerías externas.
/// 
/// Cumple con el Criterio 1 de la Actividad 4.1: UI adaptable a móviles.
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
    }

    private void Start()
    {
        FindController();
    }

    private void FindController()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController3D>();
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
