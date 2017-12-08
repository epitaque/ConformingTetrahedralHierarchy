using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DMC {
	public static class DebugAlgorithm {
		public static List<Diamond> DebugDiamonds = new List<Diamond>();

		public static uint mostRecentTetrahedronCount = 0;

		public static FindTargetDepth finder = FindTargetDepth2;

		public static Root Run(Vector3 PlayerLocation) {
			//FindCorrectCombination();
			//return null;
			//FindCorrectCombinationOfTetVerticesAndOrder();
			//CreateTestHierarchy();
			return CreateHierarchy();
			//return null;
		}

		public static Root CreateHierarchy() {
			Root root = new Root();

			root.Diamonds = new Dictionary<Vector3, Diamond>();
			root.Nodes = new Dictionary<uint, Node>();

			root.SplitQueue = new Queue<Diamond>();
			root.MergeQueue = new Queue<Diamond>();

			root.IsValid = true;

			Node[] rootNodes = new Node[6];
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
				rootNodes[i] = node;
				node.BoundingSphere = Utility.CalculateBoundingSphere(node);
			}

			Diamond rootDiamond = new Diamond();
			rootDiamond.Tetrahedra = new List<Node>(rootNodes);
			rootDiamond.Phase = 0;
			rootDiamond.Children = new List<Diamond>(6);
			rootDiamond.Level = 0;
			root.RootDiamond = rootDiamond;

			return root;
		}

		public static Root CreateTestHierarchy() {
			Root root = CreateHierarchy();

			//DebugDiamonds.Add(root.RootDiamond);

			//SplitDiamond(root, root.RootDiamond);

			for(int i = 0; i < 6; i++) {
				Node tet = root.RootDiamond.Tetrahedra[i];
				CheckSplit(root, new Vector3(0, 0, 0), tet);
			}

			return root;
		}

		public delegate float FindTargetDepth(Node node, Vector3 position);
		public static float FindTargetDepth2(Node node, Vector3 position) {
			//return 10f;

			float mapSize = 256f;
			float maxDepth = 10f;
			float dist = Mathf.Clamp(Vector3.Distance((node.CentralVertex * mapSize), position) - (node.BoundRadius * mapSize * 1.2f), 0, float.MaxValue);

			float targetDepth;
			if(dist < 60f) {
				targetDepth = 14f;
			}
			else {
				targetDepth = 1f;
			}
			float clamped = Mathf.Clamp(targetDepth, 1f, (float)maxDepth);
			return (int)clamped;
		}
		public static float DefaultFindTargetDepth(Vector3 position, Node node) {
				float mapSize = 256f;
				float maxDepth = 10f;
				float dist = Mathf.Clamp(Vector3.Distance((node.CentralVertex * mapSize), position) - (node.BoundRadius * mapSize * 1.2f), 0, float.MaxValue);

				float targetDepth = (6f / Mathf.Log((dist / 11f) + 1.2f, 10f));
				return Mathf.Clamp(targetDepth, 1, maxDepth);
		}

		public static float FindMaxTargetDepth(Diamond d, Vector3 position) {
			float max = int.MinValue;
			foreach(Node n in d.Tetrahedra) {
				float td = finder(n, position);
				if(td >  max) {
					max = td;
				}
			}
			return max;
		}

		public static float FindMinTargetDepth(Diamond d, Vector3 position) {
			float min = int.MaxValue;
			foreach(Node n in d.Tetrahedra) {
				float td = finder(n, position);
				if(td <  min) {
					min = td;
				}
			}
			return min;
		}


		public static float FindMaxDepth(Diamond d) {
			float max = int.MinValue;
			foreach(Node n in d.Tetrahedra) {
				float de = n.Depth;
				if(de >  max) {
					max = de;
				}
			}
			return max;
		}

		public static float FindMinDepth(Diamond d) {
			float min = int.MaxValue;
			foreach(Node n in d.Tetrahedra) {
				float de = n.Depth;
				if(de < min) {
					min = de;
				}
			}
			return min;
		}

		// returns true if no further adaption is required
		public static bool Adapt(Root root, Vector3 position) {
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			Refine(root, position);
			Coarsen(root, position);
			sw.Stop();
			Debug.Log("Adapt took " + sw.ElapsedMilliseconds + "ms");
			return true;
			/*bool res = true;
			foreach(Node child in root.RootDiamond.Tetrahedra) {
				if(coarsen) {
					if(!RecursiveAdaptCoarsen(root, child, position)) {
						res = false;
					}
				}
				else {
					if(!RecursiveAdaptRefine(root, child, position)) {
						res = false;
					}
				}
			}
			return res;*/
		}


		public static void Coarsen(Root root, Vector3 position) {
			for(int i = 0; i < 6; i++) {
				Coarsen(root, root.RootDiamond.Tetrahedra[i], position);
			}
		}

		public static void Coarsen(Root root, Node node, Vector3 position) {
			if(node == null || node.IsDeleted) return;
			Diamond d = root.Diamonds[node.CentralVertex];
			int maxTargetDepth = (int)FindMaxTargetDepth(d, position);
			int minDepth = (int)FindMinDepth(d);
			if( (minDepth) > maxTargetDepth) { // node needs to have higher depth (be more refined)
				MergeTetrahedron(root, node);
			}

			if(!node.IsLeaf) {
				for(int i = 0; i < 2; i++) {
					Coarsen(root, node.Children[i], position);
				}
			}
		}

		// returns true if no further adaption is required
		private static bool RecursiveAdaptRefine(Root root, Node node, Vector3 position) {
			Debug.Log("adapting at position: " + position);
			bool res = true;
			if(node.IsLeaf) {
				Diamond d = root.Diamonds[node.CentralVertex];
				int targetDepth = (int)FindMaxTargetDepth(d, position);
				if(node.Depth < targetDepth) { // node needs to have higher depth (be more refined)
					SplitDiamond(root, d);	   // so split the node
					res = false;
				}
			}
			else {
				foreach(Node child in node.Children) {
					//if(node.IsLeaf) break;
					//if(!child.IsLeaf) continue;
					if(!RecursiveAdaptRefine(root, child, position)) {
						res = false;
					}
				}
			}

			return res;
		}
		private static bool RecursiveAdaptCoarsen(Root root, Node node, Vector3 position) {
			bool needsNoFurtherAdaption = true;
			if(node.IsLeaf) {
				Diamond d = root.Diamonds[node.CentralVertex];
				int targetDepth = (int)FindMaxTargetDepth(d, position);
				if(node.Depth > targetDepth && node.Depth != 0) { // node depth is too large
					MergeTetrahedron(root, node);
					needsNoFurtherAdaption = false;
				}
			}
			else {
				foreach(Node child in node.Children) {
					if(node.IsLeaf) break;
					if(!RecursiveAdaptCoarsen(root, child, position)) {
						needsNoFurtherAdaption = false;
					}
				}
			}

			return needsNoFurtherAdaption;
		}



		public static void Adapt(Root root, Node node, Vector3 position) {
			if(node == null || node.IsDeleted) return;
			if(!node.IsLeaf) {
				Debug.Assert(node.Children[0] != null);
				Debug.Assert(node.Children[1] != null);
				Debug.Assert(root != null);
				//Debug.Log("Root: " + root + ", c1: " + node.Children[0] + ", c2: " + node.Children[1] + ", position: " + position);
				Adapt(root, node.Children[0], position);
				if(node.Children == null) return;
				Adapt(root, node.Children[1], position);
			}
			else {
				Diamond d = root.Diamonds[node.CentralVertex];
				float maxTargetDepth = FindMaxTargetDepth(d, position);
				float maxDepth = FindMaxDepth(d);
				float minDepth = FindMinDepth(d);
				if(minDepth < maxTargetDepth) {
					SplitDiamond(root, d);
				}
				else if(minDepth > maxTargetDepth) {
					//MergeTetrahedron(root, node.Parent);
				}
			}
		}

		public static bool ShouldSplitDiamond(Diamond diamond, Vector3 position) {
			float nshouldsplit = 0;
			float ntotald2 = 0;
			foreach(Node n in diamond.Tetrahedra) {
				if(n == null) continue;
				ntotald2++;
				if(finder(n, position) > n.Depth) {
					nshouldsplit++;
				}
			}
			if(nshouldsplit/ntotald2 >= 0.5) {
				return true;
			}
			return false;
		}

		public static void Refine(Root root, Vector3 position) {
			for(int i = 0; i < 6; i++) {
				CheckSplit(root, position, root.RootDiamond.Tetrahedra[i]);
			}

		}

		public static void CheckSplit(Root root, Vector3 position, Node node) {
			if(finder(node, position) > node.Depth) {
				SplitDiamond(root, root.Diamonds[node.CentralVertex]);

				CheckSplit(root, position, node.Children[0]);
				CheckSplit(root, position, node.Children[1]);
			}
		}

		public static void SplitDiamond(Root root, Diamond diamond) {
			//Debug.Log("Splitting diamond.");
			// Create the children diamonds if they don't exist

			for(int tetNum = 0; tetNum < diamond.Tetrahedra.Count; tetNum++) {
				Node tet = diamond.Tetrahedra[tetNum];

				//Debug.Log("Diamond cv: " + diamond.CentralVertex);

				if(tet.Children == null && tet.CentralVertex != diamond.CentralVertex) {
					SplitDiamond(root, root.Diamonds[tet.CentralVertex]);
				}

				if(tet.Children == null) {
					//Debug.Log("Split node called.");
					SplitNode(root, tet);
				}
			}
		}

		public static void MergeTetrahedron(Root root, Node node) {
			List<Node> toBeDeleted = new List<Node>();
			MergeTetrahedron(root, node, toBeDeleted);
			foreach(Node n in toBeDeleted) {
				n.Parent.Children = null;
				RemoveFromDictionary(root, n);
				n.IsLeaf = true;
			}
		}

		public static void MergeTetrahedron(Root root, Node node, List<Node> toBeDeleted) {
			if(node.IsLeaf) return;
			node.IsLeaf = true;

			for(int i = 0; i < 2; i++) {
				MergeTetrahedron(root, node.Children[i], toBeDeleted);
			}
			if(node.Children != null) { toBeDeleted.Add(node.Children[0]); toBeDeleted.Add(node.Children[1]); }
			CollapseNode(root, node.Parent);

			IEnumerable<Node> ds = root.Diamonds[node.CentralVertex].Tetrahedra.Where((Node n) => n.Depth == node.Depth && n != node);

			var ds2 = (Node[])ds.ToArray().Clone();

			foreach(Node dnode in ds2) {
				//if(dnode.IsDeleted) continue;
				MergeTetrahedron(root, dnode, toBeDeleted);
			}
		}

		public static void CollapseNode(Root root, Node node) {
			if(node.Children == null || node.Children[0].Children != null || node.Children[1].Children != null) {
				//Debug.Log("node child 0" + node.Children[0].Children + ", node child 1: " + node.Children[1].Children);
				//Debug.Assert(false);
				return;
			}
			RemoveFromDictionary(root, node.Children[0]);
			RemoveFromDictionary(root, node.Children[1]);

			node.IsDeleted = true;
			node.Children = null;
			node.IsLeaf = true;
		}

		public static void TryAddDiamond(Root root, Vector3 offset, Diamond parentDiamond) {
			if(IsOutOfBounds(offset)) return;
			if(root.Diamonds.ContainsKey(offset)) {
				root.Diamonds[offset].Parents.Add(parentDiamond);
			}
			Diamond d = new Diamond();
			d.CentralVertex = offset;
			d.Tetrahedra = new List<Node>();
			d.Phase = (parentDiamond.Phase + 1) % 3;
			d.Level = parentDiamond.Level;
			if(parentDiamond.Phase == 2) { d.Level++; }
			d.Parents = new List<Diamond>();
			d.Parents.Add(parentDiamond);
			d.Children = new List<Diamond>();
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


		public static bool IsOutOfBounds(Vector3 A) {
			return A.x < -1 || A.y < -1 || A.z < -1 || A.x > 1 || A.y > 1 || A.z > 1;
		}

		public static void AddToDictionary(Root root, Node node) {
			for(int edgeNum = 0; edgeNum < 6; edgeNum++) {
				Vector3 A = node.Vertices[Lookups.EdgePairs[edgeNum,0]];
				Vector3 B = node.Vertices[Lookups.EdgePairs[edgeNum,1]];

				Vector3 midpoint = (A + B) / 2f;

				if(!root.Diamonds.ContainsKey(midpoint)) {
					CreateDiamond(root, midpoint, node);
				}
				Debug.Assert(root.Diamonds.ContainsKey(midpoint));
				if(!root.Diamonds[midpoint].Tetrahedra.Contains(node)) {
					root.Diamonds[midpoint].Tetrahedra.Add(node);
				}
			}
		}

		public static void RemoveFromDictionary(Root root, Node node) {
			for(int edgeNum = 0; edgeNum < 6; edgeNum++) {
				Vector3 A = node.Vertices[Lookups.EdgePairs[edgeNum,0]];
				Vector3 B = node.Vertices[Lookups.EdgePairs[edgeNum,1]];

				Vector3 midpoint = (A + B) / 2f;

				if(root.Diamonds.ContainsKey(midpoint)) {
					if(root.Diamonds[midpoint].Tetrahedra.Contains(node)) {
						root.Diamonds[midpoint].Tetrahedra.Remove(node);
					}

				}
			}
		}

		public static void CreateDiamond(Root root, Vector3 centralVertex, Node sourceTet) {
			Diamond d = new Diamond();
			d.Level = sourceTet.Depth / 3;
			d.Phase = sourceTet.Depth % 3;
			d.Tetrahedra = new List<Node>();
			d.CentralVertex = centralVertex;
			root.Diamonds.Add(centralVertex, d);
			
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
		public static void VisualizeDiamonds() {
			float scale = 20f;

			foreach(Diamond d in DebugDiamonds) {
				Vector3 SpineA = Vector3.down * 3;
				Vector3 SpineB = Vector3.up;

				Gizmos.color = Color.black;
				foreach(Node tet in d.Tetrahedra) {
					for(int i = 0; i < 6; i++) {
						Vector3 A = tet.Vertices[Lookups.EdgePairs[i, 0]];
						Vector3 B = tet.Vertices[Lookups.EdgePairs[i,1]];
						UnityEngine.Gizmos.DrawLine(A * scale, B * scale);
						if((A + B) / 2 == d.CentralVertex) {
							SpineA = A;
							SpineB = B;
						}
					}
				}

				Debug.Assert(SpineA != Vector3.down * 3);

				d.SpineA = SpineA;
				d.SpineB = SpineB;

				Gizmos.color = Color.green;
				UnityEngine.Gizmos.DrawLine(SpineA * scale, SpineB * scale);

				Gizmos.color = Color.blue;
				UnityEngine.Gizmos.DrawSphere(d.CentralVertex * scale, 0.2f * scale);

				Gizmos.color = Color.red;
				var parents = GetParentDiamondLocations(d);
				foreach(Vector3 p in parents) {
					UnityEngine.Gizmos.DrawSphere(p * scale, 0.2f * scale);
				}
			}
		}

		public static List<Vector3> GetParentDiamondLocations(Diamond d) {
			List<Vector3> parentDiamonds = new List<Vector3>();

			Vector3 v0 = d.SpineA;
			Vector3 vd = d.SpineB;

			for(int j = 1; j <= Lookups.DiamondParents[d.Phase]; j++) {
				Vector3 ej = Lookups.ej[j];
				Vector3 vx = v0 + Vector3.Dot((v0 - vd), ej) * Vector3.one;

				parentDiamonds.Add( (vx + v0) / 2);
			}

			return parentDiamonds;

		}
	}

}

// junk
/*
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

		// returns true if successful split
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
 */