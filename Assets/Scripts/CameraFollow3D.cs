using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema cinemático de seguimiento de cámara en tercera persona para entornos 3D móviles.
/// Cumple estrictamente con los Criterios 3 y 4 de la Actividad 4.1:
/// - Criterio 3: Desplazamiento sincronizado y estable aplicado a los ejes X y Z.
/// - Criterio 4: Mantiene una posición y distancia fija y constante en el eje Z respecto al jugador.
/// 
/// Requerimientos del GameObject:
/// - Asignar este script a la Main Camera de la escena 3D.
/// - Asignar el Transform del personaje principal en el campo 'target'.
/// </summary>
public class CameraFollow3D : MonoBehaviour
{
    public static CameraFollow3D Instance { get; private set; }

    [Header("Objetivo de Seguimiento")]
    [Tooltip("Transform del personaje principal al que seguirá la cámara.")]
    [SerializeField] private Transform target;

    [Header("Configuración de Desplazamiento (Offsets)")]
    [Tooltip("Distancia relativa (Offset) respecto al personaje. X: lateral, Y: elevación, Z: profundidad.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5.5f, -9.0f);

    [Header("Game Feel / Súper Velocidad (Actividad 4.6 - FOV Kick)")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float boostFOV = 74f;

    [Header("Criterio 3 y 4: Cinemática y Restricciones de Ejes")]
    [Tooltip("Tiempo de amortiguamiento para evitar sacudidas y vibraciones bruscas en pantallas móviles.")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float smoothTime = 0.22f;

    [Tooltip("Si es verdadero, la cámara mantiene una distancia estrictamente fija y constante en Z respecto al jugador (Criterio 4).")]
    [SerializeField] private bool lockFixedZDistance = true;

    [Tooltip("Si es falso, la cámara ignora los saltos verticales del personaje en el eje Y manteniendo la altura estable (Criterio 3).")]
    [SerializeField] private bool followYAxis = false;

    [Header("Orientación y Ángulo de Visión")]
    [Tooltip("Ángulo de inclinación cenital hacia el jugador (Pitch en X).")]
    [Range(10f, 60f)]
    [SerializeField] private float lookAngleX = 25f;

    [Tooltip("Si es verdadero, la cámara apunta continuamente hacia el personaje.")]
    [SerializeField] private bool lookAtTarget = true;

    // Variables internas de suavizado
    private Vector3 currentVelocity = Vector3.zero;
    private float fixedBaseY;
    private Camera camComponent;
    private Coroutine fovCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        camComponent = GetComponent<Camera>();
        if (camComponent != null) normalFOV = camComponent.fieldOfView;
    }

    private void Start()
    {
        // Buscar automáticamente al jugador por etiqueta 'Player' si no fue asignado
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
        }

        // Registrar la altura base de la cámara para desacoplar saltos en Y
        if (target != null)
        {
            fixedBaseY = target.position.y + offset.y;
            // Posicionar de inmediato al inicio para evitar transiciones bruscas
            transform.position = CalculateTargetPosition();
            ApplyCameraRotation();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Calcular la posición ideal de seguimiento
        Vector3 desiredPosition = CalculateTargetPosition();

        // 2. Aplicar interpolación suave (SmoothDamp) para evitar jittering
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        // 3. Cumplimiento Criterio 4: Forzar distancia fija y constante en el eje Z
        if (lockFixedZDistance)
        {
            smoothedPosition.z = target.position.z + offset.z;
        }

        // 4. Cumplimiento Criterio 3: Altura estable desacoplada de los saltos del jugador en Y
        if (!followYAxis)
        {
            smoothedPosition.y = fixedBaseY;
        }

        // 5. Asignar la posición calculada
        transform.position = smoothedPosition;

        // 6. Orientación de cámara
        ApplyCameraRotation();
    }

    /// <summary>
    /// Calcula la posición objetivo sincronizando el desplazamiento en X y Z.
    /// </summary>
    private Vector3 CalculateTargetPosition()
    {
        float targetX = target.position.x + offset.x;
        float targetY = followYAxis ? (target.position.y + offset.y) : fixedBaseY;
        float targetZ = target.position.z + offset.z;

        return new Vector3(targetX, targetY, targetZ);
    }

    /// <summary>
    /// Aplica la rotación angular idónea para visión isométrica/tercera persona móvil.
    /// </summary>
    private void ApplyCameraRotation()
    {
        if (lookAtTarget)
        {
            // Apuntar ligeramente por encima del origen del personaje (hacia el pecho/cabeza)
            Vector3 lookTarget = target.position + Vector3.up * 1.2f;
            transform.LookAt(lookTarget);
        }
        else
        {
            transform.rotation = Quaternion.Euler(lookAngleX, 0f, 0f);
        }
    }

    /// <summary>
    /// Permite recalibrar el offset dinámicamente si se cambia de nivel o perspectiva.
    /// </summary>
    public void SetTarget(Transform newTarget, Vector3 newOffset)
    {
        target = newTarget;
        offset = newOffset;
        if (target != null)
        {
            fixedBaseY = target.position.y + offset.y;
        }
    }

    #region Game Feel (Actividad 4.6 - FOV Kick)
    /// <summary>
    /// Dispara un efecto cinemático de aceleración dinámica abriendo el campo de visión (FOV).
    /// </summary>
    public void TriggerSpeedKick(float duration)
    {
        if (camComponent == null) return;
        if (fovCoroutine != null) StopCoroutine(fovCoroutine);
        fovCoroutine = StartCoroutine(FovKickRoutine(duration));
    }

    private System.Collections.IEnumerator FovKickRoutine(float duration)
    {
        float elapsed = 0f;

        // 1. Abrir FOV rápidamente para impacto cinestésico
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            camComponent.fieldOfView = Mathf.Lerp(normalFOV, boostFOV, elapsed / 0.25f);
            yield return null;
        }
        camComponent.fieldOfView = boostFOV;

        // 2. Mantener FOV alto mientras dure la Súper Velocidad (máx 3 s)
        yield return new WaitForSeconds(Mathf.Max(0f, duration - 0.65f));

        // 3. Regresar suavemente al FOV nominal
        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            camComponent.fieldOfView = Mathf.Lerp(boostFOV, normalFOV, elapsed / 0.4f);
            yield return null;
        }
        camComponent.fieldOfView = normalFOV;
        fovCoroutine = null;
    }
    #endregion
}
