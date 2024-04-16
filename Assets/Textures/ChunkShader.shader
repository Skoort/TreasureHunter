Shader "Unlit/ChunkShader"
{
    Properties
    {
        _MainTexArray ("Texture Array", 2DArray) = "white" {}
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
		    #pragma target 3.5
            #pragma require 2darray
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainTexArray_ST;
            UNITY_DECLARE_TEX2DARRAY(_MainTexArray);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTexArray);
                o.uv.z = v.uv.z;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return UNITY_SAMPLE_TEX2DARRAY(_MainTexArray, i.uv);
            }
            ENDCG
        }
    }
}
