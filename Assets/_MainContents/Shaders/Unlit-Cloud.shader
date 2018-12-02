Shader "Custom/Unlit-Cloud"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed", float) = 1
        _CloudScale ("Cloud Scale", float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Cull Back
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed _ScrollSpeed;
            fixed _CloudScale;

            fixed4 frag (v2f_img  i) : SV_Target
            {
                float2 uv = (i.uv / _CloudScale) + (_Time.y / _ScrollSpeed);
                fixed4 col = tex2D(_MainTex, uv);
                col = col * _Color;
                return fixed4(col.rgb, 1 - col.a);
            }

            ENDCG
        }
    }
}
