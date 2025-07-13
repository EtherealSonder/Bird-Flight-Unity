Shader "Custom/HeightLitSurface"
{
    Properties
    {
        _MinHeight("Min Height", Float) = 0
        _MaxHeight("Max Height", Float) = 60

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
if (worldY < 5)
{
    finalColor = _WaterColor;
}
else if (worldY < 30)
{
    float t = (worldY - 5) / 25; // lerp from plains to hills
    finalColor = lerp(_PlainsColor, _HillColor, t);
}
else
{
    float t = (worldY - 30) / (_MaxHeight - 30); // lerp from hill to mountain
    finalColor = lerp(_HillColor, _MountainColor, saturate(t));
}


            o.Albedo = finalColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
