using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico de movimiento tridimensional para el personaje principal.
/// Cumple con el Criterio 2 de la Actividad 4.1: Desplazamiento en tres ejes (X, Y, Z).
/// Integra soporte híbrido para controles táctiles móviles y teclado (WASD / Flechas / Espacio).
/// 
/// Requerimientos del GameObject:
/// - Rigidbody (con Freeze Rotation en X y Z).
/// - Collider (SphereCollider).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Configuración de Movimiento 3D")]
    [Tooltip("Velocidad de traslación horizontal y en profundidad (Ejes X y Z).")]
    [SerializeField] private float moveSpeed = 9.0f;

    [Tooltip("Fuerza del impulso vertical para el salto (Eje Y).")]
    [SerializeField] private float jumpForce = 9.5f;

    [Tooltip("Multiplicador de gravedad personalizada para caída responsiva.")]
    [SerializeField] private float gravityMultiplier = 2.2f;

    [Header("Detección de Suelo (Ground Check Robusto)")]
    [Tooltip("Distancia adicional hacia abajo para verificar contacto con el suelo.")]
    [SerializeField] private float groundCheckOffset = 0.25f;

    [Tooltip("Capa que define las superficies transitables.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Referencias Opcionales de Visual")]
    [Tooltip("Transform del modelo 3D para rotar suavemente hacia la dirección de avance.")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private float rotationSpeed = 12f;

    // Referencias internas
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private bool isGrounded = true;
    private Vector2 mobileInput = Vector2.zero;

    // Jump Buffering & Coyote Time (Para garantizar que el salto NUNCA falle al presionar el botón)
    private float jumpBufferTimer = 0f;
    private const float JUMP_BUFFER_TIME = 0.25f;
    private float coyoteTimer = 0f;
    private const float COYOTE_TIME = 0.15f;

    // Propiedades públicas de consulta
    public bool IsGrounded => isGrounded;
    public Vector3 CurrentVelocity
    {
        get
        {
            if (rb == null) return Vector3.zero;
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();

        // Asegurar configuración física óptima para jugabilidad móvil
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        // 1. Verificación de suelo mediante Raycast hacia abajo
        CheckGroundStatus();

        // 2. Gestión de temporizadores de salto (Coyote Time y Jump Buffering)
        if (isGrounded)
        {
            coyoteTimer = COYOTE_TIME;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // 3. Lectura de salto desde teclado (Espacio)
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            RequestJump();
        }

        // 4. Orientación del modelo hacia el vector de movimiento
        float keyboardX = Input.GetAxisRaw("Horizontal");
        float keyboardZ = Input.GetAxisRaw("Vertical");
        float finalX = Mathf.Abs(mobileInput.x) > 0.05f ? mobileInput.x : keyboardX;
        float finalZ = Mathf.Abs(mobileInput.y) > 0.05f ? mobileInput.y : keyboardZ;

        Vector3 moveDirection = new Vector3(finalX, 0, finalZ).normalized;
        if (moveDirection.magnitude > 0.1f && modelTransform != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        // 1. Obtener entradas de movimiento combinadas (teclado + botones táctiles)
        float keyboardX = Input.GetAxisRaw("Horizontal");
        float keyboardZ = Input.GetAxisRaw("Vertical");
        float finalX = Mathf.Abs(mobileInput.x) > 0.05f ? mobileInput.x : keyboardX;
        float finalZ = Mathf.Abs(mobileInput.y) > 0.05f ? mobileInput.y : keyboardZ;

        Vector3 moveDir = new Vector3(finalX, 0f, finalZ).normalized;

        // 2. Obtener velocidad vertical actual
#if UNITY_6000_0_OR_NEWER
        float currentYVelocity = rb.linearVelocity.y;
#else
        float currentYVelocity = rb.velocity.y;
#endif

        // 3. Procesar Salto en Eje Y (con tolerancia de buffer)
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            currentYVelocity = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            isGrounded = false;
            Debug.Log("[PlayerController3D] ¡Salto en Eje Y ejecutado con éxito!");
        }
        else if (!isGrounded && currentYVelocity < 0)
        {
            // Gravedad personalizada para caída más ágil y controlada
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
    /// Detecta si la esfera está tocando una superficie transitable mediante Raycast y SphereCast.
    /// </summary>
    private void CheckGroundStatus()
    {
        float radius = (sphereCollider != null) ? sphereCollider.radius * transform.lossyScale.y : (transform.lossyScale.y * 0.5f);
        float rayDistance = radius + groundCheckOffset;
        int layerMask = groundLayer.value != 0 ? groundLayer.value : ~(1 << gameObject.layer);

        // Dispara un rayo desde el centro de la esfera hacia el suelo
        bool rayHit = Physics.Raycast(transform.position, Vector3.down, rayDistance, layerMask, QueryTriggerInteraction.Ignore);
        
        // Verificación de respaldo esférica en la base
        Vector3 sphereBase = transform.position - new Vector3(0, radius * 0.75f, 0);
        bool sphereHit = Physics.CheckSphere(sphereBase, radius * 0.45f, layerMask, QueryTriggerInteraction.Ignore);

        isGrounded = rayHit || sphereHit;
    }

    #region Métodos Públicos para la Interfaz Móvil (UI)
    /// <summary>
    /// Registra la solicitud de salto con buffer temporal para ejecución inmediata y fluida.
    /// </summary>
    public void MobileJump()
    {
        Debug.Log("[PlayerController3D] Botón táctil SALTO presionado.");
        RequestJump();
    }

    private void RequestJump()
    {
        jumpBufferTimer = JUMP_BUFFER_TIME;
    }

    /// <summary>
    /// Establece el vector de dirección proveniente de los botones táctiles en pantalla.
    /// x = desplazamiento lateral (Eje X), y = profundidad (Eje Z).
    /// </summary>
    public void SetMobileInput(Vector2 input)
    {
        mobileInput = Vector2.ClampMagnitude(input, 1.0f);
    }

    /// <summary>
    /// Detiene de inmediato cualquier entrada táctil al soltar el botón.
    /// </summary>
    public void ResetMobileInput()
    {
        mobileInput = Vector2.zero;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        float radius = (sphereCollider != null) ? sphereCollider.radius * transform.lossyScale.y : (transform.lossyScale.y * 0.5f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (radius + groundCheckOffset));
    }
}
