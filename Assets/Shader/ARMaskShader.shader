// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ARMaskedTexture"
{
	Properties{
			 _TransparentColor("Transparent Color", Color) = (1,1,1,1)
			 _Threshold("Threshhold", Float) = 0.1
			 _MainTex("Albedo (RGB)", 2D) = "white" {}
			_MaskTex("MaskTexture", 2D) = "white" {}
	}
	SubShader{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200

		Cull Off

		CGPROGRAM
		#pragma surface surf Lambert alpha
		
		sampler2D _MainTex;
		sampler2D _MaskTex;
		
		struct Input {
			float2 uv_MainTex;
			float2 uv_MaskTex;
		};

		fixed4 _Color;
		fixed4 _TransparentColor;
		half _Threshold;

		void surf(Input IN, inout SurfaceOutput o) {
			// Read color from the texture
			half4 col = tex2D(_MainTex, IN.uv_MainTex);
			half4 mask = tex2D(_MaskTex, IN.uv_MaskTex);

			col.a = mask.x * mask.x * mask.x;

			// Output colour will be the texture color * the vertex colour
			half4 output_col = col; //* _Color;

			//calculate the difference between the texture color and the transparent color
			//note: we use 'dot' instead of length(transparent_diff) as its faster, and
			//although it'll really give the length squared, its good enough for our purposes!
			half3 transparent_diff = col.xyz - _TransparentColor.xyz;
			half transparent_diff_squared = dot(transparent_diff,transparent_diff);

			//if colour is too close to the transparent one, discard it.
			//note: you could do cleverer things like fade out the alpha
			if (transparent_diff_squared < _Threshold)
				discard;

			//output albedo and alpha just like a normal shader
			o.Emission = output_col.rgb;
			o.Alpha = output_col.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
 
}