Shader "Custom/Fresnel"
{
    Properties
    {
        _Color("Color",Color) = (1,1,1,1)
        _BackColor("BackColor", Color) = (1, 1, 1, 1)
        _Power("Power",Float) = 5
        _Clip ("Clip", Range(0, 1)) = 1
        _Scale ("Scale", Float) = 1
        [Toggle] _Reflection("Reflection",Float) = 1

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" "Queue"="Geometry"}
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //关于[Toggle] [ToggleOff]如何使用看官方文档 https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html
            #pragma multi_compile __ _REFLECTION_ON 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half4 _BackColor;
            half _Power;
            float _Clip;
            float _Scale;
            CBUFFER_END


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS =TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewWS = GetWorldSpaceViewDir(positionWS);

                return OUT;
            }

            half Fresnel(half3 normal, half3 viewDir, half power)
            {
                //unity中常用这种方式实现 (1 - dot(v, n))^power
                float f1 = pow((saturate(dot(normalize(normal), normalize(viewDir)))), power);
                float f2 = pow((1 - saturate(dot(normalize(normal), normalize(viewDir)))), power);
                
                return min(f1, f2);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light light = GetMainLight();
                
                half3 normalWS = normalize(IN.normalWS);
                half3 viewWS = SafeNormalize(IN.viewWS);
                half fresnel = Fresnel(normalWS, viewWS,_Power);
                
                

                float lDotN = dot(light.direction, normalWS);
                fresnel = lDotN > 0 ? fresnel : fresnel * (1 - abs(lDotN));
                
                half4 totlaColor = lDotN > 0 ? _Color * fresnel : lerp(_Color, _BackColor, abs(lDotN)) * fresnel;
                // totlaColor = lDotN > 0 ? totlaColor : lerp(totlaColor, _BackColor * fresnel, 1 - sqrt(abs(lDotN)));
                // lDotN = lDotN > 0 ? lDotN : 1 - sqrt(abs(lDotN)) ;
                // totlaColor = 1 - totlaColor;
                // totlaColor *= _Scale;
                // clip(1 - totlaColor - _Clip);

                return half4(totlaColor.rgb, totlaColor.a);
               //  return half4(lDotN.xxxx);
            }

            ENDHLSL
        }
    }
}
