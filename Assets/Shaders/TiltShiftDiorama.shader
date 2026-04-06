Shader "Hidden/Diorama/TiltShift"
{
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
                float2 blurOffset = texelOffset * (0.45 + mask * _BlurStrength);

                float4 blurredColor = sharpColor * 0.24;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( blurOffset.x, 0.0)) * 0.12;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-blurOffset.x, 0.0)) * 0.12;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0,  blurOffset.y)) * 0.12;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0.0, -blurOffset.y)) * 0.12;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + blurOffset) * 0.10;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - blurOffset) * 0.10;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( blurOffset.x, -blurOffset.y)) * 0.09;
                blurredColor += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-blurOffset.x,  blurOffset.y)) * 0.09;

                return lerp(sharpColor, blurredColor, saturate(mask));
            }
            ENDHLSL
        }
    }
}
