using UnityEngine;

/// <summary>
/// Componente de auto-recuperación estética y corrección de shaders en tiempo real.
/// Se ejecuta automáticamente tanto en el Editor como en Modo Juego (Play Mode).
/// Escanea todos los renderizadores 3D del escenario y sustituye cualquier shader morado/magenta
/// (#FF00FF o InternalErrorShader) por el shader nativo 'Standard' con su paleta de color correspondiente,
/// garantizando que la pista y el personaje NUNCA se vean morados ni blancos sin importar el estado del proyecto.
/// </summary>
[ExecuteAlways]
public class SceneMaterialAutoFixer3D : MonoBehaviour
{
    private void Awake()
    {
        FixAllSceneMaterials();
    }

    private void Start()
    {
        FixAllSceneMaterials();
    }

    private void OnEnable()
    {
        FixAllSceneMaterials();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying && Time.frameCount % 120 == 0)
        {
            FixAllSceneMaterials();
        }
    }
#endif

    [ContextMenu("Forzar Corrección de Materiales (Eliminar Morado)")]
    public void FixAllSceneMaterials()
    {
        MaterialHelper3D.InvalidateCache();
        Shader safeShader = MaterialHelper3D.GetSafeShader();

        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (Renderer r in allRenderers)
        {
            if (r == null) continue;
            // Ignorar renderizadores de UI (CanvasRenderer no hereda de Renderer) o TrailRenderer
            if (r is TrailRenderer || r is ParticleSystemRenderer) continue;

            Material[] mats = Application.isPlaying ? r.materials : r.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null || m.shader == null || NeedsShaderFix(m.shader))
                {
                    Color targetColor = ResolveIntendedColor(r.gameObject.name, m != null ? m.color : Color.white);
                    float smoothness = ResolveSmoothness(r.gameObject.name);

                    Material fixedMat = MaterialHelper3D.CreateSafeMaterial($"Mat_{r.gameObject.name}", targetColor, smoothness);
                    mats[i] = fixedMat;
                    changed = true;
                    fixedCount++;
                }
            }

            if (changed)
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
            Debug.Log($"[SceneMaterialAutoFixer3D] Se corrigieron exitosamente {fixedCount} materiales morados asignando shader '{safeShader.name}' y colores de alto contraste.");
        }
    }

    private bool NeedsShaderFix(Shader s)
    {
        if (s == null) return true;
        string sName = s.name;

        // Si es shader de error interno
        if (sName.Contains("InternalError") || sName.Contains("Error") || sName.Contains("Hidden/InternalErrorShader"))
        {
            return true;
        }

        // Si el proyecto corre en Built-in pero el material tiene shader URP, Unity no lo puede renderizar (se ve morado)
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null && sName.Contains("Universal Render Pipeline"))
        {
            return true;
        }

        return false;
    }

    private Color ResolveIntendedColor(string objName, Color fallback)
    {
        string lower = objName.ToLower();

        if (lower.Contains("floor") || lower.Contains("piso") || lower.Contains("ground") || lower.Contains("track"))
        {
            return new Color(0.12f, 0.16f, 0.22f); // Azul pizarra oscuro elegante
        }
        if (lower.Contains("ramp") || lower.Contains("rampa"))
        {
            return new Color(0.02f, 0.75f, 0.88f); // Cian eléctrico brillante
        }
        if (lower.Contains("platform") || lower.Contains("plataforma"))
        {
            return new Color(0.08f, 0.75f, 0.45f); // Verde esmeralda
        }
        if (lower.Contains("hazard") || lower.Contains("obstacle") || lower.Contains("obstaculo") || lower.Contains("obs"))
        {
            return new Color(0.92f, 0.20f, 0.25f); // Rojo carmín de peligro
        }
        if (lower.Contains("band"))
        {
            return new Color(0.06f, 0.09f, 0.16f); // Banda azul noche oscuro
        }
        if (lower.Contains("player") || lower.Contains("ball") || lower.Contains("pelota") || lower.Contains("esfera"))
        {
            return new Color(1.0f, 0.72f, 0.05f); // Dorado solar vibrante
        }
        if (lower.Contains("goal") || lower.Contains("portal") || lower.Contains("meta"))
        {
            return new Color(1.0f, 0.85f, 0.10f); // Dorado resplandeciente
        }
        if (lower.Contains("boost") || lower.Contains("speed") || lower.Contains("pickup"))
        {
            return new Color(1.0f, 0.90f, 0.10f); // Amarillo energía
        }

        // Si el fallback no es magenta ni negro absoluto, conservarlo
        if (fallback.r > 0.9f && fallback.b > 0.9f && fallback.g < 0.1f)
        {
            return new Color(0.12f, 0.16f, 0.22f);
        }

        return fallback;
    }

    private float ResolveSmoothness(string objName)
    {
        string lower = objName.ToLower();
        if (lower.Contains("player") || lower.Contains("ball")) return 0.85f;
        if (lower.Contains("goal") || lower.Contains("boost")) return 0.95f;
        if (lower.Contains("ramp")) return 0.50f;
        return 0.25f;
    }
}
