Shader "Moments/IceEdgeGlow"
{
    // Emissive edge glow strip for hex ice tiles.
    // Shows tile health state: white=intact → cyan=cracking → dark=about to fall.
    // Animated pulse when tile is cracked.

    Properties
    {
        _GlowColor      ("Glow Color",          Color)  = (0.5, 0.9, 1.0, 1)
        _GlowIntensity  ("Glow Intensity",      Range(0, 5))    = 1.5
        _PulseSpeed     ("Pulse Speed",         Range(0, 8))    = 3.0
        _IsCracked      ("Is Cracked",          Range(0, 1))    = 0
        _CollapseAmount ("Collapse Amount",     Range(0, 1))    = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float  _GlowIntensity;
                float  _PulseSpeed;
                float  _IsCracked;
                float  _CollapseAmount;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // Base glow color: fades from cyan → orange → dark as tile collapses
                half3 intact  = _GlowColor.rgb;
                half3 cracked = half3(1.0, 0.4, 0.05);
                half3 falling = half3(0.1, 0.05, 0.0);

                half3 col = lerp(intact, cracked, _IsCracked);
                col = lerp(col, falling, _CollapseAmount);

                // Panic pulse when cracked
                float pulse = 1.0 + _IsCracked * sin(time * _PulseSpeed) * 0.5;

                // Edge mask (bright at edges, fade in center)
                float2 centered = abs(IN.uv - 0.5) * 2.0;
                float edge = max(centered.x, centered.y);
                float edgeMask = smoothstep(0.6, 1.0, edge);

                float alpha = edgeMask * 0.85;
                half3 emission = col * _GlowIntensity * pulse;

                return half4(emission, alpha);
            }
            ENDHLSL
        }
    }
}
