Shader "Pens/Pen"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ColorTop ("Eraser Color", Color) = (1,1,1,1)
        [HideInInspector] _InkColor ("Ink", Color) = (1,1,1)
        [HideInInspector] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5

        [ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 0
        [ToggleOff] _GlossyReflections ("Reflections", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
        #pragma shader_feature _GLOSSYREFLECTIONS_OFF

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
        };

        half _Glossiness;
        fixed4 _Color;
        fixed4 _ColorTop;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(half3, _InkColor)
        UNITY_INSTANCING_BUFFER_END(Props)

        float GSAA(float3 worldNormal, float perceptualRoughness)
        {
            // Kaplanyan 2016, "Stable specular highlights"
            // Tokuyoshi 2017, "Error Reduction and Simplification for Shading Anti-Aliasing"
            // Tokuyoshi and Kaplanyan 2019, "Improved Geometric Specular Antialiasing"

            // This implementation is meant for deferred rendering in the original paper but
            // we use it in forward rendering as well (as discussed in Tokuyoshi and Kaplanyan
            // 2019). The main reason is that the forward version requires an expensive transform
            // of the half vector by the tangent frame for every light. This is therefore an
            // approximation but it works well enough for our needs and provides an improvement
            // over our original implementation based on Vlachos 2015, "Advanced VR Rendering".

            float3 du = ddx(worldNormal);
            float3 dv = ddy(worldNormal);

            float variance = 0.15 * (dot(du, du) + dot(dv, dv));

            float roughness = perceptualRoughness * perceptualRoughness;
            float kernelRoughness = min(2.0 * variance, 0.1);
            float squareRoughness = saturate(roughness * roughness + kernelRoughness);

            return sqrt(sqrt(squareRoughness));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half2 uv = IN.uv_MainTex;

            half2 uvStep = saturate(step(uv.xy, 0.5));

            half3 penInk = UNITY_ACCESS_INSTANCED_PROP(Props, _InkColor);
            
            half uvTopLeft = uvStep.x - uvStep.y;
            half uvBottomRight = uvStep.y - uvStep.x;
            half uvBottomLeft = uvStep.x * uvStep.y;
            half uvTopRight = 1 - (uvStep.y * uvTopLeft);


            o.Albedo = saturate(uvTopLeft * _ColorTop + uvTopRight * _Color + uvBottomLeft * penInk);
            #ifndef SHADER_API_MOBILE
            o.Smoothness = 1 - GSAA( IN.worldNormal, 1-_Glossiness);
            #endif
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
