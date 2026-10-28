// Casual/Toon — pastel crayon (粉蠟筆) look: soft 3-band shading, lavender shadows, flattened+pastelised albedo,
// procedural world-space crayon grain / paper tooth / shadow hatching, wobbly warm hand-drawn outline.
Shader "Casual/Toon"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Albedo", 2D) = "white" {}
        _ShadeColor ("Shade (multiplier)", Color) = (0.72,0.66,0.9,1)
        _RimColor ("Rim", Color) = (1,0.97,0.92,1)
        _OutlineColor ("Outline", Color) = (0.36,0.24,0.27,1)
        _OutlineWidth ("Outline Width", Float) = 0.012
        _Cutoff ("Cutoff", Range(0,1)) = 0
        _TexAmt ("Texture detail", Range(0,1)) = 0.18
        _PaletteAmt ("Palette snap", Range(0,1)) = 0.5
        _Grain ("Crayon grain", Range(0,1)) = 1
        _UseAvg ("Use texture average colour", Range(0,1)) = 1
        _UseVC ("Use vertex colour", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float4 _BaseMap_ST; float4 _ShadeColor; float4 _RimColor; float4 _OutlineColor;
            float _OutlineWidth; float _Cutoff; float _TexAmt; float _PaletteAmt; float _Grain; float _UseAvg; float _UseVC;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        float h31(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
        float vn(float3 x) { float3 i = floor(x); float3 f = frac(x); f = f * f * (3 - 2 * f);
            return lerp(lerp(lerp(h31(i), h31(i + float3(1,0,0)), f.x), lerp(h31(i + float3(0,1,0)), h31(i + float3(1,1,0)), f.x), f.y),
                        lerp(lerp(h31(i + float3(0,0,1)), h31(i + float3(1,0,1)), f.x), lerp(h31(i + float3(0,1,1)), h31(i + float3(1,1,1)), f.x), f.y), f.z); }
        ENDHLSL

        Pass
        {
            Name "ForwardLit" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 pos:POSITION; float3 n:NORMAL; float2 uv:TEXCOORD0; float4 vc:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs:SV_POSITION; float2 uv:TEXCOORD0; float3 ws:TEXCOORD1; float3 nw:TEXCOORD2; float fog:TEXCOORD3; float4 vc:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.ws = TransformObjectToWorld(i.pos.xyz); o.nw = TransformObjectToWorldNormal(i.n); o.cs = TransformWorldToHClip(o.ws);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap); o.fog = ComputeFogFactor(o.cs.z); o.vc = i.vc; return o; }
            float3 snapPalette(float3 c)
            {
                // pastel palette: pink, mint, butter, lavender, sky, peach, cream, soft green, lilac-grey
                float3 P[9] = { float3(1,.76,.84), float3(.68,.92,.8), float3(1,.92,.6), float3(.8,.73,.95), float3(.66,.84,.98),
                                float3(1,.8,.66), float3(.98,.94,.86), float3(.74,.89,.6), float3(.8,.78,.88) };
                float best = 1e5; float3 b = c;
                [unroll] for (int k = 0; k < 9; k++) { float3 d = (c - P[k]) * float3(1.2, 1.5, 1); float e = dot(d, d); if (e < best) { best = e; b = P[k]; } }
                return b;
            }
            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 tx = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float3 avg = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, i.uv, 9).rgb;   // ~flat average colour of the texture
                clip(tx.a - _Cutoff);
                float lt = dot(tx.rgb, 0.333), la = max(dot(avg, 0.333), 0.05);
                float3 alb = lerp(float3(1,1,1), avg, _UseAvg) * _BaseColor.rgb * lerp(1, clamp(lt / la, 0.6, 1.4), _TexAmt) * lerp(float3(1,1,1), i.vc.rgb, _UseVC);   // realistic detail reduced to a faint wash
                // pastelise: gentle saturation lift, then raise value toward paper white, then nudge toward palette
                float l = dot(alb, float3(.3,.59,.11)); alb = saturate(lerp(l.xxx, alb, 1.15 + 0.25 * _UseVC));
                alb = lerp(alb, 1, 0.12 * (1 - _UseVC)); alb = max(alb, 0.28);
                alb = lerp(alb, snapPalette(alb), _PaletteAmt * (1 - _UseVC)) * lerp(0.92, 0.9, _UseVC);
                float grain = _Grain * lerp(0.12, 0.0, _UseVC);

                float3 n = normalize(i.nw); float3 ws = i.ws;
                float4 sc = TransformWorldToShadowCoord(ws); Light L = GetMainLight(sc);
                float nl = dot(n, L.direction) * 0.5 + 0.5;
                float lit = nl * lerp(0.35, 1, L.shadowAttenuation);
                // crayon grain: triplanar-ish streaky noise in world space
                float3 an = abs(n); float3 q = ws * 9.0;
                float g1 = vn(float3(q.x * 0.35 + q.z * 0.35, q.y * 2.8, q.z * 0.2) );
                float g2 = vn(q * 3.1 + 7.3);
                float g = saturate(g1 * 0.55 + g2 * 0.45);
                // wobble the band edges by the grain so they look hand-shaded
                float lw = lit + (g - 0.5) * 0.12 * grain;
                float b1 = smoothstep(0.36, 0.46, lw), b2 = smoothstep(0.66, 0.76, lw);
                float band = b1 * 0.55 + b2 * 0.45;                                          // 0 shade, .55 mid, 1 lit
                float3 shade = alb * _ShadeColor.rgb;
                float3 col = lerp(shade, alb, band);
                col *= lerp(float3(1,1,1), L.color.rgb / max(1, max(L.color.r, max(L.color.g, L.color.b))), 0.35);
                col += SampleSH(n) * alb * 0.08;
                // hatching in shade (diagonal strokes)
                float hs = frac((ws.x + ws.y * 1.3 - ws.z * 0.7) * 6.0 + vn(ws * 3) * 0.6);
                float hatch = smoothstep(0.35, 0.5, hs) * smoothstep(0.85, 0.7, hs);
                col = lerp(col, col * float3(0.86, 0.82, 0.95), hatch * (1 - band) * 0.8 * grain);
                // crayon tooth: grain darkens slightly, paper peeks through in the gaps
                col *= lerp(0.84, 1.04, g * grain + (1 - grain) * 0.6);
                float tooth = pow(saturate(1 - g2 * 1.35), 3);
                col = lerp(col, float3(1, 0.98, 0.94), tooth * 0.22 * grain);
                // toy material classes from vertex alpha: 1 matte clay, 0.5 glossy plastic, ~0.1 felt
                float3 vd = normalize(GetWorldSpaceViewDir(ws));
                float ma = lerp(1, i.vc.a, _UseVC);
                float gl = saturate(1 - abs(ma - 0.5) * 4), felt = saturate((0.3 - ma) * 5), clay = saturate((ma - 0.75) * 4);
                float3 hv = normalize(L.direction + vd);
                float sp = smoothstep(0.955, 0.975, dot(n, hv)) * L.shadowAttenuation;
                col += sp * gl * 0.55;
                col *= lerp(1, 0.97 + 0.03 * vn(ws * 20), felt);
                
                float rim = smoothstep(0.62, 0.95, 1 - saturate(dot(n, vd))) * (0.22 + 0.25 * felt + 0.1 * gl);
                col += _RimColor.rgb * rim;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Outline" Tags { "LightMode"="SRPDefaultUnlit" } Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            struct A { float4 pos:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs:SV_POSITION; float fog:TEXCOORD0; float w:TEXCOORD1; };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i);
                float3 ws = TransformObjectToWorld(i.pos.xyz); float3 nw = normalize(TransformObjectToWorldNormal(i.n));
                float d = distance(ws, GetCameraPositionWS());
                float boil = 0;                                           // hand-drawn "boil" at 4 fps
                float wob = vn(ws * 3.0 + boil * 1.7);
                float w = _OutlineWidth * clamp(d * 0.25, 0.4, 3) * lerp(0.9, 1.1, wob);
                ws += nw * w; o.cs = TransformWorldToHClip(ws); o.fog = ComputeFogFactor(o.cs.z); o.w = wob; return o; }
            half4 frag(V i) : SV_Target { float3 c = _OutlineColor.rgb * 1.0; return half4(MixFog(c, i.fog), 1); }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 pos:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A i) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(i);
                float3 ws = TransformObjectToWorld(i.pos.xyz); float3 nw = TransformObjectToWorldNormal(i.n);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nw, _LightDirection));
                #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return cs; }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct A { float4 pos:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A i) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(i); return TransformObjectToHClip(i.pos.xyz); }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
