Shader "Custom/URP/Fire Dissolve"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _DissolveMap("Dissolve Noise", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth("Fire Edge Width", Range(0.001, 0.35)) = 0.08
        _EdgeColor("Fire Edge Color", Color) = (1, 0.28, 0.02, 1)
        _CoreColor("Fire Core Color", Color) = (1, 0.9, 0.22, 1)
        _EmissionStrength("Emission Strength", Range(0, 20)) = 4
        _NoiseScale("Noise Scale", Range(0.1, 8)) = 1
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.25
        _FlowSpeed("Noise Flow Speed", Vector) = (0, 0.35, 0, 0)
        _DissolveCenter("Dissolve Center", Vector) = (0, 0, 0, 0)
        _DissolveRadiusXZ("Dissolve Radius XZ", Vector) = (1, 1, 0, 0)
        _DissolveInnerRadius("Dissolve Inner Radius", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DissolveMap_ST;
                half4 _BaseColor;
                half _DissolveAmount;
                half _EdgeWidth;
                half4 _EdgeColor;
                half4 _CoreColor;
                half _EmissionStrength;
                half _NoiseScale;
                half _NoiseStrength;
                float4 _FlowSpeed;
                float4 _DissolveCenter;
                float4 _DissolveRadiusXZ;
                half _DissolveInnerRadius;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 dissolveUV : TEXCOORD3;
                float2 positionOSXZ : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                float4 shadowCoord : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half ComputeDissolveValue(float2 dissolveUV, float2 positionOSXZ)
            {
                float2 animatedUV = dissolveUV * _NoiseScale + _Time.y * _FlowSpeed.xy;
                half noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, animatedUV).r;
                float2 centered = positionOSXZ - _DissolveCenter.xz;
                float2 radius = max(abs(_DissolveRadiusXZ.xy), float2(0.0001h, 0.0001h));
                half radial01 = saturate(length(centered / radius));
                half centerOut01 = 1.0h - saturate((radial01 - _DissolveInnerRadius) / max(0.0001h, 1.0h - _DissolveInnerRadius));
                return centerOut01 + (noise - 0.5h) * _NoiseStrength;
            }

            half ComputeCutoff(half dissolveValue)
            {
                half thresholdMin = -_NoiseStrength * 0.5h - 0.0001h;
                half thresholdMax = 1.0h + _NoiseStrength * 0.5h + 0.0001h;
                half threshold = lerp(thresholdMin, thresholdMax, _DissolveAmount);
                return dissolveValue - threshold;
            }

            half3 ComputeLighting(float3 positionWS, half3 normalWS)
            {
                half3 lighting = SampleSH(normalWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                half mainNdotL = saturate(dot(normalWS, mainLight.direction));
                lighting += mainLight.color * mainNdotL * mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, positionWS);
                    half ndotl = saturate(dot(normalWS, light.direction));
                    lighting += light.color * ndotl * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                return lighting;
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveMap);
                output.positionOSXZ = input.positionOS.xz;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.shadowCoord = TransformWorldToShadowCoord(positionInputs.positionWS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half dissolveValue = ComputeDissolveValue(input.dissolveUV, input.positionOSXZ);
                half cutoff = ComputeCutoff(dissolveValue);
                clip(cutoff);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half edgeMask = 1.0h - smoothstep(0.0h, _EdgeWidth, cutoff);
                half coreMask = 1.0h - smoothstep(0.0h, _EdgeWidth * 0.35h, cutoff);
                half3 fireColor = lerp(_EdgeColor.rgb, _CoreColor.rgb, coreMask);

                half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                half3 litColor = baseSample.rgb * ComputeLighting(input.positionWS, normalWS);
                half3 emission = fireColor * edgeMask * _EmissionStrength;
                half3 color = MixFog(litColor + emission, input.fogFactor);

                return half4(color, baseSample.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DissolveMap_ST;
                half4 _BaseColor;
                half _DissolveAmount;
                half _EdgeWidth;
                half4 _EdgeColor;
                half4 _CoreColor;
                half _EmissionStrength;
                half _NoiseScale;
                half _NoiseStrength;
                float4 _FlowSpeed;
                float4 _DissolveCenter;
                float4 _DissolveRadiusXZ;
                half _DissolveInnerRadius;
                half _Smoothness;
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
                float2 dissolveUV : TEXCOORD0;
                float2 positionOSXZ : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half ComputeDissolveValue(float2 dissolveUV, float2 positionOSXZ)
            {
                float2 animatedUV = dissolveUV * _NoiseScale + _Time.y * _FlowSpeed.xy;
                half noise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, animatedUV).r;
                float2 centered = positionOSXZ - _DissolveCenter.xz;
                float2 radius = max(abs(_DissolveRadiusXZ.xy), float2(0.0001h, 0.0001h));
                half radial01 = saturate(length(centered / radius));
                half centerOut01 = 1.0h - saturate((radial01 - _DissolveInnerRadius) / max(0.0001h, 1.0h - _DissolveInnerRadius));
                return centerOut01 + (noise - 0.5h) * _NoiseStrength;
            }

            half ComputeCutoff(half dissolveValue)
            {
                half thresholdMin = -_NoiseStrength * 0.5h - 0.0001h;
                half thresholdMax = 1.0h + _NoiseStrength * 0.5h + 0.0001h;
                half threshold = lerp(thresholdMin, thresholdMax, _DissolveAmount);
                return dissolveValue - threshold;
            }

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveMap);
                output.positionOSXZ = input.positionOS.xz;
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half dissolveValue = ComputeDissolveValue(input.dissolveUV, input.positionOSXZ);
                clip(ComputeCutoff(dissolveValue));
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
