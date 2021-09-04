Shader "Unlit/Ocean_Shader_Unlit"
{
    Properties
    {
       // _MainTex ("Texture", 2D) = "white" {}
        _Shininess ("Shininess", float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform float _Shininess;

            struct v2f
            {
                half3 normal : NORMAL;
                float4 pos : SV_POSITION;
            };

            
            v2f vert (float4 vertex: POSITION, float3 normal:NORMAL)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.normal = normal;
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float AmbientIntensity = 0.2;
                float3 AmbientColor = float3(0.2, 0.5, 0.7);
                float3 DiffuseColor = float3(0.7, 0.5, 0.3);

                float4x4 modelMatrix = unity_ObjectToWorld;
                float3x3 modelMatrixInverse = unity_WorldToObject;
                float3 normalDirection = normalize(mul(i.normal, modelMatrixInverse));
                float3 viewDirection = normalize(_WorldSpaceCameraPos - mul(modelMatrix, i.pos).xyz);

                float3 lightDir = _WorldSpaceLightPos0;

                
                float3 DiffuseLight = max(0,dot(lightDir,i.normal)) * DiffuseColor;
                float3 AmbientLight = float3(AmbientIntensity, AmbientIntensity, AmbientIntensity) * AmbientColor;
                
                float3 SpecularLight = pow(max(0.0, dot(reflect(-lightDir, i.normal), viewDirection)), _Shininess);
                

                float3 FinalLight = DiffuseLight + AmbientLight + SpecularLight;
                
                return float4(FinalLight,1);
            }
            ENDCG
        }
    }
}
