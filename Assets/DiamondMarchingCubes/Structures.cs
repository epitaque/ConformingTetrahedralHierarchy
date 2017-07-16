using System.Collections.Generic;
using UnityEngine;

namespace DMC {
	public class Node {
		public Node[] Children;
		public Vector3[] HierarchyVertices;
		public Vector3[] Vertices;
		public Vector3 Center;
		public Vector3 CentralVertex; // midpoint of longest edge
		public int TetrahedronType;
		public int Depth; // 0 indicates top-level tetrahedron
		public uint Number; // used as UID system
		public bool IsLeaf;
		public bool ReverseWindingOrder;
	}

	public class Root {
		public Node[] Children;
		public Dictionary<Vector3, List<Node>> EdgeToTetList; // index: Central Vertex Position // value:  diamond
	}

	public class Hexahedron {
		public Vector3[] Vertices; // 8
		public Face[] BaseFaces;
		public Hexahedron[] Children;
		public bool IsLeaf;
	}
	public class Face {
		public Vector3 Centroid;
		public Vector3[] Vertices;
	}
	public class MCBarycentricUnit {
		public Vector4[] BarycentricCoords;
	}
	public class MCCartesianUnit {
		public Vector3[] CartesianCoords;
		public float[] Values;
	}

}