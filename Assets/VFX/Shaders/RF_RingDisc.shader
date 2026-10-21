// Flat glowing ring / disc drawn on a Quad: shockwave rings, the whirlwind's ground circle, ground scorch.
// _RingRadius / _RingWidth place a soft ring (0..1 = centre..edge of the quad); _Fill adds a soft glowing disc inside,
// and _Arms > 0 breaks that fill into spinning spiral arms.
Shader "RingFighter/Ring Disc"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 1, 0.5, 1)
        _Intensity ("Intensity", Float) = 1
        _RingRadius ("Ring radius (0-1)", Range(0, 1)) = 0.85
        _RingWidth ("Ring width", Range(0.005, 0.5)) = 0.07
        _Fill ("Inner fill", Range(0, 1)) = 0
        _Arms ("Spiral arms (0 = none)", Float) = 0
        _Twist ("Spiral twist", Float) = 1.5
        _Spin ("Spin speed", Float) = 2
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "RingDisc"
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
                half _RingRadius;
                half _RingWidth;
                half _Fill;
                half _Arms;
                half _Twist;
                half _Spin;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 p = (input.uv - 0.5) * 2.0;
                float r = length(p);
                float ang = atan2(p.y, p.x);

                float w = (r - _RingRadius) / max(_RingWidth, 0.005);
                float ring = exp(-w * w);

                float fillMask = smoothstep(_RingRadius + _RingWidth, _RingRadius - _RingWidth, r);
                float arms = 1.0;
                if (_Arms > 0.5)
                {
                    arms = 0.5 + 0.5 * sin(ang * _Arms + r * _Twist * 6.2831853 - _Time.y * _Spin);
                    arms *= arms;
                }
                float fill = _Fill * fillMask * arms * (0.35 + 0.65 * saturate(r / max(_RingRadius, 0.01)));

                float edge = smoothstep(1.0, 0.92, r);
                half a = (half)((ring + fill) * edge) * _Intensity * _Alpha;
                // Hot white where the ring is strongest.
                half3 col = lerp(_Color.rgb, half3(1.0h, 1.0h, 1.0h), saturate((half)ring * 0.55h));
                return half4(col * a, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
