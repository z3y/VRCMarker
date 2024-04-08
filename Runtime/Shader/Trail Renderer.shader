Shader "Custom/VRCMarker/Trail Renderer"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Scale ("Width", Range(0, 1)) = 0.5
        _GradientPeriod ("Gradient Period", Float) = 70

        //[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
        ////[NoScaleOffset] _MetallicGlossMap("Mask Map", 2D) = "white" {}
        //[NoScaleOffset] _ColorMask("Color Mask Map", 2D) = "white" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5


        //[Toggle(_GRADIENT_ENABLED)] _UseGradient ("UseGradient", Float) = 0

        _GradientLength ("Gradient Length", Float) = 2
    }

    SubShader
    {

        Tags
        {
            "RenderType"="Opaque"
            "DisableBatching" = "True"
        }

        Pass
        {
            Cull Back
            Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout" }
            //ZWrite Off
            //Blend SrcAlpha OneMinusSrcAlpha
            AlphaToMask On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #pragma multi_compile_local _ _GRADIENT_ENABLED


            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                //float2 uv0 : TEXCOORD0;
                float3 otherPos : NORMAL;
                uint vertexID : SV_VertexID;

                UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                nointerpolation bool isLine : TEXCOORD1;

                #ifdef _GRADIENT_ENABLED
                    half3 gradient : TEXCOORD2;
                #endif

                UNITY_VERTEX_OUTPUT_STEREO //Insert
            };

            half3 _Color;
            half _Scale;
            half _GradientPeriod;
            half4 _Gradient[8];
            bool _UseGradient;
            uint _GradientLength;

            float3 centerEyePos()
            {
                // trail tube looks more convincing in vr without this
                // #if defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_SINGLE_PASS_STEREO) 
                //     return 0.5 * (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]);
                // #else
                    return _WorldSpaceCameraPos;
                // #endif
            }

            // thanks error.mdl for help
            float3 billboardTriangle(float3 vertex, float3 triCenter)
            {
                vertex -= triCenter;
                float3 head = centerEyePos();
                float3 center2Head = head - triCenter;
                float c2hLen = length(center2Head);
                float c2hXZLen = length(center2Head.xz);
                
                float sin1 = center2Head.y / c2hLen;
                float cos1 = c2hXZLen / c2hLen;
                float2x2 rot1 = float2x2(cos1, -sin1, -sin1, cos1);
                vertex.zy = mul(rot1, vertex.zy);
                
                float sin2 = center2Head.x / c2hXZLen;
                float cos2 = center2Head.z / c2hXZLen;
                float2x2 rot2 = float2x2(cos2, sin2, -sin2, cos2);
                vertex.xz = mul(rot2, vertex.xz);

                vertex += triCenter;
                return vertex;
            }

            // vertices always set in this order, i hope
            static const float2 offsets[7] =
            {
                float2(0, 0),
                float2(0, 1),
                float2(1, 1),
                float2(1, 0),
                float2(-0.077350269189625764509148780501f, 0),
                float2(0.5f, 1),
                float2(1.077350269189625764509148780501f, 0)
            };

            struct Gradient
            {
                int type;
                int colorsLength;
                half4 colors[8];
            };

            Gradient NewGradient(int type, int colorsLength, half4 colors[8])
            {
                Gradient output =
                {
                    type, colorsLength, colors
                };
                return output;
            }

            half3 EvaluateGradient(Gradient gradient, half time)
            {
                half3 color = gradient.colors[0].rgb;
                [unroll(6)]
                for (int c = 1; c < gradient.colorsLength; c++)
                {
                    half colorPos = saturate((time - gradient.colors[c - 1].w) / (gradient.colors[c].w - gradient.colors[c - 1].w)) * step(c, gradient.colorsLength - 1);
                    color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
                }

                return color;
            }

            half evaluateTime(uint id, half frequency)
            {
                half t = (sin((float)id / frequency) + 1.0) / 2.0;
                return t;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

                float3 vertexPos = v.vertex.xyz;
                bool isLine = any(abs(v.otherPos));

                float2 offset = offsets[v.vertexID % 7];

                UNITY_BRANCH
                if (isLine)
                {
                    float3 v1 = v.otherPos - v.vertex.xyz;
                    float3 v2 = centerEyePos() - v.vertex.xyz;
                    float3 scaleDir = normalize(cross(v1, v2));
                    scaleDir *= _Scale;

                    if (offset.x > 0) scaleDir = -scaleDir;
                    // scaleDir *= offset.x * 2 - 1;
                    
                    v.vertex.xyz += scaleDir * offset.y;
                    v.vertex.xyz -= scaleDir * (1 - offset.y);
                    
                    o.vertex = mul(UNITY_MATRIX_VP, v.vertex);

                }
                else
                {
                    float scaleFactor = 1.5;
                    float4 center = v.vertex;
                    float2 triangleOffset = offset * 2 - 1;
                    triangleOffset *= _Scale * scaleFactor;
                    triangleOffset.y += _Scale * scaleFactor * UNITY_PI * 0.106;

                    float3 vertexOffset = float3(triangleOffset.x, triangleOffset.y, 0);
                    float3 triangleVertex = float3(v.vertex.xy + triangleOffset, v.vertex.z);
                    v.vertex.xyz = billboardTriangle(triangleVertex, v.vertex.xyz);
                    o.vertex = mul(UNITY_MATRIX_VP, v.vertex);
                }

                if (all(vertexPos) == 0)
                {
                    o.vertex = 0.0 / 0.0;
                }

                //o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.uv0 = offset;
                o.isLine = isLine;

                #ifdef _GRADIENT_ENABLED

                    uint index = v.vertexID % 7;
                    float vv = index >= 2 ? v.vertexID - index : v.vertexID + (7 - index);
                    // half vv = v.vertexID;

                    if (!isLine)
                    {
                        vv = v.vertexID + index - 3;
                    }

                    half t = evaluateTime(vv, _GradientPeriod);
                    Gradient g = NewGradient(0, _GradientLength, _Gradient);
                    // color = EvaluateGradient(g,t);

                    o.gradient = EvaluateGradient(g,t);
                    // o.gradient = m;
                #endif
                
                return o;
            }


            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
                
                half alpha = 1;


                UNITY_BRANCH
                if (!i.isLine)
                {
                    float2 coord = i.uv0.xy;
                    coord.x -= 0.5;
                    coord.y -= 1./3.;

                    float circle = length(coord);
                    float size = 1.0 / 3.;

                    float pwidth = fwidth(circle);
                    alpha = saturate((size - circle) / pwidth);
                    // alpha = 0;
                }
                else
                {
                    float size = abs(i.uv0.y - 0.5) * 2;
                    size = 1-size;
                    float pwidth = fwidth(size);

                    alpha = saturate((size) / pwidth);
                    alpha = saturate(alpha + pwidth / 2);
                    // alpha = 0;
                }



                #ifdef _GRADIENT_ENABLED
                    half3 color = i.gradient;
                #else
                    half3 color = _Color;
                #endif

                clip(alpha - 0.1);


                return half4(color, alpha);
            }
            ENDCG
        }
    }
}