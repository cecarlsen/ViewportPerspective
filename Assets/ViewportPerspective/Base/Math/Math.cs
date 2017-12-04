/*
 	Original code released in 2010 by Arturo Castro with GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007.
 	https://github.com/camilosw/ofxVideoMapping/blob/master/src/homography.h
	https://github.com/camilosw/ofxVideoMapping/blob/master/LICENSE

	Ported to Unity by Brian Chasalow in 2011.
	https://github.com/brianchasalow

	Included in ViewportPerspective by Carl Emil Carlsen, 01/03/2017.
	http://sixthsensor.dk
*/

using System.Collections;
using UnityEngine;

namespace ViewportPerspectiveTools
{
	public static class Math
	{
		public static void FindHomography( Vector2[] src, Vector2[] dst, ref Matrix4x4 transformMatrix )
		{
			// Create the equation system to be solved
			// from: Multiple View Geometry in Computer Vision 2ed
			//       Hartley R. and Zisserman A.
			//
			// x' = xH
			// Where H is the homography: a 3 by 3 matrix that transformed to inhomogeneous 
			// coordinates for each point gives the following equations for each point:
			//
			// x' * (h31*x + h32*y + h33) = h11*x + h12*y + h13
			// y' * (h31*x + h32*y + h33) = h21*x + h22*y + h23
			//
			// As the homography is scale independent we can let h33 be 1 (indeed any of the terms)
			// so for 4 points we have 8 equations for 8 terms to solve: h11 - h32
			// after ordering the terms it gives the following matrix
			// that can be solved with gaussian elimination:

			float[] P = new float[72]{
				-src[0].x, 	-src[0].y, -1,  0,  0, 0,	src[0].x*dst[0].x, src[0].y*dst[0].x, -dst[0].x,	// h11
				0,   0,  0, -src[0].x, -src[0].y, -1, 	src[0].x*dst[0].y, src[0].y*dst[0].y, -dst[0].y,	// h12
				-src[1].x,  -src[1].y, -1,  0,  0,  0,	src[1].x*dst[1].x, src[1].y*dst[1].x, -dst[1].x,	// h13
				0,   0,  0, -src[1].x, -src[1].y, -1, 	src[1].x*dst[1].y, src[1].y*dst[1].y, -dst[1].y,	// h21
				-src[2].x,  -src[2].y, -1,  0,  0, 0,	src[2].x*dst[2].x, src[2].y*dst[2].x, -dst[2].x,	// h22
				0,   0,  0, -src[2].x, -src[2].y, -1, 	src[2].x*dst[2].y, src[2].y*dst[2].y, -dst[2].y,	// h23
				-src[3].x,  -src[3].y, -1,  0,  0,  0,	src[3].x*dst[3].x, src[3].y*dst[3].x, -dst[3].x,	// h31
				0,   0,  0, -src[3].x, -src[3].y, -1, 	src[3].x*dst[3].y, src[3].y*dst[3].y, -dst[3].y		// h32

			};

			float[] Pbefore = new float[72];
			for(int i = 0; i < Pbefore.Length; i++) Pbefore[i] = P[i];

			P = GaussianElimination(P,9);

			transformMatrix[0,0] = P[8];
			transformMatrix[0,1] = P[17];
			transformMatrix[0,2] = 0;
			transformMatrix[0,3] = P[26];
								   
			transformMatrix[1,0] = P[35];
			transformMatrix[1,1] = P[44];
			transformMatrix[1,2] = 0;
			transformMatrix[1,3] = P[53];
								   
			transformMatrix[2,0] = 0;
			transformMatrix[2,1] = 0;
			transformMatrix[2,2] = 0;
			transformMatrix[2,3] = 0;
								   
			transformMatrix[3,0] = P[62];
			transformMatrix[3,1] = P[71];
			transformMatrix[3,2] = 0;
			transformMatrix[3,3] = 1;
		}


		public static float[] GaussianElimination( float[] A, int n )
		{
			// http://en.wikipedia.org/wiki/Gaussian_elimination

			int i = 0;
			int j = 0;
			int m = n-1;

			while( i < m && j < n ){
				// Find pivot in column j, starting in row i:
				int maxi = i;
				for(int k = i+1; k<m; k++) if( Mathf.Abs(A[k*n+j]) > Mathf.Abs(A[maxi*n+j]) ) maxi = k;

				if( A[maxi*n+j] != 0 ){
					// Swap rows i and maxi, but do not change the value of i
					if( i != maxi )
						for( int k=0; k<n; k++ ){
							float aux = A[i*n+k];
							A[i*n+k] = A[maxi*n+k];
							A[maxi*n+k] = aux;
						}

					// Now A[i,j] will contain the old value of A[maxi,j].
					// Divide each entry in row i by A[i,j]
					float A_ij=A[i*n+j];
					for(int k=0;k<n;k++) A[i*n+k]/=A_ij;

					// Now A[i,j] will have the value 1.
					for(int u = i+1; u<m; u++){
						// Subtract A[u,j] * row i from row u
						float A_uj = A[u*n+j];
						for( int k=0; k<n; k++ ) A[u*n+k] -= A_uj * A[i*n+k];
						// Now A[u,j] will be 0, since A[u,j] - A[i,j] * A[u,j] = A[u,j] - 1 * A[u,j] = 0.
					}
					i++;
				}
				j++;
			}

			// Back substitution.
			for( i=m-2; i>=0; i-- ) for( j=i+1; j<n-1; j++ ) A[i*n+m] -= A[i*n+j] * A[j*n+m];
			return A;
		}
	}
}