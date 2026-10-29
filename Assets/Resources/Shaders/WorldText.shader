Shader "Soft/WorldText" {
  Properties { _MainTex ("Font Texture", 2D) = "white" {} _Color ("Color", Color) = (1,1,1,1) }
  SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    Lighting Off Cull Off ZWrite Off ZTest LEqual
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _TextAAOff;
      struct a { float4 v:POSITION; fixed4 c:COLOR; float2 uv:TEXCOORD0; };
      struct v2f { float4 p:SV_POSITION; fixed4 c:COLOR; float2 uv:TEXCOORD0; };
      v2f vert(a i){ v2f o; o.p=UnityObjectToClipPos(i.v); o.c=i.c*_Color;
      #ifndef UNITY_COLORSPACE_GAMMA
      o.c.rgb=GammaToLinearSpace(o.c.rgb);
      #endif
      o.uv=TRANSFORM_TEX(i.uv,_MainTex); return o; }
      fixed4 frag(v2f i):SV_Target {
        fixed4 c=i.c;
        // v0.8.1: the dynamic font atlas has no mips -> minified glyphs alias/shimmer when the camera moves.
        // Rotated-grid 5-tap box filter sized to the pixel footprint, then a footprint-aware alpha soften.
        float2 dx=ddx(i.uv), dy=ddy(i.uv);
        float a0=tex2D(_MainTex,i.uv).a;
        float a=(a0*2 + tex2D(_MainTex,i.uv+dx*0.375+dy*0.125).a + tex2D(_MainTex,i.uv-dx*0.375-dy*0.125).a
                      + tex2D(_MainTex,i.uv+dx*-0.125+dy*0.375).a + tex2D(_MainTex,i.uv-dx*-0.125-dy*0.375).a)/6;
        a=lerp(a, a0, _TextAAOff);
        c.a*=a; clip(c.a-0.01); return c; }
      ENDCG
    }
  }
}
