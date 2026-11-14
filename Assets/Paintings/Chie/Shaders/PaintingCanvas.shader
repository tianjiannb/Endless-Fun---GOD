Shader "Numen/ChiePainting"
{
    Properties
    {
        _BaseTex   ("Base",   2D) = "white" {}
        _BaseTexB  ("Base B", 2D) = "white" {}
        _FixedTex  ("Fixed",  2D) = "white" {}
        _FixedTexB ("Fixed B",2D) = "white" {}
        _MaskTex   ("Mask",   2D) = "black" {}
        _BrushTex  ("Brush",  2D) = "black" {}
        _Blend     ("Pose Blend", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "PaintingPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float2 uvb : TEXCOORD1; };

            TEXTURE2D(_BaseTex);   SAMPLER(sampler_BaseTex);
            TEXTURE2D(_BaseTexB);  SAMPLER(sampler_BaseTexB);
            TEXTURE2D(_FixedTex);  SAMPLER(sampler_FixedTex);
            TEXTURE2D(_FixedTexB); SAMPLER(sampler_FixedTexB);
            TEXTURE2D(_MaskTex);   SAMPLER(sampler_MaskTex);
            TEXTURE2D(_BrushTex);  SAMPLER(sampler_BrushTex);

            float4 _BaseTex_ST;
            float4 _BrushTex_ST;

            float4 _RestoredA;   // r=①嘴 g=②笔尖 b=③窗 a=④五官
            float4 _RestoredB;   // x=5 y=6
            float  _Blend;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseTex);
                o.uvb = TRANSFORM_TEX(v.uv, _BrushTex);
                return o;
            }

            float RestoredOf(float id)
            {
                if (id < 0.5) return 0.0;
                if (id < 1.5) return _RestoredA.r;
                if (id < 2.5) return _RestoredA.g;
                if (id < 3.5) return _RestoredA.a;   // 3 黄 = 五官
                if (id < 4.5) return _RestoredA.b;   // 4 蓝 = 窗外
                if (id < 5.5) return _RestoredB.x;
                return _RestoredB.y;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 baseA  = SAMPLE_TEXTURE2D(_BaseTex,   sampler_BaseTex,   i.uv).rgb;
                half3 baseB  = SAMPLE_TEXTURE2D(_BaseTexB,  sampler_BaseTexB,  i.uv).rgb;
                half3 fixA   = SAMPLE_TEXTURE2D(_FixedTex,  sampler_FixedTex,  i.uv).rgb;
                half3 fixB   = SAMPLE_TEXTURE2D(_FixedTexB, sampler_FixedTexB, i.uv).rgb;
                half3 m      = SAMPLE_TEXTURE2D(_MaskTex,   sampler_MaskTex,   i.uv).rgb;
                half  brush  = SAMPLE_TEXTURE2D(_BrushTex,  sampler_BrushTex,  i.uvb).a;

                half3 base   = lerp(baseB, baseA, _Blend);
                half3 fixedC = lerp(fixB,  fixA,  _Blend);

                half3 c = step(0.5, m);
                float id = c.r + c.g * 2.0 + c.b * 4.0;

                half reveal = RestoredOf(id);
                half k = max(brush, reveal);

                return half4(lerp(base, fixedC, k), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
