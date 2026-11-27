using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de personalización estética y efectos visuales del personaje (Criterio 2 - Actividad 4.4 y 4.6).
/// - Gestiona la paleta cromática del personaje (esfera y banda ecuatorial).
/// - Administra el TrailRenderer para movimiento y Súper Velocidad (Game Feel).
/// - Emite partículas procedurales en saltos físicos y colisiones fatales.
/// - Sincroniza dinámicamente con LightingAtmosphereManager3D.
/// - Auto-cura materiales morados en tiempo real asignando shaders Standard de alto contraste.
/// </summary>
public class PlayerVisualCustomizer3D : MonoBehaviour
{
    public static PlayerVisualCustomizer3D Instance { get; private set; }

    [System.Serializable]
    public struct PlayerSkin
    {
        public string skinName;
        public Color primaryColor;       // Color principal de la esfera
        public Color bandColor;          // Color de la banda ecuatorial de contraste
        public Color trailStartColor;    // Color de inicio de la estela
        public Color trailEndColor;      // Color de desvanecimiento
        public Color glowLightColor;     // Luz de apoyo
    }

    [Header("Catálogo de Skins Disponibles")]
    [SerializeField] private List<PlayerSkin> availableSkins = new List<PlayerSkin>();

    [Header("Referencias de Render y Efectos")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private Renderer bandRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private UnityEngine.UI.Text skinButtonText;

    private int currentSkinIndex = 0;
    private Material playerInstanceMaterial;
    private Material bandInstanceMaterial;

    private const string PREF_KEY_SKIN = "PlayerSelectedSkin_Index";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureDefaultSkins();
        InitializeRenderersAndMaterials();
        SetupTrailRenderer();
        SetupProceduralParticles();
    }

    private void Start()
    {
        // 1. Auto-curar cualquier material morado o incompatible en la escena activa
        FixAnyPurpleMaterialsInScene();

        // 2. Cargar skin persistente previa
        currentSkinIndex = PlayerPrefs.GetInt(PREF_KEY_SKIN, 0);
        if (currentSkinIndex < 0 || currentSkinIndex >= availableSkins.Count)
        {
            currentSkinIndex = 0;
        }

        ApplySkin(currentSkinIndex);
    }

