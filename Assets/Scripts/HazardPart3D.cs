using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente de zona letal o parte de peligro ('DeathPart').
/// Cumple con los requerimientos de la Actividad 4.3 (referencia Lucas Canary):
/// - Detecta cuando el jugador colisiona con un obstáculo letal o cae al vacío.
/// - Dispara la lógica de pérdida en el bucle de juego (GameLoopManager3D).
/// </summary>
public class HazardPart3D : MonoBehaviour
{
    [Header("Configuración de Peligro")]
    [Tooltip("Si es verdadero, destruye visualmente al jugador o congela su física al impactar.")]
    [SerializeField] private bool instantDeath = true;

    [Tooltip("Color de advertencia en el editor para identificar zonas de muerte.")]
    [SerializeField] private Color gizmoColor = Color.red;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerPlayerDefeat(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerPlayerDefeat(other.gameObject);
        }
    }

    private void TriggerPlayerDefeat(GameObject playerObj)
    {
        Debug.Log($"[HazardPart3D] Jugador impactó contra la zona letal: {gameObject.name}");

        if (GameLoopManager3D.Instance != null)
        {
            GameLoopManager3D.Instance.TriggerGameOver();
        }
        else
        {
            // Respaldo de seguridad si no existe el manager
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }
}
