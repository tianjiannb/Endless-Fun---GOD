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
        _Grain     ("Grain", Range(0,0.3)) = 0.06
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
            float  _Grain;       // 底噪强度（画室颗粒），默认 0.06

            // 画室底噪：无贴图 hash 噪声（integer hash 21 → [0,1)），不引外部 include
            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

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
                half4 bs     = SAMPLE_TEXTURE2D(_BrushTex,  sampler_BrushTex,  i.uvb);
                half  brushK = bs.a;   // 画笔通道
                half  wipeK  = bs.r;   // 橡皮通道（擦掉白/灰）

                half3 base   = lerp(baseB, baseA, _Blend);
                half3 fixedC = lerp(fixB,  fixA,  _Blend);

                half3 c = step(0.5, m);
                float id = c.r + c.g * 2.0 + c.b * 4.0;

                half reveal = RestoredOf(id);
                // 窗外(4)、窗台(5) 只认橡皮；其余四处只认画笔
                half wipeZone = (id > 3.5 && id < 5.5) ? 1.0 : 0.0;
                half k = max(reveal, lerp(brushK, wipeK, wipeZone));

                half3 col = lerp(base, fixedC, k);

                // 底噪：每帧轻微抖动的颗粒，乘性叠加，暗部/白窗都不炸
                // 用 uv 做 hash 输入；乘 1080 把密度对齐到 ~1 像素一格（1080p 高度基准），
                // 放大画面（_BaseTex_ST 缩放）时颗粒跟着画布一起被放大，符合"底材颗粒"的观感
                float2 gp = i.uv * 1080.0;
                // 取模：_Time.y 长时间累加会让 hash 的 frac() 精度退化（颗粒变块状），128 秒一循环看不出来
                float  t  = fmod(_Time.y, 128.0);
                float  n  = hash21(gp + float2(t * 37.0, t * 61.0));
                // 二级 hash 让颗粒带一点粗颗粒团簇感（油画底子），仍不给白窗上雪花
                float  n2 = hash21(floor(gp * 0.5) + float2(t * 13.0, t * 19.0));
                float  g  = (n - 0.5) * 0.75 + (n2 - 0.5) * 0.25;
                // 亮部抑制：c 越接近白，颗粒振幅越小，避免白窗变雪花屏
                half   luma = dot(col, half3(0.299, 0.587, 0.114));
                half   sup  = saturate(1.0 - luma * 0.85);
                col = col * (1.0 + g * _Grain * sup);

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
