Shader "Custom/SpriteShadow"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture", 2D) = "white" {}
        [HideInInspector] _MainTex_ST ("Main Texture ST", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
        }

        // 关键部分：模板测试
        Stencil {
            Ref 1          // 参考值为 1
            Comp NotEqual  // 如果当前像素的模板值不等于 1，则通过测试
            Pass Replace   // 测试通过后，将该像素的模板值替换为 Ref 值 (1)
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // 开启 GPU Instancing 变体编译
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 必须包裹在 CBUFFER 中以适配 SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 输入 ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 传递 ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                // 初始化 Instancing 数据
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                // 标准 URP 坐标转换
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // 设置 Instancing 环境
                UNITY_SETUP_INSTANCE_ID(i);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col *= _Color;

                // 简单的透明剔除（可选）
                if (col.a < 0.01) discard;

                return col;
            }
            ENDHLSL
        }
    }
}