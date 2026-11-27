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
    /// Primero consulta el shader que Unity asigna nativamente a un primitivo (Universal Lit en URP o Standard en Built-in).
    /// Si no está disponible, prueba secuencialmente los shaders estándar de la industria.
    /// </summary>
    public static Shader GetSafeShader()
    {
        if (s_CachedSafeShader != null)
        {
            return s_CachedSafeShader;
        }

        // 1. Método infalible: Consultar el shader que Unity asigna por defecto a un primitivo en el pipeline activo
        try
        {
            GameObject tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer r = tempPrimitive.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null)
            {
                s_CachedSafeShader = r.sharedMaterial.shader;
            }
            Object.DestroyImmediate(tempPrimitive);
        }
        catch
        {
            // Fallback si CreatePrimitive fallase en entornos sin simulación
        }

        if (s_CachedSafeShader != null)
        {
            return s_CachedSafeShader;
        }

        // 2. Si no se obtuvo mediante el primitivo, buscar candidatos por orden de prioridad técnica
        string[] candidates = new string[]
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Standard",
            "Mobile/Diffuse",
            "Legacy Shaders/Diffuse",
            "Unlit/Color",
            "UI/Default"
        };

        foreach (string candidate in candidates)
        {
            Shader foundShader = Shader.Find(candidate);
            if (foundShader != null)
            {
                s_CachedSafeShader = foundShader;
                return s_CachedSafeShader;
            }
        }

        // 3. Último recurso interno
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
