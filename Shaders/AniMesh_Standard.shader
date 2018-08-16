Shader "Instanced/AniMesh_Standard" {
	Properties {
		[PerRendererData]_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_GMA ("GMA (Gloss, Metallic, AO)", 2D) = "white"{}
		_Normal ("Normal Map", 2D) = "bump" {}

		// Animation
		_AnimateTexture("Animation Texture", 2D)= "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		// And generate the shadow pass with instancing support
		#pragma surface surf Standard vertex:vert addshadow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		// Enable instancing for this shader
		#pragma multi_compile_instancing

		// variables
		sampler2D _MainTex;
		sampler2D _AnimateTexture;
		sampler2D _Normal;
		sampler2D _GMA;

		struct Input {
			float2 uv_MainTex;
		};

		// functions
		float remap(float v){
			return (v*2.0)-1.0;
		}

		// material property block
		UNITY_INSTANCING_CBUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(float4, _AnimationData1) // Frame first animation
			UNITY_DEFINE_INSTANCED_PROP(float4, _AnimationData2) // Frame second animation
		UNITY_INSTANCING_CBUFFER_END

		// surface void
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 GMA = tex2D(_GMA,IN.uv_MainTex);
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = GMA.g;
			o.Smoothness = GMA.r;
			o.Occlusion = GMA.b;
			o.Alpha = c.a;
			o.Normal = UnpackNormal(tex2D(_Normal,IN.uv_MainTex));
		}

		// vert
		void vert(inout appdata_full v){
			float4 AnimationData1 = UNITY_ACCESS_INSTANCED_PROP(_AnimationData1);
			float4 AnimationData2 = UNITY_ACCESS_INSTANCED_PROP(_AnimationData2);

			float4 Anim1 = tex2Dlod(_AnimateTexture,float4(v.texcoord1.x,AnimationData1.r,0,0)); // get first animation frame
			float4 Anim2 = tex2Dlod(_AnimateTexture,float4(v.texcoord1.x,AnimationData2.r,0,0)); // get second animation frame

			float4 dirVec1 = (float4(remap(Anim1.r),remap(Anim1.g),remap(Anim1.b),0.0) * AnimationData1.g) * (Anim1.a * AnimationData1.b); // calculate first animation direction
			float4 dirVec2 = (float4(remap(Anim2.r),remap(Anim2.g),remap(Anim2.b),0.0) * AnimationData2.g) * (Anim2.a * AnimationData2.b);  // calculate first animation direction
			float4 finalDirection = dirVec1 + dirVec2;

			v.vertex += finalDirection;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
