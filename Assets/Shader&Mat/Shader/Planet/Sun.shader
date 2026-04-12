Shader "Custom/Sun"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags    
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        LOD 100
        
        HLSLINCLUDE
        #define _MAIN_LIGHT_SHADOWS  // 启用阴影图采样
                
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS             // 接收阴影 
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE     // 投射阴影
        #pragma multi_compile _ _SHADOWS_SOFT                   // 软阴影
        #pragma shader_feature _UseRampTex
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
        half4 _Color;
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
            
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                

            struct a2v
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 posHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float3 posOS : TEXCOORD3;
                float2 noise : TEXCOORD4;
            };
                
            v2f vert(a2v v)
            {
                v2f o;
                o.posHCS = TransformObjectToHClip(v.vertex);
                
                return o;
            }
                
            half4 frag(v2f i) : SV_TARGET
            {
                return half4(_Color.rgb, 1);
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

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