    private void Update()
    {
        // Atajo de teclado: pulsar tecla 'C' para alternar skin en tiempo real
        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleNextSkin();
        }
    }

    private void EnsureDefaultSkins()
    {
        if (availableSkins == null || availableSkins.Count == 0)
        {
            availableSkins = new List<PlayerSkin>
            {
                new PlayerSkin {
                    skinName = "Dorado Solar",
                    primaryColor = new Color(1.0f, 0.72f, 0.05f),
                    bandColor = new Color(0.06f, 0.09f, 0.16f),
                    trailStartColor = new Color(1.0f, 0.85f, 0.2f, 0.9f),
                    trailEndColor = new Color(1.0f, 0.4f, 0.0f, 0.0f),
                    glowLightColor = new Color(1.0f, 0.85f, 0.3f)
                },
                new PlayerSkin {
                    skinName = "Esmeralda Neón",
                    primaryColor = new Color(0.08f, 0.85f, 0.45f),
                    bandColor = new Color(0.04f, 0.15f, 0.10f),
                    trailStartColor = new Color(0.2f, 1.0f, 0.6f, 0.9f),
                    trailEndColor = new Color(0.0f, 0.6f, 0.3f, 0.0f),
                    glowLightColor = new Color(0.3f, 1.0f, 0.6f)
                },
                new PlayerSkin {
                    skinName = "Cian Plasma",
                    primaryColor = new Color(0.05f, 0.75f, 0.95f),
                    bandColor = new Color(0.02f, 0.12f, 0.22f),
                    trailStartColor = new Color(0.2f, 0.85f, 1.0f, 0.9f),
                    trailEndColor = new Color(0.0f, 0.3f, 0.8f, 0.0f),
                    glowLightColor = new Color(0.2f, 0.8f, 1.0f)
                },
                new PlayerSkin {
                    skinName = "Rubí Fuego",
                    primaryColor = new Color(0.95f, 0.15f, 0.20f),
                    bandColor = new Color(0.18f, 0.04f, 0.06f),
                    trailStartColor = new Color(1.0f, 0.3f, 0.2f, 0.9f),
                    trailEndColor = new Color(0.8f, 0.0f, 0.1f, 0.0f),
                    glowLightColor = new Color(1.0f, 0.3f, 0.3f)
                }
            };
        }
    }

    private void InitializeRenderersAndMaterials()
    {
        if (playerRenderer == null)
        {
            playerRenderer = GetComponent<Renderer>();
        }

        if (playerRenderer != null)
        {
            playerInstanceMaterial = MaterialHelper3D.CreateSafeMaterial("Mat_Player_Custom", new Color(1.0f, 0.72f, 0.05f), 0.85f);
            playerRenderer.material = playerInstanceMaterial;
        }

        // Buscar banda ecuatorial hija
        Transform bandChild = transform.Find("Player_Band");
        if (bandChild != null)
        {
            bandRenderer = bandChild.GetComponent<Renderer>();
            if (bandRenderer != null)
            {
                bandInstanceMaterial = MaterialHelper3D.CreateSafeMaterial("Mat_PlayerBand_Custom", new Color(0.06f, 0.09f, 0.16f), 0.9f);
                bandRenderer.material = bandInstanceMaterial;
            }
        }
    }

    private void SetupTrailRenderer()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
        }

        trailRenderer.time = 0.45f;
        trailRenderer.startWidth = 0.8f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.minVertexDistance = 0.1f;
        trailRenderer.autodestruct = false;

        // Material seguro para el trail
        Material trailMat = MaterialHelper3D.CreateSafeMaterial("Mat_Player_Trail", Color.white, 0.5f);
        trailRenderer.material = trailMat;
    }

    private void SetupProceduralParticles()
    {
        // 1. Partículas de Salto (Jump)
        if (jumpParticles == null)
        {
            Transform existingJump = transform.Find("JumpParticles");
            if (existingJump != null)
            {
                jumpParticles = existingJump.GetComponent<ParticleSystem>();
            }
            else
            {
                GameObject jumpObj = new GameObject("JumpParticles");
                jumpObj.transform.SetParent(transform);
                jumpObj.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                jumpObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                jumpParticles = jumpObj.AddComponent<ParticleSystem>();
                ConfigureParticleSystem(jumpParticles, 1.2f, 25, 0.35f, 4.0f);
            }
        }

        // 2. Partículas de Impacto (Death)
        if (deathParticles == null)
        {
            Transform existingDeath = transform.Find("DeathParticles");
            if (existingDeath != null)
            {
                deathParticles = existingDeath.GetComponent<ParticleSystem>();
            }
            else
            {
                GameObject deathObj = new GameObject("DeathParticles");
                deathObj.transform.SetParent(transform);
                deathObj.transform.localPosition = Vector3.zero;

                deathParticles = deathObj.AddComponent<ParticleSystem>();
                ConfigureParticleSystem(deathParticles, 2.5f, 60, 0.6f, 7.0f);
            }
        }
    }

    private void ConfigureParticleSystem(ParticleSystem ps, float size, int burstCount, float lifetime, float speed)
    {
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, burstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.material = MaterialHelper3D.CreateSafeMaterial("Mat_Particle_Safe", Color.white);
        }
    }

    /// <summary>
    /// Cambia cíclicamente a la siguiente skin del personaje y guarda la elección.
    /// Puede vincularse a un botón OnClick en la UI táctil.
    /// </summary>
    public void CycleNextSkin()
    {
        if (availableSkins == null || availableSkins.Count == 0) return;

        currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Count;
        ApplySkin(currentSkinIndex);
    }

    /// <summary>
    /// Aplica integralmente una skin al personaje modificando materiales, luces, estela y UI.
    /// (Criterio 2 de evaluación de Moodle).
    /// </summary>
    public void ApplySkin(int index)
    {
        if (index < 0 || index >= availableSkins.Count) return;

        currentSkinIndex = index;
        PlayerSkin skin = availableSkins[index];

        // 1. Modificar color de la esfera
        if (playerInstanceMaterial != null)
        {
            MaterialHelper3D.ApplyColorToMaterial(playerInstanceMaterial, skin.primaryColor);
        }
        else if (playerRenderer != null)
        {
            MaterialHelper3D.ApplyColorToMaterial(playerRenderer.material, skin.primaryColor);
        }

        // 2. Modificar color de la banda ecuatorial
        if (bandInstanceMaterial != null)
        {
            MaterialHelper3D.ApplyColorToMaterial(bandInstanceMaterial, skin.bandColor);
        }
        else if (bandRenderer != null)
        {
            MaterialHelper3D.ApplyColorToMaterial(bandRenderer.material, skin.bandColor);
        }

        // 3. Modificar la estela tridimensional (TrailRenderer)
        if (trailRenderer != null)
        {
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(skin.trailStartColor, 0.0f),
                    new GradientColorKey(skin.trailEndColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(skin.trailStartColor.a, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trailRenderer.colorGradient = grad;
        }

        // 4. Sincronizar luz de apoyo del jugador
        if (LightingAtmosphereManager3D.Instance != null)
        {
            LightingAtmosphereManager3D.Instance.SetPlayerGlowColor(skin.glowLightColor);
        }

        // 5. Actualizar etiqueta en botón de interfaz
        if (skinButtonText != null)
        {
            skinButtonText.text = $"SKIN: {skin.skinName.ToUpper()}";
        }

        // 6. Persistencia
        PlayerPrefs.SetInt(PREF_KEY_SKIN, index);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerVisualCustomizer3D] ¡Skin aplicada! {skin.skinName} | Color: {skin.primaryColor}");
    }

    /// <summary>
    /// Emite una ráfaga de partículas luminosas al ejecutar un salto físico en el eje Y.
    /// </summary>
    public void PlayJumpParticles()
    {
        if (jumpParticles != null)
        {
            jumpParticles.Play();
        }
    }

    /// <summary>
    /// Emite una explosión volumétrica de partículas al colisionar fatalmente con un obstáculo.
    /// </summary>
    public void PlayDeathParticles()
    {
        if (deathParticles != null)
        {
            deathParticles.transform.position = transform.position;
            deathParticles.Play();
        }
    }

    /// <summary>
    /// Potencia y expande la estela visual durante la Súper Velocidad (Criterio 2 - Actividad 4.6).
    /// </summary>
    public void EnableSuperSpeedTrail(float duration)
    {
        if (trailRenderer == null) return;
        StartCoroutine(SuperSpeedTrailRoutine(duration));
    }

    private IEnumerator SuperSpeedTrailRoutine(float duration)
    {
        float originalStartWidth = trailRenderer.startWidth;
        float originalTime = trailRenderer.time;
        Gradient originalGradient = trailRenderer.colorGradient;

        trailRenderer.startWidth = 1.6f;
        trailRenderer.time = 0.75f;

        Gradient speedGrad = new Gradient();
        speedGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1.0f, 0.95f, 0.2f), 0.0f),
                new GradientColorKey(new Color(1.0f, 0.45f, 0.0f), 0.5f),
                new GradientColorKey(new Color(1.0f, 0.1f, 0.5f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trailRenderer.colorGradient = speedGrad;

        yield return new WaitForSeconds(duration);

        trailRenderer.startWidth = originalStartWidth;
        trailRenderer.time = originalTime;
        trailRenderer.colorGradient = originalGradient;
    }

    /// <summary>
    /// Escanea el escenario en busca de materiales morados (#FF00FF o URP en Built-in)
    /// y los sustituye inmediatamente por materiales con shaders Standard de alto contraste.
    /// </summary>
    private void FixAnyPurpleMaterialsInScene()
    {
        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (Renderer r in allRenderers)
        {
            if (r == null || r is TrailRenderer || r is ParticleSystemRenderer) continue;

            Material[] mats = Application.isPlaying ? r.materials : r.sharedMaterials;
            bool fixedAny = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null || m.shader == null || m.shader.name.Contains("InternalError") || m.shader.name.Contains("Universal Render Pipeline"))
                {
                    Color col = ResolveColorByName(r.gameObject.name);
                    mats[i] = MaterialHelper3D.CreateSafeMaterial($"Mat_{r.gameObject.name}", col);
                    fixedAny = true;
                    fixedCount++;
                }
            }

            if (fixedAny)
            {
                if (Application.isPlaying)
                {
                    r.materials = mats;
                }
                else
                {
                    r.sharedMaterials = mats;
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"[PlayerVisualCustomizer3D] ¡Escenario auto-curado! {fixedCount} materiales morados corregidos a shader Standard con colores de alto contraste.");
        }
    }

    private Color ResolveColorByName(string objName)
    {
        string lower = objName.ToLower();
        if (lower.Contains("floor") || lower.Contains("piso") || lower.Contains("ground") || lower.Contains("track"))
        {
            return new Color(0.12f, 0.16f, 0.22f); // Azul pizarra oscuro
        }
        if (lower.Contains("ramp") || lower.Contains("rampa"))
        {
            return new Color(0.02f, 0.75f, 0.88f); // Cian eléctrico
        }
        if (lower.Contains("platform") || lower.Contains("plataforma"))
        {
            return new Color(0.08f, 0.75f, 0.45f); // Verde esmeralda
        }
        if (lower.Contains("hazard") || lower.Contains("obstacle") || lower.Contains("obstaculo") || lower.Contains("obs"))
        {
            return new Color(0.92f, 0.20f, 0.25f); // Rojo carmín
        }
        if (lower.Contains("band"))
        {
            return new Color(0.06f, 0.09f, 0.16f); // Azul noche profundo
        }
        if (lower.Contains("goal") || lower.Contains("portal"))
        {
            return new Color(1.0f, 0.85f, 0.10f); // Dorado brillante
        }
        if (lower.Contains("boost") || lower.Contains("speed"))
        {
            return new Color(1.0f, 0.90f, 0.10f); // Amarillo energía
        }

        return new Color(1.0f, 0.72f, 0.05f); // Dorado solar
    }
}
