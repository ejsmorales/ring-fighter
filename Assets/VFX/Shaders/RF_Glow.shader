// Additive "lightsaber" glow for ring effects. Put it on a sphere, capsule or cylinder: the centre is bright and the
// silhouette fades softly, so a stack of a few of these (white-hot core + green shells) reads as a glowing bolt or beam.
// Cheap on purpose (no textures, no lighting, no keywords) for Quest 3.
Shader "RingFighter/Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 1, 0.5, 1)
        _Intensity ("Intensity", Float) = 1
        _Power ("Edge falloff power (0 = flat)", Float) = 2
        _TailFade ("Tail fade (bright at local +Z, fades toward -Z)", Range(0, 1)) = 0
        _PulseSpeed ("Pulse speed", Float) = 0
        _PulseAmount ("Pulse amount", Range(0, 1)) = 0
        _ShimmerSpeed ("Shimmer speed", Float) = 0
        _ShimmerAmount ("Shimmer amount", Range(0, 1)) = 0
        _ShimmerScale ("Shimmer scale", Float) = 10
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Power;
                half _TailFade;
                half _PulseSpeed;
                half _PulseAmount;
                half _ShimmerSpeed;
                half _ShimmerAmount;
                half _ShimmerScale;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float tail : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vp = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vp.positionCS;
                output.positionWS = vp.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(vp.positionWS);
                // Unit sphere / capsule meshes span -0.5..0.5 along local Z; 0 = back end, 1 = front end.
                output.tail = saturate(input.positionOS.z + 0.5);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                half ndv = saturate(abs(dot(n, v)));
                half fres = pow(max(ndv, 0.0001h), _Power);

                half tail = lerp(1.0h, pow(input.tail, 1.5h), _TailFade);
                half pulse = 1.0h - _PulseAmount * (0.5h + 0.5h * sin(_Time.y * _PulseSpeed));
                half shim = 1.0h - _ShimmerAmount * (0.5h + 0.5h * sin(dot(input.positionWS, float3(0.57, 0.57, 0.57)) * _ShimmerScale - _Time.y * _ShimmerSpeed));

                half a = fres * tail * pulse * shim * _Intensity * _Alpha;
                return half4(_Color.rgb * a, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
