Shader "Custom/NoiseCloud" {
	Properties {
		_TintColor ("Tint Color", Vector) = (0.5,0.5,0.5,0.5)
		_ShadowColor ("Shadow Color", Vector) = (0.1,0.1,0.1,0.5)
		_MainTex ("CircleFade Texture", 2D) = "white" {}
		_NoiseTex1 ("Noise 1", 2D) = "white" {}
		_NoiseTex2 ("Noise 2", 2D) = "white" {}
		_TexScale ("Scale", Float) = 1
		_SunShinePower ("SunShinePower", Float) = 1
		_SunShineMult ("SunShineMult", Float) = 1
		_MaxDist ("Max Distance", Range(0.01, 100000)) = 15000
		_NoiseAlphaPower ("NoiseAlphaPower", Float) = 1
		_NoiseAlphaMult ("NoiseAlphaMult", Float) = 1
		_CurveDown ("Curve Down", Float) = 1
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
}