Shader "VRChat/Mobile/Standard Lite"
{
    Properties
    {
        _MainTex("Albedo(RGB)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)

        [NoScaleOffset] _MetallicGlossMap("Metallic(R) Smoothness(A) Map", 2D) = "white" {}
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 1.0
        _Glossiness("Smoothness", Range(0.0, 1.0)) = 1.0

        _BumpScale("Scale", Float) = 1.0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        [NoScaleOffset] _OcclusionMap("Occlusion(G)", 2D) = "white" {}
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0

        [Toggle(_EMISSION)]_EnableEmission("Enable Emission", int) = 0
        [NoScaleOffset] _EmissionMap("Emission(RGB)", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,1,1)

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
        [NoScaleOffset] _DetailMask("Detail Mask(A)", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2(RGB)", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        [NoScaleOffset] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #define UNITY_BRDF_PBS BRDF2_Unity_PBS
        #include "UnityPBSLighting.cginc"

        #pragma surface surf StandardMobile vertex:vert exclude_path:prepass exclude_path:deferred noforwardadd noshadow nodynlightmap nolppv noshadowmask

        #pragma target 3.0
        #pragma multi_compile _ _EMISSION
        #pragma multi_compile _ _DETAIL
        #pragma multi_compile _ _SPECULARHIGHLIGHTS_OFF
        #pragma multi_compile _ _GLOSSYREFLECTIONS_OFF

        // -------------------------------------

        struct Input
        {
            float2 texcoord0;
            #ifdef _DETAIL
            float2 texcoord1;
            #endif
            float4 color : COLOR;
        };

        struct SurfaceOutputStandardMobile
        {
            fixed3 Albedo;      // base (diffuse or specular) color
            float3 Normal;      // tangent space normal, if written
            half3 Emission;
            half Metallic;      // 0=non-metal, 1=metal
            // Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
            // Everywhere in the code you meet smoothness it is perceptual smoothness
            half Smoothness;    // 0=rough, 1=smooth
            half Occlusion;
            fixed Alpha;        // alpha for transparencies
        };

        UNITY_DECLARE_TEX2D(_MainTex);
        float4 _MainTex_ST;
        half4 _Color;

        UNITY_DECLARE_TEX2D(_MetallicGlossMap);
        uniform half _Glossiness;
        uniform half _Metallic;

        UNITY_DECLARE_TEX2D(_BumpMap);
        uniform half _BumpScale;

        UNITY_DECLARE_TEX2D(_OcclusionMap);
        uniform half _OcclusionStrength;

        UNITY_DECLARE_TEX2D(_EmissionMap);
        half4 _EmissionColor;

#ifdef _DETAIL
        uniform half _UVSec;
        float4 _DetailAlbedoMap_ST;
        UNITY_DECLARE_TEX2D(_DetailMask);
        UNITY_DECLARE_TEX2D(_DetailAlbedoMap);
        UNITY_DECLARE_TEX2D(_DetailNormalMap);
        uniform half _DetailNormalMapScale;
#endif

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        // -------------------------------------

        inline half4 LightingStandardMobile(SurfaceOutputStandardMobile s, float3 viewDir, UnityGI gi)
        {
            s.Normal = normalize(s.Normal);

            half oneMinusReflectivity;
            half3 specColor;
            s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

            half4 c = UNITY_BRDF_PBS(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
            UNITY_OPAQUE_ALPHA(c.a);
            return c;
        }

        inline UnityGI UnityGI_BaseMobile(UnityGIInput data, half occlusion, half3 normalWorld)
        {
            UnityGI o_gi;
            ResetUnityGI(o_gi);

            o_gi.light = data.light;
            o_gi.light.color *= data.atten;

            #if UNITY_SHOULD_SAMPLE_SH
                o_gi.indirect.diffuse = ShadeSHPerPixel(normalWorld, data.ambient, data.worldPos);
            #endif

            #if defined(LIGHTMAP_ON)
                // Baked lightmaps
                half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, data.lightmapUV.xy);
                half3 bakedColor = DecodeLightmap(bakedColorTex);

                #ifdef DIRLIGHTMAP_COMBINED
                    fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, data.lightmapUV.xy);
                    o_gi.indirect.diffuse += DecodeDirectionalLightmap(bakedColor, bakedDirTex, normalWorld);
                #else // not directional lightmap
                    o_gi.indirect.diffuse += bakedColor;
                #endif
            #endif

            o_gi.indirect.diffuse *= occlusion;
            return o_gi;
        }

        inline half3 UnityGI_IndirectSpecularMobile(UnityGIInput data, half occlusion, Unity_GlossyEnvironmentData glossIn)
        {
            half3 specular;

            #ifdef _GLOSSYREFLECTIONS_OFF
                specular = unity_IndirectSpecColor.rgb;
            #else
                half3 env0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn);
                specular = env0;
            #endif

            return specular * occlusion;
        }

        inline UnityGI UnityGlobalIlluminationMobile(UnityGIInput data, half occlusion, half3 normalWorld, Unity_GlossyEnvironmentData glossIn)
        {
            UnityGI o_gi = UnityGI_BaseMobile(data, occlusion, normalWorld);
            o_gi.indirect.specular = UnityGI_IndirectSpecularMobile(data, occlusion, glossIn);
            return o_gi;
        }

        inline void LightingStandardMobile_GI(SurfaceOutputStandardMobile s, UnityGIInput data, inout UnityGI gi)
        {
            Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
            gi = UnityGlobalIlluminationMobile(data, s.Occlusion, s.Normal, g);
        }

	void vert(inout appdata_full v, out Input o)
	{
	    UNITY_INITIALIZE_OUTPUT(Input,o);
            o.texcoord0 = TRANSFORM_TEX(v.texcoord.xy, _MainTex); // Always source from uv0
#ifdef _DETAIL
            o.texcoord1 = TRANSFORM_TEX(((_UVSec == 0) ? v.texcoord.xy : v.texcoord1.xy), _DetailAlbedoMap);
#endif
	}

        void surf(Input IN, inout SurfaceOutputStandardMobile o)
        {
            // Albedo comes from a texture tinted by color
            half4 albedoMap = UNITY_SAMPLE_TEX2D(_MainTex, IN.texcoord0) * _Color * IN.color;
            o.Albedo = albedoMap.rgb;

            // Metallic and smoothness come from slider variables
            half4 metallicGlossMap = UNITY_SAMPLE_TEX2D(_MetallicGlossMap, IN.texcoord0);
            o.Metallic = metallicGlossMap.r * _Metallic;
            o.Smoothness = metallicGlossMap.a * _Glossiness;

            // Occlusion is sampled from the Green channel to match up with Standard. Can be packed to Metallic if you insert it into multiple slots.
            o.Occlusion = UNITY_SAMPLE_TEX2D(_OcclusionMap, IN.texcoord0).g * _OcclusionStrength;

            o.Normal = UnpackScaleNormal(UNITY_SAMPLE_TEX2D(_BumpMap, IN.texcoord0), _BumpScale);

            #ifdef _DETAIL
                half4 detailMask = UNITY_SAMPLE_TEX2D(_DetailMask, IN.texcoord0);
                half4 detailAlbedoMap = UNITY_SAMPLE_TEX2D(_DetailAlbedoMap, IN.texcoord1);
                o.Albedo *= LerpWhiteTo(detailAlbedoMap.rgb * unity_ColorSpaceDouble.rgb, detailMask.a);

                half4 detailNormalMap = UNITY_SAMPLE_TEX2D(_DetailNormalMap, IN.texcoord1);
                half3 detailNormalTangent = UnpackScaleNormal(UNITY_SAMPLE_TEX2D(_DetailNormalMap, IN.texcoord1), _DetailNormalMapScale);
                o.Normal = lerp(o.Normal, BlendNormals(o.Normal, detailNormalTangent), detailMask.a);

            #endif

            #ifdef _EMISSION
                o.Emission = UNITY_SAMPLE_TEX2D(_EmissionMap, IN.texcoord0) * _EmissionColor;
            #endif
        }
        ENDCG
    }

    FallBack "VRChat/Mobile/Diffuse"
    CustomEditor "StandardLiteShaderGUI"
}
