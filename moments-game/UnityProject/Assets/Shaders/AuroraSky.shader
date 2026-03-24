Shader "Moments/AuroraSky"
{
    // Full-screen aurora borealis sky shader for Polar Push.
    // Applied to a quad/skybox mesh behind the arena.
    // Features: multi-band aurora ribbons, star field, animated shimmer,
    //           animated northern lights color cascade.

    Properties
    {
        _StarDensity    ("Star Density",        Range(50, 500)) = 200
        _StarBrightness ("Star Brightness",     Range(0, 2))    = 1.0
        _StarSize       ("Star Size",           Range(0.001, 0.02)) = 0.006

        // Aurora bands
        _Aurora1Color   ("Aurora Band 1",        Color)  = (0.15, 1.0, 0.55, 1)
        _Aurora2Color   ("Aurora Band 2",        Color)  = (0.0, 0.6, 1.0, 1)
        _Aurora3Color   ("Aurora Band 3",        Color)  = (0.8, 0.2, 1.0, 1)
        _AuroraSpeed    ("Aurora Speed",        Float)  = 0.3
        _AuroraScale    ("Aurora Scale",        Float)  = 3.0
        _AuroraIntensity("Aurora Intensity",    Range(0, 3)) = 1.4

        // Sky gradient
        _HorizonColor   ("Horizon Color",        Color)  = (0.02, 0.04, 0.12, 1)
        _ZenithColor    ("Zenith Color",         Color)  = (0.0, 0.01, 0.06, 1)
        _MoonGlow       ("Moon Glow",            Color)  = (0.85, 0.9, 1.0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Background"
            "Queue"          = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _StarDensity;
                float  _StarBrightness;
                float  _StarSize;
                float4 _Aurora1Color;
                float4 _Aurora2Color;
                float4 _Aurora3Color;
                float  _AuroraSpeed;
                float  _AuroraScale;
                float  _AuroraIntensity;
                float4 _HorizonColor;
                float4 _ZenithColor;
                float4 _MoonGlow;
            CBUFFER_END

            // Hash function for star field
            float hash(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float hash1(float n) { return frac(sin(n) * 43758.5453123); }

            // Smooth noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i + float2(0,0)), hash(i + float2(1,0)), u.x),
                            lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                for (int i = 0; i < 4; i++) { v += a * noise(p); p *= 2.0; a *= 0.5; }
                return v;
            }

            // Aurora ribbon function
            float aurora(float2 uv, float yOffset, float speed, float scale, float time)
            {
                float2 p = float2(uv.x * scale, uv.y * 2.0 + yOffset);
                float wave = fbm(float2(p.x + time * speed, p.y));
                float band = exp(-abs(uv.y - (0.6 + yOffset * 0.15) - wave * 0.15) * 18.0);
                float shimmer = sin(p.x * 12.0 + time * 3.0) * 0.5 + 0.5;
                return band * shimmer * (wave * 0.5 + 0.5);
            }

            // Star field
            float stars(float2 uv, float density, float size)
            {
                float2 grid = floor(uv * density);
                float2 frac2 = frac(uv * density) - 0.5;
                float h = hash(grid);
                float2 offset = float2(hash(grid + 12.3), hash(grid + 47.1)) - 0.5;
                float d = length(frac2 - offset * 0.5);
                float bright = hash1(h * 123.45);
                return smoothstep(size, 0.0, d) * bright * bright;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float time = _Time.y;

                // Sky gradient (horizon to zenith)
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(uv.y), 0.6));

                // Stars (only upper portion of sky)
                float starMask = smoothstep(0.2, 0.5, uv.y);
                float starField = stars(uv * float2(1.6, 1.0), _StarDensity, _StarSize);
                starField += stars(uv * float2(2.1, 1.5) + float2(0.3, 0.7), _StarDensity * 0.6, _StarSize * 0.7);
                sky += starField * _StarBrightness * starMask;

                // Aurora bands
                float a1 = aurora(uv, 0.0,  _AuroraSpeed,        _AuroraScale, time);
                float a2 = aurora(uv, 0.12, _AuroraSpeed * 0.7,  _AuroraScale * 1.3, time + 1.5);
                float a3 = aurora(uv, 0.25, _AuroraSpeed * 1.4,  _AuroraScale * 0.8, time + 3.0);

                half3 auroraColor =
                    _Aurora1Color.rgb * a1 +
                    _Aurora2Color.rgb * a2 +
                    _Aurora3Color.rgb * a3;

                // Aurora fades near horizon
                float auroraFade = smoothstep(0.2, 0.55, uv.y) * smoothstep(1.0, 0.8, uv.y);
                sky += auroraColor * _AuroraIntensity * auroraFade;

                // Moon glow (upper-right)
                float2 moonPos = float2(0.78, 0.82);
                float moonDist = length(uv - moonPos);
                float moonGlow = exp(-moonDist * moonDist * 40.0) * 0.6;
                float moonCore = exp(-moonDist * moonDist * 800.0);
                sky += _MoonGlow.rgb * (moonGlow + moonCore * 2.0);

                // HDR: allow values > 1 for bloom
                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Skybox/Procedural"
}
