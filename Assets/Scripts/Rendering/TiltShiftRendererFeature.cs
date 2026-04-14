using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

#pragma warning disable CS0108
#pragma warning disable CS0618
#pragma warning disable CS0672

public sealed class TiltShiftRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private bool effectEnabled;

    [Serializable]
    public sealed class TiltShiftSettings
    {
        [Range(0f, 1f)] public float focusCenter = 0.53f;
        [Range(0.01f, 0.4f)] public float focusWidth = 0.22f;
        [Range(0.01f, 0.35f)] public float falloff = 0.16f;
        [Range(0f, 2f)] public float blurStrength = 0.50f;
        [Range(0.5f, 4f)] public float blurRadius = 0.90f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    [SerializeField] private Shader shader;
    [SerializeField] private TiltShiftSettings settings = new();

    private Material material;
    private TiltShiftPass pass;

    public override void Create()
    {
        if (shader == null)
        {
            shader = Shader.Find("Hidden/Diorama/TiltShift");
        }

        if (shader != null && (material == null || material.shader != shader))
        {
            material = CoreUtils.CreateEngineMaterial(shader);
        }

        pass ??= new TiltShiftPass(settings);
        pass.renderPassEvent = settings.renderPassEvent;
        pass.SetMaterial(material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!effectEnabled || material == null || renderingData.cameraData.isPreviewCamera || renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        if (!renderingData.cameraData.postProcessEnabled)
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    public void SetShader(Shader newShader)
    {
        shader = newShader;
    }

    public void SetEffectEnabled(bool isEnabled)
    {
        effectEnabled = isEnabled;
    }

    public void ApplyPreset(float focusCenter, float focusWidth, float falloff, float blurStrength, float blurRadius)
    {
        settings.focusCenter = focusCenter;
        settings.focusWidth = focusWidth;
        settings.falloff = falloff;
        settings.blurStrength = blurStrength;
        settings.blurRadius = blurRadius;
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;

        if (material != null)
        {
            CoreUtils.Destroy(material);
            material = null;
        }
    }

    private sealed class TiltShiftPass : ScriptableRenderPass
    {
        private static readonly int FocusCenterId = Shader.PropertyToID("_FocusCenter");
        private static readonly int FocusWidthId = Shader.PropertyToID("_FocusWidth");
        private static readonly int FocusFalloffId = Shader.PropertyToID("_FocusFalloff");
        private static readonly int BlurStrengthId = Shader.PropertyToID("_BlurStrength");
        private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");

        private readonly TiltShiftSettings settings;
        private readonly ProfilingSampler profilingSampler = new("TiltShift Diorama");

        private Material material;

        public TiltShiftPass(TiltShiftSettings settings)
        {
            this.settings = settings;
        }

        public void SetMaterial(Material newMaterial)
        {
            material = newMaterial;
            requiresIntermediateTexture = material != null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.cameraType != CameraType.Game || resourceData.isActiveTargetBackBuffer)
            {
                return;
            }

            material.SetFloat(FocusCenterId, settings.focusCenter);
            material.SetFloat(FocusWidthId, settings.focusWidth);
            material.SetFloat(FocusFalloffId, settings.falloff);
            material.SetFloat(BlurStrengthId, settings.blurStrength);
            material.SetFloat(BlurRadiusId, settings.blurRadius);

            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid())
            {
                return;
            }

            TextureDesc tempDesc = renderGraph.GetTextureDesc(source);
            tempDesc.name = "_TiltShiftCameraColorTemp";
            tempDesc.clearBuffer = false;
            TextureHandle tempTexture = renderGraph.CreateTexture(tempDesc);

            RenderGraphUtils.BlitMaterialParameters effectParameters = new(source, tempTexture, material, 0);
            renderGraph.AddBlitPass(effectParameters, "TiltShift Diorama Effect");

            RenderGraphUtils.BlitMaterialParameters copyBackParameters = new(tempTexture, source, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
            renderGraph.AddBlitPass(copyBackParameters, "TiltShift Diorama CopyBack");
        }

        public void Dispose() { }
    }
}

#pragma warning restore CS0672
#pragma warning restore CS0618
#pragma warning restore CS0108
