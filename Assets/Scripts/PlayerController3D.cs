using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador físico de movimiento tridimensional para el personaje principal.
/// Cumple con los Criterios de la Actividad 4.1 y 4.2:
/// - Desplazamiento tridimensional en tres ejes (X, Y, Z).
/// - Soporte híbrido para controles táctiles móviles y teclado (WASD / Flechas / Espacio).
/// - Adaptación de velocidad a la curva de dificultad progresiva (DifficultyManager3D).
/// - Detección de suelo infalible y salto responsivo en Eje Y tanto móvil como en PC.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Configuración de Movimiento 3D")]
    [Tooltip("Velocidad base de traslación horizontal y en profundidad (Ejes X y Z).")]
    [SerializeField] private float baseMoveSpeed = 9.0f;

    [Tooltip("Fuerza del impulso vertical para el salto (Eje Y).")]
    [SerializeField] private float jumpForce = 10.0f;

    [Tooltip("Multiplicador de gravedad personalizada para caída responsiva.")]
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Estado de Suelo y Físicas")]
    [SerializeField] private bool isGrounded = true;

    // Referencias internas
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Vector2 mobileInput = Vector2.zero;
    private float dynamicSpeedMultiplier = 1.0f;

    // Súper Velocidad (Actividad 4.6 - UX y Game Feel)
    private bool isSuperSpeedActive = false;
    private float superSpeedMultiplier = 1.0f;
    private Coroutine superSpeedCoroutine = null;

    // Salto y amortiguamiento
    private bool jumpRequested = false;
    private float coyoteTimer = 0.25f;
    private float jumpBufferTimer = 0f;
    private const float COYOTE_TIME = 0.25f;
    private const float JUMP_BUFFER = 0.35f;

    // Propiedades públicas de consulta
    public bool IsGrounded => isGrounded;
    public bool IsSuperSpeedActive => isSuperSpeedActive;
    public float EffectiveMoveSpeed => baseMoveSpeed * dynamicSpeedMultiplier * (isSuperSpeedActive ? superSpeedMultiplier : 1.0f);
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
        // 1. Chequeo de suelo por detección geométrica multi-punto
        CheckGroundStatus();

        // 2. Temporizadores de amortiguamiento (Coyote Time y Jump Buffer)
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

            // Efecto visual de partículas al saltar (Actividad 4.4)
            if (PlayerVisualCustomizer3D.Instance != null)
            {
                PlayerVisualCustomizer3D.Instance.PlayJumpParticles();
            }

            Debug.Log("[PlayerController3D] ¡Salto en Eje Y ejecutado exitosamente!");
        }
        else if (!isGrounded && currentYVelocity < 0)
        {
            // Gravedad extra para caída ágil
            currentYVelocity += Physics.gravity.y * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        float currentSpeed = EffectiveMoveSpeed;
        Vector3 targetVelocity = new Vector3(moveDir.x * currentSpeed, currentYVelocity, moveDir.z * currentSpeed);

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = targetVelocity;
#else
        rb.velocity = targetVelocity;
#endif
    }

    /// <summary>
    /// Ajusta dinámicamente el multiplicador de velocidad conforme avanza la dificultad.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        dynamicSpeedMultiplier = Mathf.Clamp(multiplier, 0.5f, 3.0f);
    }

    /// <summary>
    /// Detecta suelo mediante colisiones físicas directas (PhysX Contact Normal).
    /// </summary>
    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.30f)
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
            if (contact.normal.y > 0.30f)
            {
                isGrounded = true;
                coyoteTimer = COYOTE_TIME;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Al salir de un collider, solo anular isGrounded si la comprobación de rayos no detecta suelo adyacente
        CheckGroundStatus();
    }

    /// <summary>
    /// Chequeo robusto multi-punto mediante SphereCast y Raycasts para garantizar
    /// que el suelo sea detectado infaliblemente en rampas, bordes y superficies inclinadas.
    /// </summary>
    private void CheckGroundStatus()
    {
        float radius = (sphereCollider != null) ? sphereCollider.radius * transform.lossyScale.y : (transform.lossyScale.y * 0.5f);
        float checkDist = radius * 0.4f + 0.45f;

        // 1. SphereCast descendente amplio
        if (Physics.SphereCast(transform.position, radius * 0.85f, Vector3.down, out RaycastHit hit, checkDist))
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject && hit.normal.y > 0.25f)
            {
                isGrounded = true;
                coyoteTimer = COYOTE_TIME;
                return;
            }
        }

        // 2. Muestreo de 5 Raycasts distribuidos (Centro, Adelante, Atrás, Izquierda, Derecha)
        Vector3[] rayOrigins = new Vector3[]
        {
            transform.position,
            transform.position + Vector3.forward * (radius * 0.5f),
            transform.position + Vector3.back * (radius * 0.5f),
            transform.position + Vector3.left * (radius * 0.5f),
            transform.position + Vector3.right * (radius * 0.5f)
        };

        float rayDist = radius + 0.50f;
        foreach (Vector3 origin in rayOrigins)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit rHit, rayDist))
            {
                if (rHit.collider != null && rHit.collider.gameObject != gameObject && rHit.normal.y > 0.25f)
                {
                    isGrounded = true;
                    coyoteTimer = COYOTE_TIME;
                    return;
                }
            }
        }

        // Si ningún rayo ni esfera detectó suelo, marcar en el aire
        isGrounded = false;
    }

    #region Métodos Públicos para la Interfaz Móvil (UI)
    /// <summary>
    /// Dispara la acción de salto desde el botón táctil en pantalla.
    /// Aplica un impulso directo si ya está en suelo o en coyote time, y amortigua en buffer si está en el aire.
    /// </summary>
    public void MobileJump()
    {
        jumpRequested = true;
        jumpBufferTimer = JUMP_BUFFER;

        // Ejecución inmediata si está en suelo o coyote time para latencia cero
        if (isGrounded || coyoteTimer > 0f)
        {
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
#else
                rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
#endif
                jumpRequested = false;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                isGrounded = false;

                if (PlayerVisualCustomizer3D.Instance != null)
                {
                    PlayerVisualCustomizer3D.Instance.PlayJumpParticles();
                }

                Debug.Log("[PlayerController3D] ¡Salto Inmediato ejecutado exitosamente!");
            }
        }
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

    #region Súper Velocidad (Actividad 4.6 - UX y Game Feel)
    /// <summary>
    /// Activa temporalmente la Súper Velocidad al interactuar con un potenciador (Criterios 1 y 2 de Moodle).
    /// Su duración máxima está estrictamente acotada a no más de 3 segundos (2.5s por defecto).
    /// </summary>
    public void ActivateSuperSpeed(float multiplier = 2.0f, float duration = 2.5f)
    {
        // Regla estricta de Moodle: no exceder 3.0 segundos
        float safeDuration = Mathf.Clamp(duration, 0.5f, 3.0f);

        if (superSpeedCoroutine != null)
        {
            StopCoroutine(superSpeedCoroutine);
        }

        superSpeedCoroutine = StartCoroutine(SuperSpeedRoutine(multiplier, safeDuration));
    }

    private System.Collections.IEnumerator SuperSpeedRoutine(float multiplier, float duration)
    {
        isSuperSpeedActive = true;
        superSpeedMultiplier = multiplier;

        Debug.Log($"[PlayerController3D] ¡SUPER SPEED ACTIVADA! Velocidad x{multiplier} durante {duration}s.");

        // 1. Efecto visual de estela intensa (TrailRenderer) en el personaje (Criterio 2)
        if (PlayerVisualCustomizer3D.Instance != null)
        {
            PlayerVisualCustomizer3D.Instance.EnableSuperSpeedTrail(duration);
        }

        // 2. Efecto de cámara cinemática FOV Kick (Game Feel)
        if (CameraFollow3D.Instance != null)
        {
            CameraFollow3D.Instance.TriggerSpeedKick(duration);
        }

        // 3. Notificar a la UI
        if (LevelProgressController3D.Instance != null)
        {
            LevelProgressController3D.Instance.ShowSpeedBoostBanner(duration, multiplier);
        }

        yield return new WaitForSeconds(duration);

        isSuperSpeedActive = false;
        superSpeedMultiplier = 1.0f;
        superSpeedCoroutine = null;

        Debug.Log("[PlayerController3D] Súper Velocidad concluida. Restaurando velocidad nominal.");
    }
    #endregion
}
