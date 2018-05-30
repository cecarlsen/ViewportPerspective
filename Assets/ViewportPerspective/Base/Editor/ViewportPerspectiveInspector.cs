/*
	Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ViewportPerspective))]
[CanEditMultipleObjects]
public class ViewportPerspectiveInspector : Editor
{
	SerializedProperty _interactable;
	SerializedProperty _edgeAntialiasing;
    SerializedProperty _backgroundColor;
    SerializedProperty overrideSourceTexture;
	SerializedProperty _runtimeSerialization;
	SerializedProperty _interactableHotkey;
	SerializedProperty _resetHotkey;

	SerializedProperty _hotkeyFold;


	void OnEnable()
	{
		_interactable = serializedObject.FindProperty( "_interactable" );
		_edgeAntialiasing = serializedObject.FindProperty( "_edgeAntialiasing" );
        _backgroundColor = serializedObject.FindProperty( "_backgroundColor" );
        overrideSourceTexture = serializedObject.FindProperty( "overrideSourceTexture" );
		_runtimeSerialization = serializedObject.FindProperty( "_runtimeSerialization" );
		_interactableHotkey = serializedObject.FindProperty( "_interactableHotkey" );
		_resetHotkey = serializedObject.FindProperty( "_resetHotkey" );
		_hotkeyFold = serializedObject.FindProperty( "_hotkeyFold" );
	}


	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField( _interactable );
		EditorGUILayout.PropertyField( _edgeAntialiasing );
        EditorGUILayout.PropertyField( _backgroundColor );
        EditorGUILayout.PropertyField( overrideSourceTexture );

		EditorGUI.BeginDisabledGroup( Application.isPlaying );
		EditorGUILayout.PropertyField( _runtimeSerialization );
		EditorGUI.EndDisabledGroup();

		_hotkeyFold.boolValue = EditorGUILayout.Foldout( _hotkeyFold.boolValue, "Hotkeys" );
		if( _hotkeyFold.boolValue){
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField( _interactableHotkey,  new GUIContent( "Interactable" ) );
			EditorGUILayout.PropertyField( _resetHotkey, new GUIContent( "Reset" ) );
			EditorGUI.indentLevel--;
		}

		serializedObject.ApplyModifiedProperties();
	}
}