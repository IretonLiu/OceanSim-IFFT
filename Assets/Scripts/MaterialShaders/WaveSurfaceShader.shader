Shader "Unlit/WaveSurfaceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 worldUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Displacement;
            sampler2D _Slope;

            float lengthScale;

            v2f vert (appdata v)
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float2 worldUV = float2(worldPos.xz);
                float4 displacement = tex2Dlod(_Displacement, float4(worldUV, 0, 0)*100000);
                v.vertex += displacement;


                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldUV = worldUV;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            struct LightProperties {
                float4 position;
                float3 color;
            };

            struct MaterialProperties {
                float3 diffuseColor;
                float3 specularColor;
                float specularExponent;
            };

            float3 lightingEquation(LightProperties light,       // light
                MaterialProperties material, // material
                float3 eyeCoords,              // pixel in eye coord
                float3 N,                      // normal of the pixel
                float3 V                       // direction to the viewer
            ) {
                float3 L, R; // Light and reflected light direction;

                if (light.position.w == 0.0) { // directional light
                    L = normalize(light.position.xyz);
                }
                else { // point light
                    L = normalize(light.position.xyz / light.position.w - eyeCoords);
                };

                if (dot(L, N) <= 0.0) { // light does not illuminate the surface
                    return 0.0;
                };

                float3 reflection = dot(L, N) * light.color * material.diffuseColor;

                R = -reflect(L, N);

                if (dot(R, V) > 0.0) { // ray is reflected toward the the viewer
                    float factor = pow(dot(R, V), material.specularExponent);
                    reflection = reflection + material.specularColor * light.color * factor;

                    return reflection;
                }
            }

            float3 calculateNormal (float3 slope) {
                float3 up = float3(0.0, 1.0, 0.0);
                float slopeMag = length(slope);
                float3 normal = (up - slope) / sqrt(1 + slopeMag * slopeMag);
                return normal;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                float4 slope = tex2Dlod(_Slope, float4(i.worldUV, 0, 0) * 100000);
                float3 normal = calculateNormal(slope.xyz);
                // sample the texture
                fixed4 col = float4(normal, 1.0);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
