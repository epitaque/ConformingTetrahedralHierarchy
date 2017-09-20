using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DMC {
	public static class DebugAlgorithm {
		public static uint mostRecentTetrahedronCount = 0;

		public static Root Run(Vector3 PlayerLocation) {
			//FindCorrectCombination();
			//return null;
			//FindCorrectCombinationOfTetVerticesAndOrder();
			return CreateHierarchy(PlayerLocation);
			//return null;
		}

		/*public static void FindCorrectCombination() {
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
			*/


		public static Root CreateHierarchy(Vector3 PlayerLocation) {
			Root root = new Root();

			root.FaceToNodeList = new Dictionary<Vector3, List<Node>>();
			root.CVToNodeList = new Dictionary<Vector3, List<Node>>();
			root.Nodes = new Dictionary<uint, Node>();

			root.Children = new Node[6];
			root.IsValid = true;
			for(int i = 0; i < 6; i++) {
				Node node = new Node();
				node.Number = mostRecentTetrahedronCount; mostRecentTetrahedronCount++;
				root.Nodes.Add(node.Number, node);
				node.Depth = 0;
				node.IsLeaf = true;
				node.HVertices = new Vector3[4];
				node.Vertices = new Vector3[4];
				for(int j = 0; j < 4; j++) {
					node.HVertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[i,j]];
					node.Vertices[j] = Lookups.StartingVerts[Lookups.RootTetrahedrons[i,j]];
				}
				if(i % 2 == 1) {
					node.Vertices[0] = node.HVertices[1];
					node.Vertices[1] = node.HVertices[0];
					node.ReversedWindingOrder = true;
				}
				node.BoundRadius = Vector3.Distance(node.HVertices[0], node.HVertices[1]) / 2f;
				node.CentralVertex = Utility.FindMidpoint(node.Vertices[0], node.Vertices[1]);
				AddToDictionary(root, node);
				root.Children[i] = node;
				node.BoundingSphere = Utility.CalculateBoundingSphere(node);
			}

			return root;
		}

		public delegate float FindTargetDepth(Node node);
		public static float DefaultFindTargetDepth(Vector3 position, Node node) {
				float mapSize = 256f;
				float maxDepth = 22f;
				float dist = Mathf.Clamp(Vector3.Distance((node.CentralVertex * mapSize), position) - (node.BoundRadius * mapSize * 1.2f), 0, float.MaxValue);

				float targetDepth = (6f / Mathf.Log((dist / 11f) + 1.2f, 10f));
				return Mathf.Clamp(targetDepth, 1, maxDepth);
		}

		public static void LoopAdapt(Root root, Vector3 position, FindTargetDepth findTargetDepth, int MaxIterations = 100) {
			// coarsen
			int i = 0;
			while(!Adapt(root, position, findTargetDepth, true)) {
				if(i > MaxIterations) {
					UnityEngine.Debug.LogWarning("WARNING: LoopAdapt Coarsen stage iterations maximum reached at " + MaxIterations);
					break;
				}
				i++;
			}
			UnityEngine.Debug.Log("LoopAdapt Coarsen stage finished at " + i + " iterations.");

			// refine
			i = 0;
			while(!Adapt(root, position, findTargetDepth, false)) {
				if(i > MaxIterations) {
					UnityEngine.Debug.LogWarning("WARNING: LoopAdapt Refine stage iterations maximum reached at " + MaxIterations);
					break;
				}
				i++;
			}
			UnityEngine.Debug.Log("LoopAdapt Refine stage finished at " + i + " iterations.");

		}

		// returns true if no further adaption is required
		public static bool Adapt(Root root, Vector3 position, FindTargetDepth findTargetDepth, bool coarsen) {
			bool res = true;
			foreach(Node child in root.Children) {
				if(coarsen) {
					if(!RecursiveAdaptCoarsen(root, child, findTargetDepth)) {
						res = false;
					}
				}
				else {
					if(!RecursiveAdaptRefine(root, child, findTargetDepth)) {
						res = false;
					}
				}
			}
			return res;
		}
		// returns true if no further adaption is required
		private static bool RecursiveAdaptRefine(Root root, Node node, FindTargetDepth findTargetDepth) {
			if(node.IsLeaf) {
				int targetDepth = (int)findTargetDepth(node);
				if(node.Depth < 6) {
					SplitNode(root, node);
					return false;
				}
				else if(node.Depth < targetDepth) { // node needs to have higher depth (be more refined)
					SplitAllNodesInDiamondIfPossible(root, node);	   // so split the node
					return false;
				}
			}
			else {
				foreach(Node child in node.Children) {
					//if(node.IsLeaf) break;
					//if(!child.IsLeaf) continue;
					if(!RecursiveAdaptRefine(root, child, findTargetDepth)) {
						return false;
					}
				}
			}

			return true;
		}
		private static bool RecursiveAdaptCoarsen(Root root, Node node, FindTargetDepth findTargetDepth) {
			bool needsNoFurtherAdaption = true;
			if(node.IsLeaf) {
				int targetDepth = (int)findTargetDepth(node);
				if(node.Depth > targetDepth && node.Depth != 0) { // node depth is too large
					// see if coarsening is possible
					MergeNodeIfPossible(root, node);
					needsNoFurtherAdaption = false;

					/*Node sibling = FindSiblingNode(node);	// get sibling node
					if(sibling.Depth > targetDepth && sibling.IsLeaf) {
						Node parent = node.Parent;
						CoarsenNodes(root, parent);
						Node parentSibling = FindSiblingNode(parent);
						if(parent.Depth > (int)findTargetDepth(parent) && parentSibling.Depth > (int)findTargetDepth(parentSibling)) {
							needsNoFurtherAdaption = false;
						}
					}*/
				}
			}
			else {
				foreach(Node child in node.Children) {
					if(node.IsLeaf) break;
					if(!RecursiveAdaptCoarsen(root, child, findTargetDepth)) {
						needsNoFurtherAdaption = false;
					}
				}
			}

			return needsNoFurtherAdaption;
		}

		public static void LoopMakeConforming(Root root, int MaxIterations = 20) {
			int i = 0;
			while(!MakeConforming(root)) {
				if(i > MaxIterations) {
					UnityEngine.Debug.LogWarning("WARNING: LoopMakeConforming iterations maximum reached at " + MaxIterations);
					break;
				}
				i++;
			}
			UnityEngine.Debug.Log("LoopMakeConforming finished at " + i + " iterations.");
		}
		public static bool MakeConforming(Root root) {
			bool needsNoFurtherIterations = true;
			foreach(Node child in root.Children) {
				if(!RecursiveMakeConforming(root, child)) {
					needsNoFurtherIterations = false;
				}
			}
			return needsNoFurtherIterations;
		}
		private static bool RecursiveMakeConforming(Root root, Node node) {
			bool needsNoFurtherIterations = true;
			if(node.IsLeaf) {
				// check to see if neighboring nodes have children of children
				List<Node> neighbors = FindNeighboringNodes(root, node);
				List<Node> toBeSplit = new List<Node>();
				foreach(Node neighbor in neighbors) {
						if(neighbor.IsLeaf) continue;
						if(neighbor.Children[0].IsLeaf && neighbor.Children[1].IsLeaf) continue;
						if(neighbor.Depth < node.Depth) continue;
						else {
							toBeSplit.Add(node);
							needsNoFurtherIterations = false;
						}
				}
				foreach(Node nodeToSplit in toBeSplit) {
					SplitAllNodesInDiamondIfPossible(root, nodeToSplit);
				}
			}
			else {
				foreach(Node child in node.Children) {
					if(!RecursiveMakeConforming(root, child)) {
						needsNoFurtherIterations = false;
					}
				}
			}
			return needsNoFurtherIterations;
		}

		public static bool SplitAllNodesInDiamondIfPossible(Root root, Node node) {
			UnityEngine.Debug.Log("SplitAllNodesInDiamondIfPossible called on Node " + node.Number);
			int MaxNumberOfTetsInDiamond = Lookups.DiamondNumberOfTetrahedra[node.Depth % 3];
			int ActualNumberOfTetsInDiamond = root.CVToNodeList[node.CentralVertex].Count;
			UnityEngine.Debug.Log("MaxNumberOfTetsInDiamond: " + MaxNumberOfTetsInDiamond + ", ActualNumberOfTetsInDiamond: " + ActualNumberOfTetsInDiamond);
			UnityEngine.Debug.Assert(ActualNumberOfTetsInDiamond <= MaxNumberOfTetsInDiamond);
			if(!(ActualNumberOfTetsInDiamond <= MaxNumberOfTetsInDiamond)) {
				string Depths = "";
				foreach(Node n in root.CVToNodeList[node.CentralVertex]) {
					Depths += n.Depth + ", ";
				}
				UnityEngine.Debug.LogError("Depths string: " + Depths);
			}
			if(MaxNumberOfTetsInDiamond == ActualNumberOfTetsInDiamond) {
				foreach(Node n in root.CVToNodeList[node.CentralVertex]) {
					if(!n.IsLeaf) {
						return false;
					}
				}
				foreach(Node n in root.CVToNodeList[node.CentralVertex]) {
					UnityEngine.Debug.Assert(n.IsLeaf);
					SplitNode(root, n);
				}
				return true;
			}
			return false;
		}

		public static void MergeNodeIfPossible(Root root, Node node) {
			List<Node> ParentDiamond = root.CVToNodeList[node.Parent.CentralVertex];

			List<Node> Children = new List<Node>();
			foreach(Node parent in ParentDiamond) {
				Children = Children.Union(parent.Children).ToList();
			}
			foreach(Node child in Children) {
				if(!child.IsLeaf) {
					Debug.Log("INFO: Failed to merge node because child of node of parent diamond is not a leaf.");
					return;
				}
			}
			foreach(Node parent in ParentDiamond) {
				CoarsenNodes(root, parent);
			}
		}

		public static Node FindSiblingNode(Node node) {
			UnityEngine.Debug.Assert(node.Depth >= 1);
			Node[] siblings = node.Parent.Children;
			return siblings[0].Number == node.Number ? siblings[1] : siblings[0];
		}
 
		public static Node FindDiamondNode(Root root, Node node) {
			List<Node> neighbors = FindNeighboringNodes(root, node);
			foreach(Node neighbor in neighbors) {
				if(neighbor.Number != node.Number && neighbor.CentralVertex == node.CentralVertex) {
					return neighbor;
				}
			}
			UnityEngine.Debug.LogError("Could not find other node that makes diamond! Node " + node.Number);
			return null;
		}

		public static List<Node> FindNeighboringNodes(Root root, Node node) {
			List<Node> neighboringNodes = new List<Node>();

			for(int i = 0; i < 4; i++) {
				Vector3 centroid = FindCentroid(node, i);

				if(root.FaceToNodeList.ContainsKey(centroid)) {
					neighboringNodes = neighboringNodes.Union(root.FaceToNodeList[centroid]).ToList();
				}
			}

			return neighboringNodes;
		}

		public static void SplitNode(Root root, Node node) {
			node.Children = new Node[2];
			node.IsLeaf = false;

			// Construct new tetrahedra
			Vector3[] CachedVertices = new Vector3[5];
			for(int i = 0; i < 4; i++) {
				CachedVertices[i] = node.HVertices[i];
			}
			CachedVertices[4] = node.CentralVertex;
			for(int i = 0; i < 2; i++) {
				Node child = new Node();
				child.Number = mostRecentTetrahedronCount;
				mostRecentTetrahedronCount++;
				root.Nodes.Add(child.Number, child);
				child.HVertices = new Vector3[4];
				child.Vertices = new Vector3[4];
				child.Depth = node.Depth + 1;
				child.IsLeaf = true;
				child.TetrahedronType = (node.TetrahedronType + 1) % 3;
				child.ReversedWindingOrder = node.ReversedWindingOrder;
				child.Parent = node;
				if(node.TetrahedronType == 0 && i == 1 ||
				   node.TetrahedronType == 2 && i == 1) {
					child.ReversedWindingOrder = !node.ReversedWindingOrder;
				}

				for(int j = 0; j < 4; j++) {
					child.HVertices[j] = CachedVertices[Lookups.TetrahedronVertexOrder[node.TetrahedronType, i, j]];
					child.Vertices[j] = child.HVertices[j];
				}
				if(child.ReversedWindingOrder) {
					child.Vertices[0] = child.HVertices[1];
					child.Vertices[1] = child.HVertices[0];
				}
				child.BoundRadius = Vector3.Distance(child.HVertices[0], child.HVertices[1]) / 2f;
				child.CentralVertex = Utility.FindMidpoint(child.HVertices[0], child.HVertices[1]);
				child.BoundingSphere = Utility.CalculateBoundingSphere(child);
				node.Children[i] = child;
				AddToDictionary(root, child);
			}
		}

		public static void CoarsenNodes(Root root, Node parent) {
			foreach(Node child in parent.Children) {
				UnityEngine.Debug.Assert(child.IsLeaf);
				root.Nodes.Remove(child.Number);
				for(int i = 0; i < 4; i++) {
					Vector3 centroid = FindCentroid(child, i);
					root.FaceToNodeList[centroid].Remove(child);
				}
			}
			parent.Children = null;
			parent.IsLeaf = true;
		}

		public static void AddToDictionary(Root root, Node node) {
			// Update FaceToNodeList
			for(int i = 0; i < 4; i++) {
				Vector3 centroid = FindCentroid(node, i);
				if(!root.FaceToNodeList.ContainsKey(centroid)) {
					root.FaceToNodeList.Add(centroid, new List<Node>());
				}
				root.FaceToNodeList[centroid].Add(node);
			}
			if(!root.CVToNodeList.ContainsKey(node.CentralVertex)) {
				root.CVToNodeList.Add(node.CentralVertex, new List<Node>());
			}
			root.CVToNodeList[node.CentralVertex].Add(node);
		}

		public static Vector3 FindCentroid(Node Node, int FaceNumber) {
			return (Node.Vertices[Lookups.TetrahedronFaces[FaceNumber, 0]] + 
				    Node.Vertices[Lookups.TetrahedronFaces[FaceNumber, 1]] +
				    Node.Vertices[Lookups.TetrahedronFaces[FaceNumber, 2]] ) / 3;
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

		public static List<MCBarycentricUnit> ConvertHexahedraToBarycentric(List<Hexahedron> Hexahedra, DMC.Node Tetrahedron) {
			List<MCBarycentricUnit> VolumeMesh = new List<MCBarycentricUnit>();

			float a = Hexahedra.Count / 4;

			for(int i = 0; i < Hexahedra.Count; i++) {
				bool flip = false;

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
		public static List<MCBarycentricUnit> CreatePrecomputedVolumeMesh(DMC.Node Tetrahedron) {
			DMC.Hexahedron[] rootHexahedra = ExtractHexahedra(Tetrahedron);
			List<DMC.Hexahedron> subdividedHexahedra = new List<DMC.Hexahedron>();
			for(int i = 0; i < 4; i++) {
				subdividedHexahedra.AddRange(DMC.DebugAlgorithm.GenerateSubdividedHexahedronList(rootHexahedra[i], 2));
			}
			return ConvertHexahedraToBarycentric(subdividedHexahedra, Tetrahedron);
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

		public static UnityEngine.Mesh PolyganiseNode(List<MCBarycentricUnit> PrecomputedMesh, DMC.Node node) {
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
