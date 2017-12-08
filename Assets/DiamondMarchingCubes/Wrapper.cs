using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DMC {
	public class Wrapper {
		public Root Hierarchy;
		public Hashtable UnityObjects;
		public List<Node> LoadedLeafNodes;

		private List<DMC.MCBarycentricUnit> PrecomputedVolumeMesh;
		private GameObject MeshPrefab;
		private Transform Parent;
		private float WorldSize;
		private int MaxDepth;

		public Wrapper(float worldSize, Vector3 startingPosition, Transform parent, GameObject meshPrefab, int maxDepth) {
			Parent = parent;
			MeshPrefab = meshPrefab;
			WorldSize = worldSize;
			MaxDepth = maxDepth;
			LoadedLeafNodes = new List<Node>();
			UnityObjects = new Hashtable();

			InitializeHierarchy();
			//Update(startingPosition);
		}

		public void InitializeHierarchy() {
			Hierarchy = DMC.DebugAlgorithm.CreateHierarchy(); //DMC.DebugAlgorithm.CreateTestHierarchy(); //DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));
			PrecomputedVolumeMesh = DMC.DebugAlgorithm.CreatePrecomputedVolumeMesh(Hierarchy.RootDiamond.Tetrahedra[0]);
		}

		public void Update(Vector3 viewerPosition) {
			//DMC.DebugAlgorithm.LoopAdapt(Hierarchy, viewerPosition, (Node node) => LinFindTargetDepth(viewerPosition, node));
			//DMC.DebugAlgorithm.LoopMakeConforming(Hierarchy, 4);
			//DMC.DebugAlgorithm.CheckSplit(Hierarchy)

			//DMC.DebugAlgorithm.Adapt(Hierarchy, viewerPosition);

			Meshify();
		}

		public void MakeConforming() {
			//DMC.DebugAlgorithm.LoopMakeConforming(Hierarchy, 1);
			Meshify();
		}

		public void Meshify() {
			List<Node> newLeafNodes = new List<Node>();
			for(int i = 0; i < 6; i++) {
				PopulateLeafNodeList(Hierarchy.RootDiamond.Tetrahedra[i], newLeafNodes);
			}
			// 1. delete the nodes that were deleted
				// items in oldlist that are not in newlist
			foreach(Node n in LoadedLeafNodes.Except(newLeafNodes)) {
				Object.Destroy((GameObject)UnityObjects[n.Number]);
				UnityObjects.Remove(n.Number);
			}
			// 2. create the nodes that were created
				// items in newlist that are not in oldlist
			foreach(Node n in newLeafNodes.Except(LoadedLeafNodes)) {
				MeshifyNode(n);
			}

			LoadedLeafNodes = newLeafNodes;
		}

		public void PopulateLeafNodeList(Node node, List<Node> leafNodes) {
			if(node.IsLeaf) {
				leafNodes.Add(node);
			}
			else {
				for(int i = 0; i < node.Children.Length; i++) {
					PopulateLeafNodeList(node.Children[i], leafNodes);
				}
			}
		}

		public void MeshifyNode(DMC.Node node) {
			GameObject clone = Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
			Color c = Utility.SinColor(node.Depth * 3f);
			clone.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.9f);
			clone.transform.localScale = Vector3.one * WorldSize;
			clone.name = "Node " + node.Number + ", Depth " + node.Depth;

			MeshFilter mf = clone.GetComponent<MeshFilter>();
			mf.mesh = DMC.DebugAlgorithm.PolyganiseNode(PrecomputedVolumeMesh, node);
			clone.GetComponent<Transform>().SetParent(Parent);

			UnityObjects[node.Number] = clone;
		}

		public void DrawGizmos() {
			//DebugAlgorithm.VisualizeDiamonds();
		}
	}
}