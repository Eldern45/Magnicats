Shader "Custom/MagnetFieldDirectional"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
        _Speed ("Flow Speed", Float) = 2.0
        _Density ("Wave Density", Float) = 5.0
        _Alpha ("Alpha", Range(0,1)) = 0.6
        _FlowDirection ("Flow Direction", Float) = 1.0 // 1 = Forward, -1 = Backward
        _ArrowSteepness ("Arrow Steepness", Float) = 1.0 // How sharp the arrow is
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float _Speed;
            float _Density;
            float _Alpha;
            float _FlowDirection;
            float _ArrowSteepness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 1. Arrow Shape Calculation
                // Distort Y based on distance from X center to create a 'V' or '^' shape
                float distFromCenter = abs(uv.x - 0.5);
                
                // We offset Y backwards at the edges to make the center lead (creating an arrow head)
                // We multiply by _FlowDirection so the arrow points the right way:
                // If _FlowDirection = 1 (Up), we want shape '^'. Edges should lag (have lower effective Y or phase matches lower Y).
                // Actually, let's look at phase = y + offset.
                // If offset is NEGATIVE at edges: y - dist = C => y = C + dist (Edges higher -> 'v' shape).
                // If offset is POSITIVE at edges: y + dist = C => y = C - dist (Edges lower -> '^' shape).
                
                // So for Up (1), we want Positive offset.
                // For Down (-1), we want Negative offset.
                float arrowOffset = distFromCenter * _ArrowSteepness * _FlowDirection;

                // 2. Wave Generation
                // Phase combines Y pos, the arrow distortion, and Time
                float phase = (uv.y + arrowOffset) * _Density - _Time.y * _Speed * _FlowDirection;
                
                // Use saw-tooth or sharp sine for clearer "arrows"
                // sin(phase) gives -1..1. 
                float waves = sin(phase);
                
                // Make it sharper: narrow the white band
                // Power function makes the peak spikier
                // Remap -1..1 to 0..1 first
                float sharpWave = 0.5 + 0.5 * waves;
                sharpWave = pow(sharpWave, 3.0); // Sharpen
                sharpWave = smoothstep(0.4, 0.9, sharpWave); // Cutoff to make distinct shapes

                // 3. Fades
                // Fade out at the very end (tip of the beam)
                float endFade = smoothstep(1.0, 0.7, uv.y);
                float startFade = smoothstep(0.0, 0.1, uv.y);

                // Fade edges (sides of the beam)
                float sideFade = 1.0 - distFromCenter * 2.0;
                sideFade = smoothstep(0.0, 0.2, sideFade);

                fixed4 col = _MainColor;
                
                // Combine
                col.a *= sharpWave; // The arrows
                col.a *= endFade;
                col.a *= startFade;
                col.a *= sideFade;
                col.a *= _Alpha;

                return col;
            }
            ENDCG
        }
    }
}