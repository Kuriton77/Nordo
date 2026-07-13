// NORDO — modern PSX shader (URP 14 / Unity 2022 LTS).
// The default shader for ALL environments and props. Implements the locked art direction:
//   - vertex snapping (the PSX "wobble")            [rule 8]
//   - affine texture mapping (perspective warp)      [PSX look]
//   - banded / harsh lighting from main + local lights, so the flashlight still works   [rules 6,7]
//   - dim ambient tint + heavy fog integration       [rules 4,5]
//   - alpha cutout for foliage/grates/decals
// Keep textures Point-filtered with no mipmaps for the crunchy 128–256px look [rule 3].
Shader "Nordo/PSX"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo (128–256px, Point, no mips)", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1,1,1,1)
        _Ambient ("Ambient Tint", Color) = (0.10, 0.13, 0.17, 1)
        [HDR] _EmissionColor ("Emission (interaction highlight)", Color) = (0,0,0,1)

        _SnapAmount ("Vertex Snap Grid (lower = chunkier)", Range(8, 480)) = 140
        _AffineAmount ("Affine Warp (0 = corrected, 1 = full PSX)", Range(0, 1)) = 0.85
        _LightBands ("Lighting Bands (harshness)", Range(1, 8)) = 3

        [Toggle(_ALPHATEST_ON)] _AlphaClipToggle ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // ---------------------------------------------------------------- Forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _Ambient;
                float4 _EmissionColor;
                float _SnapAmount;
                float _AffineAmount;
                float _LightBands;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // Perspective-correct and affine (noperspective) UVs; blended in the fragment.
                float2 uv         : TEXCOORD0;
                noperspective float2 uvAffine : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            // Snap a clip-space position to a virtual low-res grid (the PSX vertex wobble).
            float4 SnapToGrid(float4 clipPos, float grid)
            {
                float2 res = grid.xx;
                clipPos.xy = round(clipPos.xy / clipPos.w * res) / res * clipPos.w;
                return clipPos;
            }

            // Quantise a 0..1 value into N hard bands for harsh, gradient-free shading.
            float Band(float x, float bands)
            {
                bands = max(1.0, bands);
                return floor(saturate(x) * bands) / bands;
            }

            Varyings vert (Attributes input)
            {
                Varyings o = (Varyings)0;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);
                positionCS = SnapToGrid(positionCS, _SnapAmount);

                o.positionCS = positionCS;
                o.positionWS = positionWS;
                o.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));

                float2 baseUV = TRANSFORM_TEX(input.uv, _BaseMap);
                o.uv = baseUV;
                o.uvAffine = baseUV; // interpolated without perspective correction
                o.fogFactor = ComputeFogFactor(positionCS.z);
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Blend perspective-correct and affine UVs by the configured amount.
                float2 uv = lerp(input.uv, input.uvAffine, _AffineAmount);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half4 albedo = tex * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                float3 N = normalize(input.normalWS);

                // Main light, banded, with hard shadows.
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = saturate(dot(N, mainLight.direction));
                float3 lighting = mainLight.color * Band(ndl, _LightBands) * mainLight.shadowAttenuation;

                // Additional (local) lights — e.g. the flashlight — also banded.
                #ifdef _ADDITIONAL_LIGHTS
                    uint count = GetAdditionalLightsCount();
                    for (uint li = 0u; li < count; li++)
                    {
                        Light l = GetAdditionalLight(li, input.positionWS);
                        float nl = saturate(dot(N, l.direction));
                        lighting += l.color * Band(nl, _LightBands) * (l.distanceAttenuation * l.shadowAttenuation);
                    }
                #endif

                float3 color = albedo.rgb * (lighting + _Ambient.rgb);
                color += _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float4 _Ambient; float4 _EmissionColor;
                float _SnapAmount; float _AffineAmount; float _LightBands; float _Cutoff;
            CBUFFER_END

            float3 _LightDirection;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V shadowVert (A input)
            {
                V o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 shadowFrag (V input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask 0 Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float4 _Ambient; float4 _EmissionColor;
                float _SnapAmount; float _AffineAmount; float _LightBands; float _Cutoff;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V depthVert (A input)
            {
                V o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 depthFrag (V input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
