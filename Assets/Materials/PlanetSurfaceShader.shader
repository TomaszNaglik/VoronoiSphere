Shader "Custom/PlanetSurfaceShader"
{

    Properties{
          _MainTex("Texture", 2D) = "white" {}
          _Amount("Extrusion Amount", Range(-10,10)) = 0.5
    }
        SubShader{
          Tags { "RenderType" = "Opaque" }
          CGPROGRAM
          #pragma surface surf Lambert vertex:vert
          
              struct Input {
              float2 uv_MainTex;
          };
          float _Amount;
          
          sampler2D _MainTex;
          static const float PI = 3.14159265f;
          
          
          
          
          float Map(float input, float input_start, float input_end, float output_start, float output_end)
          {
              return output_start + ((output_end - output_start) / (input_end - input_start)) * (input - input_start);

          }

          float2 CartesianToPolar(float3 p)
          {
              float xzLen = length(p.xz);
              float2 result;
              result.y = atan2(p.x, p.z);
              result.x = atan2(-p.y, xzLen);

              return result;
          }
          float2 TexCoordinates(float3 p)
          {
              
              float2 polar = CartesianToPolar(p);
              float a = Map(polar.x, -PI / 2.0, PI / 2.0, 0, 1.0);
              float b = Map(polar.y, -PI, PI, 0, 1.0);
              float2 result = float2(a, b);
              

              return result;
          }
          void vert(inout appdata_full v) {
              //v.vertex.xz *= (3+(sin( (_Amount + v.vertex.y)*10)));
              
          }
          
          void surf(Input IN, inout SurfaceOutput o) {
              o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
          }
          ENDCG
          }
              Fallback "Diffuse"
}