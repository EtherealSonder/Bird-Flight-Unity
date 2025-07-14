Shader "Custom/StylizedToonWater"
{
    Properties
    {
        _MainColor("Water Color", Color) = (0.0, 0.6, 1.0, 1)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamThreshold("Foam Edge Distance", Range(0,1)) = 0.05
        _WaveSpeed("Wave Speed", Float) = 0.2
        _WaveScale("Wave Scale", Float) = 0.5
        _Transparency("Alpha", Range(0,1)) = 0.7
        _MainTex("Noise (Optional)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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

            sampler2D _MainTex;
            float4 _MainColor;
            float4 _FoamColor;
            float _FoamThreshold;
            float _WaveSpeed;
            float _WaveScale;
            float _Transparency;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv + _Time.y * _WaveSpeed;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float noise = tex2D(_MainTex, i.uv * _WaveScale).r;

                // Approximate foam edge by noise intensity
                float foamEdge = smoothstep(_FoamThreshold, _FoamThreshold + 0.02, noise);

                float4 col = lerp(_FoamColor, _MainColor, foamEdge);
                col.a *= _Transparency;

                return col;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
