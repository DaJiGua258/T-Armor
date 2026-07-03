Shader "Custom/LineBeam_HardEdge"
{
    Properties
    {
        [Header(Colors)]
        _MainColor ("Beam Color", Color) = (0.0, 0.6, 1.0, 1.0)
        _CoreColor ("Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _EdgeColor ("Edge Color", Color) = (0.5, 0.8, 1.0, 1.0)
        _Intensity ("Intensity", Float) = 2.0

        [Header(Beam Shape)]
        _BeamWidth ("Beam Width", Range(0.1, 1.0)) = 0.5
        _CoreWidth ("Core Width", Range(0.0, 0.5)) = 0.15

        [Header(Upward Flow)]
        _ScrollSpeed ("Scroll Speed", Float) = 2.0
        _BandCount ("Band Count", Float) = 6.0
        _BandFill ("Band Fill", Range(0.0, 1.0)) = 0.3
        _BandBrightness ("Band Brightness", Range(0.0, 1.0)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha One
        Cull Off

        Pass
        {
            Name "Forward"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _CoreColor;
                float4 _EdgeColor;
                float _Intensity;
                float _BeamWidth;
                float _CoreWidth;
                float _ScrollSpeed;
                float _BandCount;
                float _BandFill;
                float _BandBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // UV.x = along beam (0 = start/bottom, 1 = end/top)
                // UV.y = across beam (0 = left edge, 1 = right edge)
                float h = input.uv.x;
                float w = input.uv.y;
                float distFromCenter = abs(w - 0.5);
                float halfBeam = _BeamWidth * 0.5;

                // === 1. Beam Body (hard edge) ===
                float bodyMask = step(distFromCenter, halfBeam);

                // === 2. Core (hard edge) ===
                float coreMask = step(distFromCenter, _CoreWidth);

                // === 3. Edge lines (hard edge on both sides) ===
                float leftEdge = step(distFromCenter, halfBeam) * step(halfBeam - 0.04, distFromCenter);
                float rightEdge = leftEdge;

                // === 4. Upward喷射 Bands ===
                float scroll = h * _BandCount - _Time.y * _ScrollSpeed;
                float bandRaw = frac(scroll);
                float band = step(1.0 - _BandFill, bandRaw); // hard band edge
                float bandBright = band * _BandBrightness;

                // === 5. Secondary bands (faster, thinner) ===
                float scroll2 = h * _BandCount * 1.5 - _Time.y * _ScrollSpeed * 1.8;
                float bandRaw2 = frac(scroll2);
                float band2 = step(0.85, bandRaw2); // thin hard bands
                float bandBright2 = band2 * _BandBrightness * 0.6;

                // === 6. Combine Colors ===
                float3 col = _MainColor.rgb;

                // Core is brighter
                col = lerp(col, _CoreColor.rgb, coreMask);

                // Edge lines
                col = lerp(col, _EdgeColor.rgb, leftEdge * 0.8);

                // Bands add brightness
                col += bandBright * _MainColor.rgb * 0.5;
                col += bandBright2 * _MainColor.rgb * 0.3;

                // === 7. Final Alpha ===
                float alpha = bodyMask;

                // Boost intensity
                col *= _Intensity;

                // Apply vertex color (for LineRenderer start/end fade)
                col *= input.color.rgb;
                alpha *= input.color.a;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
