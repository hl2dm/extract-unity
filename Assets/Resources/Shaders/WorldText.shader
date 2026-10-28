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
      sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
      struct a { float4 v:POSITION; fixed4 c:COLOR; float2 uv:TEXCOORD0; };
      struct v2f { float4 p:SV_POSITION; fixed4 c:COLOR; float2 uv:TEXCOORD0; };
      v2f vert(a i){ v2f o; o.p=UnityObjectToClipPos(i.v); o.c=i.c*_Color; o.uv=TRANSFORM_TEX(i.uv,_MainTex); return o; }
      fixed4 frag(v2f i):SV_Target { fixed4 c=i.c; c.a*=tex2D(_MainTex,i.uv).a; clip(c.a-0.01); return c; }
      ENDCG
    }
  }
}
