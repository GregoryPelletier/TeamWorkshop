// Draws an outline around every bone of a 3D skeleton.
// It draws a slightly fatter copy of the model (fatter by a few pixels on screen), inside out, in one solid color.
// The normal model is drawn on top, so only a thin rim of this color shows around each bone.
// Add a material that uses this shader as an extra Element in the skeleton's Materials list.
Shader "Dragons Toll/Skeleton Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.8, 0.82, 0.86, 1)
        _OutlineWidth ("Outline Width (pixels)", Range(0, 8)) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // which way the surface faces on screen
                float3 worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
                float2 screenNormal = mul((float3x3)UNITY_MATRIX_VP, worldNormal).xy;
                float lengthOnScreen = length(screenNormal);
                if (lengthOnScreen > 0.0001) screenNormal /= lengthOnScreen;
                // push each point out by the same number of pixels, however far the camera is zoomed
                o.pos.xy += screenNormal * _OutlineWidth * 2.0 / _ScreenParams.xy * o.pos.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
