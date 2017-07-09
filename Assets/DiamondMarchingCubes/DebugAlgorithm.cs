using System.Collections.Generic;
using UnityEngine;

namespace DMC {
	public static class DebugAlgorithm {
		public static int depth_ = 40;
		public static int checks_ = 20;
		public static int tetnum_ = 0;

		public static uint mostRecentTetrahedronCount = 0;

		public static Root Run(Vector3 PlayerLocation) {
			//FindCorrectCombination();
			//return null;
			//FindCorrectCombinationOfTetVerticesAndOrder();
			return CreateHierarchy(PlayerLocation);
			//return null;
		}

		public static void FindCorrectCombination() {
			Debug.Log("FindCorrectCombination called.");
			
			int totalsteps = 100000;

			for(int i = 0; i < totalsteps; i++) {

				if(CheckValid() == true) {
					UnityEngine.Debug.Log("Successful combination found on iteration #" + i + ".");
					Debug.Log("Tetrahedron Vertex Order:");
					Debug.Log(Utility.ArrayToString(Lookups.RootTetrahedrons));

					Debug.Log("Tetrahedron Splitting Table:");
					Debug.Log(Utility.ArrayToString(Lookups.TetrahedronVertexOrder));
					return;
				}

				ScrambleTetrahedronOrderAndVertexCombos();

				if(i % 1000 == 0) {
				}
			}
			UnityEngine.Debug.Log("Couldn't find solution for tetrahedron #1. Steps: " + totalsteps);
		}

		/* Scrambles Lookups.RootTetrahedron order and Lookups.RootCases until
		 	CheckValid() returns true (AKA when splitting node, alg always picks longest edge as bisecting edge) */
		public static void FindCorrectCombinationOfTetVerticesAndOrder() {
			Debug.Log("Attempting to find correct combination of tetrahedron vertices and tet number...");

			int[] nums = new int[4];

			for(int j = 0; j < 4; j++) {
				nums[j] = Lookups.RootTetrahedrons[0, j];
			}

			for(int i = 0; i < 3; i++) {
				for(int z = 0; z < 1000; z++) {
					int[] temp = Utility.ScrambleArray(nums);
					for(int j = 0; j < 4; j++) {
						Lookups.RootTetrahedrons[0,j] = temp[j];
					}
					if(CheckValid() == true) {
						UnityEngine.Debug.Log("Valid combination found at i = " + i + " on iteration " + z);

						Debug.Log(Utility.ArrayToString(temp));

						return;
					}
				}
			}

		}

		public static void ScrambleTetrahedronOrderAndVertexCombos() {
			int[] nums = new int[4];

			for(int j = 0; j < 4; j++) {
				nums[j] = Lookups.RootTetrahedrons[tetnum_, j];
			}

			int[] temp = Utility.ScrambleArray(nums);

			for(int j = 0; j < 4; j++) {
				Lookups.RootTetrahedrons[tetnum_,j] = temp[j];
			}


		}

		// Checks each root tetrahedron to make sure that when SplitNode() is called, the
		// longest edge is always chosen.
		public static bool CheckValid() {
			return CreateHierarchy2();
		}

		public static void ScrambleTetrahedronSplittingTable() {
			for(int i = 0; i < 3; i++) {
				for(int j = 0; j < 2; j++) {
					List<int> q = new List<int>(Lookups.PossibleTetrahedronVertexOrderCombos[j]);
					for(int z = 0; z < 4; z++) {
						int index = UnityEngine.Random.Range(0, q.Count);
						int n = q[index];
						q.RemoveAt(index);
						Lookups.TetrahedronVertexOrder[i,j,z] = n;
					}
				}
			}
			if(UnityEngine.Random.Range(0, 5000000) == 500) {
				UnityEngine.Debug.Log("Scrambled Tetrahedron Splitting table: " + Utility.ArrayToString(Lookups.TetrahedronVertexOrder));
			}
		}

		public static bool CreateHierarchy2() {
			Root root = new Root();
			root.Children = new Node[1];
			Node n = new Node();
			n.Depth = 0;
			n.Vertices = new Vector3[6];
			for(int j = 0; j < 4; j++) {
				n.Vertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[tetnum_,j]];
			}

