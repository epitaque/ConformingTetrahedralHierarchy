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
		private AdaptResult Result;
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

			Result = new AdaptResult();
			Result.CoarsenList = new Node[256];
			Result.SplitList = new Node[256];
			UnityObjects = new Hashtable();

			InitializeHierarchy();
			Update(startingPosition);
		}
		public float FindTargetDepth(Vector3 position, Node node) {
			float dist = Mathf.Clamp(Vector3.Distance((node.CentralVertex * WorldSize), position) - (node.BoundRadius * 1.3f * WorldSize), 0, float.MaxValue);

			float targetDepth = 8.32f - 0.683f * Mathf.Log(dist + 1.5f, 2.718f);//(6f / Mathf.Log((dist / 11f) + 1.2f, 10f));
			float clamped = Mathf.Clamp(targetDepth, 1f, (float)MaxDepth);
			return (int)clamped;
		}
		public float FindTargetDepth2(Vector3 position, Node node) {
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
			Hierarchy = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));
			PrecomputedVolumeMesh = DMC.DebugAlgorithm.CreatePrecomputedVolumeMesh(Hierarchy.Children[0]);
		}

		public void Update(Vector3 viewerPosition) {
			DMC.DebugAlgorithm.LoopAdapt(Hierarchy, viewerPosition, (Node node) => FindTargetDepth(viewerPosition, node), Result);
			Meshify();
		}

		public void Meshify() {
			List<Node> newLeafNodes = new List<Node>();
			for(int i = 0; i < 6; i++) {
				PopulateLeafNodeList(Hierarchy.Children[i], newLeafNodes);
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
	}
}