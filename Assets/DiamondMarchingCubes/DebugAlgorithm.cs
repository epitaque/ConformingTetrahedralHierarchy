using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
//coarsen
namespace DMC {
	public static class DebugAlgorithm {
		public static List<Diamond> DebugDiamonds = new List<Diamond>();

		public static uint mostRecentTetrahedronCount = 0;

		public static FindTargetDepth finder = DefaultFindTargetDepth;

		public static Root Run(Vector3 PlayerLocation) {
			return CreateHierarchy();
		}

		public static Root CreateHierarchy() {
			Root root = new Root();

			root.Diamonds = new Dictionary<Vector3, Diamond>();
			root.Nodes = new Dictionary<uint, Node>();

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
				//node.BoundingSphere = Utility.CalculateBoundingSphere(node);
			}

			Diamond rootDiamond = new Diamond();
			rootDiamond.Tetrahedra = new List<Node>(rootNodes);
			root.RootDiamond = rootDiamond;

			return root;
		}

		public static Root CreateTestHierarchy() {
			Root root = CreateHierarchy();

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
		public static float DefaultFindTargetDepth(Node node, Vector3 position) {
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

		public static int FindMinDepth(Diamond d) {
			int min = int.MaxValue;
			foreach(Node n in d.Tetrahedra) {
				int de = n.Depth;
				if(de < min) {
					min = de;
				}
			}
			if(min == int.MaxValue) return 0;
			return min;
		}

		private static ulong frameN = 0;
		private static ulong splitDiamondsCalled = 0;
		private static ulong mergeNodesCalled = 0;
		public static void Adapt(Root root, Vector3 position) {
			frameN++;
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			System.Diagnostics.Stopwatch swR = new System.Diagnostics.Stopwatch();
			sw.Start(); swR.Start();
			Refine(root, position);
			swR.Stop();
			Coarsen(root, position);
			sw.Stop(); swR.Stop();
			string prntMessage = "Adapt took " + sw.ElapsedMilliseconds + "ms (" + swR.ElapsedMilliseconds + "ms to refine, " + (sw.ElapsedMilliseconds - swR.ElapsedMilliseconds) + " to coarsen.";
			prntMessage += " ||| mergeNodesCalled: " + mergeNodesCalled + ", splitDiamondsCalled: " + splitDiamondsCalled;
			Debug.Log(prntMessage);
			mergeNodesCalled = 0;
			splitDiamondsCalled = 0;
		}


		public static void Coarsen(Root root, Vector3 position) {
			mergeNodesCalled = 0;
			for(int i = 0; i < 6; i++) {
				Coarsen(root, root.RootDiamond.Tetrahedra[i], position);
			}
			Debug.Log("Coarsen called. MergeTetrahedron called " + mergeNodesCalled + " times.");
		}

		public static void Coarsen(Root root, Node node, Vector3 position) {
			if(node == null || node.IsDeleted) return;
			Diamond d = root.Diamonds[node.CentralVertex];
			if(d.lastFrameUpdated == frameN) {
				//return;
			}
			d.lastFrameUpdated = frameN;
			int maxTargetDepth = (int)FindMaxTargetDepth(d, position);
			int minDepth = FindMinDepth(d);
			if( (minDepth) > maxTargetDepth) { // node needs to have higher depth (be more refined)
				MergeTetrahedron(root, node);
			}

			if(!node.IsLeaf) {
				for(int i = 0; i < 2; i++) {
					Coarsen(root, node.Children[i], position);
				}
			}
		}

		public static void Refine(Root root, Vector3 position) {
			splitDiamondsCalled = 0;
			for(int i = 0; i < 6; i++) {
				CheckSplit(root, position, root.RootDiamond.Tetrahedra[i]);
			}
			Debug.Log("Refine called. SplitDiamond called " + splitDiamondsCalled + " times.");
		}

		public static void CheckSplit(Root root, Vector3 position, Node node) {
			if(node.IsLeaf) {
				if(node.Depth < finder(node, position)) {
					SplitDiamond(root, root.Diamonds[node.CentralVertex]);

					CheckSplit(root, position, node.Children[0]);
					CheckSplit(root, position, node.Children[1]);
				}
			}
			else {
				CheckSplit(root, position, node.Children[0]);
				CheckSplit(root, position, node.Children[1]);
			}
		}

		public static void SplitDiamond(Root root, Diamond diamond) {
			splitDiamondsCalled++;
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
			mergeNodesCalled++;
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
				//child.BoundingSphere = Utility.CalculateBoundingSphere(child);
				node.Children[i] = child;
				AddToDictionary(root, child);
			}
		}

		public static void AddToDictionary(Root root, Node node) {
			for(int edgeNum = 0; edgeNum < 6; edgeNum++) {
				Vector3 A = node.Vertices[Lookups.EdgePairs[edgeNum,0]];
				Vector3 B = node.Vertices[Lookups.EdgePairs[edgeNum,1]];

				Vector3 midpoint = (A + B) / 2f;

				if(!root.Diamonds.ContainsKey(midpoint)) {
					Diamond d = new Diamond();
					d.CentralVertex = midpoint;
					d.Tetrahedra = new List<Node>();
					root.Diamonds.Add(midpoint, d);
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