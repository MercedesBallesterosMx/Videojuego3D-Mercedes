using UnityEngine;

/// <summary>
/// Portal o Arco de Meta tridimensional que marca la finalización exitosa de un nivel.
/// Inspirado en el Cap. 9 de Lucas Canary ("Mejora Visual y cambio nivel").
/// Detecta la llegada del jugador mediante un Trigger físico 3D y detona el evento de nivel completado.
/// </summary>
public class GoalPortal3D : MonoBehaviour
{
    [Header("Efectos Visuales de Meta")]
    [Tooltip("Velocidad de rotación del anillo o portal de meta.")]
    [SerializeField] private float rotationSpeed = 45f;

    [Tooltip("Sistema de partículas opcional de confeti o estrellas al cruzar la meta.")]
    [SerializeField] private ParticleSystem victoryParticles;

    private bool hasTriggered = false;

    private void Update()
    {
        // Rotación continua decorativa del portal
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log($"[GoalPortal3D] ¡Jugador cruzó la meta en el nivel!");

            if (victoryParticles != null)
            {
                victoryParticles.Play();
            }

            if (LevelProgressController3D.Instance != null)
            {
                LevelProgressController3D.Instance.OnGoalReached();
            }
        }
    }
}
