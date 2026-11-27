using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Utilidad universal para la creación y gestión segura de materiales y shaders en Unity.
/// Resuelve de raíz problemas de shaders incompatibles (color magenta/fucsia #FF00FF)
/// garantizando compatibilidad automática tanto en Built-in Render Pipeline como en URP (Universal Render Pipeline).
/// </summary>
public static class MaterialHelper3D
{
    private static Shader s_CachedSafeShader = null;

    /// <summary>
    /// Resuelve de forma infalible un Shader compatible con el proyecto actual.
    /// Prioriza 'Universal Render Pipeline/Lit' para proyectos URP (estándar en Unity 6),
    /// asegurando que ningún objeto renderice con el color magenta de error (#FF00FF).
    /// </summary>
    public static Shader GetSafeShader()
    {
        if (s_CachedSafeShader != null)
        {
            return s_CachedSafeShader;
        }

        // 1. En Unity 6 y proyectos modernos con URP, buscar prioritariamente los shaders de Universal RP
        string[] urpCandidates = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit"
        };

        foreach (string candidate in urpCandidates)
        {
            Shader found = Shader.Find(candidate);
            if (found != null && found.isSupported)
            {
                s_CachedSafeShader = found;
                return s_CachedSafeShader;
            }
        }

        // Si se encontraron pero isSupported no devolvió true (contexto de importación), usar el primer URP Lit disponible
        foreach (string candidate in urpCandidates)
        {
            Shader found = Shader.Find(candidate);
            if (found != null)
            {
                s_CachedSafeShader = found;
                return s_CachedSafeShader;
            }
        }

        // 2. Si no es URP, consultar primitivo por defecto de Unity
        try
        {
            GameObject tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer r = tempPrimitive.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null)
            {
                Shader primShader = r.sharedMaterial.shader;
                if (!primShader.name.Contains("InternalErrorShader"))
                {
                    s_CachedSafeShader = primShader;
                }
            }
            Object.DestroyImmediate(tempPrimitive);
        }
        catch
        {
            // Ignorar fallos de CreatePrimitive en contextos sin simulación
        }

        if (s_CachedSafeShader != null)
        {
            return s_CachedSafeShader;
        }

        // 3. Fallbacks para Built-in Render Pipeline clásico
        string[] builtInCandidates = new string[]
        {
            "Standard",
            "Mobile/Diffuse",
            "Legacy Shaders/Diffuse",
            "Unlit/Color",
            "UI/Default"
        };

        foreach (string candidate in builtInCandidates)
        {
            Shader found = Shader.Find(candidate);
            if (found != null)
            {
                s_CachedSafeShader = found;
                return s_CachedSafeShader;
            }
        }

        s_CachedSafeShader = Shader.Find("Hidden/InternalErrorShader");
        return s_CachedSafeShader;
    }

    /// <summary>
    /// Crea una nueva instancia de Material configurando correctamente las propiedades
    /// de color tanto para URP (_BaseColor) como para Built-in (_Color) y brillo especular.
    /// </summary>
    public static Material CreateSafeMaterial(string materialName, Color color, float smoothness = 0.5f)
    {
        Shader shader = GetSafeShader();
        Material mat = new Material(shader);
        mat.name = materialName;

        ApplyColorToMaterial(mat, color);

        // Suavidad superficial (Smoothness / Glossiness)
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.05f);

        return mat;
    }

    /// <summary>
    /// Asigna de manera segura el color a un material reconociendo las palabras clave de los pipelines.
    /// </summary>
    public static void ApplyColorToMaterial(Material mat, Color color)
    {
        if (mat == null) return;

        mat.color = color;

        // URP Lit utiliza _BaseColor
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        // Built-in Standard utiliza _Color
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
    }

    /// <summary>
    /// Asigna de manera segura el color de emisión para materiales que brillan en la oscuridad o bajo iluminación 3D.
    /// </summary>
    public static void ApplyEmissionToMaterial(Material mat, Color emissionColor)
    {
        if (mat == null) return;

        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", emissionColor);
        }
    }
}
