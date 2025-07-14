Shader "Custom/ToonWaterWaveShader"
{
    Properties
    {
        _MainColor("Water Color", Color) = (0.0, 0.5, 0.9, 1)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _WaveSpeed("Wave Speed", Float) = 1.0
        _WaveHeight("Wave Height", Float) = 0.3
        _WaveFrequency("Wave Frequency", Float) = 1.5
        _FoamThreshold("Foam Distance", Range(0.01, 1)) = 0.2
        _Transparency("Alpha", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float foamAmount : TEXCOORD1;
            };

            float4 _MainColor;
            float4 _FoamColor;
            float _WaveSpeed;
            float _WaveHeight;
            float _WaveFrequency;
            float _FoamThreshold;
            float _Transparency;

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // GPU wave animation
                float wave = sin((worldPos.x + worldPos.z + _Time.y * _WaveSpeed) * _WaveFrequency);
                worldPos.y += wave * _WaveHeight;

                o.foamAmount = abs(wave);
                o.uv = v.uv;
                o.vertex = UnityObjectToClipPos(float4(worldPos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float foamEdge = smoothstep(_FoamThreshold, _FoamThreshold + 0.05, i.foamAmount);
                float4 finalCol = lerp(_MainColor, _FoamColor, foamEdge);
                finalCol.a *= _Transparency;
                return finalCol;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
