Shader "Custom/Waves"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0.5,0.8,0.9,0.8)
        [MainTexture] _BaseMap("Albedo (RGB)", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.8
        _Metallic("Metallic", Range(0,1)) = 0.0
        
        // Wave properties
        _WaveA("Wave A (dir, steepness, wavelength)", Vector) = (1,0,0.5,10)
        _WaveB("Wave B", Vector) = (0,1,0.25,20)
        _WaveC("Wave C", Vector) = (1,1,0.15,10)
        _WaveSpeed("Wave Speed", Float) = 1.0
        
        // Water fog properties
        _WaterFogColor("Water Fog Color", Color) = (0.192, 0.402, 0.518, 1.0)
        _WaterFogDensity("Water Fog Density", Range(0, 2)) = 0.1
        
        // Refraction properties
        _RefractionStrength("Refraction Strength", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Unity keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float fogFactor : TEXCOORD5;
                float4 screenPos : TEXCOORD6;
                float3 viewDirWS : TEXCOORD7;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
                float4 _WaveA, _WaveB, _WaveC;
                float _WaveSpeed;
                float3 _WaterFogColor;
                float _WaterFogDensity;
                float _RefractionStrength;
            CBUFFER_END

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
            {
                float steepness = wave.z;
                float wavelength = wave.w;
                float k = 2 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(wave.xy);
                float f = k * (dot(d, p.xz) - c * _Time.y * _WaveSpeed);
                float a = steepness / k;
                
                tangent += float3(
                    -d.x * d.x * (steepness * sin(f)),
                    d.x * (steepness * cos(f)),
                    -d.x * d.y * (steepness * sin(f))
                );
                binormal += float3(
                    -d.x * d.y * (steepness * sin(f)),
                    d.y * (steepness * cos(f)),
                    -d.y * d.y * (steepness * sin(f))
                );
                return float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
            }

            // Underwater color sampling with refraction using CameraOpaqueTexture
            float3 ColorBelowWater(float4 screenPos, float3 tangentSpaceNormal)
            {
                // Calculate screen position UV and apply refraction offset
                float2 uvOffset = tangentSpaceNormal.xy * _RefractionStrength;
                float2 uv = (screenPos.xy + uvOffset * screenPos.w) / screenPos.w;
                
                // Get depth information
                float backgroundDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(screenPos.z / screenPos.w, _ZBufferParams);
                float depthDifference = backgroundDepth - surfaceDepth;
                
                // Scale refraction based on depth difference (reduce refraction in shallow water)
                uvOffset *= saturate(depthDifference);
                uv = (screenPos.xy + uvOffset * screenPos.w) / screenPos.w;
                
                // Resample depth with corrected UV
                backgroundDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                depthDifference = backgroundDepth - surfaceDepth;
                
                // Sample background color using URP's CameraOpaqueTexture
                float3 backgroundColor = SampleSceneColor(uv);
                
                // Apply underwater fog based on depth
                float fogFactor = exp2(-_WaterFogDensity * depthDifference);
                return lerp(_WaterFogColor, backgroundColor, fogFactor);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Calculate wave position and normal
                float3 gridPoint = input.positionOS.xyz;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                
                // Apply waves
                p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
                
                // Calculate normal from modified tangent and binormal
                float3 waveNormal = normalize(cross(binormal, tangent));
                
                // Convert to world space
                VertexPositionInputs posInputs = GetVertexPositionInputs(p);
                VertexNormalInputs normInputs = GetVertexNormalInputs(waveNormal, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.screenPos = posInputs.positionNDC;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                
                // Calculate shadow coordinates
                #if defined(_MAIN_LIGHT_SHADOWS)
                    output.shadowCoord = GetShadowCoord(posInputs);
                #else
                    output.shadowCoord = float4(0, 0, 0, 0);
                #endif
                
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Get main light
                #if defined(_MAIN_LIGHT_SHADOWS)
                    Light mainLight = GetMainLight(input.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif
                
                // Basic PBR setup
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Create tangent space to world space transformation
                float3 bitangentWS = normalize(cross(normalWS, input.tangentWS.xyz) * input.tangentWS.w);
                float3x3 tangentToWorld = float3x3(
                    input.tangentWS.xyz,
                    bitangentWS,
                    normalWS
                );
                
                // Surface normal in tangent space (just use normal map values to approximate wave ripples)
                float3 tangentNormal = float3(0, 0, 1);
                
                // Direct lighting calculation
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float LdotH = saturate(dot(mainLight.direction, halfDir));
                
                // Specular term - simplified GGX
                float roughness = 1.0 - _Smoothness;
                float roughnessSq = roughness * roughness;
                float d = NdotH * NdotH * (roughnessSq - 1.0) + 1.00001;
                float specularTerm = roughnessSq / (d * d) * 0.1;
                specularTerm *= min(1.0, LdotH * 2.0);
                
                // Calculate underwater color with refraction
                float3 underwaterColor = ColorBelowWater(input.screenPos, tangentNormal);
                
                // Calculate direct lighting
                float3 directDiffuse = albedo.rgb * NdotL * mainLight.color * mainLight.shadowAttenuation;
                float3 directSpecular = specularTerm * lerp(0.04, albedo.rgb, _Metallic) * mainLight.color * mainLight.shadowAttenuation;
                
                // Ambient lighting (simplified)
                float3 ambient = albedo.rgb * 0.1;
                
                // Combine everything
                float3 finalColor = directDiffuse + directSpecular + ambient;
                
                // Add underwater color based on water transparency
                finalColor = lerp(underwaterColor, finalColor, albedo.a);
                
                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
                float4 _WaveA, _WaveB, _WaveC;
                float _WaveSpeed;
                float3 _WaterFogColor;
                float _WaterFogDensity;
                float _RefractionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
            {
                float steepness = wave.z;
                float wavelength = wave.w;
                float k = 2 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(wave.xy);
                float f = k * (dot(d, p.xz) - c * _Time.y * _WaveSpeed);
                float a = steepness / k;
                
                tangent += float3(
                    -d.x * d.x * (steepness * sin(f)),
                    d.x * (steepness * cos(f)),
                    -d.x * d.y * (steepness * sin(f))
                );
                binormal += float3(
                    -d.x * d.y * (steepness * sin(f)),
                    d.y * (steepness * cos(f)),
                    -d.y * d.y * (steepness * sin(f))
                );
                return float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
            }

            float4 GetShadowPositionHClip(float3 positionOS, float3 normalOS)
            {
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);

                #if defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS)
                    float3 lightDirectionWS = _MainLightPosition.xyz;
                    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #else
                    float4 positionCS = TransformWorldToHClip(positionWS);
                #endif
            
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 gridPoint = input.positionOS.xyz;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                
                // Apply waves
                p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
                
                float3 normal = normalize(cross(binormal, tangent));
                
                output.positionCS = GetShadowPositionHClip(p, normal);
                
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _Metallic;
                float4 _WaveA, _WaveB, _WaveC;
                float _WaveSpeed;
                float3 _WaterFogColor;
                float _WaterFogDensity;
                float _RefractionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
            {
                float steepness = wave.z;
                float wavelength = wave.w;
                float k = 2 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(wave.xy);
                float f = k * (dot(d, p.xz) - c * _Time.y * _WaveSpeed);
                float a = steepness / k;
                
                tangent += float3(
                    -d.x * d.x * (steepness * sin(f)),
                    d.x * (steepness * cos(f)),
                    -d.x * d.y * (steepness * sin(f))
                );
                binormal += float3(
                    -d.x * d.y * (steepness * sin(f)),
                    d.y * (steepness * cos(f)),
                    -d.y * d.y * (steepness * sin(f))
                );
                return float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
            }

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 gridPoint = input.positionOS.xyz;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                
                // Apply waves
                p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
                
                output.positionCS = TransformObjectToHClip(p);
                
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}