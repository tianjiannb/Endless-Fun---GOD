Shader "Numen/ChieExtract"
{
    Properties
    {
        _MainTex  ("Main", 2D) = "white" {}
        _Split    ("Split", Range(0,1)) = 0
        _Lift     ("Lift", Range(0,1)) = 0
        _Dissolve ("Dissolve", Range(0,1)) = 0
        _Rip      ("Rip", Range(0,1)) = 0
        _RipY     ("Rip Y", Float) = 0.42
        _SeaA     ("Sea Dark", Color) = (0.071, 0.102, 0.149, 1)
        _SeaB     ("Sea Light", Color) = (0.204, 0.306, 0.376, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ExtractPass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _Split; float _Lift; float _Dissolve; float _Rip; float _RipY;
            float4 _SeaA; float4 _SeaB;

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
                o.uv = v.uv;
                return o;
            }

            // 程序化海：两条正弦 + 纵向渐变
            half3 Sea(float2 uv, float t)
            {
                float w1 = sin(uv.x * 9.0 + t * 0.7) * 0.5 + 0.5;
                float w2 = sin(uv.x * 17.0 - t * 1.1 + uv.y * 4.0) * 0.5 + 0.5;
                float band = saturate(uv.y * 1.4);
                half3 c = lerp(_SeaA.rgb, _SeaB.rgb, band * (0.55 + 0.45 * w1 * w2));
                c += (w1 - 0.5) * 0.03;
                return c;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                // 裂缝边界：横向范围由 _Rip 控制，边缘带纤维抖动
                float halfW = _Rip * 0.5;
                float dx = abs(uv.x - 0.5);
                float inRipX = step(dx, halfW);
                float fiber = (hash21(float2(uv.x * 260.0, 3.7)) - 0.5) * 0.012;
                float gapHalf = _Split * 0.5 + fiber;
                float dY = abs(uv.y - _RipY);
                float inGap = inRipX * step(dY, gapHalf);

                // 画布本身：上下两片随 _Split 分开并轻微旋转
                float2 uvc = uv;
                if (uv.y > _RipY) uvc += float2(_Split * 0.010, _Split * gapHalf * 1.0);
                else              uvc -= float2(_Split * 0.010, _Split * gapHalf * 1.0);
                half3 canvas = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(uvc)).rgb;

                // 裂纹内露出下面那幅海
                half3 sea = Sea(float2(uv.x, uv.y - _RipY + 0.5), t);
                half3 col = lerp(canvas, sea, inGap);

                // 人物膜：以 rip 为中心的一块，上浮 + 缩小 + 提亮
                float2 m = (uv - float2(0.5, _RipY)) / float2(0.42, 0.62);
                float inFilm = step(length(m), 1.0);
                float2 filmUv = (uv - float2(0.5, _RipY)) / max(1.0 - _Lift * 0.08, 0.01) + float2(0.5, _RipY);
                filmUv.y += _Lift * 0.42;
                half3 film = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, saturate(filmUv)).rgb * (1.0 + _Lift * 0.18);

                // 膜边缘按噪声溶解
                float edge = 1.0 - length(m);
                float dis = _Dissolve * 0.65;
                float keep = step(hash21(floor(uv * 420.0)), saturate(edge * 3.0 + (1.0 - dis) * 1.2 - 0.2));
                float filmA = inFilm * (1.0 - _Dissolve) * keep * step(0.001, _Lift);
                col = lerp(col, film, filmA * 0.92);

                // 整体淡出：Dissolve 后段直接压黑
                col *= saturate(1.0 - max(0.0, _Dissolve - 0.55) * 2.2);

                // 裂口边缘压暗一点，像纸的厚度
                float lip = inRipX * (1.0 - step(0.008, abs(dY - gapHalf))) * 0.22;
                col *= (1.0 - lip);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
