Shader "Custom/Refraction"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 0.35)
        _Opacity("Opacity", Range(0, 1)) = 0.35
        _Smoothness("Smoothness", Range(0, 1)) = 0.65
        _Metalness("Metalness", Range(0, 1)) = 0
        _NormalMap("Normal Map", 2D) = "bump" {}
        _IndexofRefraction("Index of Refraction", Range(-1, 1)) = 0
        _ChromaticAberration("Chromatic Aberration", Range(0, 0.3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                half4 _Color;
                half _Opacity;
                half _Smoothness;
                half _Metalness;
                half _IndexofRefraction;
                half _ChromaticAberration;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _Albedo);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.uv) * _Color;
                albedo.a *= _Opacity;
                return albedo;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
