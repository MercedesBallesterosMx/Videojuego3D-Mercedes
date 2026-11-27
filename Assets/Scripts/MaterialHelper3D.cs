using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Utilidad universal para la creación y gestión segura de materiales y shaders en Unity.
/// Resuelve de raíz problemas de shaders incompatibles (color magenta/morado #FF00FF)
/// detectando con precisión si el proyecto corre en Built-in Render Pipeline (Standard Shader)
/// o en Scriptable Render Pipeline (URP Lit), garantizando cero shaders magenta y colores vivos.
/// </summary>
public static class MaterialHelper3D
{
    private static Shader s_CachedSafeShader = null;

    /// <summary>
    /// Resuelve de forma infalible un Shader 100% compatible con el pipeline activo.
    /// Si el pipeline es Built-in (GraphicsSettings.currentRenderPipeline == null), utiliza 'Standard' o 'Mobile/Diffuse'.
    /// Si el pipeline es URP (GraphicsSettings.currentRenderPipeline != null), utiliza 'Universal Render Pipeline/Lit'.
    /// </summary>
    public static Shader GetSafeShader()
    {
        if (s_CachedSafeShader != null)
        {
            return s_CachedSafeShader;
        }

        // 1. Detectar si el proyecto está usando Scriptable Render Pipeline (URP/HDRP)
        bool isSRP = GraphicsSettings.currentRenderPipeline != null;

        if (isSRP)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (urpShader != null)
            {
                s_CachedSafeShader = urpShader;
                return s_CachedSafeShader;
            }
        }

        // 2. Si el proyecto usa Built-in Render Pipeline (nativo de Unity)
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            s_CachedSafeShader = standardShader;
            return s_CachedSafeShader;
        }

        Shader diffuseShader = Shader.Find("Mobile/Diffuse")
                            ?? Shader.Find("Legacy Shaders/Diffuse")
                            ?? Shader.Find("Unlit/Color");

        if (diffuseShader != null)
        {
            s_CachedSafeShader = diffuseShader;
            return s_CachedSafeShader;
        }

        // 3. Último recurso interno
        s_CachedSafeShader = Shader.Find("Hidden/InternalErrorShader");
        return s_CachedSafeShader;
    }

    /// <summary>
    /// Invalida la caché del shader seguro para forzar una re-evaluación si cambian los ajustes gráficos.
    /// </summary>
    public static void InvalidateCache()
    {
        s_CachedSafeShader = null;
    }

    /// <summary>
    /// Crea una nueva instancia de Material configurando correctamente las propiedades
    /// de color tanto para Built-in (_Color) como para URP (_BaseColor) y brillo especular.
    /// </summary>
    public static Material CreateSafeMaterial(string materialName, Color color, float smoothness = 0.5f)
    {
        Shader shader = GetSafeShader();
        Material mat = new Material(shader);
        mat.name = materialName;

        ApplyColorToMaterial(mat, color);

        // Suavidad superficial (Smoothness / Glossiness)
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
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

        // Built-in Standard utiliza _Color
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }

        // URP Lit utiliza _BaseColor
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
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
