// Normal Mapping for a Triplanar Shader - Ben Golus 2017
// Unity Surface Shader example shader

// Implements correct triplanar normals in a Surface Shader with out computing or passing additional information from the
// vertex shader.

Shader "TriplanarAbsolute" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
		_TexScale("Texture scale", Float) = 1
		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
		_Glossiness("Smoothness", Range(0, 1)) = 0.5
		[Gamma] _Metallic("Metallic", Range(0, 1)) = 0
		[NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			#include "UnityStandardUtils.cginc"

			// flip UVs horizontally to correct for back side projection
			#define TRIPLANAR_CORRECT_PROJECTED_U

			// offset UVs to prevent obvious mirroring
			// #define TRIPLANAR_UV_OFFSET

			// Reoriented Normal Mapping
			// http://blog.selfshadow.com/publications/blending-in-detail/
			// Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
			half3 blend_rnm(half3 n1, half3 n2)
			{
				n1.z += 1;
				n2.xy = -n2.xy;

				return n1 * dot(n1, n2) / n1.z - n2;
			}

			sampler2D _MainTex;
			//float4 _MainTex_ST;
			float _TexScale;
			float4 _Color;

			sampler2D _BumpMap;
			sampler2D _OcclusionMap;

			half _Glossiness;
			half _Metallic;

			half _OcclusionStrength;

			struct Input {
				float3 worldPos;
				float3 worldNormal;
				INTERNAL_DATA
			};

			float3 WorldToTangentNormalVector(Input IN, float3 normal) {
				float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
				float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
				float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
				float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
				return normalize(mul(t2w, normal));
			}

			void surf(Input IN, inout SurfaceOutputStandard o) {
				// work around bug where IN.worldNormal is always (0,0,0)!
				IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));

				/*
				// get translation from matrix
				float3 pos = unity_WorldToObject._m03_m13_m23;

				// get unscaled rotation from matrix
				float3x3 rot = float3x3(
					normalize(unity_WorldToObject._m00_m01_m02),
					normalize(unity_WorldToObject._m10_m11_m12),
					normalize(unity_WorldToObject._m20_m21_m22)
					);
				// make box mapping with rotation preserved
				float3 map = mul(rot, IN.worldPos) + pos;
				float3 norm = mul(rot, IN.worldNormal);

				float3 blend = abs(norm) / dot(abs(norm), float3(1, 1, 1));
				float2 uv;
				if (blend.x > max(blend.y, blend.z)) {
					uv = map.yz;
				}
				else if (blend.z > blend.y) {
					uv = map.xy;
				}
				else {
					uv = map.xz;
				}
				*/

				// calculate triplanar blend
				half3 triblend = saturate(pow(IN.worldNormal, 4));
				triblend /= max(dot(triblend, half3(1,1,1)), 0.0001);

				// calculate triplanar uvs
				// applying texture scale and offset values ala TRANSFORM_TEX macro
				float2 uvX = IN.worldPos.zy * (1 / _TexScale);// +_MainTex_ST.zy;
				float2 uvY = IN.worldPos.xz * (1 / _TexScale);// +_MainTex_ST.zy;
				float2 uvZ = IN.worldPos.xy * (1 / _TexScale);// +_MainTex_ST.zy;

				// offset UVs to prevent obvious mirroring
			#if defined(TRIPLANAR_UV_OFFSET)
				uvY += 0.33;
				uvZ += 0.67;
			#endif

				// minor optimization of sign(). prevents return value of 0
				half3 axisSign = IN.worldNormal < 0 ? -1 : 1;

				// flip UVs horizontally to correct for back side projection
			#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
				uvX.x *= axisSign.x;
				uvY.x *= axisSign.y;
				uvZ.x *= -axisSign.z;
			#endif

				//fixed4 c = tex2D(_MainTex, uv * (1 / _TexScale));

				
				// albedo textures
				fixed4 colX = tex2D(_MainTex, uvX) * _Color.rgba;
				fixed4 colY = tex2D(_MainTex, uvY) * _Color.rgba;
				fixed4 colZ = tex2D(_MainTex, uvZ) * _Color.rgba;
				fixed4 col = colX * triblend.x + colY * triblend.y + colZ * triblend.z;
				

				// occlusion textures
				half occX = tex2D(_OcclusionMap, uvX).g;
				half occY = tex2D(_OcclusionMap, uvY).g;
				half occZ = tex2D(_OcclusionMap, uvZ).g;
				half occ = LerpOneTo(occX * triblend.x + occY * triblend.y + occZ * triblend.z, _OcclusionStrength);

				// tangent space normal maps
				half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
				half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
				half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));

				// flip normal maps' x axis to account for flipped UVs
			#if defined(TRIPLANAR_CORRECT_PROJECTED_U)
				tnormalX.x *= axisSign.x;
				tnormalY.x *= axisSign.y;
				tnormalZ.x *= -axisSign.z;
			#endif

				half3 absVertNormal = abs(IN.worldNormal);

				// swizzle world normals to match tangent space and apply reoriented normal mapping blend
				tnormalX = blend_rnm(half3(IN.worldNormal.zy, absVertNormal.x), tnormalX);
				tnormalY = blend_rnm(half3(IN.worldNormal.xz, absVertNormal.y), tnormalY);
				tnormalZ = blend_rnm(half3(IN.worldNormal.xy, absVertNormal.z), tnormalZ);

				// apply world space sign to tangent space Z
				tnormalX.z *= axisSign.x;
				tnormalY.z *= axisSign.y;
				tnormalZ.z *= axisSign.z;

				// sizzle tangent normals to match world normal and blend together
				half3 worldNormal = normalize(
					tnormalX.zyx * triblend.x +
					tnormalY.xzy * triblend.y +
					tnormalZ.xyz * triblend.z
					);

				// set surface ouput properties
				o.Albedo = col.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Occlusion = occ;

				// convert world space normals into tangent normals
				o.Normal = WorldToTangentNormalVector(IN, worldNormal);
			}
			ENDCG
		}
			FallBack "Diffuse"
}