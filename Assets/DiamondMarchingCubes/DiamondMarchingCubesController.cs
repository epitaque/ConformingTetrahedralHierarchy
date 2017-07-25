using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondMarchingCubesController : MonoBehaviour {
	bool running = false;

	public GameObject MeshPrefab;
	public GameObject Viewer;

	DMC.Wrapper DMCWrapper;

	void Start () {
		running = true;

		DMCWrapper = new DMC.Wrapper(256f, new Vector3(0, 0, 0), this.GetComponent<Transform>(), MeshPrefab, 10);
		DMCWrapper.Meshify();
	}

	void Update() {
		if(Input.GetKeyDown(KeyCode.R)) {
			DMCWrapper.Update(Viewer.GetComponent<Transform>().position);
		}
	}
}
