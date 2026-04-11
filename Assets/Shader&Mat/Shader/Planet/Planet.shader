Shader "Custom/PlanetFlatSurface_V3"
{
    Properties
    {
        [HideInInspector] _GradientTexture ("Gradient", 2D) = "white" {}

        [Header(Night)]
        _SeaColorNight ("SeaColorNight", Color) = (0.5, 0.5, 0.5, 1)
        _LandColorNight ("LandColorNight", Color) = (0.2, 0.2, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #define _MAIN_LIGHT_SHADOWS  // 启用阴影图采样
                
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS             // 接收阴影 
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE     // 投射阴影
        #pragma multi_compile _ _SHADOWS_SOFT                   // 软阴影
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { 
                float4 positionCS : SV_POSITION; 
                float3 positionWS : TEXCOORD0;
                float3 uv3d : TEXCOORD1; 
                float3 normalWS : TEXCOORD2;
            };

            TEXTURE3D(_HeightNoise); SAMPLER(sampler_HeightNoise);
            TEXTURE3D(_MoistureNoise); SAMPLER(sampler_MoistureNoise);
            TEXTURE2D(_GradientTexture); SAMPLER(sampler_GradientTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _SeaColor; float _SeaLevel;
                float4 _DryColor; float4 _WetColor;
                float _MoistureThreshold; float _ClimateMixStrength;
                float4 _PoleColor; float _PoleThreshold; float _PoleStrength;

                float4 _LandColorNight;
                float4 _SeaColorNight;
            CBUFFER_END

            Varyings vert (Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv3d = input.positionOS.xyz * 0.5 + 0.5;
                output.normalWS = TransformObjectToWorldNormal(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target 
            {
                float hNoise = SAMPLE_TEXTURE3D(_HeightNoise, sampler_HeightNoise, input.uv3d).r;
                float mNoise = SAMPLE_TEXTURE3D(_MoistureNoise, sampler_MoistureNoise, input.uv3d).r;

                float isLand = step(_SeaLevel, hNoise);
                half4 surfaceColor;

                if (isLand < 0.5) 
                {
                    surfaceColor = _SeaColor;
                } 
                else 
                {
                    float landH = (hNoise - _SeaLevel) / (1.0 - _SeaLevel);
                    half4 baseColor = SAMPLE_TEXTURE2D(_GradientTexture, sampler_GradientTexture, float2(landH, 0.5));
                    
                    // 硬边缘干湿混合
                    float isWet = step(_MoistureThreshold, mNoise);
                    half4 climateColor = lerp(_DryColor, _WetColor, isWet);
                    surfaceColor = lerp(baseColor, climateColor, _ClimateMixStrength);
                }

                // 极地 (硬边缘覆盖)
                float3 localPos = (input.uv3d - 0.5) * 2.0;
                float distY = abs(localPos.y) + (hNoise - 0.5) * 0.5;
                float poleMask = step(_PoleThreshold, distY);
                surfaceColor = lerp(surfaceColor, _PoleColor, poleMask * _PoleStrength);
                
                Light light = GetMainLight();  // 获取主光源结构体

                // 漫反射颜色
                float lDotN = dot(light.direction, normalize(input.normalWS)) * 0.5 + 0.5;

                half4 diffuseColor = lDotN > 0.4 ? 1 : 0.5f;

                surfaceColor = lDotN > 0.4 ? surfaceColor :
                    (isLand > 0.5 ? _LandColorNight : _SeaColorNight);

                surfaceColor = (lDotN - 0.4) * (lDotN - 0.45) > 0 ? surfaceColor :
                (isLand > 0.5 ? _LandColorNight : _SeaColorNight);

                surfaceColor *= diffuseColor;

                return half4(surfaceColor.xyz, 1.0);
            }
            ENDHLSL
        }
    }
}