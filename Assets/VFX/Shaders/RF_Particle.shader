// Additive soft dot for sparks and embers. No texture: the round falloff and white-hot centre are computed from the
// particle's UV. Use with a ParticleSystemRenderer; the particle colour (and its alpha over lifetime) tints it.
// With the Stretch render mode the dot becomes a spark streak.
Shader "RingFighter/Particle Additive"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 1
        _HotCore ("White-hot centre", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Particle"
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
                half _Intensity;
                half _HotCore;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 p = input.uv * 2.0 - 1.0;
                half d = saturate(1.0h - dot(p, p));
                half soft = d * d;
                half core = soft * soft * soft;

                half3 c = input.color.rgb * soft + core * _HotCore;
                return half4(c * input.color.a * _Intensity, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
