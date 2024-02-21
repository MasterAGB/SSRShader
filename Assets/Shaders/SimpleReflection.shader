Shader "SimpleReflection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;
            sampler2D _CameraGBufferTexture0;


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 GetInitialNormal(float2 uv)
            {
                float3 initialNormal = normalize(tex2D(_CameraNormalsTexture, uv).xyz * 2.0 - 1.0);
                return mul((float3x3)unity_WorldToCamera, initialNormal);
            }

            float3 makeRectangularAndNormalize(float3 initialNormal, float initialDepth, float3 viewRay)
            {
                float3 viewRayNormalized;
                float rectangularThreshhold = 0.9;


                bool isFloorReflection = abs(initialNormal.y) > rectangularThreshhold;
                bool isSideWallReflection = abs(initialNormal.x) > rectangularThreshhold;
                bool isDeepReflection = abs(initialNormal.z) > rectangularThreshhold;


                float verticalHorizontalResetValue = 0.02;
                float zResetValue = 1;
                float isVerticalMultiply = isFloorReflection ? verticalHorizontalResetValue : 1;
                float isHorizontalMultiply = isSideWallReflection ? verticalHorizontalResetValue : 1;
                float isDeepMultiply = isDeepReflection ? zResetValue : 1;
                viewRayNormalized = viewRay * float3(isVerticalMultiply, isHorizontalMultiply, isDeepMultiply);

                //What are those magic numbers?
                float xMultyply = 0.6;
                float yMultyply = 0.6;
                float zMultyply = 0.6;


                float magicMultiply = (initialDepth + 1.0);

                if (isFloorReflection)
                {
                    //viewRayNormalized.y *= yMultyply;
                    viewRayNormalized.y *= yMultyply * magicMultiply;
                }

                //for walls reflection
                if (isSideWallReflection)
                {
                    viewRayNormalized.x *= xMultyply * magicMultiply;
                    //viewRayNormalized.z *= zMultyply;
                }
                if (isDeepReflection)
                {
                    //viewRayNormalized.z *= zMultyply *  magicMultiply;
                    viewRayNormalized.x *= zMultyply * magicMultiply;
                    viewRayNormalized.y *= zMultyply * magicMultiply;
                }
                return viewRayNormalized;
            }

            bool hasError(float2 iuv, float3 currentUV, float3 reflectedRay)
            {
                float initialDepth = Linear01Depth(tex2D(_CameraDepthTexture, iuv).r);
                fixed4 initialColor = tex2D(_MainTex, iuv);
                float currentDepthNormal = Linear01Depth(tex2D(_CameraDepthTexture, currentUV.xy).r);

                float2 initialScreenPos = iuv * float2(_ScreenParams.x, _ScreenParams.y);
                float2 currentScreenPos = currentUV * float2(_ScreenParams.x, _ScreenParams.y);
                float2 diff = abs(currentScreenPos - initialScreenPos);
                float pixDiff = 1.0;
                bool isSamePixel = (diff.x < pixDiff) && (diff.y < pixDiff); // Порог в 1 пиксель

                if (isSamePixel)
                {
                    return true;
                }


                fixed4 currentColor = tex2D(_MainTex, currentUV);
                float colorDifference = length(currentColor - initialColor);
                if (colorDifference < 0.01f)
                {
                    // continue;
                }


                if (currentDepthNormal > initialDepth)
                {
                    if (reflectedRay.z < 0)
                    {
                        return true;
                    }
                }
                else if (currentDepthNormal < initialDepth)
                {
                    if (reflectedRay.z > 0)
                    {
                        return true;
                    }
                }


                float3 currentNormal = GetInitialNormal(currentUV.xy);

                float dotProduct = dot(reflectedRay, currentNormal);
                if (dotProduct > 0)
                {
                    return true;
                }
                return false;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float initialDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r);
                float3 viewRay = (float3(i.uv * 2.0 - 1.0, initialDepth));

                float3 initialNormal = GetInitialNormal(i.uv);
                float3 viewRayNormalized = makeRectangularAndNormalize(initialNormal, initialDepth, viewRay);
                //float3 viewRayNormalized = normalize(viewRay);
                float3 reflectedRay = reflect(viewRayNormalized, initialNormal);


                
                float3 currentUV = float3(i.uv, initialDepth);
                float3 bestUV;
                bool wasHit = false;
                float bestDifference = 1000000;
                int bestStep = 0;

                
                float _MaxSteps = 40;
                float _StepSize = 0.2;
                float _DepthHit = 0.4;

                [unroll(40)] //
                for (int step = 1; step <= _MaxSteps; step++)
                {
                    currentUV += reflectedRay * _StepSize;
                    if (currentUV.x < 0.0 || currentUV.x > 1.0 || currentUV.y < 0.0 || currentUV.y > 1.0) break;

                    float currentDepthNormal = Linear01Depth(tex2D(_CameraDepthTexture, currentUV.xy).r);
                    float oneMeterPixelDepth = (1 / _ProjectionParams.z);
                    float depthOffset = _StepSize * step * reflectedRay.z * oneMeterPixelDepth;
                    float currentDepth3D = currentDepthNormal * oneMeterPixelDepth;
                    float initialDepth3D = initialDepth * oneMeterPixelDepth;
                    float assumedDepth3D = initialDepth3D + depthOffset;
                    float depthDifference = abs(currentDepth3D - assumedDepth3D);

                    if (hasError(i.uv, currentUV, reflectedRay))
                    {
                        continue;
                    }

                    if (depthDifference <= _DepthHit)
                    {
                        wasHit = true;
                        if (bestDifference > depthDifference)
                        {
                            bestDifference = depthDifference;
                            bestUV = currentUV;
                            bestStep = step;
                        }
                    }
                }


                if (wasHit)
                {
                    float specular = tex2D(_CameraGBufferTexture0, i.uv.xy).a;
                    float reflectionKoef = specular;
                    reflectionKoef *= (1.0 - (float)bestStep / _MaxSteps) * (1.0 - bestDifference / _DepthHit);

                    fixed4 reflectedColor = tex2D(_MainTex, bestUV);
                    fixed4 originalColor = tex2D(_MainTex, i.uv);
                    fixed4 finalColor = lerp(originalColor, reflectedColor, reflectionKoef);

                    return finalColor;
                }
                else
                {
                    return tex2D(_MainTex, i.uv);
                }
            }
            ENDCG
        }
    }
}