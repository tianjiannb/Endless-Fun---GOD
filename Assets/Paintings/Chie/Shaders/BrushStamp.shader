Shader "Numen/BrushStamp"
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
                float2 d = (i.uv - _Center.xy) * float2(_Aspect, 1.0);
                float  r = length(d) / max(_Radius, 1e-5);
                float  a = 1.0 - smoothstep(1.0 - _Hardness, 1.0, r);
                return half4(0, 0, 0, a * _Value);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
