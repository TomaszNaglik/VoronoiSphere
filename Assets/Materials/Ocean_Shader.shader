Shader "Custom/Ocean_Shader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalMap2("Normal Map2", 2D) = "bump" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Speed("Speed", float) = 0.5
        _SpecPower("SpecPower", Range(0,100)) = 1

    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha:blend noshadow //fullforwardshadows
            #include "UnityCG.cginc"

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        uniform sampler2D _CameraDepthTexture;
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _NormalMap2;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_NormalMap2;
            float3 worldPos;
            float4 screenPos;
            
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Speed;
        float _SpecPower;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)



        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv1 = IN.uv_NormalMap;
            uv1.x += _Time * _Speed;
            float3 normal1 = UnpackNormal(tex2D(_NormalMap, uv1));
            
            float2 uv2 = IN.uv_NormalMap.yx;
            uv2.x += _Time * _Speed*1.03;
            //uv2.x -= _Time * _Speed * 0.07;
            float3 normal2 = UnpackNormal(tex2D(_NormalMap2, uv2));
            
            float2 coords = IN.screenPos.xy / IN.screenPos.w;
            float nonLinearDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, IN.screenPos);
            float dstToTerrain = LinearEyeDepth(nonLinearDepth);
            float dstToWater = IN.screenPos.w;
            float waterViewDepth = dstToTerrain - dstToWater;
            float3 waterColor = waterViewDepth;
            float waterAlpha = 1;
           // float wave = sin(_Time * _Speed);
            //float3 normal1 = float3(0, 0, 1);// IN.worldPos;
            //normal1 = normalize(float3(wave, 1, 1)+normal1);
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;// tex2D(_MainTex, IN.uv_MainTex)* _Color;
            o.Albedo = c;
            o.Normal = normalize(normal1 +normal2);
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = pow(_Glossiness, _SpecPower);
            o.Alpha = waterAlpha;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
