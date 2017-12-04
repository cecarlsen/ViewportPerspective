/*
	Created by Carl Emil Carlsen.
	Copyright 2017 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectorPerspectiveExamples
{ 
	[ExecuteInEditMode]
	public class OverwriteViewport : MonoBehaviour
	{
		[SerializeField] Texture _texture;
		[SerializeField] Rect _uvRect = new Rect(0,0,1,1);

		[SerializeField][HideInInspector] Shader _blitShader;

		Material _blitMatetrial;


		public Rect uvRect {
			get { return _uvRect; }
			set {
				_uvRect = value;
				if( !_blitMatetrial ) CreateMaterial();
				_blitMatetrial.SetVector( "_UvRect", new Vector4( _uvRect.x, _uvRect.y, _uvRect.width, _uvRect.height ) );
			}
		}


		void OnRenderImage( RenderTexture source, RenderTexture dest )
		{
			if( !_texture ) return;

			if( !_blitMatetrial ) CreateMaterial();
				
			Graphics.Blit( _texture, dest, _blitMatetrial, 0 );
		}


		void OnValidate()
		{
			uvRect = _uvRect;
		}


		void CreateMaterial()
		{
			_blitMatetrial = new Material( _blitShader );
			_blitMatetrial.hideFlags = HideFlags.HideAndDontSave;
		}
	}
}