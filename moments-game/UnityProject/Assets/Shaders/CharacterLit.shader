Shader "Moments/CharacterLit"
{
    // Premium toy-like character shader.
    // Features: URP Lit base + player-color rim light + emissive win pulse.
    // Shared across all 8 heroes. Player color injected via MaterialPropertyBlock.

    Properties
    {
        _BaseColor          ("Base Color",          Color)  = (1, 1, 1, 1)
        _BaseMap            ("Base Texture",         2D)    = "white" {}
        _NormalMap          ("Normal Map",           2D)    = "bump" {}
        _NormalStrength     ("Normal Strength",     Range(0, 2))  = 0.5

        _Smoothness         ("Smoothness",          Range(0, 1))  = 0.45
        _Metallic           ("Metallic",            Range(0, 1))  = 0.05

        // Rim lighting — set per player via MaterialPropertyBlock
        _RimColor           ("Rim Color (Player)",   Color)  = (0, 0.75, 1, 1)
        _RimPower           ("Rim Power",           Range(1, 8))  = 3.5
        _RimIntensity       ("Rim Intensity",       Range(0, 3))  = 0.8

        // Win/emote glow emission
        _EmissionColor      ("Emission Color",       Color)  = (0, 0, 0, 1)
        _EmissionStrength   ("Emission Strength",   Range(0, 5))  = 0.0

        // Stylization: boost shadow contrast for toy-like look
        _ShadowStrength     ("Shadow Contrast",     Range(0, 1))  = 0.65
        _WrapLighting       ("Wrap Lighting",       Range(0, 1))  = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS   : TEXCOORD5;
                float  fogCoord    : TEXCOORD6;
            };

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _NormalStrength;
                float  _Smoothness;
                float  _Metallic;
                float4 _RimColor;
                float  _RimPower;
                float  _RimIntensity;
                float4 _EmissionColor;
                float  _EmissionStrength;
                float  _ShadowStrength;
                float  _WrapLighting;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.tangentWS   = nrmInputs.tangentWS;
                OUT.bitangentWS = nrmInputs.bitangentWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.fogCoord    = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Base
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Normal
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv),
                    _NormalStrength);
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                float3 viewDir = normalize(IN.viewDirWS);

                // Main light with wrap lighting for toy softness
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float NdotL = dot(normalWS, mainLight.direction);
                float wrappedNdotL = (NdotL + _WrapLighting) / (1.0 + _WrapLighting);
                float diffuseFactor = saturate(wrappedNdotL);

                // Shadow: boost contrast for toy look
                float shadow = mainLight.shadowAttenuation;
                float shadowedDiffuse = lerp(diffuseFactor * (1.0 - _ShadowStrength),
                                             diffuseFactor, shadow);

                half3 diffuse = albedo.rgb * mainLight.color * shadowedDiffuse;

                // Specular (Blinn-Phong)
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float  spec    = pow(saturate(dot(normalWS, halfDir)), _Smoothness * 256.0);
                half3  specular = spec * mainLight.color * _Smoothness * 0.5;

                // Ambient
                half3 ambient = albedo.rgb * unity_AmbientSky.rgb * 0.35;

                // Rim light — player color
                float rim = pow(1.0 - saturate(dot(normalWS, viewDir)), _RimPower);
                half3 rimLight = _RimColor.rgb * rim * _RimIntensity;

                // Emission (win pulse, emote glow)
                half3 emission = _EmissionColor.rgb * _EmissionStrength;

                // Combine
                half3 col = diffuse + specular + ambient + rimLight + emission;
                col = MixFog(col, IN.fogCoord);

                return half4(col, albedo.a);
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
            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
