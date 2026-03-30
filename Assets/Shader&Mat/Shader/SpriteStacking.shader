Shader "Custom/SpriteStacking"
{
    Properties
    {
        _MainTex ("Sprite Sheet (Left to Right)", 2D) = "white" {}
        _LayerCount ("Layer Count", Int) = 16
        _YOffset ("Layer Y Offset", Float) = 0.02
        _StackDir ("Stack Direction (World Space)", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        // 针对 2D 透明物体的标准设置
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On   // ← 改为 On，允许写入深度缓冲
        ZTest LEqual
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            int _LayerCount;
            float _YOffset;
            float4 _StackDir;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION; // 传递对象空间坐标
                float2 uv : TEXCOORD0;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION; // 传递裁剪空间坐标
                float2 uv : TEXCOORD0;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex; 
                o.uv = v.uv;
                return o;
            }

            // 几何着色器：最大生成顶点数为 255 (即最多 85 层，255/3=85)
            // ...geom函数头部保持不变，依然保持修复后的 150 顶点限制...
            [maxvertexcount(150)] 
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                int layers = min(_LayerCount, 50); 
                float uvWidth = 1.0 / (float)max(1, _LayerCount);

                // --- 修复部分：改回正向循环，实现正确层级 ---
                // 从第 0 层 (最底层切片) 开始绘制
                // 一直绘制到最后一层 (layers-1，即最顶层切片)
                // 这样顶层后画，就会覆盖在底层上面
                for (int i = layers - 1; i >= 0; i--)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        g2f o;
                        float4 worldPos = mul(unity_ObjectToWorld, input[j].vertex);
                        
                        // 高度偏移公式依然不变，顶层 (i大) 在高处
                        worldPos.xyz += _StackDir.xyz * (i * _YOffset);
                        o.vertex = mul(UNITY_MATRIX_VP, worldPos);

                        // ✅ 关键：每层向摄像机方向偏移一点点深度
                        // 层数越大（越靠上）深度值越小（越靠近摄像机）
                        o.vertex.z += i * 0.0001 * o.vertex.w; // NDC 空间偏移，乘以 w 保持透视正确
                        
                        // UV 切片公式依然不变，顶层切分贴图最右侧的帧
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
                // 简单的透明剔除，防止透明重叠区域可能产生的渲染错误
                if (col.a < 0.05) discard; 
                return col;
            }
            ENDCG
        }
    }
}