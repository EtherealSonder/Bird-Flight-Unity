Shader "Custom/HeightLitSurface"
{
    Properties
    {
        _MinHeight("Min Height", Float) = 0
        _MaxHeight("Max Height", Float) = 60

        _WaterThreshold("Water Y Threshold", Float) = 5
        _PlainsThreshold("Plains Y Threshold", Float) = 30

        _WaterColor("Water Color", Color) = (0.0, 0.3, 0.6, 1)
        _PlainsColor("Plains Color", Color) = (0.2, 0.5, 0.2, 1)
        _HillColor("Hill Color", Color) = (0.6, 0.5, 0.2, 1)
        _MountainColor("Mountain Color", Color) = (0.5, 0.3, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #include "UnityCG.cginc"

        struct Input
        {
            float3 worldPos;
        };

        float _MinHeight;
        float _MaxHeight;
        float _WaterThreshold;
        float _PlainsThreshold;

        fixed4 _WaterColor;
        fixed4 _PlainsColor;
        fixed4 _HillColor;
        fixed4 _MountainColor;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float worldY = IN.worldPos.y;
            fixed4 finalColor;

            if (worldY < _WaterThreshold)
            {
                finalColor = _WaterColor;
            }
            else if (worldY < _PlainsThreshold)
            {
                float t = (worldY - _WaterThreshold) / (_PlainsThreshold - _WaterThreshold);
                finalColor = lerp(_PlainsColor, _HillColor, saturate(t));
            }
            else
            {
                float t = (worldY - _PlainsThreshold) / (_MaxHeight - _PlainsThreshold);
                finalColor = lerp(_HillColor, _MountainColor, saturate(t));
            }

            o.Albedo = finalColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
