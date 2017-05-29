#ifndef MICRO_DETAIL_FABRIC_BRDF
#define MICRO_DETAIL_FABRIC_BRDF

#include "UnityLightingCommon.cginc"
#include "UnityGlobalIllumination.cginc"

	inline half3 KajiyaKay(half th)
	{
		return pow(sqrt(1 - th*th), 128);
	}

	//General Cloth Scattering
	inline half DongseokKimDistribution(half nh)
	{
		return 0.96 * pow((1 - nh), 2) + 0.057;
	}

	//lower fresnel power for more rim lighting
	inline half3 FresnelPow4(half3 f0, half cosA)
	{
		half t = Pow4(1 - cosA);
		return f0 + (1 - f0) * t;
	}

	//ala Naughty Dog cheap subsurface
	half4 _ScatteringColor;
	half  _ScatteringAmount;
	inline half3 WrappedDiffuse(half nl)
	{
		half diffuse = saturate((nl + _ScatteringAmount) / (1 + _ScatteringAmount));
		return saturate(_ScatteringColor + saturate(nl)) * diffuse;
	}

	float _FabricScale;
	inline float2 FabricUV(float2 uv)
	{
		uv.x = -uv.x;
		uv.y = -uv.y;
		return uv * _FabricScale;
	}

	sampler2D _WeavePatternMap;
	inline half4 WeaveElement(float2 uv)
	{
		half4 sample = tex2D(_WeavePatternMap, uv);
		sample.rgb = (sample.rgb * 2) - 0.5; //For some reason -0.5 works??
		return sample;
	}

	inline half4 WeaveElementLOD(float2 uv, int mipLevel)
	{
		half4 sample = tex2Dlod(_WeavePatternMap, float4(uv, 0, mipLevel));
		sample.rgb = (sample.rgb * 2) - 0.5; //For some reason -0.5 works??
		return sample;
	}

	inline half average_vector_components(half3 v)
	{
		return (v.x + v.y + v.z) / 3;
	}

	half _MicroShadowStrength;
	inline float MicroShadow(half ao, half nl, UnityLight light)
	{
		half shadowSample = average_vector_components(light.color);
		half aperture = 2 * ao * ao;
		half microShadow = saturate(abs(nl) + aperture - 1.0);

		return lerp(shadowSample, shadowSample * microShadow, _MicroShadowStrength);
	}

	inline half3 WorldWeave(half3 v, half3 t, half3 b, half3 n)
	{
		return normalize(t * v.x + b * v.y + n * v.z);
	}

	half _MicroDetailStrength;
	half4 MicroDetailFabricBRDF (half3 diffColor, half3 specColor,
								  half oneMinusReflectivity, half oneMinusRoughness,
								  half3 normal, half3 viewDir,
								  UnityLight light, UnityIndirect gi,
								  float2 uv,
								  float3 posWorld,
								  half3 tangent, half3 bitangent, half3 vertexNormal)
	{

#if defined(_MICRO_DETAILS_ON)

		half4 delta = WeaveElement(FabricUV(uv));
		normal = lerp(normal, WorldWeave(half3(delta.xy, 1), tangent, bitangent, normal), saturate(dot(vertexNormal, viewDir)));

		#if (_MICRO_SCATTERING_ON && _SSS_ON && _MICRO_DETAILS_ON)
			half4 deltaLow = WeaveElementLOD(FabricUV(uv), 5);
			half3 normalLow = WorldWeave(half3(deltaLow.xy, 1), tangent, bitangent, normal);
			half3 rN = lerp(normal, normalLow, _ScatteringColor.r);
			half3 gN = lerp(normal, normalLow, _ScatteringColor.g);
			half3 bN = lerp(normal, normalLow, _ScatteringColor.b);
		#endif

#endif

		half roughness = 1-oneMinusRoughness;
		half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);

	#if UNITY_BRDF_GGX 
		half shiftAmount = dot(normal, viewDir);
		normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;

#if (_MICRO_SCATTERING_ON && _SSS_ON && _MICRO_DETAILS_ON)
		half nl = half3(DotClamped(rN, light.dir), DotClamped(gN, light.dir), DotClamped(bN, light.dir));
#else
		half nl = DotClamped(normal, light.dir);
#endif
		
	#else
		half nl = light.ndotl;
	#endif

		half nh = BlinnTerm (normal, halfDir);
		half nv = DotClamped(normal, viewDir);

		half lv = DotClamped (light.dir, viewDir);
		half lh = DotClamped (light.dir, halfDir);

		half nlPow5 = Pow5 (1-nl);
		half nvPow5 = Pow5 (1-nv);
		half Fd90 = 0.5 + 2 * lh * lh * roughness;
		half disneyDiffuse = (1 + (Fd90-1) * nlPow5) * (1 + (Fd90-1) * nvPow5);
		
#if defined(_SSS_ON)
		half3 diffuseTerm = disneyDiffuse * WrappedDiffuse(nl);
		diffuseTerm *= nl;
#else
		half3 diffuseTerm = disneyDiffuse * nl;
#endif

#if defined(_MICRO_DETAILS_ON)
		half o = lerp(1.0, delta.z, _MicroDetailStrength);
		o = clamp(o, 0.1, 1.0);
		half giAO  = o;
		half aoFadeTerm = saturate(dot(vertexNormal, viewDir));
		o = lerp(1.0, o, aoFadeTerm);
#else
		half o = 1;
		half giAO = 1;
#endif

//#if 1
		half vh = dot(viewDir, halfDir);
		half3 specularTerm = UNITY_PI * (FresnelPow4(specColor, vh) * saturate(DongseokKimDistribution(nh)));
//#elif 0
//		half3 specularTerm = KajiyaKay(dot(tangent, halfDir));
//#endif
		specularTerm *= nl;

#if defined(_MICRO_SHADOWS_ON)
		half microShadowTerm = MicroShadow(o, nl, light); //ala Naughty Dog
#else
		half microShadowTerm = 1;
#endif

		half3 color = diffColor * (gi.diffuse * giAO + light.color * diffuseTerm * microShadowTerm * o)
					  + specularTerm * light.color * microShadowTerm * o;

		return half4(color, 1);
	}

#endif // MICRO_DETAIL_FABRIC_BRDF