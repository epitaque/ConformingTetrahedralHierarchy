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
			Update(startingPosition);
		}
		public float FindTargetDepth(Vector3 position, Node node) {
			float dist = Mathf.Clamp(Vector3.Distance((node.BoundingSphere.Center), position/WorldSize) - (node.BoundRadius), 0, float.MaxValue) * WorldSize;

			float targetDepth = 8.32f - 0.683f * Mathf.Log(dist + 1.5f, 2.718f);//(6f / Mathf.Log((dist / 11f) + 1.2f, 10f));
			float clamped = Mathf.Clamp(targetDepth, 1f, (float)MaxDepth);
			return (int)clamped;
		}
		public bool LinShouldSplit(Vector3 position, Node node) {
			if (node.Depth < 8f) {
                return true;
			}
            if (node.Depth < WorldSize * 4f / 3f + 8f) {
                float a = 1.0f;
                float b = 2.0f;
                float c = 0.7f;
                float r = node.BoundingSphere.Radius;
				float d = Mathf.Pow(Vector3.Distance(position / WorldSize, node.BoundingSphere.Center), 2f) / r - b;

                bool split = d * a < c;
                return split;
            }
			return false;
		}

		public float LinFindTargetDepth(Vector3 position, Node node) {
			if(LinShouldSplit(position, node) && node.Depth < MaxDepth) {
				return node.Depth + 1f;
			}
			else if(node.Parent != null && !LinShouldSplit(position, node.Parent) && node.Depth > 2f) {
				return node.Depth - 1f;
			}
			return node.Depth;

		}
		public float FindTargetDepth4(Vector3 position, Node node) {
			float targetDepth = 0;
			position = new Vector3(0, 0, 0);
			float dist = Vector3.Distance(node.BoundingSphere.Center, position) - node.BoundingSphere.Radius;
			dist = Mathf.Clamp(dist, 0, float.MaxValue);
			if(dist < 0.1f) {
				targetDepth = 6;
			}
			UnityEngine.Debug.Log(node.Number + ": Node depth: " + node.Depth + ", target depth: " + targetDepth + 
			", distance: " + (dist + node.BoundRadius) + " node cv: " + node.CentralVertex + 
			"bound radius: " + node.BoundingSphere.Radius + "bounding sphere center: " + node.BoundingSphere.Center);
			return targetDepth;
		}
		public float FindTargetDepth3(Vector3 position, Node node) {
			float targetDepth = 0;
			float dist = Vector3.Distance(position, node.BoundingSphere.Center) - node.BoundingSphere.Radius * 1.1f;
			dist = Mathf.Clamp(dist, 0, float.MaxValue);
			targetDepth = (6f / Mathf.Log((dist / 11f) + 1.2f, 10f));
			float clamped = Mathf.Clamp(targetDepth, 1f, (float)MaxDepth);
			return (int)clamped;
		}
		public void InitializeHierarchy() {
			Hierarchy = DMC.DebugAlgorithm.CreateTestHierarchy();//DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));
			PrecomputedVolumeMesh = DMC.DebugAlgorithm.CreatePrecomputedVolumeMesh(Hierarchy.RootDiamond.Tetrahedra[0]);
		}

		public void Update(Vector3 viewerPosition) {
			//DMC.DebugAlgorithm.LoopAdapt(Hierarchy, viewerPosition, (Node node) => LinFindTargetDepth(viewerPosition, node));
			//DMC.DebugAlgorithm.LoopMakeConforming(Hierarchy, 4);
			//DMC.DebugAlgorithm.CheckSplit(Root)

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