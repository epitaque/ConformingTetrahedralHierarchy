using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DMC2 {
	public static class DebugAlgorithm2 {
		public static List<Diamond> DebugDiamonds = new List<Diamond>();	
		public delegate void AfterSplit(Vector3Int parent, Vector3Int child);
		public static uint mostRecentTetrahedronCount = 0;

		public static readonly int offsetMultiplier = (int)System.Math.Pow(2, 25);

		public static Root CreateHierarchy(Vector3 PlayerLocation) {
			Root root = new Root();

			root.Diamonds = new Dictionary<Vector3Int, Diamond>();
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
				node.HVertices = new Vector3Int[4];
				node.Vertices = new Vector3Int[4];
				for(int j = 0; j < 4; j++) {
					Vector3 pos = DMC.Lookups.StartingVerts[DMC.Lookups.RootTetrahedrons[i,j]];
					Vector3Int iPos = new Vector3Int((int)pos.x * offsetMultiplier, (int)pos.y * offsetMultiplier, (int)pos.z * offsetMultiplier);
					Debug.Log("iPos: " + iPos);

					node.HVertices[j] = iPos;
					node.Vertices[j] = iPos;
				}
				if(i % 2 == 1) {
					node.Vertices[0] = node.HVertices[1];
					node.Vertices[1] = node.HVertices[0];
					node.ReversedWindingOrder = true;
				}
				node.BoundRadius = Vector3Int.Distance(node.HVertices[0], node.HVertices[1]) / 2f;
				node.CentralVertex = FindMidpoint(node.Vertices[0], node.Vertices[1]);
				AddToDictionary(root, node);
				rootNodes[i] = node;
				//node.BoundingSphere = Utility.CalculateBoundingSphere(node);
			}

			Diamond rootDiamond = new Diamond();
			rootDiamond.Tetrahedra = new List<Node>(rootNodes);
			rootDiamond.Phase = 0;
			rootDiamond.Children = new List<Diamond>(6);
			rootDiamond.Level = 0;
			root.RootDiamond = rootDiamond;

			return root;
		}

		public static Vector3Int FindMidpoint(Vector3Int A, Vector3Int B) {
			Vector3Int sum = A + B;
			return new Vector3Int(sum.x/2, sum.y/2, sum.z/2);
		}

		public static Root CreateTestHierarchy(AfterSplit fn) {
			Root root = CreateHierarchy(new Vector3(0, 0, 0));

			//DebugDiamonds.Add(root.RootDiamond);

			//SplitDiamond(root, root.RootDiamond);

			for(int i = 0; i < 6; i++) {
				Node tet = root.RootDiamond.Tetrahedra[i];
				CheckSplit(root, new Vector3(0, 0, 0), tet, fn);
			}

			IEnumerable<Diamond> ds = root.Diamonds.Values.Where(d => d.Phase == 1);

			foreach(Diamond d in root.Diamonds.Values.Where(d => d.Phase == 2 && d.Tetrahedra.Count == 8)) {
				DebugDiamonds.Add(d);
			}

			return root;
		}

		public delegate float FindTargetDepth(Node node);

		public static void CheckSplit(Root root, Vector3 position, Node node, AfterSplit fn) {
			if(node.Depth < 9) {
				SplitDiamond(root, root.Diamonds[node.CentralVertex], fn);

				CheckSplit(root, position, node.Children[0], fn);
				CheckSplit(root, position, node.Children[1], fn);
			}
		}

		public static void SplitDiamond(Root root, Diamond diamond, AfterSplit fn) {
			//Debug.Log("Splitting diamond.");
			// Create the children diamonds if they don't exist

			for(int tetNum = 0; tetNum < diamond.Tetrahedra.Count; tetNum++) {
				Node tet = diamond.Tetrahedra[tetNum];

				//Debug.Log("Diamond cv: " + diamond.CentralVertex);

				if(tet.Children == null && tet.CentralVertex != diamond.CentralVertex) {
					fn(diamond.CentralVertex, tet.CentralVertex);
					SplitDiamond(root, root.Diamonds[tet.CentralVertex], fn);
				}

				if(tet.Children == null) {
					//Debug.Log("Split node called.");
					SplitNode(root, tet);
				}
			}
		}


		public static List<Diamond> FindParentDiamonds(Root root, Diamond diamond) {
			return null;
		}

		public static void TryAddDiamond(Root root, Vector3Int offset, Diamond parentDiamond) {
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
			Vector3Int[] CachedVertices = new Vector3Int[5];
			for(int i = 0; i < 4; i++) {
				CachedVertices[i] = node.HVertices[i];
			}
			CachedVertices[4] = node.CentralVertex;
			for(int i = 0; i < 2; i++) {
				Node child = new Node();
				child.Number = mostRecentTetrahedronCount;
				mostRecentTetrahedronCount++;
				root.Nodes.Add(child.Number, child);
				child.HVertices = new Vector3Int[4];
				child.Vertices = new Vector3Int[4];
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
					child.HVertices[j] = CachedVertices[DMC.Lookups.TetrahedronVertexOrder[node.TetrahedronType, i, j]];
					child.Vertices[j] = child.HVertices[j];
				}
				if(child.ReversedWindingOrder) {
					child.Vertices[0] = child.HVertices[1];
					child.Vertices[1] = child.HVertices[0];
				}
				child.BoundRadius = Vector3.Distance(child.HVertices[0], child.HVertices[1]) / 2f;
				child.CentralVertex = FindMidpoint(child.HVertices[0], child.HVertices[1]);
				node.Children[i] = child;
				AddToDictionary(root, child);
			}
		}


		public static bool IsOutOfBounds(Vector3 A) {
			return A.x < -1 || A.y < -1 || A.z < -1 || A.x > 1 || A.y > 1 || A.z > 1;
		}

		public static void AddToDictionary(Root root, Node node) {
			// Update FaceToNodeList
			/*for(int i = 0; i < 4; i++) {
				Vector3 centroid = FindCentroid(node, i);
				if(!root.FaceToNodeList.ContainsKey(centroid)) {
					root.FaceToNodeList.Add(centroid, new List<Node>());
				}
				root.FaceToNodeList[centroid].Add(node);
			}
			if(!root.CVToNodeList.ContainsKey(node.CentralVertex)) {
				root.CVToNodeList.Add(node.CentralVertex, new List<Node>());
			}*/
			for(int edgeNum = 0; edgeNum < 6; edgeNum++) {
				Vector3Int A = node.Vertices[DMC.Lookups.EdgePairs[edgeNum,0]];
				Vector3Int B = node.Vertices[DMC.Lookups.EdgePairs[edgeNum,1]];

				Vector3Int midpoint = FindMidpoint(A, B);

				if(!root.Diamonds.ContainsKey(midpoint)) {
					//root.Diamonds.Add(midpoint, new Diamond());
					CreateDiamond(root, midpoint, node);
				}
				Debug.Assert(root.Diamonds.ContainsKey(midpoint));
				if(!root.Diamonds[midpoint].Tetrahedra.Contains(node)) {
					root.Diamonds[midpoint].Tetrahedra.Add(node);
				}
				

			}
			//root.Diamonds[node.CentralVertex].Tetrahedra.Add(node);
		}

		public static void CreateDiamond(Root root, Vector3Int centralVertex, Node sourceTet) {
			Diamond d = new Diamond();
			d.Level = sourceTet.Depth / 3;
			d.Phase = sourceTet.Depth % 3;
			d.Tetrahedra = new List<Node>();
			d.CentralVertex = centralVertex;
			root.Diamonds.Add(centralVertex, d);
			
		}

		public static Hexahedron[] ExtractHexahedra(Node node) {
			// step 1: get all the faces of the tetrahedron
			Face[] faces = new Face[4];
			for(int i = 0; i < 4; i++) {
				faces[i] = new Face();
				faces[i].Vertices = new Vector3[3];
				Vector3 sum = Vector3.zero;
				for(int j = 0; j < 3; j++) {
					faces[i].Vertices[j] = node.Vertices[DMC.Lookups.TetrahedronFaces[i, j]];
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
					hexfaces[j] = faces[DMC.Lookups.TetrahedronFaces[i, j]];
				}
				for(int j = 0; j < 3; j++) {
					hexahedra[i].Vertices[j + 1] = hexfaces[j].Centroid;

					// for each face combination, find the midpoint of the shared edge
					Face face1 = hexfaces[DMC.Lookups.FaceCombinations[j, 0]];
					Face face2 = hexfaces[DMC.Lookups.FaceCombinations[j, 1]];
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
					CachedVertices[j] = hexahedra[i].Vertices[DMC.Lookups.HexahedronVertexReorder[j]];
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
					sum += hex.Vertices[DMC.Lookups.HexahedronFaces[i, j]];
				}
				CachedVertices[1 + i] = sum / 4;
			}

			// 7-18
			for(int i = 0; i < 12; i++) {
				sum = Vector3.zero;
				for(int j = 0; j < 2; j++) {
					sum += hex.Vertices[DMC.Lookups.HexahedronEdgePairs[i, j]];
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
					SubdividedHexahedra[i].Vertices[j] = CachedVertices[DMC.Lookups.HexahedronSubHexahedra[i, j]];
				}
			}

			hex.Children = SubdividedHexahedra;
			hex.IsLeaf = false;
		}

		public static List<MCBarycentricUnit> ConvertHexahedraToBarycentric(List<Hexahedron> Hexahedra, Node Tetrahedron) {
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
		public static List<MCBarycentricUnit> CreatePrecomputedVolumeMesh(Node Tetrahedron) {
			Hexahedron[] rootHexahedra = ExtractHexahedra(Tetrahedron);
			List<Hexahedron> subdividedHexahedra = new List<Hexahedron>();
			for(int i = 0; i < 4; i++) {
				subdividedHexahedra.AddRange(GenerateSubdividedHexahedronList(rootHexahedra[i], 2));
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
						Vector3 A = tet.Vertices[DMC.Lookups.EdgePairs[i, 0]];
						Vector3 B = tet.Vertices[DMC.Lookups.EdgePairs[i,1]];
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
				UnityEngine.Gizmos.DrawSphere((Vector3)d.CentralVertex * scale, 0.2f * scale);

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

			for(int j = 1; j <= DMC.Lookups.DiamondParents[d.Phase]; j++) {
				Vector3 ej = DMC.Lookups.ej[j];
				Vector3 vx = v0 + Vector3.Dot((v0 - vd), ej) * Vector3.one;

				parentDiamonds.Add( (vx + v0) / 2);
			}

			return parentDiamonds;

		}
	}

}