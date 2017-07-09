using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMC {
	public static class Algorithm {
		public static int depth_ = 0;

		public static Root Run(Vector3 PlayerLocation) {
			return CreateHierarchy(PlayerLocation);
		}
		
		public static Root CreateHierarchy(Vector3 PlayerLocation) {

			Root root = new Root();
			root.Children = new Node[6];
			for(int i = 0; i < 6; i++) {
				Node n = new Node();
				n.Depth = 0;
				n.Vertices = new Vector3[6];
				n.TetrahedronType = 0;
				for(int j = 0; j < 4; j++) {
					n.Vertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[i,j]];
				}

				//RecursiveSplitNode(depth_ - 1, n, 0);
				//UnityEngine.Debug.Log("2nd tri split");
				//RecursiveSplitNode(22, n, 1);

				root.Children[i] = n;
			}

			return root;
		}

		public static bool RecursiveSplitNode(int n, Node toSplit, int childToSplit) {
			if(n <= 0) return true;
			int f = childToSplit;
			if(childToSplit == 2) { 
				f = UnityEngine.Random.Range(0, 2);
			}
			if(SplitNode(toSplit) == false) {
				return false;
			}
			return RecursiveSplitNode(n - 1, toSplit.Children[f], childToSplit);
		}

		public static bool SplitNode(Node node) {
			node.Children = new Node[2];
			// Find the longest edge (its the edge between v0 and v1)
			// Find the midpoint between the longest edge (v0 + v1)/2

			float dist_longest = 0;
			int pair;
			for(int i = 0; i < 6; i++) {
				float dist = Vector3.Distance(node.Vertices[Lookups.EdgePairs[i,0]], node.Vertices[Lookups.EdgePairs[i,1]]);
				if(dist > dist_longest) {
					dist_longest = dist;
					pair = i;
				}
			}

			float dist_01 = Vector3.Distance(node.Vertices[0], node.Vertices[1]);

			UnityEngine.Debug.Log("D" + node.Depth + ": Empircal longest edge: " + dist_longest + ", 0-1 edge distance: " + dist_01);

			Vector3 midpoint = (node.Vertices[0] + node.Vertices[1])/2;


			if(!(dist_01 == dist_longest)) {
				return false;
			}

			// Construct new tetrahedra
			for(int i = 0; i < 2; i++) {
				Node child = new Node();
				child.Vertices = new Vector3[4];
				child.Depth = node.Depth + 1;
				child.TetrahedronType = (node.TetrahedronType + 1) % 3;
				for(int j = 0; j < 4; j++) {
					//UnityEngine.Debug.Log("TetrahedronType: " + child.TetrahedronType);
					if(Lookups.TetrahedronVertexOrder[child.TetrahedronType, i, j] == -1) {
						child.Vertices[j] = midpoint;
					}
					else {
						child.Vertices[j] = node.Vertices[Lookups.TetrahedronVertexOrder[child.TetrahedronType, i, j]];
					}
				}
				//child.Vertices[3] = midpoint;
				node.Children[i] = child;
			}

			return true;
		}
	}

}
