Shader "Custom/PlanetClouds_Final"
{
    Properties
    {
        _NoiseTexture ("Noise 3D", 3D) = "white" {}
        _GradientTexture ("Gradient 1D", 2D) = "white" {}
        _CloudSpeed ("Cloud Speed", Float) = 0.05
        _CloudScale2 ("Detail Scale", Float) = 2.0
        _LandColor ("Land Color", Color) = (0,0,0,1)

        // 隐藏控制变量，由 C# 驱动
        [HideInInspector] _CloudCoverage ("Coverage", Range(0, 1)) = 0.5
        [HideInInspector] _CloudSoftness ("Softness", Range(0.01, 0.5)) = 0.1
        [HideInInspector] _CloudClip ("Clip", Range(0, 1)) = 0.3
        [HideInInspector] _CloudDir ("Direction", Vector) = (1, 0, 0, 0)
        [HideInInspector] _CloudHeight ("Height", Float) = 1.0
    }

    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off 

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posOS      : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 uv3d       : TEXCOORD2; 
            };

            TEXTURE3D(_NoiseTexture); SAMPLER(sampler_NoiseTexture);
            TEXTURE2D(_GradientTexture); SAMPLER(sampler_GradientTexture);

            CBUFFER_START(UnityPerMaterial)
                float _CloudHeight;
                float _CloudClip;
                float _CloudSpeed;
                float3 _CloudDir;
                float _CloudScale2;
                float _CloudCoverage;
                float _CloudSoftness;
                half4 _LandColor;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                // 还原你原始的挤出逻辑
                float3 pos = input.positionOS.xyz + input.normalOS * (_CloudHeight * 0.05);
                output.positionCS = TransformObjectToHClip(pos);
                output.posOS = input.positionOS.xyz;
                output.uv3d = input.normalOS.xyz * 0.5 + 0.5;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag (Varyings i) : SV_Target
            {   
                // 1. 还原你原始的 UV 偏移逻辑
                float3 movingUV = i.uv3d;
                movingUV.x += _Time.y * _CloudDir.x * _CloudSpeed;
                movingUV.y += _Time.y * _CloudDir.y * _CloudSpeed;
                movingUV.z += _Time.y * _CloudDir.z * _CloudSpeed;

                // 2. 还原你原始的双重噪声
                float noise1 = SAMPLE_TEXTURE3D(_NoiseTexture, sampler_NoiseTexture, movingUV).r;
                float noise2 = SAMPLE_TEXTURE3D(_NoiseTexture, sampler_NoiseTexture, movingUV * _CloudScale2 - _Time.y * _CloudSpeed).r;
                float combinedNoise = noise1 * noise2;

                // 3. 【新增：赤道蔓延权重】
                // posOS.y 从 -0.5 到 0.5。计算到赤道的距离（0为赤道，1为两极）
                float distFromEquator = abs(i.posOS.y) * 2.0; 
                // 覆盖率逻辑：_CloudCoverage 越大，允许距离赤道越远的地方有云
                float mask = smoothstep(_CloudCoverage + _CloudSoftness, _CloudCoverage - _CloudSoftness, distFromEquator);
                combinedNoise *= mask;

                // 4. 还原原始裁剪
                clip(combinedNoise - _CloudClip);

                // 5. 还原原始光照和颜色决策
                Light light = GetMainLight();
                float3 normal = normalize(i.normalWS);
                float lDotN = dot(light.direction, normal) * 0.5 + 0.5;
                half3 toonDiffuse = lDotN > 0.4 ? 1.0 : 0.5;

                half4 cloudCol = SAMPLE_TEXTURE2D(_GradientTexture, sampler_GradientTexture, float2(combinedNoise, 0.5));
                
                half3 finalRGB;
                if (lDotN > 0.4) {
                    finalRGB = combinedNoise > _CloudClip + 0.1 ? cloudCol.rgb : cloudCol.rgb * 0.8;
                } else {
                    finalRGB = _LandColor.rgb;
                }

                finalRGB = (lDotN - 0.4) * (lDotN - 0.42) > 0 ? finalRGB : _LandColor.rgb;
                finalRGB *= toonDiffuse;

                return half4(finalRGB, 1);
            }
            ENDHLSL
        }
    }
}