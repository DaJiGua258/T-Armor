Shader "Custom/BillboardLine"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _LineWidth("Manual Width Multiplier", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // LineRenderer 会传递顶点颜色
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _BaseColor;
            float _LineWidth;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 1. 获取世界坐标
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                
                // 2. 广告牌核心逻辑：将物体坐标转换到观察空间 (View Space)
                // 在观察空间中，Z轴指向相机，XY平面永远平行于屏幕
                float3 viewPos = TransformWorldToView(worldPos);

                // 3. 计算顶点偏移（此处可选，LineRenderer 默认已处理顶点位置，
                // 但如果你发现线条变扁，可以在此处根据 uv.y 再次修正高度）
                
                output.positionCS = TransformWViewToHClip(viewPos);
                output.uv = input.uv;
                output.color = input.color * _BaseColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, input.uv);
                return texColor * input.color;
            }
            ENDHLSL
        }
    }
}