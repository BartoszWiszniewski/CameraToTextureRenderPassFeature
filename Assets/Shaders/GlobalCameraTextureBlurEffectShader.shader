Shader "Custom/GlobalCameraTextureBlurEffectShader"
{
     Properties
    {
        _BlurSize ("Blur Size", Range(0.0, 20.0)) = 2.0
        _BlurRadius ("Blur Radius", Range(0.0, 8.0)) = 4.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);
            float _BlurSize;
            float _BlurRadius;

            half4 Frag (Varyings input) : SV_Target
            {
                half4 color = half4(0,0,0,0);
                float step = _BlurSize / 1000.0;

                int radius = (int)_BlurRadius;
                
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.texcoord + float2(x, y) * step);
                    }
                }

                float kernel = pow((float)(radius * 2 + 1), 2);
                color /= kernel;
                return color;
            }
            ENDHLSL
        }

    }
}
