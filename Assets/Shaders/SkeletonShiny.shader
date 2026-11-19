// A simple shiny metal look for the silver skeletons and the silver shield.
// It lights the model from a fixed direction (up and to the front), adds a bright
// highlight and a soft rim, so the bones look like polished silver from the side view.
Shader "Dragons Toll/Skeleton Shiny"
{
    Properties
    {
        _Color ("Color", Color) = (0.78, 0.8, 0.84, 1)
        _MainTex ("Color Texture (optional)", 2D) = "white" {}
        _OcclusionMap ("Shadow Texture (optional)", 2D) = "white" {}
        _OcclusionStrength ("Shadow Strength", Range(0, 1)) = 0.6
        _Shine ("Shine", Range(0, 1)) = 0.7
        _Ambient ("Darkest Side", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Cull Back
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex; float4 _MainTex_ST;
            sampler2D _OcclusionMap;
            float _OcclusionStrength, _Shine, _Ambient;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float3 worldPos : TEXCOORD2; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.normal);
                float3 toLight = normalize(float3(-0.35, 0.65, -0.68));     // light from above, in front
                float3 toCamera = normalize(_WorldSpaceCameraPos - i.worldPos);
                float diffuse = _Ambient + (1 - _Ambient) * saturate(dot(normal, toLight));
                float highlight = pow(saturate(dot(normal, normalize(toLight + toCamera))), 24) * _Shine;
                float rim = pow(1 - saturate(dot(normal, toCamera)), 3) * 0.35 * _Shine;
                float shadow = lerp(1, tex2D(_OcclusionMap, i.uv).r, _OcclusionStrength);
                fixed3 baseColor = _Color.rgb * tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).rgb;
                fixed3 finalColor = baseColor * diffuse * shadow + highlight + rim;
                return fixed4(finalColor, 1);
            }
            ENDCG
        }
    }
}
