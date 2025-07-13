Shader "Custom/HeightColorShader"
{
    Properties
    {
        _MinHeight("Min Height", Float) = 0
        _MaxHeight("Max Height", Float) = 20
        _WaterColor("Water Color", Color) = (0.0, 0.3, 0.6, 1)
        _PlainsColor("Plains Color", Color) = (0.2, 0.5, 0.2, 1)
        _HillColor("Hill Color", Color) = (0.6, 0.5, 0.2, 1)
        _MountainColor("Mountain Color", Color) = (0.7, 0.7, 0.7, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float height : TEXCOORD0;
            };

            float _MinHeight;
            float _MaxHeight;
            fixed4 _WaterColor;
            fixed4 _PlainsColor;
            fixed4 _HillColor;
            fixed4 _MountainColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.height = (v.vertex.y - _MinHeight) / (_MaxHeight - _MinHeight);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float h = saturate(i.height);

                fixed4 color;
                if (h < 0.3)
                    color = _WaterColor;
                else if (h < 0.6)
                    color = lerp(_PlainsColor, _HillColor, (h - 0.3) / 0.3);
                else
                    color = lerp(_HillColor, _MountainColor, (h - 0.6) / 0.4);

                return color;
            }
            ENDCG
        }
    }
}
