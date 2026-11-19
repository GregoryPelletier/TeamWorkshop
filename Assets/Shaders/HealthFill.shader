// Draws the player's picture and fills it with red from the feet up as he loses health.
// PlayerFighter sets Fill Level (a height in the world) every frame:
//   everything on the player below that height is drawn red, everything above looks normal.
Shader "Dragons Toll/Health Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillColor ("Fill Color", Color) = (0.9, 0.08, 0.08, 1)
        _FillStrength ("Fill Strength", Range(0, 1)) = 0.85
        _FillLevel ("Fill Level (world height)", Float) = -10000
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _FillColor;
            float _FillStrength;
            float _FillLevel;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float worldHeight : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldHeight = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 picture = tex2D(_MainTex, i.uv) * i.color;
                if (i.worldHeight < _FillLevel)
                {
                    // keep a little of the picture's light and dark so he still looks like himself
                    float brightness = dot(picture.rgb, float3(0.3, 0.59, 0.11));
                    fixed3 red = _FillColor.rgb * (0.6 + 0.8 * brightness);
                    picture.rgb = lerp(picture.rgb, red, _FillStrength);
                }
                picture.rgb *= picture.a;
                return picture;
            }
            ENDCG
        }
    }
}
