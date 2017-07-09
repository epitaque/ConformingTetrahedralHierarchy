using System.Collections.Generic;
using UnityEngine;

namespace DMC {
	public class Node {
		public Node[] Children;
		public Vector3[] Vertices;
		public Vector3 Center;
		public int TetrahedronType;
		public int Depth; // 0 indicates top-level tetrahedron
		public uint Number; // used as UID system
	}

	public class Root {
		public Node[] Children;
		public Dictionary<Vector3, List<Node>> EdgeToTetList; // index: Central Vertex Position // value:  diamond
	}

}