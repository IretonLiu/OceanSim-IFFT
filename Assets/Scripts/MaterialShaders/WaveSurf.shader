Shader "Custom/WaveSurf"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma multi_compile

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0

        sampler2D _MainTex;
        sampler2D _Displacement;
        sampler2D _Slope;

        float lengthScale;



        struct Input
        {
            float4 vertex;
            float3 worldPos;
            float2 worldUV;
            float3 normal;
            float3 viewVector;
            INTERNAL_DATA
        };


        float3 calculateNormal(float3 slope) {
            float3 up = float3(0.0, 1.0, 0.0);
            float slopeMag = length(slope);
            float3 normal = normalize(float3(slope.x, 1.0, slope.z)); // this is techniquelly not correct, can't figure out why tho
            return normal;
        }

        void vert(inout appdata_full v, out Input o) {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float2 worldUV = float2(worldPos.xz);
            float4 displacement = tex2Dlod(_Displacement, float4(worldUV, 0, 0) / lengthScale);
            v.vertex += mul(unity_ObjectToWorld, displacement);
            float4 slope = tex2Dlod(_Slope, float4(worldUV, 0, 0) / lengthScale);
            float3 normal = calculateNormal(slope.xyz);
            float3 viewVector = _WorldSpaceCameraPos.xyz - worldPos; // This is V in the lighting equations

            o.vertex = UnityObjectToClipPos(v.vertex);
            o.worldPos = worldPos;
            o.worldUV = worldUV;
            o.normal = normal;
            o.viewVector = viewVector;
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float4 lightingEquation(float3 eyeCoords,              // pixel in eye coord
                float3 N,                      // normal of the pixel
                float3 V                       // direction to the viewer
            ) {
            float3 L, R; // Light and reflected light direction;

            float4 lightPos = _WorldSpaceLightPos0;

            if (lightPos.w == 0.0) { // directional light
                L = normalize(lightPos.xyz);
            }
            else { // point light
                L = normalize(lightPos.xyz / lightPos.w - eyeCoords);
            };


            float4 emissive_color = float4(1.0, 1.0, 1.0, 1.0);
            float4 ambient_color = float4(0.0, 0.65, 0.95, 1.0);
            float4 diffuse_color = float4(0.5, 0.65, 0.75, 1.0);
            float4 specular_color = float4(1.0, 0.8, 0.8, 1.0);

            float emissive_contribution = 0.00;
            float ambient_contribution = 0.30;
            float diffuse_contribution = 0.30;
            float specular_contribution = 1.80;

            float3 H = normalize(L + V);

            if (dot(L, N) <= 0.0) { // light does not illuminate the surface
                return ambient_color *0.1;
                //return 0.0;
            };

            float4 color = emissive_color * emissive_contribution +
                ambient_color * ambient_contribution +
                diffuse_color * diffuse_contribution * max(dot(L, N), 0) +
                specular_color * specular_contribution * max(0.001,pow(dot(N, H), 80.0));

            return color;
        }

        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1, 0, 0));
            float3 t2w1 = WorldNormalVector(IN, float3(0, 1, 0));
            float3 t2w2 = WorldNormalVector(IN, float3(0, 0, 1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float3 viewDir = normalize(IN.viewVector);
            float3 normal = WorldToTangentNormalVector(IN, IN.normal);

            fixed4 c = fixed4(lightingEquation(IN.worldPos, IN.normal, viewDir));
            o.Albedo = c;
            // Metallic and smoothness come from slider variables
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
