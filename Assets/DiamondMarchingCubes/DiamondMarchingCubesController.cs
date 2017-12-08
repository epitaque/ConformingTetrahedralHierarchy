using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DiamondMarchingCubesController : MonoBehaviour {
	bool running = false;

	public GameObject MeshPrefab;
	public GameObject Viewer;
	public GameObject Console;

	public int MaxDepth = 8;

	DMC.Wrapper DMCWrapper;
	DMC.Debugger Debugger;

	void Start () {
		running = true;

		DMCWrapper = new DMC.Wrapper(256f, new Vector3(0, 0, 0), this.GetComponent<Transform>(), MeshPrefab, MaxDepth);
		DMCWrapper.Meshify();

		Debugger = new DMC.Debugger(256f, DMCWrapper, MeshPrefab, Console.GetComponent<Console>(), Viewer.GetComponent<Transform>());
		Console.GetComponent<Console>().Debugger = Debugger;
	}

	void Update() {
		DMCWrapper.Update(Viewer.GetComponent<Transform>().position);
		if(Input.GetKeyDown(KeyCode.R)) {
			//DMCWrapper.Update(Viewer.GetComponent<Transform>().position);
		}
		if(Input.GetKeyDown(KeyCode.M)) {
			//DMCWrapper.MakeConforming();
		}
		if(Input.GetKeyDown(KeyCode.Return)) {
			//Console.ProcessCommand(ConsoleInputString.GetComponent<)
		}
	}

	void OnDrawGizmos() {
		if(running) {
			Debugger.DrawGizmos();
			DMCWrapper.DrawGizmos();
		}
	}

}
