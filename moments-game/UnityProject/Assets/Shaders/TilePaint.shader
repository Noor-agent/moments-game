Shader "Moments/TilePaint"
{
    // Color Clash arena floor tile shader.
    // Lerps between player colors with animated splat reveal.
    // MaterialPropertyBlock sets _TargetColor and _BlendFactor per tile.

    Properties
    {
        _BaseColor      ("Base Color",          Color)  = (0.9, 0.9, 0.9, 1)
        _TargetColor    ("Target (Paint) Color", Color)  = (0, 0.75, 1, 1)
        _BlendFactor    ("Blend Factor",        Range(0, 1)) = 0.0
        _SplatMask      ("Splat Mask",           2D)    = "white" {}
        _SplatStrength  ("Splat Reveal",        Range(0, 1)) = 0.0
        _Smoothness     ("Smoothness",          Range(0, 1)) = 0.5
        _GridLines      ("Grid Line Strength",  Range(0, 1)) = 0.08
        _GridScale      ("Grid Line Scale",     Float)  = 32.0
        _EmissionBoost  ("Edge Emission",       Range(0, 2)) = 0.3
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

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
            };

            TEXTURE2D(_SplatMask); SAMPLER(sampler_SplatMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TargetColor;
                float  _BlendFactor;
                float4 _SplatMask_ST;
                float  _SplatStrength;
                float  _Smoothness;
                float  _GridLines;
                float  _GridScale;
                float  _EmissionBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _SplatMask);
                OUT.fogCoord   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Paint color blend (animated via script setting _BlendFactor)
                half blend = _BlendFactor;
                half3 paintedColor = lerp(_BaseColor.rgb, _TargetColor.rgb, blend);

                // Splat mask reveal
                half splatMask = SAMPLE_TEXTURE2D(_SplatMask, sampler_SplatMask, IN.uv).r;
                blend = max(blend, splatMask * _SplatStrength);
                paintedColor = lerp(_BaseColor.rgb, _TargetColor.rgb, saturate(blend));

                // Subtle grid lines between tiles
                float2 gridUV = frac(IN.uv * _GridScale);
                float  gridLine = step(1.0 - _GridLines * 0.5, gridUV.x) + step(1.0 - _GridLines * 0.5, gridUV.y);
                gridLine = saturate(gridLine);
                paintedColor *= (1.0 - gridLine * 0.3);

                // Emission pulse at tile edges when freshly painted
                half3 emission = _TargetColor.rgb * blend * _EmissionBoost * (1.0 - gridLine);

                // Lighting
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float NdotL = saturate(dot(IN.normalWS, mainLight.direction));
                half3 diffuse  = paintedColor * mainLight.color * NdotL * mainLight.shadowAttenuation;
                half3 ambient  = paintedColor * unity_AmbientSky.rgb * 0.4;

                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float  spec    = pow(saturate(dot(IN.normalWS, halfDir)), _Smoothness * 128.0);
                half3  specular = spec * mainLight.color * _Smoothness * 0.4;

                half3 col = diffuse + ambient + specular + emission;
                col = MixFog(col, IN.fogCoord);

                return half4(col, 1.0);
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
