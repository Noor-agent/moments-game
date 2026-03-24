Shader "Moments/IceSurface"
{
    Properties
    {
        _BaseColor          ("Base Color",          Color)  = (0.66, 0.85, 0.92, 1)
        _BaseMap            ("Base Texture",         2D)    = "white" {}
        _NormalMap          ("Normal Map",           2D)    = "bump" {}
        _NormalStrength     ("Normal Strength",     Float)  = 0.4

        // Subsurface scattering approximation
        _SubsurfaceColor    ("Subsurface Color",     Color)  = (0.4, 0.75, 1.0, 1)
        _SubsurfaceStrength ("Subsurface Strength", Range(0,1)) = 0.55

        // Refraction / distortion
        _RefractionStrength ("Refraction Strength", Range(0, 0.05)) = 0.015
        _Smoothness         ("Smoothness",          Range(0, 1))    = 0.88
        _Metallic           ("Metallic",            Range(0, 1))    = 0.0

        // Crack overlay
        _CrackMap           ("Crack Decal",          2D)    = "black" {}
        _CrackStrength      ("Crack Strength",      Range(0, 1)) = 0.0
        _CrackColor         ("Crack Color",          Color)  = (0.15, 0.25, 0.4, 1)

        // Rim / fresnel
        _RimColor           ("Rim Color",            Color)  = (0.5, 0.8, 1.0, 1)
        _RimPower           ("Rim Power",           Range(0.5, 8)) = 3.0

        // Voronoi crack pattern
        _VoronoiScale       ("Voronoi Scale",       Float)  = 8.0
        _VoronoiStrength    ("Voronoi Strength",    Range(0, 0.15)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
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

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_CrackMap);       SAMPLER(sampler_CrackMap);

            CBUFFER_START(UnityPerMaterial)
                float4  _BaseColor;
                float4  _BaseMap_ST;
                float   _NormalStrength;
                float4  _SubsurfaceColor;
                float   _SubsurfaceStrength;
                float   _RefractionStrength;
                float   _Smoothness;
                float   _Metallic;
                float   _CrackStrength;
                float4  _CrackColor;
                float4  _RimColor;
                float   _RimPower;
                float   _VoronoiScale;
                float   _VoronoiStrength;
            CBUFFER_END

            // Simple voronoi for procedural crack pattern overlay
            float2 VoronoiNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float minDist = 8.0;
                float2 minPoint = 0;
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 h = frac(sin(float2(
                            dot(i + neighbor, float2(127.1, 311.7)),
                            dot(i + neighbor, float2(269.5, 183.3))
                        )) * 43758.5453);
                        float2 diff = neighbor + h - f;
                        float d = dot(diff, diff);
                        if (d < minDist) { minDist = d; minPoint = h; }
                    }
                return float2(minDist, minPoint.x);
            }

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
                // Base color + texture
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Normal map
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv),
                    _NormalStrength
                );
                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));

                // Fresnel / rim
                float3 viewDir = normalize(IN.viewDirWS);
                float  fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _RimPower);
                half3  rimContrib = _RimColor.rgb * fresnel;

                // Subsurface (fake): light from below seen through ice
                Light mainLight = GetMainLight();
                float sss = saturate(dot(-normalWS, mainLight.direction)) * _SubsurfaceStrength;
                half3 subsurface = _SubsurfaceColor.rgb * sss;

                // Voronoi crack lines
                float2 voronoi = VoronoiNoise(IN.uv * _VoronoiScale);
                float crackLine = 1.0 - smoothstep(0.0, 0.08, voronoi.x);
                half3 voronoiContrib = crackLine * _VoronoiStrength * half3(0.6, 0.8, 1.0);

                // Explicit crack decal
                half crackDecal = SAMPLE_TEXTURE2D(_CrackMap, sampler_CrackMap, IN.uv).r * _CrackStrength;
                half3 crackContrib = crackDecal * _CrackColor.rgb;

                // Basic PBR lighting
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = baseColor.rgb * mainLight.color * NdotL;
                half3 ambient = baseColor.rgb * unity_AmbientSky.rgb * 0.4;

                // Specular (blinn-phong approx)
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), _Smoothness * 256.0);
                half3 specular = spec * mainLight.color * _Smoothness;

                // Combine
                half3 col = diffuse + ambient + subsurface + rimContrib + voronoiContrib + crackContrib + specular;
                half  alpha = baseColor.a - fresnel * 0.15 + 0.05; // Slightly transparent edges

                // Fog
                col = MixFog(col, IN.fogCoord);

                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }

        // Shadow caster pass
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
