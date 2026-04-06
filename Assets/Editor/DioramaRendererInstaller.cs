#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class DioramaRendererInstaller
{
    private const string ShaderPath = "Assets/Shaders/TiltShiftDiorama.shader";
    private const string MaterialPath = "Assets/Settings/TiltShiftDiorama.mat";
    private const string FullScreenFeatureName = "Tilt Shift Diorama Full Screen";

    static DioramaRendererInstaller()
    {
        EditorApplication.delayCall += EnsureInstalled;
    }

    [MenuItem("Tools/Diorama/Install Tilt Shift Renderer Feature")]
    public static void EnsureInstalled()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "TiltShiftDiorama"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        string[] rendererAssetPaths =
        {
            "Assets/Settings/PC_Renderer.asset",
            "Assets/Settings/Mobile_Renderer.asset"
        };

        foreach (string rendererPath in rendererAssetPaths)
        {
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                continue;
            }

            TiltShiftRendererFeature legacyFeature = rendererData.rendererFeatures.OfType<TiltShiftRendererFeature>().FirstOrDefault();
            if (legacyFeature != null)
            {
                legacyFeature.SetShader(shader);
                legacyFeature.SetEffectEnabled(false);
                legacyFeature.SetActive(false);
                EditorUtility.SetDirty(legacyFeature);
            }

            FullScreenPassRendererFeature fullScreenFeature = rendererData.rendererFeatures
                .OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(feature => feature.name == FullScreenFeatureName);

            if (fullScreenFeature == null)
            {
                fullScreenFeature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                fullScreenFeature.name = FullScreenFeatureName;
                AssetDatabase.AddObjectToAsset(fullScreenFeature, rendererData);
                rendererData.rendererFeatures.Add(fullScreenFeature);
            }

            fullScreenFeature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            fullScreenFeature.fetchColorBuffer = true;
            fullScreenFeature.requirements = ScriptableRenderPassInput.None;
            fullScreenFeature.passMaterial = material;
            fullScreenFeature.passIndex = 0;
            fullScreenFeature.bindDepthStencilAttachment = false;
            fullScreenFeature.SetActive(true);
            fullScreenFeature.Create();

            EditorUtility.SetDirty(fullScreenFeature);
            EditorUtility.SetDirty(rendererData);
            InvokeSetDirty(rendererData);
        }

        AssetDatabase.SaveAssets();
    }

    private static void InvokeSetDirty(ScriptableRendererData rendererData)
    {
        MethodInfo setDirtyMethod = typeof(ScriptableRendererData).GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        setDirtyMethod?.Invoke(rendererData, null);
    }
}
#endif
