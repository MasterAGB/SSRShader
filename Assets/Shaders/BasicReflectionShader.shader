Shader "Custom/DepthAndNormalsVisualizer"
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
        LOD 100


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
            sampler2D _CameraGBufferTexture1;

            int _MaxSteps;
            float _IsRectangular;
            float _StepSize;
            float _CameraFarPlane;
            float _DepthHit;


            float _TestNumber;
            float _TestNumber2;
            float _TestNumber3;
            float _TestNumber4;
            float3 _TestVector3;
            float4x4 _ScreenSpaceProjectionMatrix;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            // Расчет отраженного луча с учетом ориентации поверхности и позиции наблюдателя
            float3 CalculateReflectedRay(float3 viewDirection, float3 normal)
            {
                // Отражаем viewDirection относительно нормали поверхности
                return reflect(-viewDirection, normal);
            }

            float3 ModifyRayForRectangularEffect(float3 ray, float3 normal)
            {
                float threshold = 0.9; // Порог для определения "прямоугольности"
                float3 modifiedRay = ray;

                if (abs(normal.y) > threshold)
                {
                    // Если нормаль указывает вверх или вниз, модифицируем X и Z компоненты луча
                    modifiedRay.x = 0;
                    modifiedRay.z = 0;
                }
                // Аналогично можно добавить условия для обработки других ориентаций нормали

                return modifiedRay;
            }


            /*
            float3 ConvertUVandDepthTo3D(float2 uv, float depth)
            {
                //TODO: convert depth to Z coord, knowing the camera far clip plance, that can help calculate the white pixel distance.
                //Probably this is correct?
                 //float oneMeterPixelDepth = (255.0 / _ProjectionParams.z) / 255.0;
                //float currentDepth3D = depth * oneMeterPixelDepth;
                //TODO: convert uv coords (0..1) to 3D space real coordinates -X to X (And -Y to Y), where 0 is in the middle, X is max left meters, that i can see on this uv coord in this depth. musst know camera proj-FOV
                return float3(x,y,z);
            }
            float3 Convert3DToUVandDepth(float3 xyz)
            {
                //TODO - and make the back calculation here:
                float depth=
                float uvx=
                float uvy=                    
                return float3(uvx, uvy, depth);
            }
            */


            float3 GetInitialNormal(float2 uv)
            {
                //normalInViewSpace -// Получение нормали из текстуры нормалей 
                float3 initialNormal = normalize(tex2D(_CameraNormalsTexture, uv).xyz * 2.0 - 1.0);

                // Преобразование нормали из пространства вида в мировое пространство
                float3 normalInWorldSpace = mul((float3x3)unity_WorldToCamera, initialNormal);
                //unity_WorldToCamera is unity prefefined const for that - _CameraToWorldMatrix = unity_WorldToCamera

                initialNormal = normalInWorldSpace;
                return initialNormal;
            }

            float3 GetViewSpacePosition(float2 uv, float depth)
            {
                //float depth = _CameraDepthTexture.SampleLevel(sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv), 0).r;
                float4 result = float4(
                    float2(2.0 * uv - 1.0) * float2(unity_CameraInvProjection[0][0], unity_CameraInvProjection[1][1]),
                    depth * unity_CameraInvProjection[2][2] + unity_CameraInvProjection[2][3],
                    _ZBufferParams.z * depth + _ZBufferParams.w);
                // Use _ZBufferParams as it accounts for 0...1 depth value range
                return result.xyz / result.w;
            }


            float3 UVDepthToViewSpace(float2 uv, float depth)
            {
                return GetViewSpacePosition(uv, depth);

                // Pārveidojums no UV un normālizēta dziļuma uz NDC (normālizētās koordinātas ierīces telpā)
                float2 ndcXY = uv * 2.0 - 1.0; // UV no [0,1] uz [-1,1]
                float ndcZ = depth * 2.0 - 1.0; // Depth no [0,1] uz [-1,1]

                // Pārveidojums no NDC uz klipa telpu
                float4 clipSpacePos = float4(ndcXY, ndcZ, 1.0);

                // Pārveidojums no klipa telpas uz skata telpu
                float4 viewSpacePosition = mul(unity_CameraInvProjection, clipSpacePos);
                viewSpacePosition /= viewSpacePosition.w;

                return viewSpacePosition.xyz;
            }


            float4 ProjectToScreenSpace(float3 position)
            {

                //float4x4 _ScreenSpaceProjectionMatrix = unity_CameraProjection;
                
                return float4(
                    _ScreenSpaceProjectionMatrix[0][0] * position.x + _ScreenSpaceProjectionMatrix[0][2] * position.z,
                    _ScreenSpaceProjectionMatrix[1][1] * position.y + _ScreenSpaceProjectionMatrix[1][2] * position.z,
                    _ScreenSpaceProjectionMatrix[2][2] * position.z + _ScreenSpaceProjectionMatrix[2][3],
                    _ScreenSpaceProjectionMatrix[3][2] * position.z
                );
            }


            
            float3 ViewSpaceToUVDepth(float3 viewSpacePosition)
            {
                return ProjectToScreenSpace(viewSpacePosition);
                // Pārveido no skata telpas uz klipa telpu
                float4 clipSpacePosition = mul(unity_CameraProjection, float4(viewSpacePosition, 1.0));

                // Normalizējam perspektīvu, dalot ar w
                clipSpacePosition.xyz /= clipSpacePosition.w;

                // Pārveido no klipa telpas uz NDC
                float3 ndcPos = clipSpacePosition.xyz;

                // Pārveido no NDC uz UV (0..1 diapazonā)
                float2 uv = (ndcPos.xy * 0.5 + 0.5);

                // Pārveido klipa telpas dziļumu atpakaļ uz normālizētu dziļumu (0..1)
                float normalizedDepth = (ndcPos.z * 0.5 + 0.5);

                return float3(uv, normalizedDepth);
            }


            fixed4 frag(v2f i) : SV_Target
            {
                /**** Start of my Shader ***/


                fixed4 initialColor = tex2D(_MainTex, i.uv);


                //


                float specular = tex2D(_CameraGBufferTexture0, i.uv.xy).a;
                if (specular < 0.1)
                {
                    return initialColor;
                }


                float3 initialNormal = GetInitialNormal(i.uv);


                //i have this initialnormal - but i wanna it to be mulriplied by cameras rotation, so in my shader i think, that normal is rotated towards me, even if it is rotated backwards from camera, but camera is facing back. understand? so i can also track correctly the normals, when i rotate camera back:


                // Получаем нормаль                
                float initialDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r);
                // Получаем глубину                
                float2 uvSS = i.uv * 2.0 - 1.0;
                float3 viewRay = (float3(uvSS, initialDepth)); // Создаем луч взгляда


                bool breakOnFirstMet = false;
                bool testWorldConvertions = (_TestNumber2 > 0.5);
                bool testWorldConvertions2 = (_TestNumber3 > 0.5);
                bool useWorldCoords = (_TestNumber4 > 0.5);


                if (useWorldCoords)
                {
                    viewRay = UVDepthToViewSpace(i.uv, initialDepth) + _TestVector3;
                }


                if (testWorldConvertions)
                {
                    float3 xyz3 = UVDepthToViewSpace(i.uv, initialDepth);
                    float3 xyz4 = ViewSpaceToUVDepth(xyz3);
                    if (testWorldConvertions2)
                    {
                        return float4(xyz4.z, xyz4.x, xyz4.y, 1);
                    }
                    return float4(xyz3.y, xyz3.x, xyz3.z, 1);
                }

                //fixed4 x = tex2D(_MainTex, viewRay);return x;

                //float3 viewRayNormalized = normalize(viewRay);
                //float3 viewRayNormalized = (viewRay); //We dont normalize it, it works weird

                //in this line - i wanna make the X be multiplied by Zero, if the initialNormal is some sort of vertical (up or down)
                //i wanna make the effect of Rectangular ReflectionProbe, not spherical, right?
                //float3 viewRayNormalized = float3(viewRay.x, viewRay.y, viewRay.z);
                // Определяем, является ли нормаль вертикальной

                float3 viewRayNormalized;
                if (_IsRectangular > 0.5f)
                {
                    float rectangularThreshhold = 0.9;


                    bool isFloorReflection = abs(initialNormal.y) > rectangularThreshhold;
                    bool isSideWallReflection = abs(initialNormal.x) > rectangularThreshhold;
                    bool isDeepReflection = abs(initialNormal.z) > rectangularThreshhold;


                    float verticalHorizontalResetValue = 0.02;
                    float zResetValue = 1;

                    float isVerticalMultiply = isFloorReflection ? verticalHorizontalResetValue : 1;
                    // Это условие проверяет, насколько "вертикальна" нормаль

                    float isHorizontalMultiply = isSideWallReflection ? verticalHorizontalResetValue : 1;
                    // Это условие проверяет, насколько "X" нормаль


                    float isDeepMultiply = isDeepReflection ? zResetValue : 1;
                    // Это условие проверяет, насколько "Z" нормаль

                    viewRayNormalized = viewRay;
                    viewRayNormalized *= float3(isVerticalMultiply, isHorizontalMultiply, isDeepMultiply);

                    //What are those magic numbers?
                    float xMultyply = 0.6;
                    float yMultyply = 0.6;
                    float zMultyply = 0.6;


                    float magicMultiply = (initialDepth + 1.0);

                    //For floor reflection
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
                }
                else
                {
                    viewRayNormalized = (viewRay);
                }

                float3 reflectedRay = reflect(viewRayNormalized, initialNormal); // Отражаем луч от поверхности


                //fixed4 x = tex2D(_MainTex, reflectedRay);return x;

                float3 initialUV = float3(i.uv, initialDepth);
                float3 currentUV = float3(i.uv, initialDepth);
                float3 bestUV = float3(i.uv, initialDepth);
                bool wasHit = false;


                if (useWorldCoords)
                {
                    initialUV = UVDepthToViewSpace(i.uv, initialDepth);
                    currentUV = UVDepthToViewSpace(i.uv, initialDepth);
                    bestUV = UVDepthToViewSpace(i.uv, initialDepth);
                }


                float bestDifference = 1000000;


                int bestStep = 0;

                [unroll(40)]
                for (int step = 1; step <= _MaxSteps; step++)
                {
                    currentUV += reflectedRay * _StepSize; // Двигаемся по направлению отраженного луча
                    // Проверяем границы

                    if (useWorldCoords)
                    {
                    }
                    else
                    {
                        if (currentUV.x < 0.0 || currentUV.x > 1.0 || currentUV.y < 0.0 || currentUV.y > 1.0) break;
                    }


                    float currentDepthNormal = Linear01Depth(tex2D(_CameraDepthTexture, currentUV.xy).r);
                    if (useWorldCoords)
                    {
                        currentDepthNormal =
                            Linear01Depth(tex2D(_CameraDepthTexture, ViewSpaceToUVDepth(currentUV)).r);
                    }
                    float oneMeterPixelDepth = (255.0 / _ProjectionParams.z) / 255.0;
                    //We dont use FAR_PLANE, stuff is in _ProjectionParams!
                    float depthOffset = _StepSize * step * reflectedRay.z * oneMeterPixelDepth;
                    float currentDepth3D = currentDepthNormal * oneMeterPixelDepth;
                    float initialDepth3D = initialDepth * oneMeterPixelDepth;
                    float assumedDepth3D = initialDepth3D + depthOffset;
                    float depthDifference = abs(currentDepth3D - assumedDepth3D);

                    if (useWorldCoords)
                    {
                        depthDifference = length(initialUV - currentUV);
                    }


                    if (true)
                    {
                        // Преобразование UV координат в экранные координаты (в пикселях)
                        float2 initialScreenPos = i.uv * float2(_ScreenParams.x, _ScreenParams.y);
                        float2 currentScreenPos = currentUV * float2(_ScreenParams.x, _ScreenParams.y);
                        if (useWorldCoords)
                        {
                            currentScreenPos = ViewSpaceToUVDepth(currentUV) *
                                float2(_ScreenParams.x, _ScreenParams.y);
                        }
                        // Проверка, находится ли текущая точка в пределах некоторого порога от исходной точки
                        float2 diff = abs(currentScreenPos - initialScreenPos);
                        float pixDiff = 1.0;
                        bool isSamePixel = (diff.x < pixDiff) && (diff.y < pixDiff); // Порог в 1 пиксель

                        if (isSamePixel)
                        {
                            continue;
                        }


                        fixed4 currentColor = tex2D(_MainTex, currentUV);
                        if (useWorldCoords)
                        {
                            currentColor = tex2D(_MainTex, ViewSpaceToUVDepth(currentUV));
                        }
                        // Вычисляем разницу между текущим и начальным цветом
                        float colorDifference = length(currentColor - initialColor);
                        // Сравниваем разницу с пороговым значением
                        if (colorDifference < 0.01f)
                        {
                            // continue;
                        }


                        //HERE We must check, if the Depth at least is not BEHIND the ferlected ray.. So if reflectedRay goes forward, means, we dont accept depth, that is behind..
                        //It will help not to reflect object itself, when the ray reflects right in the camera
                        //Для реализации этой логики в вашем TODO разделе, вам нужно сравнить currentDepth (текущая глубина в точке, куда указывает отраженный луч) с initialDepth (глубина в точке отражения). Однако, просто использовать разницу между этими значениями недостаточно, так как вам также необходимо учесть направление луча относительно камеры.
                        // Проверяем, что текущая глубина больше начальной, что указывает на "передний" объект
                        if (currentDepthNormal > initialDepth)
                        {
                            if (reflectedRay.z < 0)
                            {
                                continue;
                            }
                        }
                        else if (currentDepthNormal < initialDepth)
                        {
                            if (reflectedRay.z > 0)
                            {
                                continue;
                            }
                        }


                        //instead of getting just the normal - we also normalize it towards camera
                        //float3 currentNormal = normalize(tex2D(_CameraNormalsTexture, currentUV.xy).xyz * 2.0 - 1.0);
                        float3 currentNormal = GetInitialNormal(currentUV.xy);
                        if (useWorldCoords)
                        {
                            currentNormal = GetInitialNormal(ViewSpaceToUVDepth(currentUV));
                        }

                        //now we have to check, if we are not trying to reflect the object, that actually has normale, that is not able to be reflected, because it faces kinda same direction, not opposize..
                        //So we have to check, if the RAY - reflectedRay is facing kinda not same direction as this currentNormale, right? currentNormal
                        // Проверяем направление отраженного луча и нормали текущей точки
                        float dotProduct = dot(reflectedRay, currentNormal);
                        // Если скалярное произведение положительно, это означает, что луч и нормаль
                        // направлены примерно в одном направлении, и поверхность не должна отражать луч.
                        // В этом случае мы пропускаем текущий шаг цикла.
                        if (dotProduct > 0)
                        {
                            continue;
                        }
                    }

                    // Если находим поверхность ближе к камере, чем начальная точка + некий порог, считаем, что произошло столкновение
                    if (depthDifference <= _DepthHit)
                    {
                        wasHit = true;

                        if (bestDifference > depthDifference)
                        {
                            bestDifference = depthDifference;
                            bestUV = currentUV;
                            bestStep = step;
                        }
                        if (breakOnFirstMet)
                        {
                            break; // Выходим из цикла, найдено столкновение
                        }
                    }
                }


                if (wasHit)
                {
                    float reflectionKoef = (1.0 - (float)bestStep / (float)_MaxSteps);
                    float diffKoef = (1.0 - (float)bestDifference / (float)_DepthHit);

                    reflectionKoef = reflectionKoef * diffKoef * specular;
                    if (useWorldCoords)
                    {
                        bestUV = ViewSpaceToUVDepth(bestUV);
                    }
                    // Выборка и возвращение цвета из основной текстуры в точке столкновения
                    fixed4 reflectedColor = tex2D(_MainTex, bestUV);
                    // Исходный цвет в текущей позиции камеры
                    fixed4 originalColor = tex2D(_MainTex, i.uv);

                    // Интерполяция между отраженным цветом и исходным цветом на основе коэффициента отражения
                    fixed4 finalColor = lerp(originalColor, reflectedColor, reflectionKoef);

                    return finalColor;
                }
                else
                {
                    //return fixed4(0, 0, 0, 0); // Пример для прозрачности
                    fixed4 reflectedColor = tex2D(_MainTex, i.uv);
                    return reflectedColor;
                }
            }


            /**** End of my shader ***/
            ENDCG
        }
    }
}