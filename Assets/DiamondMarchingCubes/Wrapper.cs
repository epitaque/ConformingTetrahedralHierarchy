using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DMC {
	public class Wrapper {
		Root Hierarchy;
		List<DMC.MCBarycentricUnit> PrecomputedVolumeMesh;
		AdaptResult Result;

		GameObject MeshPrefab;
		Transform Parent;

		float WorldSize;
		int MaxDepth;

		Hashtable UnityObjects;

		List<Node> LoadedLeafNodes;

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
				float dist = Mathf.Clamp(Vector3.Distance((node.CentralVertex * WorldSize), position) - (node.BoundRadius * WorldSize), 0, float.MaxValue);

				float targetDepth = (6f / Mathf.Log((dist / 11f) + 1.2f, 10f));
				float clamped = Mathf.Clamp(targetDepth, 1f, (float)MaxDepth);
				return (int)clamped;
		}

		public void InitializeHierarchy() {
			Hierarchy = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));
			PrecomputedVolumeMesh = DMC.DebugAlgorithm.CreatePrecomputedVolumeMesh(Hierarchy.Children[0]);
		}

		public void Update(Vector3 viewerPosition) {
			DMC.DebugAlgorithm.Adapt(Hierarchy, viewerPosition, (Node node) => FindTargetDepth(viewerPosition, node), Result);
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
			clone.GetComponent<MeshRenderer>().material.color = Utility.SinColor(node.Depth * 3f);
			clone.transform.localScale = Vector3.one * WorldSize;

			MeshFilter mf = clone.GetComponent<MeshFilter>();
			mf.mesh = DMC.DebugAlgorithm.PolyganiseNode(PrecomputedVolumeMesh, node);
			clone.GetComponent<Transform>().SetParent(Parent);

			UnityObjects[node.Number] = clone;
		}
	}
}