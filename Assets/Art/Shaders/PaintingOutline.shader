Shader "Numen/PaintingOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 0.949, 0.68, 0.9)
        _OutlineWidth ("Outline Width (px)", Range(0.5, 24)) = 6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PaintingOutline"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _Color;
            half4 _OutlineColor;
            float _OutlineWidth;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half AlphaAt(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 以屏幕像素为单位的采样步长，保证描边粗细与缩放无关
                float2 stepUV = fwidth(i.uv) * _OutlineWidth;
                half own = AlphaAt(i.uv);
                half inner = 0;
                half outer = 0;

                [unroll(24)]
                for (int k = 0; k < 24; k++)
                {
                    float ang = (k + 0.5) * 6.2831853 / 24.0;
                    float2 dir = float2(cos(ang), sin(ang));
                    inner = max(inner, AlphaAt(i.uv + dir * stepUV));
                    outer = max(outer, AlphaAt(i.uv + dir * stepUV * 2.0));
                }

                // own 覆盖画作本体，只保留本体之外的扩张环，形成外轮廓
                half core = saturate(inner - own);
                half halo = saturate(outer - max(inner, own)) * 0.45;
                half a = max(core, halo) * _OutlineColor.a * i.color.a * _Color.a;

                return half4(_OutlineColor.rgb * i.color.rgb * _Color.rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
