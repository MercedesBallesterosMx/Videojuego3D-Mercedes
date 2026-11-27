using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Obstáculo dinámico en tres dimensiones que oscila o rota en el escenario.
/// Demuestra las dinámicas tridimensionales y la curva de dificultad progresiva,
/// ajustando su velocidad en función del DifficultyManager3D.
/// </summary>
public class MovingObstacle3D : MonoBehaviour
{
    public enum MovementType
    {
        OscillateHorizontalX, // Se desplaza de izquierda a derecha en X
        RotateVerticalY,      // Gira continuamente sobre el eje Y
        VerticalBobbingY      // Sube y baja rítmicamente en el eje Y
    }

    [Header("Tipo de Dinámica")]
    [SerializeField] private MovementType movementType = MovementType.OscillateHorizontalX;

    [Header("Parámetros de Movimiento")]
    [Tooltip("Distancia máxima de oscilación.")]
    [SerializeField] private float amplitude = 3.2f;

    [Tooltip("Velocidad base del movimiento o rotación.")]
    [SerializeField] private float baseSpeed = 2.0f;

    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        // Obtener multiplicador de dificultad dinámico
        float diffMultiplier = DifficultyManager3D.Instance != null ? DifficultyManager3D.Instance.CurrentObstacleSpeedMultiplier : 1.0f;
        float currentSpeed = baseSpeed * diffMultiplier;

        switch (movementType)
        {
            case MovementType.OscillateHorizontalX:
                float xOffset = Mathf.Sin(Time.time * currentSpeed) * amplitude;
                transform.position = new Vector3(initialPosition.x + xOffset, initialPosition.y, initialPosition.z);
                break;

            case MovementType.RotateVerticalY:
                transform.Rotate(Vector3.up, currentSpeed * 45f * Time.deltaTime);
                break;

            case MovementType.VerticalBobbingY:
                float yOffset = Mathf.Sin(Time.time * currentSpeed) * (amplitude * 0.4f);
                transform.position = new Vector3(initialPosition.x, initialPosition.y + yOffset, initialPosition.z);
                break;
        }
    }
}
