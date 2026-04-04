Shader "Custom/SpriteStackingWind"
{
    Properties
    {
        _Color ("MainColor", Color)  = (1, 1, 1, 1)
        _MainTex      ("Sprite Sheet", 2D)                   = "white" {}
        _LayerCount   ("Layer Count", Int)                   = 16
        _YOffset      ("Layer Y Offset", Float)              = 0.02
        _StackDir     ("Stack Direction (World)", Vector)    = (0, 1, 0, 0)

        [Header(Wind)]
        _WindStrength  ("Wind Strength", Float)  = 0.05  // 风力强度
        _WindFrequency ("Wind Frequency", Float) = 1.2  // 风力频率
        _WindDirection ("Wind Direction XZ", Vector) = (1, 0, 0, 0)  // 风力方向
        _WindTurbulence("Wind Turbulence", Float) = 0.3  // 风力湍流
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            sampler2D _MainTex;
            int   _LayerCount;
            float _YOffset;
            float4 _StackDir;

            float  _WindStrength;
            float  _WindFrequency;
            float4 _WindDirection;
            float  _WindTurbulence;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2g    { float4 vertex : POSITION;  float2 uv : TEXCOORD0; };
            struct g2f    { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv     = v.uv;
                return o;
            }

            // ─── 风力辅助函数 ──────────────────────────────────────────
            // 用两个不同频率的 sin 叠加，产生自然的阵风感
            float2 WindOffset(int layerIndex, int totalLayers, float3 worldPos)
            {
                float t = _Time.y * _WindFrequency;

                // 归一化层高比 [0,1]，底部为 0，顶部为 1
                float heightRatio = (float)layerIndex / (float)max(1, totalLayers - 1);

                // 二次曲线：底部几乎不动，顶部偏移最大（像真实树干）
                float influence = (heightRatio * heightRatio) + 1;

                // 用物体世界坐标 xz 加入相位，让相邻树不完全同步
                float phase = worldPos.x * 0.37 + worldPos.z * 0.29;

                // 主摆动 + 湍流（小幅高频抖动）
                float sway  = sin(t + phase) * _WindStrength;
                float turb  = sin(t * 2.7 + phase * 1.5) * _WindStrength * _WindTurbulence;

                float2 windDir = normalize(_WindDirection.xy);
                return windDir * (sway + turb) * influence;
            }
            // ──────────────────────────────────────────────────────────

            [maxvertexcount(150)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                int layers   = min(_LayerCount, 50);
                float uvWidth = 1.0 / (float)max(1, _LayerCount);

                // 用三角形中心估算物体世界坐标（用于相位计算）
                float4 centerWorld = mul(unity_ObjectToWorld,
                    (input[0].vertex + input[1].vertex + input[2].vertex) / 3.0);

                for (int i = layers - 1; i >= 0; i--)
                {
                    // 每层的风偏移（世界空间 XZ）
                    float2 wind = WindOffset(i, layers, centerWorld.xyz);

                    for (int j = 0; j < 3; j++)
                    {
                        g2f o;
                        float4 worldPos = mul(unity_ObjectToWorld, input[j].vertex);

                        // 垂直堆叠偏移
                        worldPos.xyz += _StackDir.xyz * (i * _YOffset);

                        // 水平风力偏移（叠加在世界空间 XZ）
                        worldPos.x += wind.x;
                        worldPos.y += wind.y;

                        o.vertex = mul(UNITY_MATRIX_VP, worldPos);

                        // 深度微偏移，保证层级正确遮挡
                        o.vertex.z += i * 0.0001 * o.vertex.w;

                        o.uv.x = (input[j].uv.x * uvWidth) + (i * uvWidth);
                        o.uv.y = input[j].uv.y;

                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                if (col.a < 0.05) discard;
                return col * _Color;
            }
            ENDCG
        }
    }
}