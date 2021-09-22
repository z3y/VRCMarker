Shader "Custom/Pens"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ColorTop ("Top Color", Color) = (1,1,1,1)
        [HideInInspector] _InkColor ("Ink", Color) = (1,1,1,1)
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
        };

        half _Glossiness;
        fixed4 _Color;
        fixed4 _ColorTop;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _InkColor)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half2 uv = IN.uv_MainTex;

            half uvMask1 = float4(step(uv.yyy,0.25),1);
            half uvMaskBase = float4(step(uv.yyy,0.75),1) * (1- uvMask1);
            half uvMask3 = (1 - uvMask1) - uvMaskBase;

            half3 penInk = (uvMask1 * UNITY_ACCESS_INSTANCED_PROP(Props, _InkColor));
            
            o.Albedo = uvMask3 * _ColorTop + ( uvMaskBase * _Color) + penInk;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
