/*
	Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ProjectorPerspectiveExamples
{
	public class MultiDisplaySetup : MonoBehaviour
	{
		[SerializeField] int _expectedDisplayCount = 2;
		[SerializeField] StringEvent _onInfoEvent = new StringEvent();

		[System.Serializable] public class StringEvent : UnityEvent<string>{}


		void Start()
		{
			System.Text.StringBuilder infoText = new System.Text.StringBuilder();

			int displayCount = Display.displays.Length;
			infoText.AppendLine( "Total display count: " + displayCount );

			for( int d = 0; d < displayCount; d++ )
			{
				Display display = Display.displays[d];

				bool activate = d < _expectedDisplayCount;
				if( activate ) display.Activate( display.systemWidth, display.systemHeight, 60 );

				infoText.AppendLine( "\tDisplay #" + (d+1) + ( activate ? " (ACTIVE)" : "" ) + ( display == Display.main ? " (main)" : "" ) );
				infoText.AppendLine( "\t\tRender: " + display.renderingWidth + "x" + display.renderingHeight + ", System: " + display.systemWidth + "x" + display.systemHeight );
			}

			_onInfoEvent.Invoke( infoText.ToString() );
		}
	}
}