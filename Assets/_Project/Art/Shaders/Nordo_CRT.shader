// NORDO — optional CRT / VHS full-screen effect (URP 14 / Unity 2022 LTS)  [art-direction rule 9].
// Usage: create a Material with this shader, then add a "Full Screen Pass Renderer Feature" to the
// URP Renderer and assign the material. Expose the on/off toggle in the options menu (off by default).
// Effect: barrel curvature + vignette, scanlines, mild chromatic aberration, and animated grain.
Shader "Nordo/CRT"
{
    Properties
    {
        _Curvature ("Screen Curvature", Range(0, 0.4)) = 0.12
        _Vignette ("Vignette Strength", Range(0, 2)) = 0.6
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.25
        _ScanlineCount ("Scanline Count", Range(120, 1080)) = 480
        _Aberration ("Chromatic Aberration", Range(0, 0.01)) = 0.0025
        _GrainStrength ("Grain Strength", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "NordoCRT"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Curvature;
            float _Vignette;
            float _ScanlineStrength;
            float _ScanlineCount;
            float _Aberration;
            float _GrainStrength;

            // Cheap hash for animated grain.
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Warp UVs toward a barrel-distorted CRT surface.
            float2 CurveUV(float2 uv, out float mask)
            {
                float2 c = uv * 2.0 - 1.0;
                float2 offset = c.yx * c.yx * _Curvature;
                c += c * offset;
                uv = c * 0.5 + 0.5;

                // Mask (and later black) anything that curves off the tube.
                float2 edge = smoothstep(0.0, 0.02, uv) * smoothstep(0.0, 0.02, 1.0 - uv);
                mask = edge.x * edge.y;
                return uv;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // HLSL requires the out-argument to be declared before the call (no C#-style inline out).
                float mask;
                float2 uv = CurveUV(input.texcoord, mask);

                // Chromatic aberration: sample RGB at slightly offset UVs.
                float2 dir = uv - 0.5;
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + dir * _Aberration).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - dir * _Aberration).b;
                float3 color = float3(r, g, b);

                // Scanlines.
                float scan = sin(uv.y * _ScanlineCount * 3.14159265) * 0.5 + 0.5;
                color *= 1.0 - _ScanlineStrength * scan;

                // Animated grain.
                float grain = Hash(uv * _ScreenParams.xy + frac(_Time.y) * 100.0) - 0.5;
                color += grain * _GrainStrength;

                // Vignette + off-tube mask.
                float2 v = (uv - 0.5) * 2.0;
                float vignette = saturate(1.0 - dot(v, v) * _Vignette);
                color *= vignette * mask;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
