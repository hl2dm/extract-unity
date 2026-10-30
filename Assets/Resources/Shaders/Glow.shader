Shader "Soft/Glow"
{   // v0.9.3 stable (no noise/animation) additive light glow: _Bill 1 = camera-facing halo, 0 = flat ground pool
    Properties { _Color ("Color", Color) = (1,0.75,0.45,0.3) _Bill ("Billboard", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Blend One One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Bill; CBUFFER_END
            struct A { float4 v : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 p : SV_POSITION; float2 uv : TEXCOORD0; float f : TEXCOORD1; };
            V vert(A a)
            {
                V o; float3 c = TransformObjectToWorld(float3(0,0,0));
                float sx = length(UNITY_MATRIX_M._m00_m10_m20);
                float3 w;
                if (_Bill > 0.5) { float3 r = UNITY_MATRIX_V[0].xyz, u = UNITY_MATRIX_V[1].xyz; w = c + (r * a.v.x + u * a.v.y) * sx; c = w; }
                else w = TransformObjectToWorld(a.v.xyz);
                o.p = TransformWorldToHClip(w); o.uv = a.uv;
                o.f = saturate(distance(w, _WorldSpaceCameraPos) / 1.5);  // fade when camera is inside the glow
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.uv * 2 - 1); float k = saturate(1 - r); k = k * k * (3 - 2 * k); k *= k;
                return half4(_Color.rgb * _Color.a * k * i.f, 0);
            }
            ENDHLSL
        }
    }
}
