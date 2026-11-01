Shader "Soft/NoiseRing"
{   // v0.9.19 soft translucent noise ring: faint filled disc + feathered glowing rim + gentle outward ripple (alpha blended, no hard line)
    Properties { _Color ("Color", Color) = (1,0.8,0.4,1) _A ("Alpha", Float) = 0.3 _Edge ("Rim width (uv)", Float) = 0.06 }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off Offset -2, -2
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _A; float _Edge; CBUFFER_END
            struct A { float4 v : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 p : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A a) { V o; o.p = TransformObjectToHClip(a.v.xyz); o.uv = a.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.uv * 2 - 1); if (r > 1) discard;
                float e = max(_Edge, 0.01);
                float fill = 0.24 * smoothstep(1.0, 0.75, r) * r * r;                    // translucent tint, clear near the kid, builds toward the rim
                float d = (r - (1 - 1.6 * e)) / e; float rim = exp(-d * d) + 0.55 * exp(-d * d * 6);                       // feathered rim + brighter soft core so the boundary stays readable up close                 // feathered glowing rim
                float p = frac(_Time.y * 0.55); float dw = (r - p) / (e * 1.6);
                float wave = 0.16 * exp(-dw * dw) * (1 - p) * p * 4;                        // soft ripple expanding outward
                float fo = saturate((1 - r) * 12);                      // fade to 0 at the very edge
                float a = saturate((fill + rim + wave) * _A * fo);
                return half4(_Color.rgb * (1 + 0.25 * rim), a);
            }
            ENDHLSL
        }
    }
}
