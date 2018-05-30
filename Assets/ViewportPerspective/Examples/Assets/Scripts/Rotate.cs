/*
	Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectorPerspectiveExamples
{
	public class Rotate : MonoBehaviour
	{
		[SerializeField] Vector3 _angularSpeed = new Vector3( 20, 5, 0 );


		void Update()
		{
			transform.Rotate( _angularSpeed * Time.deltaTime );
		}

	}
}