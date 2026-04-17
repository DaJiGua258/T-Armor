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
                // 转换到屏幕像素坐标
                float2 screenCoord = input.texcoord * _ScreenParams.xy;

                // 在屏幕像素空间 floor，保证块大小严格等于 _PixelSize
                float2 snappedCoord = floor(screenCoord / _PixelSize) * _PixelSize;

                // 采样像素块中心（+0.5 避免采样边界）
                float2 uv = (snappedCoord + 0.5) / _ScreenParams.xy;

                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
            }
            ENDHLSL 
        }
    }
}
