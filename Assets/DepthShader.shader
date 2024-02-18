Shader "Hidden/DepthShader"
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
            float _TestNumber;
            float _TestNumber2;
            float _TestNumber3;
            float _TestNumber4;
            float _StepSize;
            float _CameraFarPlane;
            float4x4 _CameraToWorldMatrix;
            float _DepthHit;


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                /**** Start of my Shader ***/
            
                float initialDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r); // Получаем глубину
                return float4(initialDepth,initialDepth,initialDepth,1);
                
            }


            /**** End of my shader ***/
            ENDCG
        }
    }
}