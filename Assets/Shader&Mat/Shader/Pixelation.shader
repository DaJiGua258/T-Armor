Shader "Custom/PostProcessing/Pixelation"
{
    Properties
    {
        _PixelSize ("Pixel Size", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PixelSize;

            half4 frag(Varyings input) : SV_Target
            {
                float2 pixelCount = _ScreenParams.xy / _PixelSize;
                float2 uv = floor(input.texcoord * pixelCount) / pixelCount;
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
            }
            ENDHLSL 
        }
    }
}
