using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace DMC2 {
	public class Node {
		public Node[] Children;
		public Vector3Int[] Vertices; // used for everything else (consistent clockwise ordering)
		public Vector3Int[] HVertices; // used for splitting tetrahedrons
		public Vector3 Center;
		public Vector3Int CentralVertex; // midpoint of longest edge, center of bounding sphere
		public float BoundRadius; // half of longest edge length
		public int TetrahedronType;
		public int Depth; // 0 indicates top-level tetrahedron
		public uint Number; // used as UID system
		public bool IsLeaf;
		public bool ReversedWindingOrder;
		public GameObject UnityObject;
		public Node Parent;
		public Sphere BoundingSphere;
	}

	public class Root {
		public Dictionary<uint, Node> Nodes; // Index: Node number | Value: Node
		public Dictionary<Vector3Int, Diamond> Diamonds;

		public Queue<Diamond> SplitQueue;
		public Queue<Diamond> MergeQueue;

		public Diamond RootDiamond;

		public bool IsValid; // true if every split uses the longest edge
	}

	public class Diamond {
		public List<Node> Tetrahedra;
		public int Phase;
		public int Level;
		public List<Diamond> Parents;
		public List<Diamond> Children;
		public Vector3Int CentralVertex;
		public Vector3 SpineA;
		public Vector3 SpineB;
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