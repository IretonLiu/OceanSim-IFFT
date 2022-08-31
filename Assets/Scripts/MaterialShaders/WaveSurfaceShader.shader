Shader "Unlit/WaveSurfaceShader"
{
    Properties
    {
        _LightPos("Light Position", Vector) = (0, 0, 0, 0)
        _LightColor("Light Color", Color) = (1, 1, 1, 1)
        _MatDiffuseColor("Material Diffuse Color", Color) = (1, 1, 1, 1)
        _MatSpecularColor("Material Specular Color", Color) = (1, 1, 1, 1)
        _MatSpecularExponent("Specular Exponent", Float) = 0.5

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
            // make fog work
            #pragma multi_compile_fog
            #pragma target 4.0
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
                float3 worldPos: TEXCOORD1;
                float2 worldUV : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float3 viewVector: TEXCOORD4;   
                /*INTERNAL_DATA*/
            };

            //float4  _LightPos;
            float4  _LightColor;
            float4  _MatDiffuseColor;
            float4  _MatSpecularColor;
            float _MatSpecularExponent;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Displacement;
            sampler2D _Slope;

            float lengthScale;

            float3 calculateNormal(float3 slope) {
                float3 up = float3(0.0, 1.0, 0.0);
                float slopeMag = length(slope);
                float3 normal = normalize(float3(-slope.x, 1.0, -slope.z));
                return normal;
            }


            v2f vert (appdata v)
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float2 worldUV = float2(worldPos.xz);
                float4 displacement = tex2Dlod(_Displacement, float4(worldUV, 0, 0) / lengthScale);
                v.vertex += mul(unity_ObjectToWorld, displacement);
                float4 slope = tex2Dlod(_Slope, float4(worldUV, 0, 0) / lengthScale);
                float3 normal = calculateNormal(slope.xyz);
                float3 viewVector = _WorldSpaceCameraPos.xyz - worldPos; // This is V in the lighting equations

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = worldPos;
                o.uv = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.worldUV = worldUV;
                o.normal = normal;
                o.viewVector = viewVector;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 lightingEquation(float3 eyeCoords,              // pixel in eye coord
                float3 N,                      // normal of the pixel
                float3 V                       // direction to the viewer
            ) {
                float3 L, R; // Light and reflected light direction;

                float4 lightDir = -_WorldSpaceLightPos0;

                if (lightDir.w == 0.0) { // directional light
                    L = -normalize(lightDir.xyz);
                }
                else { // point light
                    L = normalize(lightDir.xyz / lightDir.w - eyeCoords);
                };


                float4 emissive_color = float4(1.0, 1.0, 1.0, 1.0);
                float4 ambient_color = float4(0.0, 0.65, 0.75, 1.0);
                float4 diffuse_color = float4(0.5, 0.65, 0.75, 1.0);
                float4 specular_color = float4(1.0, 0.25, 0.0, 1.0);

                float emissive_contribution = 0.00;
                float ambient_contribution = 0.30;
                float diffuse_contribution = 0.30;
                float specular_contribution = 1.80;

                float3 H = normalize(L + V);

                if (dot(L, N) <= 0.0) { // light does not illuminate the surface
                    //return float4(0.0, 0.65, 0.75, 1.0) *0.2;
                    return 0.0;
                };

                //float3 reflection = dot(L, N) * _LightColor.rgb * _MatDiffuseColor.rgb;

                //R = -reflect(L, N);

                //if (dot(R, V) > 0.0) { // ray is reflected toward the the viewer
                //    float factor = pow(dot(R, V), _MatSpecularExponent);
                //    reflection = reflection + _MatSpecularColor.rgb * _LightColor.rgb * factor;

                //}
                float4 color = emissive_color * emissive_contribution +
                    ambient_color * ambient_contribution +
                    diffuse_color * diffuse_contribution * max(dot(L, N), 0) +
                    specular_color * specular_contribution * pow(dot(N, H), 80.0);

                return color;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                //float4 slope = tex2Dlod(_Slope, float4(i.worldUV, 0, 0)*0.001);
                
                // sample the texture

                float3 viewDir = normalize(i.viewVector);
                
                fixed4 col = fixed4(lightingEquation(i.worldPos, i.normal, viewDir));
                //fixed4 col = fixed4(_WorldSpaceLightPos0.xyz, 1.0);
                //fixed4 col = dot(i.normal, normalize(_WorldSpaceLightPos0.xyz));
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
