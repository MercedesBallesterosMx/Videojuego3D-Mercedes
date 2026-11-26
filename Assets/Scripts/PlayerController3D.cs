using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico de movimiento tridimensional para el personaje principal.
/// Cumple con el Criterio 2 de la Actividad 4.1: Desplazamiento en tres ejes (X, Y, Z).
/// Integra soporte híbrido para controles táctiles móviles y teclado (WASD / Flechas / Espacio).
/// 
/// Requerimientos del GameObject:
/// - Rigidbody (con Freeze Rotation en X, Y, Z para evitar balanceos no deseados).
/// - Collider (SphereCollider, CapsuleCollider o BoxCollider).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Configuración de Movimiento 3D")]
    [Tooltip("Velocidad de traslación horizontal y en profundidad (Ejes X y Z).")]
    [SerializeField] private float moveSpeed = 8.5f;

    [Tooltip("Fuerza del impulso vertical para el salto (Eje Y).")]
    [SerializeField] private float jumpForce = 7.0f;

    [Tooltip("Multiplicador de gravedad personalizada para caída responsiva.")]
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Detección de Suelo (Ground Check)")]
    [Tooltip("Punto de referencia en los pies del personaje para verificar el suelo.")]
    [SerializeField] private Transform groundCheckPoint;

    [Tooltip("Radio de detección esférica de contacto con la superficie.")]
    [SerializeField] private float groundCheckRadius = 0.3f;

    [Tooltip("Capa que define las superficies transitables.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Referencias Opcionales de Visual")]
    [Tooltip("Transform del modelo 3D para rotar suavemente hacia la dirección de avance.")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private float rotationSpeed = 12f;

    // Referencias internas
    private Rigidbody rb;
    private bool isGrounded;
    private Vector2 mobileInput = Vector2.zero;
    private bool jumpRequested = false;

    // Propiedades públicas de consulta
    public bool IsGrounded => isGrounded;
    public Vector3 CurrentVelocity => rb != null ? rb.linearVelocity : Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Asegurar configuración física óptima para jugabilidad móvil
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Auto-crear punto de verificación de suelo si no fue asignado
        if (groundCheckPoint == null)
        {
            GameObject checkObj = new GameObject("GroundCheckPoint");
            checkObj.transform.SetParent(transform);
            checkObj.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheckPoint = checkObj.transform;
        }
    }

    private void Update()
    {
        // 1. Verificación de suelo mediante OverlapSphere
        CheckGroundStatus();

        // 2. Lectura de entrada de teclado (para pruebas en editor o PC)
        float keyboardX = Input.GetAxisRaw("Horizontal");
        float keyboardZ = Input.GetAxisRaw("Vertical");

        // Fusión de entrada: teclado + input táctil móvil
        float finalX = Mathf.Abs(mobileInput.x) > 0.05f ? mobileInput.x : keyboardX;
        float finalZ = Mathf.Abs(mobileInput.y) > 0.05f ? mobileInput.y : keyboardZ;

        // 3. Solicitud de salto (Eje Y)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequested = true;
        }

        // 4. Orientación del modelo hacia el vector de movimiento
        Vector3 moveDirection = new Vector3(finalX, 0, finalZ).normalized;
        if (moveDirection.magnitude > 0.1f && modelTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        // 1. Obtener entradas de movimiento
        float keyboardX = Input.GetAxisRaw("Horizontal");
        float keyboardZ = Input.GetAxisRaw("Vertical");
        float finalX = Mathf.Abs(mobileInput.x) > 0.05f ? mobileInput.x : keyboardX;
        float finalZ = Mathf.Abs(mobileInput.y) > 0.05f ? mobileInput.y : keyboardZ;

        Vector3 moveDir = new Vector3(finalX, 0f, finalZ).normalized;

        // 2. Aplicar movimiento en los ejes X y Z preservando la física en Y
#if UNITY_6000_0_OR_NEWER
        float currentYVelocity = rb.linearVelocity.y;
#else
        float currentYVelocity = rb.velocity.y;
#endif

        // 3. Procesar Salto en Eje Y
        if (jumpRequested)
        {
            currentYVelocity = jumpForce;
            jumpRequested = false;
        }
        else if (!isGrounded && currentYVelocity < 0)
        {
            // Gravedad extra para caída más satisfactoria (gameplay móvil)
            currentYVelocity += Physics.gravity.y * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        Vector3 targetVelocity = new Vector3(moveDir.x * moveSpeed, currentYVelocity, moveDir.z * moveSpeed);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = targetVelocity;
#else
        rb.velocity = targetVelocity;
#endif
    }

    /// <summary>
    /// Detecta si el personaje está en contacto con el suelo o plataformas transitables.
    /// </summary>
    private void CheckGroundStatus()
    {
        if (groundCheckPoint == null) return;

        // Si no se asignó layer específica, verifica contra todo excepto la propia capa del jugador
        int layerMask = groundLayer.value != 0 ? groundLayer.value : ~(1 << gameObject.layer);
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, layerMask, QueryTriggerInteraction.Ignore);
    }

    #region Métodos Públicos para la Interfaz Móvil (UI)
    /// <summary>
    /// Establece el vector de dirección proveniente de los botones táctiles o joystick virtual.
    /// x = desplazamiento lateral (Eje X), y = profundidad (Eje Z).
    /// </summary>
    public void SetMobileInput(Vector2 input)
    {
        mobileInput = Vector2.ClampMagnitude(input, 1.0f);
    }

    /// <summary>
    /// Dispara la acción de salto desde el botón táctil en pantalla (Eje Y).
    /// </summary>
    public void MobileJump()
    {
        if (isGrounded)
        {
            jumpRequested = true;
        }
    }

    /// <summary>
    /// Detiene de inmediato cualquier entrada táctil acumulada (al soltar botones).
    /// </summary>
    public void ResetMobileInput()
    {
        mobileInput = Vector2.zero;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
