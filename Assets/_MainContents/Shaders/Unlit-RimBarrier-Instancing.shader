Shader "Unlit/Unlit-RimBarrier-Instancing"
{
    Properties
    {
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _Intensity ("Rim Power", float) = 1
        _Alpha ("Alpha", float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewDir  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _RimColor;
            float _Intensity;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(mul(unity_ObjectToWorld, v.normal));
                float3 WorldSpaceCameraPos = float3(_WorldSpaceCameraPos.xy, _WorldSpaceCameraPos.z - 1000);
                o.viewDir = normalize(WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float rim = 1 - abs(dot(i.viewDir, i.normal));
                fixed4 col = _RimColor * pow(rim, _Intensity);
                return col * _Alpha;
            }
            ENDCG
        }
    }
}
