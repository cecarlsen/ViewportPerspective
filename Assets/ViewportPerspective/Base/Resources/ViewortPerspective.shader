/*
    Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

Shader "Hidden/ViewportPerspective"
{
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ClearColor ("Clear Color", Color) = (0,0,0,1)
	}


	CGINCLUDE

	#include "UnityCG.cginc"
	#pragma multi_compile SHOW_TEXTURE SHOW_GRID
	#pragma multi_compile __ ANTIALIASING

	sampler2D _MainTex;
	float4x4 _Matrix;
	float2 _GridSize;

	static const float GRID_THICKNESS = 1.2;


	struct vertexInput {
		float4 vertex : POSITION;
		half2 uv : TEXCOORD0;
	};


	struct fragInput {
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};



	float compute_grid( float2 uv )
	{
		// Grid.
		float2 pos = uv * _GridSize;
		float2 offPos = pos + 0.5;
		float2 f  = abs( frac( offPos ) - 0.5 );		// Frac is the fractoinal part, like f = v - floor(v)
		float2 df = fwidth( offPos ) * GRID_THICKNESS;	// Fwidth (fragment width): sum of approximate window-space partial derivatives magnitudes
		float2 g  = smoothstep( -df, df, f );			// Grid

		// Cross.
		float l1 = ( uv.y - uv.x ) / 1.4142135624;
		l1 = saturate( smoothstep( -df, df, abs( l1 * 10 ) ) );
		float l2 = ( uv.x + uv.y - 1 ) / 1.4142135624;
		l2 = saturate( smoothstep( -df, df, abs( l2 * 10 ) ) );  

		// Circle.
		float minSize = _GridSize.x > _GridSize.y ? _GridSize.y : _GridSize.x;
		float2 fromCenter = _GridSize-pos*2;
		float radius = length( fromCenter );
		df *= 2; // Thicker stroke.
		float c = saturate( smoothstep( -df, df, abs(radius - minSize) ) );

		return 1 - saturate( g.x * g.y * c * l1 * l2 );
	}


	fragInput vert( vertexInput v )
	{
		fragInput o;
		o.pos = mul( _Matrix, v.vertex );
		o.uv = v.uv.xy;
		return o;
	}


	fixed4 frag( fragInput i ) : COLOR
	{
		float4 col = tex2D( _MainTex, i.uv );

		#ifdef SHOW_GRID
			float grid = compute_grid( i.uv );
			col = col * 0.5 + grid;
		#endif

		#ifdef ANTIALIASING
			float2 pos = i.uv + 0.5;
			float2 f  = abs( frac( pos ) - 0.5 );
			float2 df = fwidth( pos ) * 0.7;
			float2 e  = smoothstep( -df, df, f );
			col.rgb *= saturate( e.x * e.y );
		#endif

		return col;
	}



	ENDCG


	SubShader
	{
		ZWrite Off
 		Lighting Off
 		ZTest Always
 		
		// Pass 0: Clear background.
		Pass
		{
			Material {
				Ambient[_ClearColor]
			}
			
			SetTexture[_MainTex] {
                constantColor [_ClearColor]
                Combine constant, constant
            }
		}
		
		// Pass 1: Draw transformed quad.
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
	FallBack Off
}