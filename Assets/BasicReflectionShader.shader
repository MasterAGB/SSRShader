Shader "Custom/DepthAndNormalsVisualizer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mode ("Visualization Mode (0: Depth, 1: Normals, 2: Texture, 3: Fog, 4: Reflections1, 5: Reflections2, 6: Reflections3)", Float) = 0
        _MaxSteps ("_MaxSteps", Int) = 10
        _StepSize ("_StepSize", Float) = 0.1
        _DepthHit ("_DepthHit", Float) = 0.1


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

            float _Mode;
            int _MaxSteps;
            float _IsRectangular;
            float _TestNumber;
            float _StepSize;
            float _CameraFarPlane;
            float _DepthHit;


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

            fixed4 frag(v2f i) : SV_Target
            {
                /**** Start of my Shader ***/


                fixed4 initialColor = tex2D(_MainTex, i.uv);


                float specular = tex2D(_CameraGBufferTexture0, i.uv.xy).a;
                if (specular < 0.1)
                {
                    return initialColor;
                }

                // Простейшая демонстрация "трассировки луча" для screen-space отражений
                float3 initialNormal = normalize(tex2D(_CameraNormalsTexture, i.uv).xyz * 2.0 - 1.0);
                // Получаем нормаль
                float initialDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r); // Получаем глубину
                float3 viewRay = (float3(i.uv * 2.0 - 1.0, initialDepth)); // Создаем луч взгляда

                //fixed4 x = tex2D(_MainTex, viewRay);return x;

                //float3 viewRayNormalized = normalize(viewRay);
                //float3 viewRayNormalized = (viewRay); //We dont normalize it, it works weird

                //in this line - i wanna make the X be multiplied by Zero, if the initialNormal is some sort of vertical (up or down)
                //i wanna make the effect of Rectangular ReflectionProbe, not spherical, right?
                //float3 viewRayNormalized = float3(viewRay.x, viewRay.y, viewRay.z);
                // Определяем, является ли нормаль вертикальной

                float3 viewRayNormalized = viewRay;
                if (_IsRectangular > 0.5f)
                {
                    float rectangularThreshhold = 0.9;


                    float isVerticalMultiply = abs(initialNormal.y) > rectangularThreshhold ? 0 : 1;
                    // Это условие проверяет, насколько "вертикальна" нормаль
                    float isHorizontalMultiply = abs(initialNormal.x) > rectangularThreshhold ? 0 : 1;
                    // Это условие проверяет, насколько "X" нормаль
                    float isDeepMultiply = abs(initialNormal.z) > rectangularThreshhold ? 0 : 1;
                    // Это условие проверяет, насколько "Z" нормаль

                    if (abs(initialNormal.y) > rectangularThreshhold)
                    {
                    }

                    viewRayNormalized *= float3(isVerticalMultiply, isHorizontalMultiply, isDeepMultiply);


                    if (_TestNumber > 0.5f)
                    {
                        //viewRayNormalized = normalize(viewRayNormalized);
                    }
                }

                float3 reflectedRay = reflect(viewRayNormalized, initialNormal); // Отражаем луч от поверхности


                //fixed4 x = tex2D(_MainTex, reflectedRay);return x;

                float3 currentUV = float3(i.uv, initialDepth);
                float3 bestUV = float3(i.uv, initialDepth);
                bool wasHit = false;


                float bestDifference = 1000000;


                int bestStep = 0;

                [unroll(30)]
                for (int step = 1; step <= _MaxSteps; step++)
                {
                    currentUV += reflectedRay * _StepSize; // Двигаемся по направлению отраженного луча
                    // Проверяем границы
                    if (currentUV.x < 0.0 || currentUV.x > 1.0 || currentUV.y < 0.0 || currentUV.y > 1.0) break;


                    // Преобразование UV координат в экранные координаты (в пикселях)
                    float2 initialScreenPos = i.uv * float2(_ScreenParams.x, _ScreenParams.y);
                    float2 currentScreenPos = currentUV * float2(_ScreenParams.x, _ScreenParams.y);
                    // Проверка, находится ли текущая точка в пределах некоторого порога от исходной точки
                    float2 diff = abs(currentScreenPos - initialScreenPos);
                    float pixDiff = 1.0;
                    bool isSamePixel = (diff.x < pixDiff) && (diff.y < pixDiff); // Порог в 1 пиксель

                    if (isSamePixel)
                    {
                        continue;
                    }


                    fixed4 currentColor = tex2D(_MainTex, currentUV);
                    // Вычисляем разницу между текущим и начальным цветом
                    float colorDifference = length(currentColor - initialColor);
                    // Сравниваем разницу с пороговым значением
                    if (colorDifference < 0.01f)
                    {
                        // continue;
                    }


                    float currentDepth = Linear01Depth(tex2D(_CameraDepthTexture, currentUV.xy).r);
                    float oneMeterPixelDepth = (255.0 / _CameraFarPlane) / 255.0;
                    float depthOffset = _StepSize * step * reflectedRay.z * oneMeterPixelDepth;
                    float assumedDepth = initialDepth + depthOffset;
                    float depthDifference = abs(currentDepth - assumedDepth);


                    //HERE We must check, if the Depth at least is not BEHIND the ferlected ray.. So if reflectedRay goes forward, means, we dont accept depth, that is behind..
                    //It will help not to reflect object itself, when the ray reflects right in the camera
                    //Для реализации этой логики в вашем TODO разделе, вам нужно сравнить currentDepth (текущая глубина в точке, куда указывает отраженный луч) с initialDepth (глубина в точке отражения). Однако, просто использовать разницу между этими значениями недостаточно, так как вам также необходимо учесть направление луча относительно камеры.
                    // Проверяем, что текущая глубина больше начальной, что указывает на "передний" объект
                    if (currentDepth > initialDepth)
                    {
                        if (reflectedRay.z < 0)
                        {
                            continue;
                        }
                    }
                    else if (currentDepth < initialDepth)
                    {
                        if (reflectedRay.z > 0)
                        {
                            continue;
                        }
                    }


                    float3 currentNormal = normalize(tex2D(_CameraNormalsTexture, currentUV.xy).xyz * 2.0 - 1.0);
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
                        //break; // Выходим из цикла, найдено столкновение
                    }
                }


                if (wasHit)
                {
                    float reflectionKoef = (1.0 - (float)bestStep / (float)_MaxSteps);
                    float diffKoef = (1.0 - (float)bestDifference / (float)_DepthHit);

                    reflectionKoef = reflectionKoef * diffKoef * specular;

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