Shader "Custom/SpriteStackingWind_URP"
{
    Properties
    {
        _Color ("MainColor", Color)  = (1, 1, 1, 1)
        _MainTex      ("Sprite Sheet", 2D)                   = "white" {}
        _YOffset      ("Layer Y Offset", Float)              = 0.02
        _StackDir     ("Stack Direction (World)", Vector)    = (0, 1, 0, 0)

        [Header(Wind)]
        _WindStrength  ("Wind Strength", Float)  = 0.05
        _WindFrequency ("Wind Frequency", Float) = 1.2
        _WindDirection ("Wind Direction XZ", Vector) = (1, 0, 0, 0)
        _WindTurbulence("Wind Turbulence", Float) = 0.3
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
            
            // 启用 GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 使用 CBUFFER 支持 SRP Batcher 和 Instancing
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _YOffset;
                float4 _StackDir;
                float _WindStrength;
                float _WindFrequency;
                float4 _WindDirection;
                float _WindTurbulence;
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

            // ─── 风力辅助函数 (HLSL 适配) ──────────────────────────────────
            float2 WindOffset(int layerIndex, int totalLayers, float3 worldPos)
            {
                // 使用 URP 内置的 _Time 变量
                float t = _Time.y * _WindFrequency;

                float heightRatio = (float)layerIndex / (float)max(1, totalLayers - 1);
                float influence = (heightRatio * heightRatio) + 1;

                float phase = worldPos.x * 0.37 + worldPos.z * 0.29;

                float sway  = sin(t + phase) * _WindStrength;
                float turb  = sin(t * 2.7 + phase * 1.5) * _WindStrength * _WindTurbulence;

                float2 windDir = normalize(_WindDirection.xy);
                return windDir * (sway + turb) * influence;
            }

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
                // 几何着色器实例初始化
                UNITY_SETUP_INSTANCE_ID(input[0]);

                // 纹理按“每帧为正方形”自动推导层数：layerCount = width / height
                int layerCount = max(1, (int)round(_MainTex_TexelSize.z / max(1.0, _MainTex_TexelSize.w)));
                int layers = min(layerCount, 50);
                float uvWidth = 1.0 / (float)layers;

                // 计算物体世界中心坐标（用于风力相位）
                float3 objectWorldCenter = TransformObjectToWorld((input[0].vertex.xyz + input[1].vertex.xyz + input[2].vertex.xyz) / 3.0);

                for (int i = layers - 1; i >= 0; i--)
                {
                    float2 wind = WindOffset(i, layers, objectWorldCenter);

                    for (int j = 0; j < 3; j++)
                    {
                        g2f o;
                        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                        UNITY_TRANSFER_INSTANCE_ID(input[0], o);

                        float3 worldPos = TransformObjectToWorld(input[j].vertex.xyz);
                        
                        // 垂直堆叠
                        worldPos.xyz += _StackDir.xyz * (i * _YOffset);
                        
                        // 叠加水平风力（注意：WindOffset 返回的是 XY 向量对应场景中的世界 XZ 或偏移）
                        // 保持原逻辑：wind.x 加到 x，wind.y 加到 y (虽然 y 通常是高度，这里保留用户原逻辑)
                        worldPos.x += wind.x;
                        worldPos.y += wind.y;

                        o.vertex = TransformWorldToHClip(worldPos);

                        // 深度偏移修正
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
                
                return col * _Color;
            }
            ENDHLSL
        }
    }
}