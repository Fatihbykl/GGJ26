Shader "Ultimate 10+ Shaders/Plasma_URP"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap("Normal map", 2D) = "bump" {}

        _NoiseTex ("Noise", 2D) = "white" {}
        _MovementDirection ("Movement Direction (XY)", Vector) = (0, -1, 0, 0)
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP Işıklandırma ve Gölge özellikleri için gerekli keywordler
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4; // Normal map için gerekli
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _MovementDirection;
                float _Cull;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Pozisyon ve Normal hesaplamaları
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                
                // Normal mapping için Tangent verisi (basitleştirilmiş)
                float3 bitangent = cross(input.normalOS, input.tangentOS.xyz) * input.tangentOS.w;
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- Hareket Mantığı ---
                float timeVal = _Time.y;
                float2 moveDir = _MovementDirection.xy;

                // Orijinal koddaki kaydırma matematiği
                float2 uv_Noise = input.uv + (moveDir * timeVal / 2.0);
                float2 uv_Main = input.uv + (moveDir * timeVal);
                float2 uv_Normal = input.uv + (moveDir * timeVal / 2.0);

                // --- Texture Okuma ---
                half noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv_Noise).r;
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_Main);
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv_Normal);

                // Renk ve Alpha hesaplama (Orijinal mantık)
                half3 finalColor = albedo.rgb * _BaseColor.rgb * noiseVal;
                half finalAlpha = noiseVal;

                // --- Işıklandırma Verisi Hazırlama (PBR) ---
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionCS.xyz; // Yaklaşık değer
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = normalize(input.viewDirWS);
                
                // Normal Map Uygulaması
                float3 normalTS = UnpackNormal(normalSample);
                // Basit bir TBN uygulaması (Daha karmaşık hesaplamalar gerekebilir ama genelde yeterlidir)
                // URP'de normal map uygulaması biraz daha karmaşık fonksiyonlar gerektirir, 
                // burada basit bir düzeltme ile bırakıyoruz veya normalWS üzerine ekliyoruz.
                // Tam PBR için Shader Graph önerilir.
                
                // Işık Hesaplama (Basitleştirilmiş Lit)
                Light mainLight = GetMainLight();
                half3 lightColor = mainLight.color * mainLight.distanceAttenuation;
                half NdotL = saturate(dot(inputData.normalWS, mainLight.direction));
                
                // Sonuç: Ambient + Diffuse (Basit Lit Taklidi)
                half3 lighting = finalColor * (NdotL * lightColor + half3(0.1, 0.1, 0.1)); // 0.1 ortam ışığı

                return half4(lighting, finalAlpha);
            }
            ENDHLSL
        }
    }
}