// 	Created by Carl Emil Carlsen.
//	Copyright 2017 Sixth Sensor.
//	All rights reserved.
//	http://sixthsensor.dk

Shader "Hidden/OverwriteViewport"
{
	Properties {
		_MainTex ("Texture", 2D) = "clear" {}
		_UvRect ("UV Rect", Vector) = (0,0,1,1)
	}


	CGINCLUDE

	#include "UnityCG.cginc"

	uniform sampler2D _MainTex;
	uniform float4 _UvRect;


	fixed4 frag( v2f_img i ) : SV_TARGET
	{
		i.uv.xy *= _UvRect.zw;
		i.uv.xy += _UvRect.xy;
		return tex2D( _MainTex, i.uv );
	}


	ENDCG


	SubShader
	{
		ZWrite Off
 		Lighting Off
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			ENDCG
		}
	}
	FallBack Off
}