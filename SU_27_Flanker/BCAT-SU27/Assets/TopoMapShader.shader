Shader "Joon/TopoMap" 
{
    Properties
    {
        _startDepth ("Start Depth", float) = 0
        _scale ("Depth Scale", float) = 0
        _lineHeight ("Line Height", float) = 0
        _Gradient("", 2D) = "" {}
        _colorLine ("Color Line", Color) = (0,0,0,0) // color
        _lineWidth ("Line Width", float) = 0.1
    }
    

	SubShader{
		Tags{ "RenderType" = "Opaque" }
		Pass{
			CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 worldPos : NORMAL;
			};

			float _startDepth;
			float _scale;
			float _lineHeight;
            sampler2D_float _CameraDepthTexture;
            float2 _CameraDepthTexture_TexelSize;
            
            float4 _colorLine;
            sampler2D _Gradient;
            float _lineWidth;

            half4 frag(v2f_img i) : SV_Target
            {
                float3 viewDir = UNITY_MATRIX_V[2].xyz;
                viewDir = normalize(viewDir);
                
                // sample points of the depth texture
                float2 uv1 = i.uv + float2(0,_CameraDepthTexture_TexelSize.y);           // BR
                float2 uv2 = i.uv + float2(_CameraDepthTexture_TexelSize.x, 0); // TR
                
                // Depth samples
                float zs0 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float zs1 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv1);
                float zs2 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv2);

                // Convert to linear depth values.
                float z0 = floor(zs0 / _lineHeight);
                float z1 = floor(zs1 / _lineHeight);
                float z2 = floor(zs2 / _lineHeight);

                // calculate gradient sampled color                
                float v = ((z0 * _lineHeight) - _startDepth) * _scale;
                half4 gradientColor = tex2D(_Gradient, float2(v, 0));
        
                // check if any point lies in a different height slice
                float z = abs(z0-z1+z0-z2);
                                
                // line anti-aliasing
                float z_diff = abs(zs0 / _lineHeight - z0);
                float aa = smoothstep(0, _lineWidth, z_diff);
                half4 lineColor = lerp(_colorLine, gradientColor, aa);

                if(abs(z) > 0) return lineColor;

                // else, return gradient sampled color
                return gradientColor;
            }
            
            ENDCG
		}
	}
}