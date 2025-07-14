Shader "Custom/AnimatedLake"
{
    Properties
    {
        _Color("Water Tint", Color) = (0.2, 0.4, 0.8, 0.6)
        _Speed("Wave Speed", Float) = 0.2
        _Scale("Wave Scale", Float) = 0.5
        _Transparency("Transparency", Range(0,1)) = 0.5
        _MainTex("Noise Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Lighting Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _Speed;
            float _Scale;
            float _Transparency;

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

            float _TimeY;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv + float2(_Time.y * _Speed, _Time.y * _Speed); // animated UV
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float wave = tex2D(_MainTex, i.uv * _Scale).r;
                float alpha = _Transparency * (0.5 + 0.5 * wave); // subtle alpha flicker
                return float4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
