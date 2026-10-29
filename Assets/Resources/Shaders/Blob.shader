Shader "Soft/Blob"
{
    Properties { _BaseColor ("Color", Color) = (0.28, 0.2, 0.14, 0.38) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-50" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert (A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }
            half4 frag (V i) : SV_Target
            {
                float d = saturate(length(i.uv - 0.5) * 2.0);
                float a = 1.0 - d; a = a * a * (3.0 - 2.0 * a);
                return half4(_BaseColor.rgb, _BaseColor.a * a);
            }
            ENDHLSL
        }
    }
}
