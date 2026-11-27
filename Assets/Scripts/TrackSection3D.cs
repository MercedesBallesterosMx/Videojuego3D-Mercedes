using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Módulo individual de pista tridimensional para el generador procedimental de niveles.
/// Define las dimensiones del tramo, el punto de acoplamiento del siguiente módulo
/// y la dificultad asociada para la generación continua.
/// </summary>
public class TrackSection3D : MonoBehaviour
{
    [Header("Dimensiones del Módulo")]
    [Tooltip("Longitud de este módulo a lo largo del eje Z.")]
    [SerializeField] private float sectionLength = 25f;

    [Tooltip("Nivel mínimo de dificultad requerido para instanciar este módulo (1 = Inicial, 4 = Extremo).")]
    [Range(1, 4)]
    [SerializeField] private int requiredDifficultyTier = 1;

    [Tooltip("Punto de referencia donde se conectará la siguiente sección en Z.")]
    [SerializeField] private Transform connectionPoint;

    public float SectionLength => sectionLength;
    public int RequiredDifficultyTier => requiredDifficultyTier;

    /// <summary>
    /// Retorna la posición en el mundo donde debe ensamblarse la siguiente sección.
    /// </summary>
    public Vector3 GetConnectionPosition()
    {
        if (connectionPoint != null)
        {
            return connectionPoint.position;
        }

        // Por defecto, se proyecta hacia adelante en el eje Z según su longitud
        return transform.position + new Vector3(0f, 0f, sectionLength);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + new Vector3(0f, 0f, sectionLength * 0.5f), 
            new Vector3(14f, 1f, sectionLength));
    }
}
