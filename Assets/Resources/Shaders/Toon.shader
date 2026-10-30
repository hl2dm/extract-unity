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
        _MatA ("Material class id (non-VC)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float4 _BaseMap_ST; float4 _ShadeColor; float4 _RimColor; float4 _OutlineColor;
            float _OutlineWidth; float _Cutoff; float _TexAmt; float _PaletteAmt; float _Grain; float _UseAvg; float _UseVC; float _MatA;
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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // v0.8 lighting globals (0 = v0.7 look): x fill, y AO, z lamp pools, w rim boost
            float _MatDbg; float4 _ToonLight; float4 _ToonFillCol; float4 _ToonFillDir; float4 _ToonSkyCol;
            float4 _ToonSoft; float4 _ToonDusk;
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
                alb = lerp(alb, 1, 0.12 * (1 - _UseVC) * (1 - 0.85 * _ToonSoft.y)); alb = max(alb, 0.28); float Vv = _ToonSoft.w; alb = min(alb, 1 - 0.2 * _ToonSoft.z * (1 - Vv)) * lerp(1, float3(1.0, 0.97, 0.92), (1 - Vv) * _ToonSoft.z * saturate((dot(alb, 0.333) - 0.7) * 3.3));
                { float lv = dot(alb, float3(.3,.59,.11)); alb = saturate(lerp(lv.xxx, alb, 1 + 0.4 * Vv)); lv = dot(alb, float3(.3,.59,.11)); alb *= lerp(1, min(1, 0.66 / max(lv, 0.01)), Vv); float chv = max(alb.r, max(alb.g, alb.b)) - min(alb.r, min(alb.g, alb.b)); alb *= lerp(1, float3(1.0, 0.91, 0.78), Vv * saturate((0.2 - chv) * 7) * saturate((lv - 0.45) * 4)); }
                alb = lerp(alb, snapPalette(alb), _PaletteAmt * (1 - _UseVC)) * lerp(0.92, 0.9, _UseVC);
                float grain = _Grain * lerp(0.12, 0.0, _UseVC);

                float3 n = normalize(i.nw); float3 ws = i.ws;
                float4 sc = TransformWorldToShadowCoord(ws); Light L = GetMainLight(sc);
                float nl = dot(n, L.direction) * 0.5 + 0.5;
                float S = _ToonSoft.x;   // v0.8.1 softness (0 = v0.8)
                float S2 = _ToonSoft.y;  // v0.9.1 overcast softness (0 = v0.9)
                float D = _ToonDusk.a;   // v0.9.3 dusk: real low-sun shading, warm key on lit side only
                nl = lerp(nl, nl * 0.45 + 0.55, S2 * (1 - 0.55 * D));   // wrapped, flatter overcast key
                float lit = nl * lerp(lerp(lerp(lerp(0.35, 0.62, S), 0.84, S2), 0.5, D), 1, L.shadowAttenuation);
                // crayon grain: triplanar-ish streaky noise in world space
                float3 an = abs(n); float3 q = ws * 9.0;
                float g1 = vn(float3(q.x * 0.35 + q.z * 0.35, q.y * 2.8, q.z * 0.2) );
                float g2 = vn(q * 3.1 + 7.3);
                float g = saturate(g1 * 0.55 + g2 * 0.45);
                // wobble the band edges by the grain so they look hand-shaded
                float lw = lit + (g - 0.5) * 0.12 * grain;
                float b1 = smoothstep(0.36 - 0.2 * S - 0.1 * S2, 0.46 + 0.2 * S + 0.1 * S2, lw), b2 = smoothstep(0.66 - 0.2 * S - 0.1 * S2, 0.76 + 0.2 * S + 0.1 * S2, lw);
                float band = b1 * 0.55 + b2 * 0.45;                                          // 0 shade, .55 mid, 1 lit
                float3 shade = alb * lerp(lerp(lerp(_ShadeColor.rgb, float3(1, 0.98, 0.95), 0.32 * S), float3(0.86, 0.83, 0.8), S2), lerp(float3(0.82, 0.74, 0.86), _ToonDusk.rgb, _ToonDusk.a), _ToonSoft.w);
                float3 col = lerp(shade, alb, band);
                col *= lerp(float3(1,1,1), L.color.rgb / max(1, max(L.color.r, max(L.color.g, L.color.b))), lerp(0.35, 0.85 * band, D));
                col += SampleSH(n) * alb * (0.08 + 0.1 * S);
                // hatching in shade (diagonal strokes)
                float hs = frac((ws.x + ws.y * 1.3 - ws.z * 0.7) * 6.0 + vn(ws * 3) * 0.6);
                float hatch = smoothstep(0.35, 0.5, hs) * smoothstep(0.85, 0.7, hs);
                col = lerp(col, col * float3(0.86, 0.82, 0.95), hatch * (1 - band) * 0.8 * (1 - 0.5 * D) * grain);
                // crayon tooth: grain darkens slightly, paper peeks through in the gaps
                col *= lerp(0.84, 1.04, g * grain + (1 - grain) * 0.6);
                float tooth = pow(saturate(1 - g2 * 1.35), 3);
                col = lerp(col, float3(1, 0.98, 0.94), tooth * 0.22 * (1 - 0.7 * D) * grain * (1 - 0.6 * S2) * (1 - _ToonSoft.w));
                // toy material classes from vertex alpha: 1 matte clay, 0.5 glossy plastic, ~0.1 felt
                float3 vd = normalize(GetWorldSpaceViewDir(ws));
                float ma = lerp(_MatA > 0 ? _MatA : 1, i.vc.a, _UseVC);
                float gl = saturate(1 - abs(ma - 0.5) * 4), felt = saturate((0.3 - ma) * 5), clay = saturate((ma - 0.75) * 4);
                float3 hv = normalize(L.direction + vd);
                float sp = smoothstep(0.955 - 0.07 * S, 0.975 + 0.02 * S, dot(n, hv)) * L.shadowAttenuation;
                col += sp * gl * 0.55 * (1 - 0.82 * S) * (1 - 0.9 * S2) * (1 - D);
                if (D > 0)
                {   // v0.9.3 material library (vertex alpha id): soft, dusk-coloured, capped spec so nothing glares or sparkles
                    float id = round(ma * 20), ks = 0.005, ex = 4, fr = 0, mt = 0, sh = 0.1, bk = 0, gs = 0;
                    if (id > 19.5)      { ks = 0.02; ex = 6;  fr = 0.03; sh = 0; col *= 0.98 + 0.03 * vn(ws * 3.7); }                      // plaster / painted wall: matte
                    else if (id > 18.5) { ks = 0.26 * (0.5 + g1); ex = 18; fr = 0.12; sh = 0; float gr = frac((ws.x + ws.z) * 0.6 + ws.y * 7.0 + vn(ws * float3(2, 0.4, 2)) * 1.6); col *= 0.97 + 0.03 * smoothstep(0.3, 0.7, gr); }   // v0.9.5: no forced grain texture, just a faint sheen variation     // wood door/furniture: satin, grain sheen
                    else if (id > 17.5) { ks = 0.2 * (0.8 + 0.4 * g1); ex = 14; fr = 0.32; sh = 0; }     // varnished floor: broad soft gloss
                    else if (id > 16.5) { ks = 0.3; ex = 44; fr = 0.4; sh = 0; float2 jj = abs(frac(ws.xz * 1.6) - 0.5); col *= 1 - 0.22 * smoothstep(0.43, 0.49, max(jj.x, jj.y)); col *= 0.95 + 0.1 * vn(ws * 6); }                         // tile / terrazzo
                    else if (id > 15.5) { ks = 0; ex = 4; fr = 0.01; sh = 0; col *= (0.86 + 0.2 * vn(ws * 9)) * (0.9 + 0.16 * step(0.82, vn(ws * 37))); }                            // asphalt: very rough
                    else if (id > 14.5) { ks = 0.3; ex = 80; fr = 0.55; sh = 0; gs = 1; }                 // glass: sharp + fresnel sky
                    else if (id > 13.5) { ks = 0.7; ex = 90; fr = 0.32; mt = 1; sh = 0; col *= 0.92 + 0.12 * frac(ws.y * 9 + ws.x * 0.3); }                 // metal: albedo-tinted spec
                    else if (id > 12.5) { ks = 0.012; ex = 6; fr = 0.02; bk = 0.25; sh = 0; }             // foliage: rough + backlight
                    else if (id > 11.5) { ks = 0.12; ex = 26; fr = 0.1; sh = 0; }                         // roof tiles: semi-gloss
                    else if (id > 9.5)  { ks = 0.13; ex = 34; fr = 0.1; sh = 0; }                         // painted plastic / trim
                    else if (id > 3.5)  { ks = 0.008; ex = 4; sh = 0.16; }                                // fabric: rough + sheen
                    ex = min(ex, lerp(24, 80, saturate(_ToonLight.w * 2 - 1)));                           // mobile: broader = cheaper/stabler
                    float3 Lc = L.color.rgb / max(1, max(L.color.r, max(L.color.g, L.color.b)));
                    float nh = saturate(dot(n, hv)), nlr = saturate(dot(n, L.direction)), nv = saturate(dot(n, vd));
                    float spc = min(ks * pow(nh, ex) * sqrt(ex) * 0.5, 0.4 + 0.2 * mt) * nlr * L.shadowAttenuation;
                    col *= lerp(1, 0.82, mt) * lerp(1, 0.86, gs);
                    col += spc * lerp(Lc, alb * Lc * 1.3, mt);
                    float fv = fr * pow(1 - nv, 4);
                    float3 rv = reflect(-vd, n);
                    col = lerp(col, SampleSH(rv) * lerp(0.9, 1.15, gs), saturate(fv * 0.6 + fr * 0.5));   // glossy classes mirror the dusk sky a little even head-on
                    float3 fd = normalize(_ToonFillDir.xyz + float3(0, 0.6, 0));                            // soft secondary highlight from the lavender sky fill (shade side)
                    col += min(ks * pow(saturate(dot(n, normalize(fd + vd))), max(ex * 0.35, 3)) * 2.5, 0.3) * (1 - band) * lerp(_ToonFillCol.rgb * 2.2, alb * 0.6, mt);
                    col += alb * Lc * sh * pow(1 - nv, 3) * (0.4 + 0.6 * band);
                    col += alb * Lc * bk * pow(saturate(dot(vd, -L.direction)), 3);
                    col += gs * float3(1.0, 0.66, 0.34) * 0.75;
                    col += alb * (1 - band) * float3(0.12, 0.055, 0.0) * saturate(1.2 - abs(n.y)) * saturate(0.6 - n.y);   // v0.9.4 warm street bounce on shaded walls (cores stay violet)
                    if (_MatDbg > 0.5) col = frac(float3(id * 0.37, id * 0.61, id * 0.13)) * 0.8 + 0.1;   // warm lit-interior glow behind window glass (static)
                }
                col *= lerp(1, 0.97 + 0.03 * vn(ws * 20), felt);
                
                float rim = smoothstep(0.62, 0.95, 1 - saturate(dot(n, vd))) * (0.22 + 0.25 * felt + 0.1 * gl);
                // v0.8: cool sky fill (wraps into the shade side), sky bounce from above, lamp/window light pools, SSAO, sun-aligned rim
                float fl = saturate(dot(n, _ToonFillDir.xyz) * 0.6 + 0.4) * (1 - band * 0.9);
                col += alb * _ToonFillCol.rgb * fl * _ToonLight.x;
                col -= col * _ToonSkyCol.rgb * saturate(-n.y * 0.5 + 0.2) * _ToonLight.x;   // darker undersides = grounded contact
                #if defined(_ADDITIONAL_LIGHTS)
                if (_ToonLight.z > 0)
                {
                    float3 pool = 0; uint lc = GetAdditionalLightsCount();
                    for (uint li = 0u; li < lc; ++li)
                    {
                        Light A = GetAdditionalLight(li, ws);
                        float an2 = saturate(dot(n, A.direction) * 0.5 + 0.5);
                        pool += A.color * min(A.distanceAttenuation, 1.0) * smoothstep(0.25, 0.8, an2);
                    }
                    col += alb * min(pool * 0.5, lerp(0.55, 0.28, S2)) * _ToonLight.z;   // soft capped warm pool, never blows out the pastel
                }
                #endif
                #if defined(_SCREEN_SPACE_OCCLUSION)
                if (_ToonLight.y > 0)
                {
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.cs));
                    col *= lerp(1, lerp(lerp(lerp(0.45, 0.72, S2), 0.58, _ToonSoft.w), 1, ao.indirectAmbientOcclusion), _ToonLight.y);
                }
                #endif
                float sunSide = saturate(dot(normalize(cross(cross(vd, L.direction), vd) + 1e-4), n) * 0.5 + 0.5);
                rim *= (1 + _ToonLight.w * (0.6 + sunSide)) * (1 - 0.4 * S) * (1 - 0.65 * S2);
                col += _RimColor.rgb * rim * lerp(1, L.color.rgb / max(1, max(L.color.r, max(L.color.g, L.color.b))), _ToonLight.w * 0.5);
                col = lerp(col, col * 0.93 + float3(0.07, 0.065, 0.055), S);   // lifted blacks, warm
                { float3 kn = col > 0.55 ? 0.55 + (col - 0.55) * 0.4 : col;   // v0.9.1 soft knee: whites settle at ~0.78, no paper-white glare
                  col = lerp(col, kn * 0.96 + float3(0.03, 0.026, 0.018), S2); }
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
            float4 _ToonSoft;
            half4 frag(V i) : SV_Target { float3 c = _OutlineColor.rgb * 1.0; c = lerp(c, lerp(c, _BaseColor.rgb * 0.55, 0.3), _ToonSoft.z); return half4(MixFog(c, i.fog), 1); }
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
            Name "DepthNormals" Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct A { float4 pos:POSITION; float3 n:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 cs:SV_POSITION; float3 nw:TEXCOORD0; };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); o.cs = TransformObjectToHClip(i.pos.xyz); o.nw = TransformObjectToWorldNormal(i.n); return o; }
            half4 frag(V i) : SV_Target { return half4(NormalizeNormalPerPixel(i.nw), 0); }
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
