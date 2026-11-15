Shader "Numen/BrushErase"
{
    Properties
    {
        _Center   ("Center UV", Vector) = (0.5, 0.5, 0, 0)
        _Radius   ("Radius UV", Float)  = 0.02
        _Aspect   ("Aspect", Float)     = 1.7777778
        _Hardness ("Hardness", Float)   = 0.55
        _Value    ("Value", Float)      = 1
        _Shape    ("Shape", 2D)         = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend One One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Center; float _Radius; float _Aspect; float _Hardness; float _Value;
            TEXTURE2D(_Shape); SAMPLER(sampler_Shape);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 笔刷局部坐标：_Center 为原点、按 _Radius 归一化，x 再乘 _Aspect 保证屏幕上是正圆
                float2 d = (i.uv - _Center.xy) * float2(_Aspect, 1.0) / max(_Radius, 1e-5);
                float  r = length(d);

                // 解析式圆：_Hardness 越大边缘越硬，语义与旧版一致
                float  a = 1.0 - smoothstep(1.0 - _Hardness, 1.0, r);

                // 形状贴图 uv 由 i.uv 推出：归一化圆 [-1,1] 映射到 [0,1]，正好铺满 128x128 笔尖
                float2 suv = d * 0.5 + 0.5;
                float  s   = SAMPLE_TEXTURE2D(_Shape, sampler_Shape, suv).a;

                // 最终强度 = 解析式圆 × 贴图 alpha
                a *= s;
                return half4(a * _Value, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
