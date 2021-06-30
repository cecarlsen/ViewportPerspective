/*
	Copyright © Carl Emil Carlsen 2018-2021
	http://cec.dk
*/

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ViewportPerspective))]
[CanEditMultipleObjects]
public class ViewportPerspectiveInspector : Editor
{
	ViewportPerspective _vp;

	SerializedProperty _interactableProp;
	SerializedProperty _edgeAntialiasingProp;
	SerializedProperty _backgroundColorProp;
	SerializedProperty overrideSourceTextureProp;
	SerializedProperty _runtimeSerializationProp;
	SerializedProperty _fileNameWithoutExtensionProp;
	SerializedProperty _interactableHotkeyProp;
	SerializedProperty _resetHotkeyProp;
	SerializedProperty _hotkeyFoldProp;


	void OnEnable()
	{
		_vp = target as ViewportPerspective;

		_interactableProp = serializedObject.FindProperty( "_interactable" );
		_edgeAntialiasingProp = serializedObject.FindProperty( "_edgeAntialiasing" );
		_backgroundColorProp = serializedObject.FindProperty( "_backgroundColor" );
		overrideSourceTextureProp = serializedObject.FindProperty( "overrideSourceTexture" );
		_runtimeSerializationProp = serializedObject.FindProperty( "_runtimeSerialization" );
		_fileNameWithoutExtensionProp = serializedObject.FindProperty( "_fileNameWithoutExtension" );
		_interactableHotkeyProp = serializedObject.FindProperty( "_interactableHotkey" );
		_resetHotkeyProp = serializedObject.FindProperty( "_resetHotkey" );
		_hotkeyFoldProp = serializedObject.FindProperty( "_hotkeyFold" );
	}


	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField( _interactableProp );
		EditorGUILayout.PropertyField( _edgeAntialiasingProp );
		EditorGUILayout.PropertyField( _backgroundColorProp );
		EditorGUILayout.PropertyField( overrideSourceTextureProp );

		EditorGUI.BeginDisabledGroup( Application.isPlaying );
		EditorGUILayout.PropertyField( _runtimeSerializationProp );
		if( ( ViewportPerspective.SerializationMethod) _runtimeSerializationProp.enumValueIndex == ViewportPerspective.SerializationMethod.StreamingAssets ) {
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( _fileNameWithoutExtensionProp );
			if( EditorGUI.EndChangeCheck() ) _vp.fileNameWithoutExtension = _fileNameWithoutExtensionProp.stringValue; // Trigger TryLoadRuntimeSettings
		}
		EditorGUI.EndDisabledGroup();

		_hotkeyFoldProp.boolValue = EditorGUILayout.Foldout( _hotkeyFoldProp.boolValue, "Hotkeys" );
		if( _hotkeyFoldProp.boolValue){
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField( _interactableHotkeyProp,  new GUIContent( "Interactable" ) );
			EditorGUILayout.PropertyField( _resetHotkeyProp, new GUIContent( "Reset" ) );
			EditorGUI.indentLevel--;
		}

		serializedObject.ApplyModifiedProperties();
	}
}