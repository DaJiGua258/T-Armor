Shader "Custom/Noise3D_Gradient"
{
    Properties
    {
        // _NoiseTex 由 C# 自动分配
        _GradientTex ("Gradient Map (1D)", 2D) = "white" {}

        // 晨昏线设置
        _BackRange ("BackRange", Range(0, 1)) = 0.3  
        _LineCount ("LineCount", Range(0, 1)) = 0.25
        
        [Header(Night)]  // 暗面颜色
        _SeaThreshold ("SeaThreshold", Range(0, 1)) = 0.15
        _SeaColor ("SeaColor", Color) = (0.5, 0.5, 0.5, 1)
        _LandColor ("LandColor", Color) = (0.2, 0.2, 0.2, 1)
        
        [Header(Cloud)]  // 云层颜色
        _CloudHeight ("CloudHeight", Float) = 1
        _CloudClip ("CloudClip", Float) = 0.5

        
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #define _MAIN_LIGHT_SHADOWS  // 启用阴影图采样
                
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS             // 接收阴影 
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE     // 投射阴影
        #pragma multi_compile _ _SHADOWS_SOFT                   // 软阴影
        #pragma shader_feature _UseRampTex
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        
        // 3D 噪声图
        TEXTURE3D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);
        
        // 1D 渐变贴图
        TEXTURE2D(_GradientTex);
        SAMPLER(sampler_GradientTex);

        CBUFFER_START(UnityPerMaterial)
        // 
        float _BackRange;
        float _ForwardRange;

        float _BackSecRange;
        half4 _Color;
        half4 _BackColor;

        //
        float _SeaThreshold;
        half4 _SeaColor;
        half4 _LandColor;

        int _LineCount;

        float _CloudHeight;
        float _CloudClip;        
        CBUFFER_END
        
        ENDHLSL

        Pass
        {   
            Name "MyPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off
            ZWrite On
            

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

            struct a2v {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 positionCS : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;

            };
        
            

            v2f vert(a2v i) {
                v2f o;
                o.positionCS = TransformObjectToHClip(i.vertex.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normal);
                o.localPos = i.vertex.xyz + 0.5; 
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                // 采样 3D 噪声 (取得 0-1 的 R 通道)
                float noiseVal = SAMPLE_TEXTURE3D(_NoiseTex, sampler_NoiseTex, i.localPos).r;
                // 使用噪声值作为 U 坐标采样渐变贴图
                float2 uv_grad = float2(noiseVal, 0.5);
                half4 texColor = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, uv_grad);
                texColor.a = texColor.r;

                Light light = GetMainLight();  // 获取主光源结构体
                half3 lightColor = light.color;  // 主光颜色
                
                float lDotN = dot(light.direction, normalize(i.normalWS)) * 0.5 + 0.5;

                half3 diffuseColor = lDotN > 0.4 ? 1 : 0.5;

                texColor = lDotN > 0.4 ? texColor :
                (texColor.a > _SeaThreshold ? _LandColor : _SeaColor);

                texColor = (lDotN - 0.4) * (lDotN - 0.45) > 0 ? texColor :
                (texColor.a > _SeaThreshold ? _LandColor : _SeaColor);

                texColor.rgb = texColor.rgb * lightColor * diffuseColor;

                return half4(texColor.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            ZWrite On // the only goal of this pass is to write depth!
            ZTest LEqual // early exit at Early-Z stage if possible            
            ColorMask 0 // we don't care about color, we just want to write depth, ColorMask 0 will save some write bandwidth
            Cull Off // support Cull[_Cull] requires "flip vertex normal" using VFACE in fragment shader, which is maybe beyond the scope of a simple tutorial shader
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex  vert
            #pragma fragment  frag
            
            CBUFFER_START(UnityPerMaterial)
            
            CBUFFER_END

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(a2v v)
            {
                v2f o;
                float3 posWS = TransformObjectToWorld(v.vertex.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
               
                posWS = ApplyShadowBias(posWS, normalWS, GetMainLight().direction);
                
                float4 posCS = TransformWorldToHClip(posWS);
                
                // #if UNITY_REVERSED_Z
                //     posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                // #else
                //     posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
                // #endif
                o.pos = posCS;
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return 0;
            }
                
            ENDHLSL
        }
    
        // UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
        Pass 
        {
            Name "DepthNormals"
            
            Tags 
            {
                "LightMode" = "DepthNormals"
            }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}