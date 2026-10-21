// Hex-grid barrier with a bright rim and a ripple that spreads from where a bolt hits. Put it on the flattened
// cylinder the shield uses: the pattern is computed from the cylinder's local XZ position (-0.5..0.5), so it
// follows the disc however it is scaled. _HexScale sets how many hexagons fit (set it from the disc's size).
Shader "RingFighter/Shield"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 1, 0.5, 1)
        _Intensity ("Intensity", Float) = 1
        _HexScale ("Hex scale", Float) = 3.5
        _LineWidth ("Hex line width", Range(0.01, 0.3)) = 0.07
        _Base ("Base tint", Range(0, 0.5)) = 0.06
        _RimWidth ("Rim width", Range(0.02, 0.5)) = 0.16
        _Flash ("Flash", Range(0, 1)) = 0
        _RippleCenter ("Ripple centre (disc space, xy)", Vector) = (0, 0, 0, 0)
        _RippleAge ("Ripple age (seconds)", Float) = 99
        _RippleLife ("Ripple life (seconds)", Float) = 0.7
        _RippleSpeed ("Ripple speed", Float) = 1.8
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Shield"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _HexScale;
                half _LineWidth;
                half _Base;
                half _RimWidth;
                half _Flash;
                float4 _RippleCenter;
                float _RippleAge;
                half _RippleLife;
                half _RippleSpeed;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 disc : TEXCOORD0;   // -1..1 across the disc
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.disc = input.positionOS.xz * 2.0;
                return output;
            }

            float2 Mod2(float2 x, float2 y)
            {
                return x - y * floor(x / y);
            }

            // x = distance to the nearest hexagon edge (0 on the edge, about 0.5 in the middle), yz = cell id.
            float3 HexInfo(float2 uv)
            {
                const float2 r = float2(1.0, 1.7320508);
                const float2 h = r * 0.5;
                float2 a = Mod2(uv, r) - h;
                float2 b = Mod2(uv - h, r) - h;
                float2 gv = dot(a, a) < dot(b, b) ? a : b;
                float2 id = uv - gv;
                float2 q = abs(gv);
                float hd = max(dot(q, float2(0.5, 0.8660254)), q.x);
                return float3(0.5 - hd, id);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 p = input.disc;
                float r = length(p);

                float3 hex = HexInfo(p * _HexScale);
                half lines = 1.0h - smoothstep(0.0h, _LineWidth, (half)hex.x);
                float rnd = frac(sin(dot(hex.yz, float2(12.9898, 78.233))) * 43758.5453);
                half cell = (half)(0.5 + 0.5 * sin(_Time.y * 1.6 + rnd * 6.2831853));

                half rim = smoothstep(1.0h - _RimWidth, 0.99h, (half)r);

                float d = distance(p, _RippleCenter.xy);
                float front = _RippleAge * _RippleSpeed;
                float rw = (d - front) / 0.16;
                half life = saturate(1.0h - (half)_RippleAge / max(_RippleLife, 0.01h));
                half ripple = (half)exp(-rw * rw) * life;

                half a = _Base
                       + lines * 0.85h
                       + cell * lines * 0.35h
                       + rim * 1.1h
                       + ripple * (0.7h + lines * 2.5h);
                a *= (1.0h + _Flash * 1.4h);
                a *= _Intensity * _Alpha;

                half3 col = lerp(_Color.rgb, half3(1.0h, 1.0h, 1.0h), saturate(ripple * 0.8h + rim * 0.3h + _Flash * 0.4h));
                return half4(col * a, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
