// 	Created by Carl Emil Carlsen.
//	Copyright 2017 Sixth Sensor.
//	All rights reserved.
//	http://sixthsensor.dk

Shader "Hidden/ViewportPerspectiveUIHandle"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		Blend One One
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			sampler2D _MainTex;


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};


			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};


			v2f vert( appdata v )
			{
				v2f o;
				o.vertex = v.vertex;
				o.vertex.z += 0.1; // Stay within clip space.
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}


			fixed4 frag( v2f i ) : SV_Target
			{
				fixed4 col = tex2D( _MainTex, i.uv );
				return col * i.color;
			}
			ENDCG
		}
	}
}
