using UnityEngine;

namespace DMC {
	public static class Lookups {
		public static int[,] RootTetrahedrons =
		 /*{	
			{6, 0, 7, 3},
			{0, 6, 4, 7},
			{0, 6, 4, 5}, 
			{6, 0, 5, 1}, 
			{0, 6, 1, 2}, 
			{6, 0, 2, 3}, 
		};*/
		{	
			{6, 0, 7, 3},
 			{0, 6, 4, 7},
 			{0, 6, 4, 5}, 
 			{6, 0, 5, 1}, 
 			{0, 6, 1, 2}, 
 			{6, 0, 2, 3}, 
		};
		public static readonly int[,] TetrahedronFaces = {
			{1, 0, 2}, {1, 2, 3}, {2, 0, 3}, {0, 1, 3}
		};

		public static readonly int[,] FaceCombinations = {
			{0, 1}, {1, 2}, {0, 2}	
		};

		public static readonly int[,] EdgePairs = {
			{0, 1}, {0, 2}, {0, 3}, {1, 2}, {1, 3}, {2, 3}
		};

		public static readonly int[,] HexahedronFaces = {
			{0, 1, 2, 3}, {0, 1, 5, 4}, {1, 5, 6, 2}, 
			{2, 6, 7, 3}, {0, 4, 7, 3}, {4, 5, 6, 7}
		};

		public static readonly int[,] HexahedronEdgePairs = {
			{0, 1}, {1, 2}, {2, 3}, {3, 0}, 
			{0, 4}, {1, 5}, {2, 6}, {3, 7}, 
			{4, 5}, {5,6}, {6, 7}, {7, 4}
		};
		public static readonly int[,] HexahedronSubHexahedra = {
			// bottom 4
			{19, 7, 1, 10, 11, 2, 0, 5},
			{7, 20, 8, 1, 2, 12, 3, 0},
			{10, 1, 9, 22, 5, 0, 4, 14},
			{1, 8, 21, 9, 0, 3, 13, 4},

			// top 4
			{11, 2, 0, 5, 23, 15, 6, 18},
			{2, 12, 3, 0, 15, 24, 16, 6},
			{5, 0, 4, 14, 18, 6, 17, 26},
			{0, 3, 13, 4, 6, 16, 25, 17},
		};

		public static readonly int[][] PossibleTetrahedronVertexOrderCombos = {
			new [] {1, 2, 3, -1}, // Tetrahedron A (bottom tetrahedron in figure 4)
			new [] {0, 3, 2, -1}, // Tetrahedron B (top tetrahedron in figure 4)
		};
		
		// see p4 of Simplex and Diamond Hierarchies Models and Applications
		// [cycle number(0-2), tetrahedron number (0-1), vertex number(0-3)]

		/*public static int[,,] TetrahedronVertexOrder = 
		{
			{ {2, 1, 3, 4},  {3, 0, 2, 4} }, 
			{ {2, 1, 3, 4},  {0, 2, 3, 4} }, 
			{ {2, 1, 3, 4},  {2, 0, 3, 4} }, 
		};*/
		public static int[,,] TetrahedronVertexOrder = 
		{ // 4 signifies longest edge midpoint
			{ {2, 1, 3, 4},  {0, 3, 2, 4} }, 
			{ {2, 1, 3, 4},  {0, 2, 3, 4} }, 
			{ {2, 1, 3, 4},  {2, 0, 3, 4} }, 
		};

		public static readonly Vector3[] StartingVerts = { 
			new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 0), 
			new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };	

		public static int[] HexahedronVertexReorder = {
			3, 5, 2, 0, 6, 7, 4, 1
		};
	}

}