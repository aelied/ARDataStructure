Shader "UI/DottedOutlineRoundedFixed"
{
    Properties
    {
        _Color("Outline Color", Color) = (1,1,1,1)
        _Thickness("Outline Thickness", Range(0.001, 0.1)) = 0.02
        _DotSpacing("Dot Spacing", Range(1.0, 50.0)) = 15.0
        _CornerRadius("Corner Radius", Range(0.0, 0.5)) = 0.1
        [PerRendererData]_MainTex("MainTex", 2D) = "white" {}
    }

    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Lighting Off
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Thickness;
            float _DotSpacing;
            float _CornerRadius;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            // Signed distance to a rounded rectangle centered in UV space (0–1)
            float roundedBox(float2 uv, float2 size, float radius)
            {
                float2 q = abs(uv - 0.5) * 2.0 - size + radius * 2.0;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Distance field for rounded rectangle
                float dist = roundedBox(uv, float2(1.0, 1.0), _CornerRadius);

                // Define outer and inner edges of border
                float outer = smoothstep(0.0, 0.001, dist);
                float inner = smoothstep(_Thickness, _Thickness + 0.001, dist);
                float borderMask = outer - inner;

                // Generate dotted pattern along border using UV length
                float pattern = step(0.5, frac((uv.x + uv.y) * _DotSpacing));

                // Combine: only draw pattern where borderMask is visible
                float alpha = borderMask * pattern;

                return float4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
