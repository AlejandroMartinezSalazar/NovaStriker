// Versión para URP del shader original del Soccer Stadium Pack (público animado de las gradas).
// Mismo nombre y mismas propiedades: los materiales que ya lo usan (Mat_PeopleA, Mat_PeopleB...) no cambian.
// El original era para el Built-in Render Pipeline (CGPROGRAM, ForwardBase) y en URP se veía morado.
Shader "Custom/SoccerStadiumPack/UVScroll"
{
    Properties
    {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0.0, 1.0)) = 0.5
        [KeywordEnum(Linear, Step)] _Type("Type", Float) = 0
        _ScrollSpeed("ScrollSpeed", Vector) = (0.1, 0.1, 0, 0)
        _TimeSpeed("TimeSpeed", Float) = 1.0
        _Step("StepUV", Vector) = (0.25, 0.25, 0, 0)
        _SecPerStep("SecPerStep", Vector) = (1.0, 1.0, 0, 0)
        _LightmapTex("Lightmap (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 150

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_LightmapTex);
            SAMPLER(sampler_LightmapTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _LightmapTex_ST;
                float _Cutoff;
                float _Type;
                float4 _ScrollSpeed;
                float _TimeSpeed;
                float4 _Step;
                float4 _SecPerStep;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uvMain : TEXCOORD0;
                float2 uvLightmap : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.color = input.color;
                output.uvMain = input.uv0;
                output.uvLightmap = input.uv1;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Misma animación que el original: desplazamiento continuo (Linear) o por fotogramas (Step).
                float2 scroll = (1 - _Type) * frac(_Time.y * _ScrollSpeed.xy * _TimeSpeed)
                              + _Type * frac(_Time.y * _Step.xy / _SecPerStep.xy * _TimeSpeed);
                scroll = (1 - _Type) * scroll + _Type * floor(scroll / _Step.xy) * _Step.xy;
                float2 uv = frac(input.uvMain + scroll);

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                clip(color.a - _Cutoff);

                half4 lightmap = SAMPLE_TEXTURE2D(_LightmapTex, sampler_LightmapTex, input.uvLightmap);
                half shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(input.positionWS));

                half3 rgb = color.rgb * input.color.rgb * lightmap.rgb * shadow;
                rgb = MixFog(rgb, input.fogFactor);
                return half4(rgb, color.a);
            }
            ENDHLSL
        }
    }
}
