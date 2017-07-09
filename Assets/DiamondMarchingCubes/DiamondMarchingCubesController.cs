using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamondMarchingCubesController : MonoBehaviour {
	bool running = false;

	DMC.Root root;

	void Start () {
		colors = Utility.GetRandomColorArray(DMC.DebugAlgorithm.depth_);
		root = DMC.DebugAlgorithm.Run(new Vector3(0, 0, 0));
		running = true;
		UnityEngine.Debug.Log("Root #children: " + root.Children.Length);
	}
	
	Color[] colors = { Color.red, Color.yellow, Color.white, Color.cyan, Color.green, Color.blue, Color.gray, Color.black };


	void OnDrawGizmos() {
		if(running) {
			Utility.DrawHierarchy(root);
		}
	}

	void Update () {
		
	}
}
