using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace DMC {
    public class Debugger {
        private Wrapper DMCWrapper;
        private Console Console;
        private float WorldSize;
        private List<GameObject> PreviouslyHighlightedObjects;
        private List<Line> Gizmos;
        private List<GameObject> NodeMeshes;
        private List<Sphere> HighlightedBoundingSpheres;
        private GameObject MeshPrefab;

		private Transform Camera;

        class Line {
            public Vector3 A;
            public Vector3 B;
            public Color Color;
        }


        public Debugger(float WorldSize, Wrapper DMCWrapper, GameObject MeshPrefab, Console Console, Transform Camera) {
            this.DMCWrapper = DMCWrapper;
            this.Console = Console;
            this.PreviouslyHighlightedObjects = new List<GameObject>();
            this.Gizmos = new List<Line>();
            this.NodeMeshes = new List<GameObject>();
            this.HighlightedBoundingSpheres = new List<Sphere>();
            this.WorldSize = WorldSize;
            this.MeshPrefab = MeshPrefab;
			this.Camera = Camera;
        }

        public Node StringToNode(string nodeNumberStr) {
            uint nodeNumber;
            if(uint.TryParse(nodeNumberStr, out nodeNumber)) {
                if(DMCWrapper.Hierarchy.Nodes.ContainsKey(nodeNumber)) {
                    Console.PrintString("INFO: Found node " + nodeNumber);
                    return DMCWrapper.Hierarchy.Nodes[nodeNumber];
                }
                else {
                    Console.PrintString("ERROR: Unable to find node with ID " + nodeNumber);
                }
            }
            else {
                Console.PrintString("ERROR: Unable to parse ID " + nodeNumber);
            }
            return null;
        }

        public void HighlightNeighbors(string nodeNumberStr) {
            UnityEngine.Debug.Log("HighlightNeighbors called");
            uint nodeNumber;
            if(uint.TryParse(nodeNumberStr, out nodeNumber)) {
                if(DMCWrapper.Hierarchy.Nodes.ContainsKey(nodeNumber)) {
                    Console.PrintString("Node " + nodeNumber + " exists. Print: " + DMCWrapper.Hierarchy.Nodes[nodeNumber]);
                    foreach(GameObject obj in PreviouslyHighlightedObjects) {
                        DehighlightObject(obj);
                    }
                    foreach(GameObject obj in NodeMeshes) {
                        UnityEngine.Object.Destroy(obj);
                    }
                    NodeMeshes.Clear();
                    PreviouslyHighlightedObjects.Clear();
                    Gizmos.Clear();
                    List<Node> Neighbors = new List<Node>(); //DMC.DebugAlgorithm.FindNeighboringNodes(DMCWrapper.Hierarchy, DMCWrapper.Hierarchy.Nodes[nodeNumber]);
                    
                    foreach(Node node in Neighbors) {
                        HighlightNode(node);
                    }
                }
                else {
                    Console.PrintString("ERROR: There is no node with ID " + nodeNumber);
                }
            }
            else {
                Console.PrintString("ERROR: Failed to highlight neighbors; could not parse argument " + nodeNumberStr);
            }
        }

        public void HighlightNode(Node node) {
            if(!node.IsLeaf) {
                UnityEngine.Debug.Log("Error highlighting node: not a leaf");
                return;
            }
            GameObject obj = (GameObject)DMCWrapper.UnityObjects[node.Number];
            PreviouslyHighlightedObjects.Add(obj);
            Color c = obj.GetComponent<MeshRenderer>().material.color;
            obj.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 1.0f);

            //highlight bounds
            for(int i = 0; i < 6; i++) {
                Line l = new Line();
                l.A = node.Vertices[Lookups.EdgePairs[i, 0]] * WorldSize;
                l.B = node.Vertices[Lookups.EdgePairs[i, 1]] * WorldSize;
                l.Color = Utility.SinColor(node.Depth * 3f);
                Gizmos.Add(l);
            }
            
            // Make mesh of node
            Mesh m = MeshifyNodeBounds(node);
            GameObject mobj = UnityEngine.Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            mobj.GetComponent<Transform>().localScale = Vector3.one * WorldSize;
            mobj.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.7f);
            mobj.GetComponent<MeshFilter>().mesh = m;
            mobj.name = "Node Bounds " + node.Number + " Depth " + node.Depth;
            NodeMeshes.Add(mobj);
        }

        public void HighlightBoundingSphere(Node node) {
            //this.HighlightedBoundingSpheres.Add(node.BoundingSphere);
        }

		public void SplitNode(string nodeNumberStr) {
			Node n = StringToNode(nodeNumberStr);

			if(n != null) {
				DMC.DebugAlgorithm.SplitDiamond(DMCWrapper.Hierarchy, DMCWrapper.Hierarchy.Diamonds[n.CentralVertex]);
				DMCWrapper.Meshify();
			}
		}

		public void Coarsen() {
			Debug.Log("Coarsening at position: " + Camera.position);

			DMC.DebugAlgorithm.Coarsen(DMCWrapper.Hierarchy, Camera.position);
			DMCWrapper.Meshify();
		}

		public void Refine() {
			Debug.Log("Refining at position: " + Camera.position);
	
			DMC.DebugAlgorithm.Refine(DMCWrapper.Hierarchy, Camera.position);
			DMCWrapper.Meshify();
		}

		public void MergeNode(string nodeNumberStr) {
			Node n = StringToNode(nodeNumberStr);

			if(n != null) {
				DMC.DebugAlgorithm.MergeTetrahedron(DMCWrapper.Hierarchy, n.Parent);
				DMCWrapper.Meshify();
			}
		}

		public void Adapt() {
			DMC.DebugAlgorithm.Adapt(DMCWrapper.Hierarchy, Camera.position);
			//DMC.DebugAlgorithm.Adapt(DMCWrapper.Hierarchy, Camera.position);
			DMCWrapper.Meshify();
		}

        public void DrawGizmos() {
            foreach(Line l in Gizmos) {
                UnityEngine.Gizmos.color = l.Color;
                UnityEngine.Gizmos.DrawLine(l.A, l.B);
            }
            foreach(Sphere s in HighlightedBoundingSpheres) {
                UnityEngine.Gizmos.color = new Color(0.5f, 0.3f, 0.0f, 0.5f);
                UnityEngine.Gizmos.DrawSphere(s.Center, s.Radius);
            }
        }

        public Mesh MeshifyNodeBounds(Node n) {
            UnityEngine.Mesh m = new Mesh();		
            
            Vector3[] verts = new Vector3[4];		
            for(int i = 0; i < 4; i++) {		
                verts[i] = n.Vertices[i];		
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

            /*UnityEngine.Mesh m = new UnityEngine.Mesh();
            Vector3[] vertices = new Vector3[Lookups.TetrahedronFaces.GetLength(0) * 3];
            for(int i = 0; i < Lookups.TetrahedronFaces.GetLength(0); i++) {
                for(int j = 0; j < 3; j++) {
                    vertices[i + j] = n.HVertices[Lookups.TetrahedronFaces[i, j]];
                }
            }
            m.vertices = vertices;
            int[] tris = new int[vertices.Length];
            for(int i = 0; i < tris.Length; i++) tris[i] = i;
            m.triangles = tris;
            m.RecalculateNormals();*/
            return m;
        }

        public void DehighlightObject(GameObject obj) {
            Color c = obj.GetComponent<MeshRenderer>().material.color;
            obj.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.1f);

        }
    }
}