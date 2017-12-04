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
	public class Rotate : MonoBehaviour
	{
		[SerializeField] Vector3 _angularSpeed = new Vector3( 20, 5, 0 );


		void Update()
		{
			transform.Rotate( _angularSpeed * Time.deltaTime );
		}

	}
}