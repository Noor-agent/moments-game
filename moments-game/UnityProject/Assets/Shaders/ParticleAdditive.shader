Shader "Moments/ParticleAdditive"
{
    // Unlit additive particle shader for all VFX pools.
    // Supports: _TintColor (HDR), _Softness (depth fade), _Brightness multiplier.
    // Used by: HitSpark, DashTrail, ScoreBurst, Confetti, Explosion,
    //          PaintSplat, IceCrack, ElimFlash, JoinPing.

    Properties
    {
        _MainTex     ("Particle Texture",  2D)    = "white" {}
        _TintColor   ("Tint Color",        Color)  = (1, 1, 1, 1)
        _Brightness  ("Brightness",       Range(0, 8)) = 2.0
        _Softness    ("Depth Softness",   Range(0, 3)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent+100"
            "IgnoreProjector"= "True"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType"    = "Plane"
        }

        Pass
        {
            Name "Unlit_Additive"
            Blend SrcAlpha One      // Additive blending → glow / bloom
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _TintColor;
                half   _Brightness;
                half   _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;       // Per-particle colour from ParticleSystem
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                float  fogCoord   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color * _TintColor;
                OUT.fogCoord   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 col = tex * IN.color;
                col.rgb  *= _Brightness;

                // Keep alpha for soft blending
                col.a = tex.a * IN.color.a;

                // Subtle fog darkening (additive particles ignore fog colour)
                col.rgb = MixFogColor(col.rgb, half3(0,0,0), IN.fogCoord);

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
