using UnityEngine;

namespace DMC {
	public static class Lookups {
		public static int[,] RootTetrahedrons =
		 {	
			{6, 0, 7, 3},
			{0, 6, 4, 7},
			{0, 6, 4, 5}, 
			{6, 0, 5, 1}, 
			{0, 6, 1, 2}, 
			{6, 0, 2, 3}, 
		};

		public static readonly int[,] EdgePairs = {
			{0, 1}, {0, 2}, {0, 3}, {1, 2}, {1, 3}, {2, 3}
		};


		public static readonly int[][] PossibleTetrahedronVertexOrderCombos = {
			new [] {1, 2, 3, -1}, // Tetrahedron A (bottom tetrahedron in figure 4)
			new [] {0, 3, 2, -1}, // Tetrahedron B (top tetrahedron in figure 4)
		};
		
		// see p4 of Simplex and Diamond Hierarchies Models and Applications
		// [cycle number(0-2), tetrahedron number (0-1), vertex number(0-3)]

		public static int[,,] TetrahedronVertexOrder = 
		{	{ {2, 1, 3, -1},  {2, 0, 3, -1} }, 
			{ {1, 2, 3, -1},  {3, 0, 2, -1} }, 
			{ {2, 1, 3, -1},  {0, 2, 3, -1} }, 
		};
		public static readonly Vector3[] StartingVerts = { 
			new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 0), 
			new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };	
	}

}