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

        [NoScaleOffset] _EmissionMap("Emission(RGB)", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,1,1)

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
        [NoScaleOffset] _DetailMask("Detail Mask(A)", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2(RGB)", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        [NoScaleOffset] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 0

        [Toggle(_ENABLE_GEOMETRIC_SPECULAR_AA)] _EnableGeometricSpecularAA("EnableGeometricSpecularAA", Float) = 1.0
        _SpecularAAScreenSpaceVariance("SpecularAAScreenSpaceVariance", Range(0.0, 1.0)) = 0.1
        _SpecularAAThreshold("SpecularAAThreshold", Range(0.0, 1.0)) = 0.2

        [Enum(Default,0,MonoSH,1,MonoSH (no highlights),2)] _LightmapType ("Lightmap Type", Float) = 0

        // TODO: This has questionable performance impact on Mobile but very little discernable impact on PC. Should
        // make a toggle once we have properly branched compilation between those platforms, that's PC-only
        [Toggle(_BICUBIC)] _Bicubic ("Enable Bicubic Sampling", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        
        //#define _DEBUG_VRC 
        #ifdef _DEBUG_VRC
            #define DEBUG_COL(rgb) debugCol = half4(rgb, 1)
            #define DEBUG_VAL(val) debugCol = half4(val, val, val, 1)
                half4 debugCol = half4(0,0,0,1);
        #else
            #define DEBUG_COL(rgb) 
            #define DEBUG_VAL(val)
        #endif
        
        #pragma target 3.0
        #pragma multi_compile _ _EMISSION
        #pragma multi_compile _ _DETAIL
        #pragma multi_compile _ _SPECULARHIGHLIGHTS_OFF
        #pragma multi_compile _ _GLOSSYREFLECTIONS_OFF
        #pragma multi_compile _ _MONOSH_SPECULAR _MONOSH_NOSPECULAR
        #pragma multi_compile _ _ENABLE_GEOMETRIC_SPECULAR_AA
        //#pragma multi_compile _ _BICUBIC

        #if defined(LIGHTMAP_ON)
            #if defined(_MONOSH_SPECULAR) || defined(_MONOSH_NOSPECULAR)
                #define _MONOSH
                #if defined(_MONOSH_SPECULAR)
                    #define _LMSPEC
                #endif
            #endif
        #endif

        #include "VRChat.cginc"

        #pragma surface surf StandardVRC vertex:vert exclude_path:prepass exclude_path:deferred noforwardadd noshadow nodynlightmap nolppv noshadowmask

        // -------------------------------------

        struct Input
        {
            float2 texcoord0;
            #ifdef _DETAIL
            float2 texcoord1;
            #endif
            fixed4 color : COLOR;
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

        uniform half _SpecularAAScreenSpaceVariance;
        uniform half _SpecularAAThreshold;

#ifdef _EMISSION
        UNITY_DECLARE_TEX2D(_EmissionMap);
        half4 _EmissionColor;
#endif

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
	void vert(inout appdata_full v, out Input o)
	{
	    UNITY_INITIALIZE_OUTPUT(Input,o);
            o.texcoord0 = TRANSFORM_TEX(v.texcoord.xy, _MainTex); // Always source from uv0
#ifdef _DETAIL
            o.texcoord1 = TRANSFORM_TEX(((_UVSec == 0) ? v.texcoord.xy : v.texcoord1.xy), _DetailAlbedoMap);
#endif
	}

        void surf(Input IN, inout SurfaceOutputStandardVRC o)
        {
            // Albedo comes from a texture tinted by color
            half4 albedoMap = UNITY_SAMPLE_TEX2D(_MainTex, IN.texcoord0) * _Color * IN.color;
            o.Albedo = albedoMap.rgb;

            // Metallic and smoothness come from slider variables
            half4 metallicGlossMap = UNITY_SAMPLE_TEX2D(_MetallicGlossMap, IN.texcoord0);
            o.Metallic = metallicGlossMap.r * _Metallic;
            o.Smoothness = metallicGlossMap.a * _Glossiness;

            // Occlusion is sampled from the Green channel to match up with Standard. Can be packed to Metallic if you insert it into multiple slots.
            o.Occlusion = LerpOneTo(UNITY_SAMPLE_TEX2D(_OcclusionMap, IN.texcoord0).g, _OcclusionStrength);

            // only takes into account directional lights, so only use if using noforwardadd!
            float dx0 = ddx(IN.texcoord0);
            float dy0 = ddy(IN.texcoord0);
            #if defined(LIGHTMAP_ON) && !defined(_MONOSH) && !defined(DIRLIGHTMAP_COMBINED) && defined(_GLOSSYREFLECTIONS_OFF)
                UNITY_BRANCH if (any(_LightColor0.xyz))
            #else
                if (true)
            #endif
            {
                o.Normal = UnpackScaleNormal(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, IN.texcoord0, dx0, dy0), _BumpScale);
            } else {
                o.Normal = half3(0, 0, 1);
            }

            #ifdef _ENABLE_GEOMETRIC_SPECULAR_AA
                o.SpecularAAVariance = _SpecularAAScreenSpaceVariance;
                o.SpecularAAThreshold = _SpecularAAThreshold;
            #endif

            #ifdef _DETAIL
                half4 detailMask = UNITY_SAMPLE_TEX2D(_DetailMask, IN.texcoord0);
                float dx1 = ddx(IN.texcoord1);
                float dy1 = ddy(IN.texcoord1);
                UNITY_BRANCH
                if (detailMask.a > 0)
                {
                    half4 detailAlbedoMap = SAMPLE_TEXTURE2D_GRAD(_DetailAlbedoMap, sampler_DetailAlbedoMap, IN.texcoord1, dx1, dy1);
                    o.Albedo *= LerpWhiteTo(detailAlbedoMap.rgb * unity_ColorSpaceDouble.rgb, detailMask.a);

                    #if defined(LIGHTMAP_ON) && !defined(_MONOSH) && !defined(DIRLIGHTMAP_COMBINED) && defined(_GLOSSYREFLECTIONS_OFF)
                        UNITY_BRANCH if (any(_LightColor0.xyz))
                    #else
                        if (true)
                    #endif
                    {
                        half3 detailNormalTangent = UnpackScaleNormal(SAMPLE_TEXTURE2D_GRAD(_DetailNormalMap, sampler_DetailNormalMap, IN.texcoord1, dx1, dy1), _DetailNormalMapScale);
                        o.Normal = lerp(o.Normal, BlendNormals(o.Normal, detailNormalTangent), detailMask.a);
                    }
                }
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
