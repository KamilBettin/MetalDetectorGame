Shader "MetalDetector/Ocean Water"
{
    Properties
    {
        _Color ("Compatibility Color", Color) = (0.02, 0.18, 0.28, 0.72)
        _BaseColor ("Compatibility Base Color", Color) = (0.02, 0.18, 0.28, 0.72)
        _DeepColor ("Deep Color", Color) = (0.02, 0.18, 0.28, 0.72)
        _ShallowColor ("Shallow Color", Color) = (0.08, 0.38, 0.46, 0.58)
        _FoamColor ("Foam Color", Color) = (0.78, 0.92, 0.92, 0.78)
        _WaveHeight ("Wave Height", Float) = 0.25
        _WaveScale ("Wave Scale", Float) = 0.018
        _WaveSpeed ("Wave Speed", Float) = 0.45
        _FoamIntensity ("Foam Intensity", Float) = 0.75
        _Sparkle ("Sparkle", Float) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseColor;
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float _WaveHeight;
                float _WaveScale;
                float _WaveSpeed;
                float _FoamIntensity;
                float _Sparkle;
            CBUFFER_END

            float Wave(float2 position, float time)
            {
                float waveA = sin(position.x * _WaveScale + position.y * _WaveScale * 0.31 + time);
                float waveB = sin(dot(position, float2(0.58, 0.82)) * _WaveScale * 1.55 - time * 1.37);
                float waveC = sin(dot(position, float2(-0.74, 0.48)) * _WaveScale * 0.72 + time * 0.63);
                return waveA * 0.52 + waveB * 0.32 + waveC * 0.16;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float2 localXZ = positionOS.xz;
                float time = _Time.y * _WaveSpeed;
                positionOS.y += Wave(localXZ, time) * _WaveHeight * 0.12;

                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _WaveSpeed;
                float2 worldXZ = input.positionWS.xz;
                float wave = Wave(worldXZ, time);
                float waveOffsetX = Wave(worldXZ + float2(1.3, 0.0), time);
                float waveOffsetZ = Wave(worldXZ + float2(0.0, 1.3), time);
                float3 normalWS = normalize(float3((wave - waveOffsetX) * 1.2, 1.0, (wave - waveOffsetZ) * 1.2));

                Light mainLight = GetMainLight();
                float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirection)), 2.1);
                float3 reflectionDirection = reflect(-viewDirection, normalWS);
                float horizon = saturate(reflectionDirection.y * 0.5 + 0.5);
                float3 lowReflection = float3(0.01, 0.05, 0.075);
                float3 highReflection = float3(0.36, 0.56, 0.62);
                float3 skyReflection = lerp(lowReflection, highReflection, horizon);
                float3 reflectedLight = reflect(-mainLight.direction, normalWS);
                float glint = pow(saturate(dot(reflectedLight, viewDirection)), 38.0) * _Sparkle;
                float broadGlint = pow(saturate(dot(reflectedLight, viewDirection)), 10.0) * _Sparkle * 0.18;
                float brushedA = sin(dot(worldXZ, float2(0.88, 0.47)) * _WaveScale * 5.2 + time * 1.4);
                float brushedB = sin(dot(worldXZ, float2(-0.26, 0.97)) * _WaveScale * 4.1 - time * 1.1);
                float brushedReflection = (brushedA * 0.5 + brushedB * 0.5) * 0.5 + 0.5;
                float metalRipple = smoothstep(0.12, 0.88, brushedReflection);

                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(wave * 0.28 + 0.42));
                waterColor = lerp(waterColor, skyReflection, saturate(0.34 + fresnel * 0.58));
                waterColor *= lerp(0.86, 1.16, metalRipple);
                waterColor += metalRipple * float3(0.035, 0.055, 0.06);
                waterColor += glint + broadGlint;

                float alpha = lerp(_DeepColor.a, _ShallowColor.a, saturate(wave * 0.28 + 0.42));
                alpha = saturate(alpha + fresnel * 0.08);
                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
}
