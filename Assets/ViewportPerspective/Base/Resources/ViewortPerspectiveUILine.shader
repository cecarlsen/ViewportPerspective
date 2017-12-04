// 	Created by Carl Emil Carlsen.
//	Copyright 2017 Sixth Sensor.
//	All rights reserved.
//	http://sixthsensor.dk

Shader "Hidden/ViewportPerspectiveUILine"
{
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		Blend One One
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
			};


			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
			};


			v2f vert( appdata v )
			{
				v2f o;
				o.vertex = v.vertex;
				o.vertex.z += 0.1; // Stay within clip space.
				o.color = v.color;
				return o;
			}


			fixed4 frag( v2f i ) : SV_Target
			{
				return i.color;
			}

			ENDCG
		}
	}
}