Shader "Instanced/MultiColoredDetail" {
	Properties {
		_BaseColor ("BaseColor", Vector) = (1,1,1,1)
		_ColorR ("ColorR", Vector) = (1,1,1,1)
		_ColorG ("ColorG", Vector) = (1,1,1,1)
		_ColorB ("ColorB", Vector) = (1,1,1,1)
		_ColorMap ("Color Map (AlphaSpec)", 2D) = "white" {}
		_MainTex ("Detail", 2D) = "white" {}
		_Specular ("Specular", Vector) = (0.5,0.5,0.5,1)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}