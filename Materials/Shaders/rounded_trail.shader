// original shader by phi16 https://phi16.github.io/VRC_storage/#rounded_trail
Shader "Unlit/Custom Rounded Trail Vertex Color"
{
	Properties
	{
		_Color ("Solid Color", Color) = (1,1,1,1)
		_Invisible ("Invisible Length", Float) = 1.0
		_Width ("Width", Float) = 0.03
	}
	
	SubShader // pc shader
	{
		Tags { "RenderType"="Opaque" "Queue"="Transparent" }
		LOD 100
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile_fog
			#pragma exclude_renderers gles3
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 color : COLOR;
			};

			struct v2g
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 color : COLOR;
			};

			struct g2f
			{
				
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float d : TEXCOORD1;
				float3 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _Color;
			float _Width;
			float _Invisible;
			
			v2g vert (appdata v)
			{
				v2g o;
				o.vertex = v.vertex;
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			[maxvertexcount(10)]
			void geom(triangle v2g IN[3], inout TriangleStream<g2f> stream) {
				g2f o;
				o.color = IN[0].color;

				if(IN[0].uv.x + IN[2].uv.x > IN[1].uv.x * 2) return;
				float3 p = IN[0].vertex.xyz, v = IN[1].vertex.xyz;
				v -= p;
				
				float4 vp1 = UnityObjectToClipPos(float4(p, 1));
				float4 vp2 = UnityObjectToClipPos(float4(p + v, 1));
				float2 vd = vp1.xy / vp1.w - vp2.xy / vp2.w;
				float aspectRatio = - UNITY_MATRIX_P[0][0] / UNITY_MATRIX_P[1][1];
				vd.x /= aspectRatio;
				o.d = length(vd);
				if(length(vd) < 0.0001) vd = float2(1,0);
				else vd = normalize(vd);
				float2 vn = vd.yx * float2(-1,1);

				//if(abs(UNITY_MATRIX_P[0][2]) < 0.01) size *= 2; 
				float sz = _Width;
				vn *= sz;
				vn.x *= aspectRatio;
				
				if(length(v) < _Invisible) {
					o.d = 0;
					o.uv = float2(-1,-1);
					o.vertex = vp1+float4(+vn,0,0);
					stream.Append(o);
					o.uv = float2(-1,1);
					o.vertex = vp1+float4(-vn,0,0);
					stream.Append(o);
					o.uv = float2(1,-1);
					o.vertex = vp2+float4(+vn,0,0);
					stream.Append(o);
					o.uv = float2(1,1);
					o.vertex = vp2+float4(-vn,0,0);
					stream.Append(o);
					stream.RestartStrip();
				}
				
				o.d = 1;
				sz *= 2.0;
				if(IN[1].uv.x >= 0.999999) {
					o.uv = float2(0,1);
					o.vertex = vp2+float4(o.uv*sz*float2(aspectRatio,1),0,0);
					stream.Append(o);
					o.uv = float2(-0.9,-0.5);
					o.vertex = vp2+float4(o.uv*sz*float2(aspectRatio,1),0,0);
					stream.Append(o);
					o.uv = float2(0.9,-0.5);
					o.vertex = vp2+float4(o.uv*sz*float2(aspectRatio,1),0,0);
					stream.Append(o);
					stream.RestartStrip();
				}

				o.uv = float2(0,1);
				o.vertex = vp1+float4(o.uv*sz*float2(aspectRatio,1),0,0);
				stream.Append(o);
				o.uv = float2(-0.9,-0.5);
				o.vertex = vp1+float4(o.uv*sz*float2(aspectRatio,1),0,0);
				stream.Append(o);
				o.uv = float2(0.9,-0.5);
				o.vertex = vp1+float4(o.uv*sz*float2(aspectRatio,1),0,0);
				stream.Append(o);
				stream.RestartStrip();
			}
			
			fixed4 frag (g2f i) : SV_Target
			{
				float4 vertexColor = float4(GammaToLinearSpace(i.color), 1);
				float l = length(i.uv);
				clip(- min(i.d - 0.5, l - 0.5));
				return float4(vertexColor.xyz,1);
			}
			ENDCG
		}
	}
	
	SubShader // quest shader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 vertexColor = float4(GammaToLinearSpace(i.color), 1);
                return vertexColor;
            }
            ENDCG
        }
    }
}
