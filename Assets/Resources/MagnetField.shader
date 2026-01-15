Shader "Custom/MagnetField"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
        _Speed ("Flow Speed", Float) = 2.0
        _Density ("Wave Density", Float) = 10.0
        _CoreSize ("Core Size (UV width, UV height)", Vector) = (0.2, 0.2, 0, 0) 
        _Alpha ("Alpha", Range(0,1)) = 0.6
        _Hardness ("Edge Hardness", Float) = 0.5
        _FlowDirection ("Flow Direction", Float) = 1.0 // 1 = Out, -1 = In
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
            float4 _CoreSize;
            float _Alpha;
            float _Hardness;
            float _FlowDirection;

            // Signed Distance Function for a Box
            float sdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;

                // 1. Box Distance
                float2 boxSize = _CoreSize.xy * 0.5; 
                float dist = sdBox(uv, boxSize);

                // 2. Masks
                float outerFade = smoothstep(0.5, 0.35, length(uv));
                float insideFade = smoothstep(0.0, 0.02, dist);

                // 3. Waves
                // Use _FlowDirection to flip time
                float wavePhase = dist * _Density - _Time.y * _Speed * _FlowDirection;
                float waves = sin(wavePhase);
                waves = smoothstep(-0.2, 0.2, waves);

                fixed4 col = _MainColor;
                float waveAlpha = 0.3 + 0.7 * waves; 
                
                col.a *= waveAlpha;
                col.a *= insideFade;
                col.a *= outerFade;
                col.a *= _Alpha;

                return col;
            }
            ENDCG
        }
    }
}