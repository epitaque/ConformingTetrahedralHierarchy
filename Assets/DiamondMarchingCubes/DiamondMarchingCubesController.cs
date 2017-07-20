using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondMarchingCubesController : MonoBehaviour {
	bool running = false;

	public GameObject MeshPrefab;
	DMC.Root root;

	DMC.Hexahedron[] extractedHexahedra;
	List<DMC.Hexahedron> subHexahedronList;

	List<DMC.MCBarycentricUnit> PrecomputedVolumeMesh;

	List<Strucs.GridCell> CartesianUnitsTest;


	List<GameObject> TestMeshes = new List<GameObject>();

	Color[] colors;

	void Start () {
		colors = Utility.GetRandomColorArray(DMC.DebugAlgorithm.depth_);
		root = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));

		running = true;
		UnityEngine.Debug.Log("Root #children: " + root.Children.Length);
		extractedHexahedra = DMC.DebugAlgorithm.ExtractHexahedra(root.Children[0]);
		DMC.DebugAlgorithm.SplitTreeAtPosition(root, new Vector3(0, 0, 0), 4f, 1024, 10);

		subHexahedronList = new List<DMC.Hexahedron>();
		SubdivideHexahedra();

		PrecomputedVolumeMesh = DMC.DebugAlgorithm.CreatePrecomputedVolumeMesh(subHexahedronList, root.Children[0]);

		//CartesianUnitsTest = DMC.DebugAlgorithm.ConvertVolumeMeshToCartesian(PrecomputedVolumeMesh, root.Children[1]);
		PolyganiseRoot();

		UnityEngine.Debug.Log("valid LEB scheme? " + root.IsValid);
	}
	
 	Color[] colors2 = { new Color(0,1,0,1), new Color(1,0,0,1), new Color(1,1,1,1), new Color(0,0,1,1),  new Color(1,1,0,1), new Color(0, 0, 0, 1)};

	void SubdivideHexahedra() {
		for(int i = 0; i < 4; i++) {
			subHexahedronList.AddRange(DMC.DebugAlgorithm.GenerateSubdividedHexahedronList(extractedHexahedra[i], 2));

		}
	}

	void PolyganiseRoot() {
		for(int i = 0; i < 6; i++) {
			PolyganiseNodeRecursive(root.Children[i]);
		}
	}



	void PolyganiseNodeRecursive(DMC.Node node) {
		if(node.IsLeaf) {
			Mesh m = DMC.DebugAlgorithm.PolyganiseNode(PrecomputedVolumeMesh, node);
			Color c = Utility.SinColor(node.Depth * 3f); //colors2[ (node.Depth % 6) ];


			/*for(int i = 0; i < m.vertices.Length; i++) {
				m.colors[i] = c;
			}*/

			GameObject clone = Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);

			clone.GetComponent<MeshRenderer>().material.color = c;

			//clone.transform.(-0.5f, -0.5f, -0.5f);
			clone.transform.localScale = Vector3.one * 256f;

			MeshFilter mf = clone.GetComponent<MeshFilter>();
			mf.mesh = m;
			clone.GetComponent<Transform>().SetParent(this.GetComponent<Transform>());

		}
		else {
			foreach(DMC.Node n in node.Children) {
				PolyganiseNodeRecursive(n);
			}
		}
	}

	void OnDrawGizmos() {
		if(running) {
			Utility.DrawNode(root.Children[0], 4f);
		}
	}

	void Update () {
		if(!running) return;
		if(Input.GetKey("r")) {
			for(int i = 0; i < TestMeshes.Count; i++) {
				Destroy(TestMeshes[i], 0);
			}
			TestMeshes = new List<GameObject>();
			root = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));
		}

	}
}
