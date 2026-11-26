using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico de movimiento tridimensional para el personaje principal.
/// Cumple con el Criterio 2 de la Actividad 4.1: Desplazamiento en tres ejes (X, Y, Z).
/// Integra soporte híbrido para controles táctiles móviles y teclado (WASD / Flechas / Espacio).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Configuración de Movimiento 3D")]
    [Tooltip("Velocidad de traslación horizontal y en profundidad (Ejes X y Z).")]
    [SerializeField] private float moveSpeed = 9.0f;

    [Tooltip("Fuerza del impulso vertical para el salto (Eje Y).")]
    [SerializeField] private float jumpForce = 9.2f;

    [Tooltip("Multiplicador de gravedad personalizada para caída responsiva.")]
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Estado de Suelo")]
    [SerializeField] private bool isGrounded = true;

    // Referencias internas
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Vector2 mobileInput = Vector2.zero;

    // Salto y amortiguamiento
    private bool jumpRequested = false;
    private float coyoteTimer = 0.2f;
    private float jumpBufferTimer = 0f;
    private const float COYOTE_TIME = 0.2f;
    private const float JUMP_BUFFER = 0.25f;

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
        // 1. Contador de Coyote Time
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

        // 2. Detección de suelo por Raycast hacia abajo (complemento de OnCollisionStay)
        CheckRaycastGround();

        // 3. Lectura de tecla Espacio para PC / Editor
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
        {
            MobileJump();
        }
    }

    private void FixedUpdate()
    {
        // 1. Entrada de movimiento (Teclado WASD/Flechas fusionado con D-Pad móvil)
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

        // 3. Ejecutar Salto en Eje Y si fue solicitado y estamos en suelo
        if ((jumpRequested || jumpBufferTimer > 0f) && (isGrounded || coyoteTimer > 0f))
        {
            currentYVelocity = jumpForce;
            jumpRequested = false;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            isGrounded = false;
            Debug.Log("[PlayerController3D] ¡Salto en Eje Y ejecutado exitosamente!");
        }
        else if (!isGrounded && currentYVelocity < 0)
        {
            // Gravedad extra para caída ágil
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
    /// Detecta suelo mediante colisiones físicas directas (PhysX Contact Normal).
    /// </summary>
    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // Si la normal apunta hacia arriba, estamos parados sobre una superficie
            if (contact.normal.y > 0.35f)
            {
                isGrounded = true;
                coyoteTimer = COYOTE_TIME;
                return;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.35f)
            {
                isGrounded = true;
                coyoteTimer = COYOTE_TIME;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    /// <summary>
    /// Chequeo complementario de suelo por Raycast desde el centro hacia abajo.
    /// </summary>
    private void CheckRaycastGround()
    {
        float radius = (sphereCollider != null) ? sphereCollider.radius * transform.lossyScale.y : (transform.lossyScale.y * 0.5f);
        float checkDist = radius + 0.3f;

        // Dispara un rayo hacia abajo sin excluir ninguna capa
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, checkDist))
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject && hit.normal.y > 0.35f)
            {
                isGrounded = true;
                coyoteTimer = COYOTE_TIME;
            }
        }
    }

    #region Métodos Públicos para la Interfaz Móvil (UI)
    /// <summary>
    /// Dispara la acción de salto desde el botón táctil en pantalla.
    /// </summary>
    public void MobileJump()
    {
        jumpRequested = true;
        jumpBufferTimer = JUMP_BUFFER;
    }

    /// <summary>
    /// Establece el vector de dirección proveniente de los botones táctiles en pantalla.
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
}
