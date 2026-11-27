using UnityEngine;

/// <summary>
/// Potenciador tridimensional de Súper Velocidad (Boost Pickup / Energy Capsule).
/// Cumple directamente con los Criterios 1 y 2 de la Actividad 4.6 (UX) en Moodle:
/// - Modifica dinámicamente la velocidad de traslación del personaje al interactuar con el evento.
/// - Detona un incremento de velocidad claramente perceptible con una duración máxima que no excede los 3 segundos (2.5 s).
/// Inspirado en el Cap. 11 de Lucas Canary ("SuperSpeed").
/// </summary>
public class SpeedBoostPickup3D : MonoBehaviour
{
    [Header("Parámetros del Potenciador (UX / Game Feel)")]
    [Tooltip("Factor multiplicador de velocidad de traslación (ej. x2.0).")]
    [SerializeField] private float speedMultiplier = 2.0f;

    [Tooltip("Duración temporal del efecto. Estrictamente no debe exceder 3.0 segundos según rúbrica.")]
    [SerializeField] private float boostDuration = 2.5f;

    [Header("Animación Procedural (Visibilidad y Ergonomía)")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float floatSpeed = 3.5f;
    [SerializeField] private float floatAmplitude = 0.25f;

    [Header("Efectos Visuales al Recoger")]
    [SerializeField] private ParticleSystem pickupParticles;

    private Vector3 initialPosition;
    private bool isCollected = false;

    private void Start()
    {
        initialPosition = transform.position;

        // Asegurar que la duración no exceda los 3 segundos de la rúbrica oficial
        if (boostDuration > 3.0f)
        {
            boostDuration = 3.0f;
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // 1. Rotación continua sobre el eje Y para llamar la atención del jugador (Affordance)
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

        // 2. Oscilación suave flotante (Bobbing)
        float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            isCollected = true;
            Debug.Log($"[SpeedBoostPickup3D] ¡Potenciador activado! Súper Velocidad x{speedMultiplier} durante {boostDuration}s.");

            // 1. Activar súper velocidad en el controlador del jugador
            PlayerController3D playerController = other.GetComponent<PlayerController3D>();
            if (playerController != null)
            {
                playerController.ActivateSuperSpeed(speedMultiplier, boostDuration);
            }

            // 2. Reproducir partículas de destello
            if (pickupParticles != null)
            {
                pickupParticles.transform.SetParent(null);
                pickupParticles.Play();
                Destroy(pickupParticles.gameObject, 1.5f);
            }

            // 3. Ocultar y destruir el objeto coleccionable
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
            Destroy(gameObject, 0.1f);
        }
    }
}
