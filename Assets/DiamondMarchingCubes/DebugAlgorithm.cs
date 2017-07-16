using System.Collections.Generic;
using UnityEngine;

namespace DMC {
	public static class DebugAlgorithm {
		public static int depth_ = 8;
		public static int checks_ = 20;
		public static int tetnum_ = 0;
		public static int debugdepth_ = 22;

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
			
			int totalsteps = 10000000;

			for(int i = 0; i < totalsteps; i++) {
				ScrambleTetrahedronSplittingTable();

				if(CheckValid() == true) {
					UnityEngine.Debug.Log("Successful combination found on iteration #" + i + ".");
					Debug.Log("Tetrahedron Vertex Order:");
					Debug.Log(Utility.ArrayToString(Lookups.RootTetrahedrons));

					Debug.Log("Tetrahedron Splitting Table:");
					Debug.Log(Utility.ArrayToString(Lookups.TetrahedronVertexOrder));
					return;
				}


			}
			UnityEngine.Debug.Log("Couldn't find solution for tetrahedron #1. Steps: " + totalsteps);
		}

		public static void FindNewCorrectTetSplittingTable() {
			FindCorrectCombination();
		}

		/* Scrambles Lookups.RootTetrahedron order  until
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
			n.HierarchyVertices = new Vector3[6];
			for(int j = 0; j < 4; j++) {
				n.HierarchyVertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[tetnum_,j]];
			}

			for(int j = 0; j < checks_; j++) {
				if(RecursiveSplitNode2(debugdepth_, n, 2) == false) {
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
				Node node = new Node();
				node.Number = mostRecentTetrahedronCount;
				mostRecentTetrahedronCount++;
				node.Depth = 0;
				node.IsLeaf = true;
				node.Vertices = new Vector3[4];
				for(int j = 0; j < 4; j++) {
					node.Vertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[i,j]];
				}
				node.CentralVertex = Utility.FindMidpoint(node.Vertices[0], node.Vertices[1]);
				AddTetToDictionary(root, node);
				RecursiveSplitNode(depth_, node, root, 3);
				root.Children[i] = node;
			}

			return root;
		}

		public static bool RecursiveSplitNode(int n, Node toSplit, Root root, int childToSplit) {
			bool returning = true;
			if(n <= 0) return true;
			int f = childToSplit;
			if(childToSplit == 2) { 
				f = UnityEngine.Random.Range(0, 2);
			}
			if(childToSplit == 3) {
				f = 0;
			}
			if(SplitNode(root, toSplit) == false) {
				returning = false;
			}
			RecursiveSplitNode(n - 1, toSplit.Children[0], root, childToSplit);
			return RecursiveSplitNode(n - 1, toSplit.Children[1], root, childToSplit);
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
		// 3rd parameter prevents infinite recursive calls
		public static bool SplitNode(Root root, Node node, uint doNotSplit = uint.MaxValue) {
			node.Children = new Node[2];
			node.IsLeaf = false;

			//
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

			//UnityEngine.Debug.Log("D" + node.Depth + ": Empircal longest edge: " + dist_longest + ", 0-1 edge distance: " + dist_01);

			// Ensure conforming by checking neighboring tetrahedra
			for(int i = 0; i < 6; i++) {
				Vector3 edgeMidpoint = Utility.FindMidpoint(node.Vertices[Lookups.EdgePairs[i, 0]], node.Vertices[Lookups.EdgePairs[i, 1]]);
				if(root.EdgeToTetList.ContainsKey(edgeMidpoint)) {
					List<Node> neighboringTetrahedra = root.EdgeToTetList[edgeMidpoint];
					for(int j = 0; j < neighboringTetrahedra.Count; j++) {
						Node neighbor = neighboringTetrahedra[j];
						if(neighbor.Number == node.Number || neighbor.Number == doNotSplit || !neighbor.IsLeaf) {
							continue;
						}
						if(neighbor.Depth == node.Depth + 1) {
							SplitNode(root, neighbor, node.Number);
						}
						// neighbor and node form diamond, must split both
						else if(neighbor.CentralVertex == node.CentralVertex) {
							SplitNode(root, neighbor, node.Number);
						}
					}
				}
			}

			// Construct new tetrahedra
			for(int i = 0; i < 2; i++) {
				Node child = new Node();
				child.Number = mostRecentTetrahedronCount;
				mostRecentTetrahedronCount++;
				child.Vertices = new Vector3[4];
				child.Depth = node.Depth + 1;
				child.IsLeaf = true;
				child.TetrahedronType = (node.TetrahedronType + 1) % 3;
				child.ReverseWindingOrder = node.ReverseWindingOrder;

				if(node.TetrahedronType == 2 && i == 1) {
					child.ReverseWindingOrder = !node.ReverseWindingOrder;
				}

				string str = "";
				for(int j = 0; j < 4; j++) {
					int vertexIndex = Lookups.TetrahedronVertexOrder[node.TetrahedronType, i, j];

					if(vertexIndex == -1) {
						child.Vertices[j] = node.CentralVertex;
					}
					else {
						child.Vertices[j] = node.Vertices[vertexIndex];
					}
					str += child.Vertices[j];
				}
				//UnityEngine.Debug.Log("Tetrahedron vertices: " + str);
				child.CentralVertex = Utility.FindMidpoint(child.Vertices[0], child.Vertices[1]);
				//UnityEngine.Debug.Log("Tetrahedron CentralVertex: " + child.CentralVertex);
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
				child.ReverseWindingOrder = node.ReverseWindingOrder;

				if(node.TetrahedronType == 2 && i == 1) {
					child.ReverseWindingOrder = !node.ReverseWindingOrder;
				}

				for(int j = 0; j < 4; j++) {
					int vertexIndex = Lookups.TetrahedronVertexOrder[node.TetrahedronType, i, j];

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

		public static Hexahedron[] ExtractHexahedra(Node node) {
			// step 1: get all the faces of the tetrahedron
			Face[] faces = new Face[4];
			for(int i = 0; i < 4; i++) {
				faces[i] = new Face();
				faces[i].Vertices = new Vector3[3];
				Vector3 sum = Vector3.zero;
				for(int j = 0; j < 3; j++) {
					faces[i].Vertices[j] = node.Vertices[Lookups.TetrahedronFaces[i, j]];
					sum += faces[i].Vertices[j];
				}
				faces[i].Centroid = sum / 3;
			}

			// step 2: calculate centroid of tetrahedron
			Vector3 centroid = Vector3.zero;
			for(int i = 0; i < 4; i++) {
				centroid += node.Vertices[i];
			}
			centroid /= 4;

			// step 3: each combination of faces is a new hexehedron
			Hexahedron[] hexahedra = new Hexahedron[4];
			for(int i = 0; i < 4; i++) {
				hexahedra[i] = new Hexahedron();
				hexahedra[i].Vertices = new Vector3[8];
				hexahedra[i].Vertices[0] = centroid;

				Face[] hexfaces = new Face[3];

				hexahedra[i].BaseFaces = faces;

				for(int j = 0; j < 3; j++) {
					// calculate centroid of each face.
					hexfaces[j] = faces[Lookups.TetrahedronFaces[i, j]];
				}
				for(int j = 0; j < 3; j++) {
					hexahedra[i].Vertices[j + 1] = hexfaces[j].Centroid;

					// for each face combination, find the midpoint of the shared edge
					Face face1 = hexfaces[Lookups.FaceCombinations[j, 0]];
					Face face2 = hexfaces[Lookups.FaceCombinations[j, 1]];
					int currentPoint = 0;
					Vector3[] sharedPoints = new Vector3[2];

					for(int z = 0; z < 3; z++) {
						for(int ii = 0; ii < 3; ii++) {
							if(face1.Vertices[z] == face2.Vertices[ii] && !(currentPoint >= 2))  {
								sharedPoints[currentPoint] = face1.Vertices[z];
								currentPoint++;
							}
						}
					}

					hexahedra[i].Vertices[j + 4] = Utility.FindMidpoint(sharedPoints[0], sharedPoints[1]);

				}

				// find shared point between all the faces
				for(int j = 0; j < 3; j++) {
					for(int z = 0; z < 3; z++) {
						for(int ii = 0; ii < 3; ii++) {
							if(hexfaces[0].Vertices[j] == hexfaces[1].Vertices[z] && hexfaces[0].Vertices[j] == hexfaces[2].Vertices[ii]) {
								hexahedra[i].Vertices[7] = hexfaces[0].Vertices[j];
							}
						}
					}
				}
				Vector3[] CachedVertices = new Vector3[12];
				for(int j = 0; j < 8; j++) {
					CachedVertices[j] = hexahedra[i].Vertices[Lookups.HexahedronVertexReorder[j]];
				}
				hexahedra[i].Vertices = CachedVertices;
			}

			return hexahedra;
		}

		public static List<Hexahedron> GenerateSubdividedHexahedronList(Hexahedron hex, int depth) {
			RecursiveSubdivideHexahedron(hex, depth);
			List<Hexahedron> Leaves = new List<Hexahedron>();
			PopulateSubdividedHexahedronList(hex, Leaves);
			return Leaves;
		}

		public static void PopulateSubdividedHexahedronList(Hexahedron hex, List<Hexahedron> listToPopulate) {
			if(hex.IsLeaf) {
				listToPopulate.Add(hex);
			}
			else {
				for(int i = 0; i < 8; i++) {
					PopulateSubdividedHexahedronList(hex.Children[i], listToPopulate);
				}
			}
		}

		public static void RecursiveSubdivideHexahedron(Hexahedron hex, int n) {
			if(n <= 0) {
				return;
			}
			else {
				SubdivideHexahedron(hex);
				for(int i = 0; i < 8; i++) {
					RecursiveSubdivideHexahedron(hex.Children[i], n - 1);
				}
			}
		}

		public static void SubdivideHexahedron(Hexahedron hex) {
			// 0: centroid 
			// 1-6: face centroids
			// 7-18: edge midpoints
			// 19-26: original hexahedron vertices
			Vector3[] CachedVertices = new Vector3[27];

			// 0
			Vector3 sum = Vector3.zero;
			for(int i = 0; i < 8; i++) sum += hex.Vertices[i];
			CachedVertices[0] = sum / 8;

			// 1-6
			for(int i = 0 ; i < 6; i++) {
				sum = Vector3.zero;
				for(int j = 0; j < 4; j++) {
					sum += hex.Vertices[Lookups.HexahedronFaces[i, j]];
				}
				CachedVertices[1 + i] = sum / 4;
			}

			// 7-18
			for(int i = 0; i < 12; i++) {
				sum = Vector3.zero;
				for(int j = 0; j < 2; j++) {
					sum += hex.Vertices[Lookups.HexahedronEdgePairs[i, j]];
				}
				CachedVertices[7 + i] = sum / 2;
			}

			// 19-26
			for(int i = 0; i < 8; i++) {
				CachedVertices[19 + i] = hex.Vertices[i];
			}

			Hexahedron[] SubdividedHexahedra = new Hexahedron[9];
			for(int i = 0; i < 8; i++) {
				SubdividedHexahedra[i] = new Hexahedron();
				SubdividedHexahedra[i].Vertices = new Vector3[8];
				SubdividedHexahedra[i].IsLeaf = true;
				for(int j = 0; j < 8; j++) {
					SubdividedHexahedra[i].Vertices[j] = CachedVertices[Lookups.HexahedronSubHexahedra[i, j]];
				}
			}

			hex.Children = SubdividedHexahedra;
			hex.IsLeaf = false;
		}

		public static List<MCBarycentricUnit> CreatePrecomputedVolumeMesh(List<Hexahedron> Hexahedra, DMC.Node Tetrahedron) {
			List<MCBarycentricUnit> VolumeMesh = new List<MCBarycentricUnit>();

			float a = Hexahedra.Count / 4;

			for(int i = 0; i < Hexahedra.Count; i++) {
				bool flip = false;

				if(i < a || (i >= a * 2 && i < a * 3)) {
					flip = true;
				}

				MCBarycentricUnit u = new MCBarycentricUnit();
				u.BarycentricCoords = new Vector4[8];
				for(int j = 0; j < 8; j++) {
					if(flip) {
						u.BarycentricCoords[j] = Math.Tet_CartToBary(Tetrahedron.Vertices[0], Tetrahedron.Vertices[1], 
							Tetrahedron.Vertices[2], Tetrahedron.Vertices[3], Hexahedra[i].Vertices[(j + 4) % 8]);
					}
					else {
						u.BarycentricCoords[j] = Math.Tet_CartToBary(Tetrahedron.Vertices[0], Tetrahedron.Vertices[1], 
							Tetrahedron.Vertices[2], Tetrahedron.Vertices[3], Hexahedra[i].Vertices[j]);
					}
				}
				VolumeMesh.Add(u);
			}
			return VolumeMesh;
		}

		public static List<Strucs.GridCell> ConvertVolumeMeshToCartesian(List<MCBarycentricUnit> PrecomputedMesh, DMC.Node Tetrahedron) {
			List<Strucs.GridCell> Units = new List<Strucs.GridCell>();
			for(int i = 0; i < PrecomputedMesh.Count; i++) {
				Strucs.GridCell unit = new Strucs.GridCell();
				unit.Points = new Strucs.Point[8];
				for(int j = 0; j < 8; j++) {
					unit.Points[j] = new Strucs.Point();
					unit.Points[j].Position = Math.Tet_BaryToCart(Tetrahedron.Vertices[0], Tetrahedron.Vertices[1], 
						Tetrahedron.Vertices[2], Tetrahedron.Vertices[3], PrecomputedMesh[i].BarycentricCoords[j]);
					unit.Points[j].Density = Utility.DebugNoise(unit.Points[j].Position);
				}
				Units.Add(unit);
			}
			return Units;
		}

		public static UnityEngine.Mesh PolyganiseNode(List<MCBarycentricUnit> PrecomputedMesh, DMC.Node node, bool ReverseWindingOrder) {
			List<Strucs.GridCell> MCCells = ConvertVolumeMeshToCartesian(PrecomputedMesh, node);

			List<Vector3> Vertices = new List<Vector3>();

			foreach(Strucs.GridCell cell in MCCells) {
				Polyganiser.Polyganise(cell, Vertices, 0);
			}

			int[] triangles = new int[Vertices.Count];
			for(int i = 0; i < Vertices.Count; i++) {
				triangles[i] = i;
			}

			UnityEngine.Mesh m = new UnityEngine.Mesh();
			m.vertices = Vertices.ToArray();
			m.triangles = triangles;

			m.RecalculateNormals();

			return m;

		}
	}

}
