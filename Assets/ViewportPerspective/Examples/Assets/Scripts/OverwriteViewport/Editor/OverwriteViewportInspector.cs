/*
	Created by Carl Emil Carlsen.
	Copyright 2017 Sixth Sensor.
	All rights reserved.
	http://sixthsensor.dk
*/

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace ProjectorPerspectiveExamples
{
	[CustomEditor(typeof(OverwriteViewport))]
	[CanEditMultipleObjects]
	public class OverwriteViewportInspector : Editor
	{
		SerializedProperty _texture;
		SerializedProperty _uvRect;


		void OnEnable()
		{
			_texture = serializedObject.FindProperty( "_texture" );
			_uvRect = serializedObject.FindProperty( "_uvRect" );
		}


		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField( _texture );

			EditorGUILayout.PropertyField( _uvRect, new GUIContent( "UvRect (texture cropping)" ) );

			serializedObject.ApplyModifiedProperties();
		}
	}
}