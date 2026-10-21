// Whirlwind funnel: scrolling spiral streaks on a funnel mesh (u = around, v = 0 at the bottom .. 1 at the top).
// Two streak layers wind in opposite directions so it reads as moving wind, plus a faint body so the shape holds.
Shader "RingFighter/Funnel"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 1, 0.55, 1)
        _Intensity ("Intensity", Float) = 1
        _Streaks ("Streak count", Float) = 9
        _Twist ("Twist along height", Float) = 2.5
        _Speed ("Scroll speed", Float) = 2.5
        _Body ("Faint body glow", Range(0, 1)) = 0.14
        _BottomFade ("Bottom fade", Range(0.01, 1)) = 0.12
        _TopFade ("Top fade", Range(0.01, 1)) = 0.45
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Funnel"
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
                half _Streaks;
                half _Twist;
                half _Speed;
                half _Body;
                half _BottomFade;
                half _TopFade;
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

            // A soft streak: bright in the middle of each repeat, dark between.
            half Streak(float x)
            {
                half f = (half)frac(x);
                return smoothstep(0.0h, 0.18h, f) * (1.0h - smoothstep(0.32h, 0.62h, f));
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float u = input.uv.x;
                float v = input.uv.y;
                float t = _Time.y * _Speed;

                half s1 = Streak(u * _Streaks + v * _Twist - t);
                half s2 = Streak(u * _Streaks * 0.5 - v * _Twist * 0.7 + t * 0.6 + 0.37);

                half fade = smoothstep(0.0h, _BottomFade, (half)v) * (1.0h - smoothstep(1.0h - _TopFade, 1.0h, (half)v));
                half a = (_Body + s1 + s2 * 0.55h) * fade * _Intensity * _Alpha;

                half3 col = lerp(_Color.rgb, half3(1.0h, 1.0h, 1.0h), saturate(s1 * 0.35h));
                return half4(col * a, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
