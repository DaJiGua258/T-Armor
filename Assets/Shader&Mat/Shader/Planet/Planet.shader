Shader "Custom/SpriteStackingURP_Instanced"
{
    Properties
    {
        _MainColor ("MainColor", Color)  = (1, 1, 1, 1)
        _MainTex ("Sprite Sheet (Left to Right)", 2D) = "white" {}
        _LayerCount ("Layer Count", Int) = 16
        _YOffset ("Layer Y Offset", Float) = 0.02
        _StackDir ("Stack Direction (World Space)", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On   
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            
            // 启用 GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 实例属性缓冲区
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
                UNITY_DEFINE_INSTANCED_PROP(int, _LayerCount)
                UNITY_DEFINE_INSTANCED_PROP(float, _YOffset)
                UNITY_DEFINE_INSTANCED_PROP(float4, _StackDir)
            UNITY_INSTANCING_BUFFER_END(Props)

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 输入实例 ID
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 传递实例 ID
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 最终传递给片元
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

            [maxvertexcount(150)] 
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                // 获取当前实例 ID
                UNITY_SETUP_INSTANCE_ID(input[0]);

                // 从缓冲区读取属性
                int layers = min(UNITY_ACCESS_INSTANCED_PROP(Props, _LayerCount), 50); 
                float yOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _YOffset);
                float4 stackDir = UNITY_ACCESS_INSTANCED_PROP(Props, _StackDir);
                
                float uvWidth = 1.0 / (float)max(1, layers);

                for (int i = layers - 1; i >= 0; i--)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        g2f o;
                        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                        UNITY_TRANSFER_INSTANCE_ID(input[0], o);

                        float3 worldPos = TransformObjectToWorld(input[j].vertex.xyz);
                        worldPos += stackDir.xyz * (i * yOffset);
                        
                        o.vertex = TransformWorldToHClip(worldPos);
                        o.vertex.z += i * 0.0001 * o.vertex.w; 
                        
                        o.uv.x = (input[j].uv.x * uvWidth) + (i * uvWidth);
                        o.uv.y = input[j].uv.y;

                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }   

            float4 frag(g2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (col.a < 0.05) discard; 
                
                return col * UNITY_ACCESS_INSTANCED_PROP(Props, _MainColor);
            }
            ENDHLSL
        }
    }
}