			for(int j = 0; j < checks_; j++) {
				if(RecursiveSplitNode2(depth_, n, 2) == false) {
					return false;
				}

			}
			
			root.Children[0] = n;
			return true;
		}

		public static Root CreateHierarchy(Vector3 PlayerLocation) {
			Root root = new Root();

			root.EdgeToTetList = new Dictionary<Vector3, List<Node>>();

			root.Children = new Node[6];
			for(int i = 0; i < 6; i++) {
				Node n = new Node();
				n.Number = mostRecentTetrahedronCount;
				mostRecentTetrahedronCount++;
				n.Depth = 0;
				n.Vertices = new Vector3[4];
				for(int j = 0; j < 4; j++) {
					n.Vertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[i,j]];
				}
				AddTetToDictionary(root, n);
				RecursiveSplitNode(depth_, n, root, 2);
				root.Children[i] = n;
			}

			return root;
		}

		public static bool RecursiveSplitNode(int n, Node toSplit, Root root, int childToSplit) {
			if(n <= 0) return true;
			int f = childToSplit;
			if(childToSplit == 2) { 
				f = UnityEngine.Random.Range(0, 2);
			}
			if(SplitNode(root, toSplit) == false) {
				return false;
			}
			return RecursiveSplitNode(n - 1, toSplit.Children[f], root, childToSplit);
		}
		public static bool RecursiveSplitNode2(int n, Node toSplit, int childToSplit) {
			if(n <= 0) return true;
			int f = childToSplit;
			if(childToSplit == 2) { 
				f = UnityEngine.Random.Range(0, 2);
			}
			if(SplitNode2(toSplit) == false) {
				return false;
			}
			return RecursiveSplitNode2(n - 1, toSplit.Children[f], childToSplit);
		}

		public static bool SplitNode(Root root, Node node) {
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

			// Ensure conforming by checking neighboring tetrahedra
			for(int i = 0; i < 6; i++) {
				Vector3 edgeMidpoint = Utility.FindMidpoint(node.Vertices[Lookups.EdgePairs[i, 0]], node.Vertices[Lookups.EdgePairs[i, 1]]);
				List<Node> neighboringTetrahedra = root.EdgeToTetList[edgeMidpoint];
			}

			// Construct new tetrahedra
			for(int i = 0; i < 2; i++) {
				Node child = new Node();
				child.Number = mostRecentTetrahedronCount;
				mostRecentTetrahedronCount++;
				child.Vertices = new Vector3[4];
				child.Depth = node.Depth + 1;
				child.TetrahedronType = (node.TetrahedronType + 1) % 3;
				for(int j = 0; j < 4; j++) {
					int vertexIndex = Lookups.TetrahedronVertexOrder[child.TetrahedronType, i, j];

					if(vertexIndex == -1) {
						child.Vertices[j] = midpoint;
					}
					else {
						child.Vertices[j] = node.Vertices[vertexIndex];
					}
				}
				node.Children[i] = child;
			}

			return true;
		}

		public static bool SplitNode2(Node node) {
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
					int vertexIndex = Lookups.TetrahedronVertexOrder[child.TetrahedronType, i, j];

					if(vertexIndex == -1) {
						child.Vertices[j] = midpoint;
					}
					else {
						child.Vertices[j] = node.Vertices[vertexIndex];
					}
				}
				node.Children[i] = child;
			}

			return true;
		}

		public static void AddTetToDictionary(Root root, Node node) {
			for(int j = 0; j < 6; j++) {
				Vector3 edgeMidpoint = Utility.FindMidpoint(node.Vertices[Lookups.EdgePairs[j, 0]], node.Vertices[Lookups.EdgePairs[j, 1]]);
				if(root.EdgeToTetList.ContainsKey(edgeMidpoint)) {
					root.EdgeToTetList[edgeMidpoint].Add(node);
				}
				else {
					List<Node> tetList = new List<Node>();
					tetList.Add(node);
					root.EdgeToTetList.Add(edgeMidpoint, tetList);
				}
			}

		}
	}

}
