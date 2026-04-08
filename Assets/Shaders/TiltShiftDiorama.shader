Shader "Hidden/Diorama/TiltShift"
{
    Properties
    {
        _FocusCenter("Focus Center", Range(0, 1)) = 0.53
        _FocusWidth("Focus Width", Range(0.01, 0.4)) = 0.3
        _FocusFalloff("Focus Falloff", Range(0.01, 0.35)) = 0.08
        _BlurStrength("Blur Strength", Range(0, 2)) = 0.16
        _BlurRadius("Blur Radius", Range(0.5, 4)) = 0.58
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "TiltShift"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _FocusCenter;
            float _FocusWidth;
            float _FocusFalloff;
            float _BlurStrength;
            float _BlurRadius;

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float4 sharpColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float dist = abs(uv.y - _FocusCenter);
                float mask = smoothstep(_FocusWidth, _FocusWidth + max(_FocusFalloff, 0.0001), dist);

                float2 texelOffset = _BlitTexture_TexelSize.xy * max(_BlurRadius, 0.001);
                float2 blurOffset = texelOffset * (0.28 + mask * _BlurStrength);

                float4 blurredColor = sharpColor * 0.30;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( blurOffset.x, 0.0)) * 0.105;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-blurOffset.x, 0.0)) * 0.105;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0,  blurOffset.y)) * 0.105;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0, -blurOffset.y)) * 0.105;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + blurOffset) * 0.08;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - blurOffset) * 0.08;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( blurOffset.x, -blurOffset.y)) * 0.06;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-blurOffset.x,  blurOffset.y)) * 0.06;

                return lerp(sharpColor, blurredColor, saturate(mask));
            }
            ENDHLSL
        }
    }
}
