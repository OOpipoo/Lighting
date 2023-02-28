// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D

Shader "Custom/RoomObjectShader"
{
    Properties
    {
        [LM_Albedo] [LM_Transparency] _BaseColorMap ("Color", Color) = (1,1,1,1)
        [LM_MasterTilingOffset] [LM_Albedo] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [LM_NormalMap] _BumpMap ("NormalMap", 2D) = "bump" {}
        [LM_Metallic] [LM_Glossiness] _MetallicMap ("MetallicMap", 2D) = "bump" {}
        [LM_Emission] _EmissionMap ("EmissionMap", 2D) = "black" {}
        [LM_Emission] [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)
        [NoScaleOffset] _DissolveMap ("DisolveMap", 2D) = "black" {}
        _Dissolve ("Dissolve", Range(0,1)) = 0.0
        [HDR] _DissolveColor ("Dissolve Color", Color) = (0,0,0)

        [LM_Glossiness] _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [LM_Metallic] _Metallic ("Metallic", Range(0,1)) = 0.0
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.0
    }
    
    SubShader
    {
       
        Pass
        {
            Name "META"
            Tags {"LightMode"="Meta"}
            Cull Off
            CGPROGRAM

            #include"UnityStandardMeta.cginc"
            #if BAKERY_INCLUDED
            #include "Assets/Bakery/BakeryMetaPass.cginc"
            #endif


            sampler2D _GIAlbedoTex;
            fixed4 _GIAlbedoColor;
            float4 frag_meta2 (v2f_meta i): SV_Target
            {
                FragmentCommonData data = UNITY_SETUP_BRDF_INPUT (i.uv);
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);
                fixed4 c = tex2D (_GIAlbedoTex, i.uv);
                o.Albedo = fixed3(c.rgb * _GIAlbedoColor.rgb);
                o.Emission = Emission(i.uv.xy) * 10;
                return UnityMetaFragment(o);
            }

            //#pragma vertex vert_meta
            #pragma fragment frag_meta2
            #pragma vertex vert_bakerymt
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            ENDCG
        }
        
        Pass
        {
            Name "META_BAKERY"
            Tags {"LightMode"="Meta"}
            Cull Off
            CGPROGRAM
            #if BAKERY_INCLUDED
            #include"UnityStandardMeta.cginc"
            #include"Assets/Bakery/BakeryMetaPass.cginc"

            float4 frag_customMeta (v2f_bakeryMeta i): SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                if (unity_MetaFragmentControl.z)
                {
                    float3 customNormalMap = UnpackNormal(tex2D(_BumpMap, pow(abs(i.uv), 1.5))); // example: UVs are procedurally distorted
                    float3 customWorldNormal = TransformNormalMapToWorld(i, customNormalMap);

                    return float4(BakeryEncodeNormal(customWorldNormal),1);
                }

                o.Albedo = tex2D(_MainTex, i.uv);
                o.Emission = Emission(i.uv.xy) * 10;
                return UnityMetaFragment(o);
            }

            // Must use vert_bakerymt vertex shader
            #pragma vertex vert_bakerymt
            #pragma fragment frag_customMeta
            #endif
            ENDCG
        }
        
        Tags { "RenderType"="Opaque"}
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows  addshadow 
        #include "UnityPBSLighting.cginc"

        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _BumpMap; 
        sampler2D _MetallicMap;
        sampler2D _EmissionMap;
        sampler2D _DissolveMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float2 uv_MetallicMap;
            float3 cameraRelativeWorldPos;
            float3 worldNormal;
            float3 worldPos;
        };
        
        half _Glossiness;
        half _GlossinessMax;
        half _Metallic;
        half _MetallicMax;
        half _Dissolve;
        half _ShadowStrength;
        fixed4 _BaseColorMap;
        fixed4 _EmissionColor;
        fixed4 _DissolveColor;
        float3 _ROOM_CENTER;


        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
        
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 lightmap = DecodeLightmap ( UNITY_SAMPLE_TEX2D ( unity_Lightmap, IN.uv_MainTex) );
            
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _BaseColorMap;
            fixed4 dissolve = tex2D (_DissolveMap, IN.uv_MainTex / 1);
            fixed4 emission = tex2D (_EmissionMap, IN.uv_MainTex);
            fixed4 metal = tex2D(_MetallicMap, IN.uv_MetallicMap);

            clip(dissolve.r - _Dissolve);
            
            o.Albedo =c.rgb;
            //o.Albedo *= lightmap.rgb;
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap)); 
            
            o.Metallic =  metal.r * _Metallic;
            o.Smoothness = metal.a * _Glossiness;
            o.Emission = emission.rgb * _EmissionColor +( step( dissolve.r - _Dissolve, 0.005f) * _DissolveColor);
            o.Alpha = c.a;
            o.Occlusion = metal.g;

            _LightShadowData.rgb = 0;
            _LightShadowData.a = 1 - _ShadowStrength;

            #if defined(NGSS_GLOBAL_OPACITY_DEFINED)
            //_LightShadowData.r = NGSS_GLOBAL_OPACITY;
            #endif
        } 
        ENDCG
         
        
        
        
    }
      FallBack "Standard"
}
