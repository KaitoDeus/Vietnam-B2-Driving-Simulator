Shader "UI/ProceduralCircle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.0 // Set > 0 to make it a ring
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.48
        _Smoothness ("Smoothness (Anti-aliasing)", Range(0.0001, 0.02)) = 0.005
        
        _StartAngle ("Start Angle (Degrees)", Float) = 0.0
        _EndAngle ("End Angle (Degrees)", Float) = 360.0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;
            float _Smoothness;
            float _StartAngle;
            float _EndAngle;
            half4 _TextureSampleAdd; // Cần thiết cho hệ thống UI Clipping của Unity
            sampler2D _MainTex;      // Phải khai báo sampler2D cho Texture chính

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sampling text của UI (Cực kỳ quan trọng để hệ thống UI và Mask của Unity hoạt động)
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // Tọa độ UV dạng đối xứng từ -0.5 đến 0.5
                float2 uv = IN.texcoord - 0.5;
                float dist = length(uv);

                // Khử răng cưa mịn màng cho vòng tròn ngoài và trong
                float alphaOuter = smoothstep(_OuterRadius + _Smoothness, _OuterRadius, dist);
                float alphaInner = smoothstep(_InnerRadius - _Smoothness, _InnerRadius, dist);
                float alpha = alphaOuter * alphaInner;

                // Tính toán góc của pixel hiện tại (Rad -> Deg, trong khoảng [0, 360])
                float angleRad = atan2(uv.y, uv.x);
                float angleDeg = angleRad * 57.295779513; // degrees()
                if (angleDeg < 0.0) angleDeg += 360.0;

                // Normalize start/end angle về [0, 360]
                float startAng = _StartAngle;
                float endAng = _EndAngle;

                // Nếu cung quét lớn hơn hoặc bằng 360 độ thì luôn hiển thị đầy đủ vòng tròn
                float rawDiff = abs(endAng - startAng);
                if (rawDiff < 359.9)
                {
                    while (startAng < 0.0) startAng += 360.0;
                    while (startAng >= 360.0) startAng -= 360.0;

                    while (endAng < 0.0) endAng += 360.0;
                    while (endAng >= 360.0) endAng -= 360.0;

                    // Kiểm tra xem góc của pixel có nằm trong khoảng sweep [Start, End] (ngược chiều kim đồng hồ) hay không
                    float diff = endAng - startAng;
                    if (diff < 0.0) diff += 360.0;

                    float targetDiff = angleDeg - startAng;
                    if (targetDiff < 0.0) targetDiff += 360.0;

                    if (targetDiff > diff)
                    {
                        alpha = 0.0;
                    }
                }

                color.a *= alpha;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
