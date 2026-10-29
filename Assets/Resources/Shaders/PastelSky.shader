// Casual/PastelSky — soft crayon gradient sky: peach horizon -> pink -> lavender -> sky blue, soft clouds + grain.
Shader "Casual/PastelSky"
{
    Properties
    {
        _Top ("Top", Color) = (0.62,0.8,0.98,1)
        _Mid ("Mid", Color) = (0.78,0.87,0.94,1)
        _Horizon ("Horizon", Color) = (1,0.94,0.84,1)
        _Ground ("Ground", Color) = (0.95,0.88,0.8,1)
        _Cloud ("Cloud", Color) = (1,0.98,0.96,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Top, _Mid, _Horizon, _Ground, _Cloud;
            struct A { float4 pos:POSITION; };
            struct V { float4 cs:SV_POSITION; float3 d:TEXCOORD0; };
            float h21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vn(float2 x) { float2 i = floor(x), f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(h21(i), h21(i + float2(1,0)), f.x), lerp(h21(i + float2(0,1)), h21(i + float2(1,1)), f.x), f.y); }
            V vert(A i) { V o; o.cs = TransformObjectToHClip(i.pos.xyz); o.d = i.pos.xyz; return o; }
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.d); float y = d.y;
                float3 c = lerp(_Horizon.rgb, _Mid.rgb, smoothstep(0.0, 0.28, y));
                c = lerp(c, _Top.rgb, smoothstep(0.22, 0.75, y));
                c = lerp(c, _Ground.rgb, smoothstep(0.0, -0.12, y));
                float2 uv = d.xz / max(y + 0.25, 0.05);
                float cl = vn(uv * 1.3 + _Time.x * 0.3) * 0.6 + vn(uv * 3.1) * 0.3 + vn(uv * 7) * 0.1;
                cl = smoothstep(0.55, 0.75, cl) * smoothstep(0.02, 0.2, y);
                c = lerp(c, _Cloud.rgb, cl * 0.85);
                float g = vn(i.cs.xy * 0.9) * 0.5 + vn(float2(i.cs.x * 0.15, i.cs.y * 1.8)) * 0.5;   // screen-space crayon grain
                c *= lerp(0.995, 1.005, g);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
