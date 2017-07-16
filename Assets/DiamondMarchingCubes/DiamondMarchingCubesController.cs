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

	void Start () {
		colors = Utility.GetRandomColorArray(DMC.DebugAlgorithm.depth_);
		root = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));

		running = true;
		UnityEngine.Debug.Log("Root #children: " + root.Children.Length);
		//MeshifyRoot();
		extractedHexahedra = DMC.DebugAlgorithm.ExtractHexahedra(root.Children[0]);

		subHexahedronList = new List<DMC.Hexahedron>();
		SubdivideHexahedra();

		PrecomputedVolumeMesh = DMC.DebugAlgorithm.CreatePrecomputedVolumeMesh(subHexahedronList, root.Children[0]);

		UnityEngine.Debug.Log("Volume Mesh length: " + PrecomputedVolumeMesh.Count);

		CartesianUnitsTest = DMC.DebugAlgorithm.ConvertVolumeMeshToCartesian(PrecomputedVolumeMesh, root.Children[1]);
		PolyganiseRoot();
		MeshRootTetrahedrons();

		UnityEngine.Debug.Log("valid LEB scheme? " + DMC.DebugAlgorithm.CheckValid());
	}
	
	Color[] colors = { Color.red, Color.yellow, Color.white, Color.cyan, Color.green, Color.blue, Color.gray, Color.black };

	void PolyganiseRoot() {
		for(int i = 0; i < 2; i++) {
			PolyganiseNodeRecursive(root.Children[i]);
		}
	}

	void PolyganiseNodeRecursive(DMC.Node node, bool reverseWindingOrder = false) {
		if(node.IsLeaf) {
			Mesh m = DMC.DebugAlgorithm.PolyganiseNode(PrecomputedVolumeMesh, node, reverseWindingOrder);

			GameObject clone = Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);

			clone.transform.localScale = Vector3.one * 4f;

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
			DrawExtractedHexahedra();

			//Utility.DrawHierarchy(root);
			//DrawSubdividedHexahedra();
			//DrawCartesianUnitsTest();
		}
	}

	void DrawExtractedHexahedra() {
		Gizmos.color = Color.green;
		//int i = 1;
		for(int i = 0; i < 4; i++) {
			DrawHexahedron(extractedHexahedra[2]);
		}
	}

	void DrawSubdividedHexahedra() {
		Gizmos.color = Color.blue;
		for(int i = 0; i < subHexahedronList.Count; i++) {
			DrawHexahedron(subHexahedronList[i]);
		}
	}

	void DrawCartesianUnitsTest() {
		Gizmos.color = Color.magenta;
		float scale = 4f;
		foreach(Strucs.GridCell u in CartesianUnitsTest) {
			for(int i = 0; i < 12; i++) {
				Gizmos.DrawLine(u.Points[DMC.Lookups.HexahedronEdgePairs[i, 0]].Position * scale, u.Points[DMC.Lookups.HexahedronEdgePairs[i, 1]].Position * scale);
			}
			//Debug.Log("Vertex 0: " + u.CartesianCoords[0]);
		}
	}

	void SubdivideHexahedra() {
		for(int i = 0; i < 4; i++) {
			subHexahedronList.AddRange(DMC.DebugAlgorithm.GenerateSubdividedHexahedronList(extractedHexahedra[i], 2));

		}
	}


	void MeshRootTetrahedrons() {
		for(int i = 0; i < root.Children.Length; i++) {
			RecursiveMeshifyTetrahedron(root.Children[i], 8, new Vector3(2 * i - 30f, 0, 0));
		}
	}

	void RecursiveMeshifyTetrahedron(DMC.Node node, int n, Vector3 offset) {
		if(n <= 0) return;
		MeshifyTetrahedron(node, offset);
		for(int i = 0; i < 2; i++) {
			RecursiveMeshifyTetrahedron(node.Children[i], n - 1, new Vector3(0f, 0f, 2f) + offset);
		}
	}

	void MeshifyTetrahedron(DMC.Node node, Vector3 offset) {
		UnityEngine.Mesh m = new Mesh();

		Vector3[] verts = new Vector3[4];
		for(int i = 0; i < 4; i++) {
			verts[i] = node.Vertices[i];
		}


		if(node.ReverseWindingOrder) {
			UnityEngine.Debug.Log("Reverse winding order true.");
			Vector3 a = verts[1];
			verts[1] = verts[0];
			verts[0] = a;
			/*Vector3 b = 1 * vertices[3];
			vertices[3] = vertices[2];
			vertices[2] = b; */
		}

		List<Vector3> vertices = new List<Vector3>();
		for(int j = 0; j < DMC.Lookups.TetrahedronFaces.GetLength(0); j++) {
			for(int w = 0; w < 3; w++) {
				vertices.Add(verts[DMC.Lookups.TetrahedronFaces[j, w]]);
			}
		}

		m.SetVertices(vertices);

		int[] triangles = new int[vertices.Count];
		for(int j = 0; j < vertices.Count; j++) triangles[j] = j;

		m.triangles = triangles;
		m.RecalculateNormals();

		GameObject clone = Instantiate(MeshPrefab, offset, Quaternion.identity);

		clone.transform.localScale = Vector3.one * 1.2f;

		MeshFilter mf = clone.GetComponent<MeshFilter>();
		mf.mesh = m;
		clone.GetComponent<Transform>().SetParent(this.GetComponent<Transform>());
		TestMeshes.Add(clone);
	}

	void DrawHexahedron(DMC.Hexahedron hexahedron) {
		float scale = 4f;
		for(int i = 0; i < 8; i++) {
			Vector3 v = hexahedron.Vertices[i];
			//Gizmos.color = Color.white;
			//Gizmos.DrawCube(v * scale, Vector3.one * 0.05f * scale);
			//UnityEditor.Handles.Label (new Vector3(v.x, v.y + 0.07f, v.z) * scale, "" + i);
		}
		for(int i = 0; i < 12; i++) {
			Gizmos.DrawLine(hexahedron.Vertices[DMC.Lookups.HexahedronEdgePairs[i, 0]] * scale, hexahedron.Vertices[DMC.Lookups.HexahedronEdgePairs[i, 1]] * scale);
		}
		/*for(int i_ = 0; i_ < hexahedron.BaseFaces.Length; i_++) {
			DMC.Face f = hexahedron.BaseFaces[i_];
			Gizmos.color = Color.green;
			for(int j = 0; j < f.Vertices.Length; j++) {
				Vector3 v = f.Vertices[j];
				Gizmos.DrawCube(v * (scale * 1.1f), Vector3.one * 0.05f * scale);
			}
		}*/
	}

	void Update () {
		if(!running) return;
		if(Input.GetKey("r")) {
			DMC.DebugAlgorithm.FindNewCorrectTetSplittingTable();

			for(int i = 0; i < TestMeshes.Count; i++) {
				Destroy(TestMeshes[i], 0);
			}
			TestMeshes = new List<GameObject>();
			root = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));

			MeshRootTetrahedrons();
		}

	}
}
