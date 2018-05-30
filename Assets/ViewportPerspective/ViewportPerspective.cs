/*
	Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Image effect for adjusting the viewport perspective using four corner handles.
/// </summary>
[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("Image Effects/ViewportPerspective")]
[RequireComponent( typeof(Camera) )]
public class ViewportPerspective : MonoBehaviour
{
	[SerializeField] bool _interactable = false;
	[SerializeField] bool _edgeAntialiasing = true;
    [SerializeField] Color _backgroundColor = Color.black;
	[SerializeField] SerializationMethod _runtimeSerialization = SerializationMethod.PlayerPrefs;
	[SerializeField] KeyCode _interactableHotkey = KeyCode.P;
	[SerializeField] KeyCode _resetHotkey = KeyCode.Backspace;
    public Texture overrideSourceTexture;

	[SerializeField][HideInInspector] string _saveKey;
	[SerializeField][HideInInspector] Matrix4x4 _matrix;
	[SerializeField][HideInInspector] Material _blitMaterial;
	[SerializeField][HideInInspector] Vector2[] _cornerPoints = new Vector2[4];	// Clip space (0,0) t0 (1,1)

	[SerializeField][HideInInspector] bool _hotkeyFold;

	bool _isDirty = true;
	bool _preparedForRuntime = false;
	static Texture2D _cornerTexture;
	 
	Camera _cam;
	
	int _selectedIndex = -1;
	int _hoveredIndex = -1;
	Vector3 _multiDisplayOffset;

	KeyValuePair<ViewportPerspective,Camera>[] _otherViewsLookups; // Other ViewportPerspective scripts rendering to the same display.

	CommandBuffer _uiRenderCommands;
	Mesh _handleMesh;
	Vector3[] _handleVertices = new Vector3[4*4];
	Color32[] _handleColors = new Color32[4*4];
	Vector2[] _quadOffset = new Vector2[]{ new Vector2(-0.5f,-0.5f), new Vector2(-0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,-0.5f) };
	Mesh _uiLineMesh;
	Vector3[] _uiLineVertices = new Vector3[4+4]; // Cross hair plus screen edges.

    int _matrixPropId;
    int _gridSizePropId;
    int _backgroundColorPropId;

	static readonly Vector2[] _sourcePoints = new []{
		new Vector2( 0, 0 ),
		new Vector2( 0, 1 ),
		new Vector2( 1, 1 ),
		new Vector2( 1, 0 )
	};

	const string saveKeyBase = "ViewportPerspective";
	const string logPrepend = "<b>[ViewportPerspective]</b> ";
	const string shaderMissingMessage = "Shader is missing. Ensure that shaders are located in 'ViewportPerspective/Base/Resources'.";
	const string textureMissingMessage = "Texture is missing. Ensure that texture exists in 'ViewportPerspective/Base/Resources'.";

	const float aspect_16_9 = 16/9f;
	const float aspect_9_16 = 9/16f;
	const float aspect_16_10 = 16/10f;
	const float aspect_10_16 = 10/16f;
	const float aspect_4_3 = 4/3f;
	const float aspect_3_4 = 3/4f;
	const float aspect_5_4 = 5/4f;
	const float aspect_4_5 = 4/5f;
	const float aspect_3_2 = 3/2f;
	const float aspect_2_3 = 2/3f;
	const float aspect_2_1 = 2/1f;
	const float aspect_1_2 = 1/2f;

	const float handleSizeMult = 0.08f;

	string blitShaderName = "Hidden/ViewportPerspective";
	string uiHandleShaderName = "Hidden/ViewportPerspectiveUIHandle";
	string uiLineShaderName = "Hidden/ViewportPerspectiveUILine";
	string handleTextureName = "ViewportPerspectiveHandle";

	static readonly Color32 idleColor = Color.white;
	static readonly Color32 hoverColor = Color.white;
	static readonly Color32 activeColor = Color.magenta;

	[Serializable] public enum SerializationMethod { PlayerPrefs, StreamingAssets, None };

	/// <summary>
	/// Toggle edit mode.
	/// </summary>
	public bool interactable {
		get { return _interactable; }
		set {
			_interactable = value;

			if( !Application.isPlaying ) return;
			if( !_preparedForRuntime ) PrepareForRuntime();

			if( _interactable ) {
				_blitMaterial.EnableKeyword( "SHOW_GRID" );
				_cam.AddCommandBuffer( CameraEvent.AfterImageEffects, _uiRenderCommands );
				UpdateOtherViewsLookup();
			} else {
				_selectedIndex = -1;
				_blitMaterial.DisableKeyword( "SHOW_GRID" );
				_cam.RemoveCommandBuffer( CameraEvent.AfterImageEffects, _uiRenderCommands );
			}
		}
	}

	/// <summary>
	/// Toggle use of edge antialiasing.
	/// </summary>
	public bool edgeAntialiasing {
		get { return _edgeAntialiasing; }
		set {
			_edgeAntialiasing = value;

			if( !_blitMaterial && !TryCreateBlitMaterial() ) return;

			if( _edgeAntialiasing ) _blitMaterial.EnableKeyword( "ANTIALIASING" );
			else _blitMaterial.DisableKeyword( "ANTIALIASING" );
		}
	}

    /// <summary>
	/// Background color.
	/// </summary>
	public Color backgroundColor {
		get { return _backgroundColor; }
		set {
			_backgroundColor = value;
			if( !_blitMaterial && !TryCreateBlitMaterial() ) return;
			_blitMaterial.SetColor( _backgroundColorPropId, _backgroundColor );
		}
	}
    

	/// <summary>
	/// Resets the perspective.
	/// </summary>
	public void Reset()
	{
		_cornerPoints[0] = new Vector2( -1, -1 );
		_cornerPoints[1] = new Vector2( -1, 1 );
		_cornerPoints[2] = new Vector2( 1, 1 );
		_cornerPoints[3] = new Vector2( 1, -1 ); 
		_isDirty = true;
	}


	/// <summary>
	/// Gets a corner at index in Unity viewport space; lower-left (0,0) upper-right (1,1). Index order: lower-left, upper-left, upper-right, lower-right.
	/// </summary>
	public Vector2 GetCorner( int index )
	{
		if( index < 0 || index > 3 ) return Vector2.zero;
		return _cornerPoints[index] * 0.5f + new Vector2(0.5f,0.5f); // Clip space to viewport space.
	}


	/// <summary>
	/// Sets a corner at index in Unity viewport space; lower-left (0,0) upper-right (1,1). Index order: lower-left, upper-left, upper-right, lower-right.
	/// </summary>
	public void SetCorner( int index, Vector2 viewportPosition )
	{
		if( index < 0 || index > 3 ) return;
		_cornerPoints[index] = viewportPosition * 2 - Vector2.one; // Viewport space to clip space.
		_isDirty = true;
	}


	string streamingAssetPath { get { return Application.streamingAssetsPath + "/" + this.GetType().Name + "/" + name + ".dat"; } }


	void Awake()
	{
		// Ensure that we have a save key.
		if( string.IsNullOrEmpty( _saveKey ) ) _saveKey = GetUniqueSaveKey();

		// Load settings or reset.
		if( !TryLoadRuntimeSettings() ) Reset();
			
		// Prepare for runtime.
		if( Application.isPlaying && !_preparedForRuntime ) PrepareForRuntime();

        // Get shader propery IDs.
        _matrixPropId = Shader.PropertyToID( "_Matrix" );
        _gridSizePropId = Shader.PropertyToID( "_GridSize" );
        _backgroundColorPropId = Shader.PropertyToID( "_ClearColor" );
	}


	void Update()
	{
		// Only update in runtime.
		if( !Application.isPlaying ) return;

		// Toggle interaction mode.
		if( Input.GetKeyUp( _interactableHotkey ) && !Input.GetKey( KeyCode.LeftCommand ) && !Input.GetKey( KeyCode.RightCommand ) ){
			interactable = !_interactable;
		}

		// Update interaction.
		if( _interactable ){
			if( Input.GetKeyDown( _resetHotkey ) ) Reset();
			if( Input.GetKeyDown( KeyCode.Tab ) ){
				if( _selectedIndex != -1 ){
					_selectedIndex += Input.GetKey( KeyCode.LeftShift ) ? -1 : 1;
					_selectedIndex = (int) Mathf.Repeat( _selectedIndex, 4 );
				} else if( _hoveredIndex != -1 ){
					_selectedIndex = _hoveredIndex;
				}
			}
			UpdateInteraction();
			UpdateUIMeshes();
		}
	}

	 
	void OnRenderImage( RenderTexture source, RenderTexture dest )
	{
		if( !_blitMaterial && !TryCreateBlitMaterial() ) return;

		if( !_preparedForRuntime ) PrepareForRuntime();

		if( _isDirty || ( Application.isEditor && !_blitMaterial.HasProperty(_matrixPropId) ) ){
			ViewportPerspectiveTools.Math.FindHomography( _sourcePoints, _cornerPoints, ref _matrix );
			_blitMaterial.SetMatrix( _matrixPropId, _matrix );
			_isDirty = false;
		}

		if( Application.isPlaying && _interactable ){
			int tileCountX, tileCountY;
			if( !GetAspectComponents( _cam.aspect, out tileCountX, out tileCountY ) ) tileCountX = tileCountY = 10;
			_blitMaterial.SetVector( _gridSizePropId, new Vector2( tileCountX, tileCountY ) );
		}

		Graphics.Blit( null, dest, _blitMaterial, 0 );		// Clear background.
		Graphics.Blit( overrideSourceTexture ? overrideSourceTexture : source, dest, _blitMaterial, 1 );	// Render with perspective.
	}
	 
	 
	void OnApplicationQuit()
	{
		SaveRuntimeSettings();
	}


	void OnValidate()
	{ 
		interactable = _interactable;
		edgeAntialiasing = _edgeAntialiasing;
        backgroundColor = _backgroundColor;
	}


	bool TryLoadRuntimeSettings()
	{
		switch( _runtimeSerialization ){
		case SerializationMethod.None: return false;
		case SerializationMethod.PlayerPrefs:
			if( !PlayerPrefs.HasKey( _saveKey ) ) return false;
			string[] values = PlayerPrefs.GetString( _saveKey ).Split( ' ' );
			if( values.Length == 8 ) for( int c=0; c<4; c++ ) _cornerPoints[c] = new Vector2( float.Parse( values[c*2] ), float.Parse( values[c*2+1] ) );
			return true;
		case SerializationMethod.StreamingAssets:
			Data data = Data.Deserialize( streamingAssetPath );
			if( data == null || data.corners.Length == 0 ) return false;
			_cornerPoints = data.corners;
			return true;
		}
		return false;
	}


	void SaveRuntimeSettings()
	{
		switch( _runtimeSerialization ){
		case SerializationMethod.PlayerPrefs:
			if( string.IsNullOrEmpty( _saveKey ) ) _saveKey = GetUniqueSaveKey();
			string values = "";
			for( int c=0; c<4; c++ ) values += ( c==0 ? "" : " " ) + _cornerPoints[c].x + " " + _cornerPoints[c].y;
			PlayerPrefs.SetString( _saveKey, values );
			break;
		case SerializationMethod.StreamingAssets:
			Data data = new Data();
			data.corners = _cornerPoints;
			data.Serialize( streamingAssetPath );
			break;
		}
	}



	bool TryCreateBlitMaterial()
	{
		Shader blitShdaer = Shader.Find( blitShaderName );
		if( !blitShdaer ){
			Debug.LogWarning( logPrepend + shaderMissingMessage + "\n" );
			return false;
		}
		_blitMaterial = new Material( blitShdaer );
		_blitMaterial.hideFlags = HideFlags.HideAndDontSave;
		_isDirty = true;
		return true;
	}


	void PrepareForRuntime()
	{
		if( !Application.isPlaying ) return;

		if( !_blitMaterial && !TryCreateBlitMaterial() ) return;

		Input.simulateMouseWithTouches = true;

		if( !_cam ) _cam = GetComponent<Camera>();

		Shader uiHandleShader = Shader.Find( uiHandleShaderName );
		Shader uiLineShader = Shader.Find( uiLineShaderName );
		if( !uiHandleShader || !uiLineShader ){
			Debug.LogWarning( logPrepend + shaderMissingMessage + "\n" );
			return;
		}
		Material handleMat = new Material( uiHandleShader );
		Material uiLineMat = new Material( uiLineShader );

		if( !_cornerTexture ){
			_cornerTexture = Resources.Load<Texture2D>( handleTextureName );
			if( !_cornerTexture ){
				Debug.LogWarning( logPrepend + textureMissingMessage + "\n" );
			}
		}
		handleMat.mainTexture = _cornerTexture;

		int[] handleTriLookup = new int[]{ 0, 1, 2, 2, 3, 0 };
		Vector2[] handleUVLookup = new Vector2[]{ new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
		int[] tris = new int[4*6];
		Vector2[] uv = new Vector2[4*4];
		int i = 0;
		for( int c = 0; c < 4; c++ ) for( int t = 0; t < 6; t++ ) tris[i++] = (c*4) + handleTriLookup[t];
		i = 0;
		for( int c = 0; c < 4; c++ ) for( int q = 0; q < 4; q++ ) uv[i++] = handleUVLookup[q];
		_handleMesh = new Mesh();
		_handleMesh.MarkDynamic();
		_handleMesh.vertices = _handleVertices;
		_handleMesh.triangles = tris;
		_handleMesh.uv = uv;

		int[] uiLineIndices = new int[]{ 0, 1, 2, 3, 4, 5, 5, 6, 6, 7, 7, 4 };
		Color32[] uiLineColors = new Color32[]{ Color.grey, Color.grey, Color.grey, Color.grey, Color.white, Color.white, Color.white, Color.white };
		_uiLineMesh = new Mesh();
		_uiLineMesh.MarkDynamic();
		_uiLineMesh.vertices = _uiLineVertices;
		_uiLineMesh.SetIndices( uiLineIndices, MeshTopology.Lines, 0 );
		_uiLineMesh.colors32 = uiLineColors;

		_uiRenderCommands = new CommandBuffer();
		_uiRenderCommands.name = "ViewportPerspective UI";
		_uiRenderCommands.DrawMesh( _handleMesh, Matrix4x4.identity, handleMat );
		_uiRenderCommands.DrawMesh( _uiLineMesh, Matrix4x4.identity, uiLineMat );

		_preparedForRuntime = true;
	}


	void UpdateOtherViewsLookup()
	{
		ViewportPerspective[] allViews = FindObjectsOfType<ViewportPerspective>();
		List<KeyValuePair<ViewportPerspective,Camera>> list = new List<KeyValuePair<ViewportPerspective,Camera>>();
		foreach( ViewportPerspective view in allViews ){
			if( view == this ) continue;
			Camera viewCam = view.GetComponent<Camera>();
			if( viewCam && viewCam.targetDisplay == _cam.targetDisplay ) list.Add( new KeyValuePair<ViewportPerspective,Camera>( view, viewCam ) );
		}
		_otherViewsLookups = list.ToArray();
	}



	string GetUniqueSaveKey()
	{
		return saveKeyBase + " " + System.Guid.NewGuid().ToString();
	}


	void UpdateUIMeshes()
	{
		Vector2 quadScale = new Vector2( 1/_cam.aspect, 1 ) * handleSizeMult;
		int v = 0;
		for( int c = 0; c < 4; c++ ){
			Color32 color = c == _selectedIndex ? activeColor : ( c == _hoveredIndex ? hoverColor : idleColor );
			for( int q = 0; q < 4; q++ ){
				Vector2 scaledQuad = _quadOffset[q];
				scaledQuad.Scale( quadScale );
				_handleVertices[v] = (Vector3) ( _cornerPoints[c] + scaledQuad );
				_handleColors[v] = color;
				v++;
			}
		}
		_handleMesh.vertices = _handleVertices;
		_handleMesh.colors32 = _handleColors;

		Vector3 mousePos = Input.mousePosition - _multiDisplayOffset;
		mousePos = 2 * ( (Vector2) _cam.ScreenToViewportPoint( mousePos ) ) - Vector2.one;
		float pixWidth = 2 / (float) _cam.pixelWidth;
		float pixHeight = 2 / (float) _cam.pixelHeight;
		if( _hoveredIndex != -1 ) { // Only render cross hair if hovering this display.
			_uiLineVertices[0].Set( -1, mousePos.y, 0 );
			_uiLineVertices[1].Set( 1, mousePos.y, 0 );
			_uiLineVertices[2].Set( mousePos.x, -1, 0 );
			_uiLineVertices[3].Set( mousePos.x, 1, 0 );
		} else {
			_uiLineVertices[0].Set(0,0,0);
			_uiLineVertices[1].Set(0,0,0);
			_uiLineVertices[2].Set(0,0,0);
			_uiLineVertices[3].Set(0,0,0);
		}
		_uiLineVertices[4].Set( -1+pixWidth, -1, 0 );
		_uiLineVertices[5].Set( -1+pixWidth, 1-pixHeight, 0 );
		_uiLineVertices[6].Set( 1, 1-pixHeight, 0 );
		_uiLineVertices[7].Set( 1, -1, 0 );
		_uiLineMesh.vertices = _uiLineVertices;
	}


	void UpdateInteraction()
	{
		_hoveredIndex = HitTest();
			
		// Check for drag begin.
		if( Input.GetMouseButtonDown( 0 ) ) _selectedIndex = _hoveredIndex;

		// Update selected.
		if( _selectedIndex != -1 )
		{
			bool mousePressed = Input.GetMouseButton( 0 );
			bool arrowPressed = Input.GetKey( KeyCode.DownArrow ) || Input.GetKey( KeyCode.UpArrow ) || Input.GetKey( KeyCode.LeftArrow ) || Input.GetKey( KeyCode.RightArrow );
			if( mousePressed || arrowPressed )
			{
				if( mousePressed ) {
					// Drag.
					Vector3 mousePos = Input.mousePosition - _multiDisplayOffset;
					_cornerPoints[ _selectedIndex ] = 2 * ( (Vector2) _cam.ScreenToViewportPoint( mousePos ) ) - Vector2.one;
				} else {
					// Translate.
					Vector2 delta = new Vector2( Input.GetAxisRaw( "Horizontal" ), Input.GetAxisRaw( "Vertical" ) * _cam.aspect ) * 0.1f;
					if( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) ) delta *= 10;
					else if(  Input.GetKey( KeyCode.LeftControl ) || Input.GetKey( KeyCode.RightControl ) ) delta *= 0.2f;
					_cornerPoints[ _selectedIndex ] += delta * Time.deltaTime;
				}

				_isDirty = true;
			}
		}
	}


	int HitTest()
	{
		if( !_cam ) _cam = GetComponent<Camera>();

		// Handle multidisplay.
		Vector3 mousePos = Input.mousePosition;
		if( !Application.isEditor && Display.displays.Length > 1 ) {
			Vector3 relMousePos = Display.RelativeMouseAt( mousePos ); // Unity 5.6 Windows only! https://docs.unity3d.com/ScriptReference/Display.RelativeMouseAt.html
			int hoveredDisplay = (int) relMousePos.z;
			if( hoveredDisplay == _cam.targetDisplay ) {
				_multiDisplayOffset = mousePos - relMousePos;
				_multiDisplayOffset.z = 0;
			} else {
				// We hit another display.
				return -1;
			}
		}

		// Check hit.
		int cornerIndex = -1; 
		mousePos -= _multiDisplayOffset;
		Vector2 hit = 2 * ( (Vector2) _cam.ScreenToViewportPoint( mousePos ) ) - Vector2.one;
		float minDist = 999999;
		for( int c = 0; c < _cornerPoints.Length; c++ ) {
			float dist = Vector2.SqrMagnitude( _cornerPoints[ c ] - hit );
			if( dist < minDist ) {
				minDist = dist;
				cornerIndex = c;
			}
		}

		// Handle multiple view on same display.
		foreach( KeyValuePair<ViewportPerspective,Camera> pair in _otherViewsLookups ){
			hit = (Vector2) pair.Value.ScreenToViewportPoint( mousePos );
			if( !new Rect(0,0,1,1).Contains( hit ) ) continue;
			for( int c = 0; c < _cornerPoints.Length; c++ ){
				Vector2 corner = pair.Key.GetCorner( c );
				float dist = Vector2.SqrMagnitude( corner - hit );
				if( dist < minDist ){
					// We hit another ViewportPerspective in same display.
					return -1;
				}
			}
		}

		return cornerIndex;
	}


	static bool GetAspectComponents( float aspect, out int x, out int y )
	{
		if( Almost( aspect, 1 ) )					{ x = 10; y = 10; }
		else if( Almost( aspect, aspect_16_9 ) )	{ x = 16; y = 9; }
		else if( Almost( aspect, aspect_9_16 ) )	{ x = 9; y = 16; }
		else if( Almost( aspect, aspect_16_10 ) )	{ x = 16; y = 10; }
		else if( Almost( aspect, aspect_10_16 ) )	{ x = 10; y = 16; }
		else if( Almost( aspect, aspect_4_3 ) )		{ x = 8; y = 6; }
		else if( Almost( aspect, aspect_3_4 ) )		{ x = 6; y = 8; }
		else if( Almost( aspect, aspect_5_4 ) )		{ x = 10; y = 8; }
		else if( Almost( aspect, aspect_4_5 ) )		{ x = 8; y = 10; }
		else if( Almost( aspect, aspect_3_2 ) )		{ x = 12; y = 8; }
		else if( Almost( aspect, aspect_2_3 ) )		{ x = 8; y = 12; }
		else if( Almost( aspect, aspect_2_1 ) )		{ x = 10; y = 5; }
		else if( Almost( aspect, aspect_1_2 ) )		{ x = 5; y = 10; }
		else 										{ x = 0; y = 0; return false; }
		return true;
	}


	// A more generous version of Mathf.Approximately.
	static bool Almost( float a, float b )
	{
		return a > b - 0.002f && a < b + 0.002f;
	}


	[System.Serializable]
	public class Data
	{
		[NonSerialized] public Vector2[] corners = new Vector2[0];

		float[] _corners = new float[0]; // Vector3 is not serializable.


		public void Serialize( string filePath )
		{
			// Ensure we have a directory.
			string dataDirectoryPath = filePath.Substring( 0, filePath.LastIndexOf( '/' ) );
			if( !System.IO.Directory.Exists( dataDirectoryPath ) ) System.IO.Directory.CreateDirectory( dataDirectoryPath );

			// Convert non-serializable.
			_corners = new float[corners.Length*2];
			for( int c = 0; c < corners.Length; c++ ) for( int cc = 0; cc < 2; cc++ ) _corners[c*2+cc] = corners[c][cc];

			// Serialize.
			using( Stream fileStream = new FileStream( filePath, FileMode.Create, FileAccess.Write, FileShare.None ) ){
				IFormatter formatter = new BinaryFormatter();
				formatter.Serialize( fileStream, this );
			}
		}


		public static Data Deserialize( string filePath )
		{
			// Check if file exists.
			if( !File.Exists( filePath ) ) return null;

			// Load data.
			Data d;
			try {
				using( Stream fileStream = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read ) ){
					IFormatter formatter = new BinaryFormatter();
					d = (Data) formatter.Deserialize( fileStream );
				}
			} catch( IOException e ){
				Debug.LogWarning( e );
				d = new Data();
			}

			// Convert non-serializable.
			d.corners = new Vector2[d._corners.Length/2];
			for( int c = 0; c < d.corners.Length; c++ ) d.corners[c] = new Vector2( d._corners[c*2], d._corners[c*2+1] );

			// Done.
			return d;
		}
	}
}