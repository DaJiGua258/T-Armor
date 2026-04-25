Shader "Custom/SpriteStacking_URP"
{
    Properties
    {
        _Color ("MainColor", Color)  = (1, 1, 1, 1) // 统一改为 _Color
        _MainTex ("Sprite Sheet (Left to Right)", 2D) = "white" {}
        _YOffset ("Layer Y Offset", Float) = 0.02
        _StackDir ("Stack Direction (World Space)", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags { 
            "RenderPipeline" = "UniversalPipeline"
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On   
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color; // 统一变量名
                float _YOffset;
                float4 _StackDir;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex : INTERNAL_POS; 
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2g vert(appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            [maxvertexcount(144)] 
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                UNITY_SETUP_INSTANCE_ID(input[0]);

                // 纹理按“每帧为正方形”自动推导层数：layerCount = width / height
                int layerCount = max(1, (int)round(_MainTex_TexelSize.z / max(1.0, _MainTex_TexelSize.w)));
                int layers = min(layerCount, 50);
                float uvWidth = 1.0 / (float)layers;

                for (int i = layers - 1; i >= 0; i--)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        g2f o;
                        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                        UNITY_TRANSFER_INSTANCE_ID(input[0], o);

                        float3 worldPos = TransformObjectToWorld(input[j].vertex.xyz);
                        worldPos.xyz += _StackDir.xyz * (i * _YOffset);
                        o.vertex = TransformWorldToHClip(worldPos);

                        o.vertex.z += i * 0.0001 * o.vertex.w;
                        o.uv.x = (input[j].uv.x * uvWidth) + (i * uvWidth);
                        o.uv.y = input[j].uv.y;

                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            half4 frag(g2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (col.a < 0.05) discard; 
                return col * _Color; // 使用统一后的变量
            }
            ENDHLSL
        }
    }
}