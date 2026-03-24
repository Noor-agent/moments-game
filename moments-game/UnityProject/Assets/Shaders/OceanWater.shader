Shader "Moments/OceanWater"
{
    // Wave Rider arena water shader.
    // Two-layer scrolling normal maps, vertex displacement, foam at crests, depth fog.

    Properties
    {
        _ShallowColor   ("Shallow Color",       Color)  = (0.0, 0.78, 0.82, 0.85)
        _DeepColor      ("Deep Color",          Color)  = (0.0, 0.25, 0.55, 0.95)
        _FoamColor      ("Foam Color",          Color)  = (1, 1, 1, 1)
        _FoamThreshold  ("Foam Threshold",      Range(0, 1)) = 0.75

        _NormalMap1     ("Normal Map Layer 1",   2D)    = "bump" {}
        _NormalMap2     ("Normal Map Layer 2",   2D)    = "bump" {}
        _NormalScale1   ("Normal Scale 1",      Float)  = 1.0
        _NormalScale2   ("Normal Scale 2",      Float)  = 0.7
        _ScrollSpeed1   ("Scroll Speed 1",       Vector) = (0.04, 0.02, 0, 0)
        _ScrollSpeed2   ("Scroll Speed 2",       Vector) = (-0.02, 0.05, 0, 0)

        _WaveHeight     ("Wave Height",         Float)  = 1.5
        _WaveFrequency  ("Wave Frequency",      Float)  = 0.8
        _WaveSpeed      ("Wave Speed",          Float)  = 1.2

        _Smoothness     ("Smoothness",          Range(0, 1)) = 0.92
        _DepthFogStart  ("Depth Fog Start",     Float)  = 0
        _DepthFogEnd    ("Depth Fog End",       Float)  = 8
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent-10"
            "RenderPipeline" = "UniversalPipeline"
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
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                float  fogCoord   : TEXCOORD4;
                float  waveHeight : TEXCOORD5;
            };

            TEXTURE2D(_NormalMap1); SAMPLER(sampler_NormalMap1);
            TEXTURE2D(_NormalMap2); SAMPLER(sampler_NormalMap2);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float  _FoamThreshold;
                float4 _NormalMap1_ST;
                float4 _NormalMap2_ST;
                float  _NormalScale1;
                float  _NormalScale2;
                float4 _ScrollSpeed1;
                float4 _ScrollSpeed2;
                float  _WaveHeight;
                float  _WaveFrequency;
                float  _WaveSpeed;
                float  _Smoothness;
                float  _DepthFogStart;
                float  _DepthFogEnd;
            CBUFFER_END

            // Gerstner wave displacement
            float GerstnerWave(float2 pos, float freq, float speed, float time)
            {
                return sin(dot(pos, float2(freq, freq * 0.7)) + time * speed);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Vertex wave displacement
                float time = _Time.y;
                float3 posOS = IN.positionOS.xyz;
                float wave1 = GerstnerWave(posOS.xz, _WaveFrequency, _WaveSpeed, time);
                float wave2 = GerstnerWave(posOS.xz * 1.3 + float2(3.7, 1.2), _WaveFrequency * 0.6, _WaveSpeed * 0.8, time);
                float wave3 = GerstnerWave(posOS.xz * 0.7 + float2(-1.5, 2.8), _WaveFrequency * 1.4, _WaveSpeed * 1.3, time);
                float displacement = (wave1 + wave2 * 0.5 + wave3 * 0.25) / 1.75 * _WaveHeight;
                posOS.y += displacement;
                OUT.waveHeight = saturate((displacement + _WaveHeight) / (_WaveHeight * 2.0));

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.uv         = IN.uv;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.fogCoord   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // Scrolling normal maps
                float2 uv1 = IN.uv * _NormalMap1_ST.xy + _ScrollSpeed1.xy * time;
                float2 uv2 = IN.uv * _NormalMap2_ST.xy + _ScrollSpeed2.xy * time;
                half3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap1, sampler_NormalMap1, uv1), _NormalScale1);
                half3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2), _NormalScale2);
                half3 blendedNormal = normalize(n1 + n2);

                // Transform to world space (simplified — flat normal for now)
                float3 normalWS = normalize(IN.normalWS + float3(blendedNormal.xy * 0.5, 0));

                float3 viewDir = normalize(IN.viewDirWS);

                // Depth-based color blend
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, 1.0 - IN.waveHeight);

                // Foam at wave crests
                float foam = step(_FoamThreshold, IN.waveHeight);
                waterColor = lerp(waterColor, _FoamColor.rgb, foam * 0.85);

                // Specular
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float  spec    = pow(saturate(dot(normalWS, halfDir)), _Smoothness * 512.0);
                half3  specular = spec * mainLight.color;

                // Fresnel for edge transparency
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), 4.0);
                half  alpha   = lerp(_ShallowColor.a, _DeepColor.a, 1.0 - IN.waveHeight);
                alpha = saturate(alpha + fresnel * 0.2);

                half3 col = waterColor + specular * _Smoothness;
                col = MixFog(col, IN.fogCoord);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
