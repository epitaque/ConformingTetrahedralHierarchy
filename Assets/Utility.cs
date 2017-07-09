using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public static class Math {
	public static Vector4 Tet_CartToBary(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector3 P) {
		Vector3 vAP = P - A;
		Vector3 vBP = P - B;

		Vector3 vAB = B - A;
		Vector3 vAC = C - A;
		Vector3 vAD = D - A;

		Vector3 vBC = C - B;
		Vector3 vBD = D - B;
		// ScTP comPutes the scAlAr triPle Product
		float vA6 = ScTP(vBP, vBD, vBC);
		float vB6 = ScTP(vAP, vAC, vAD);
		float vc6 = ScTP(vAP, vAD, vAB);
		float vd6 = ScTP(vAP, vAB, vAC);
		float v6 = 1 / ScTP(vAB, vAC, vAD);
		return new Vector4(vA6*v6, vB6*v6, vc6*v6, vd6*v6);

	}

	public static float ScTP(Vector3 A, Vector3 B, Vector3 C) {
		return Vector3.Dot(Vector3.Cross(A, B), C);
	}
}

public static class Utility {
    public static Color[] GetRandomColorArray(int length) {
        Color[] colors = new Color[length];
		for(int i = 0; i < colors.Length; i++) {
			colors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
		}
        return colors;
    }
    public static void DrawHierarchy(DMC.Root root) {
		for(int i = 0; i < 6; i++) {
			if(root.Children[i] != null) {
				DrawNode(root.Children[i], i);
			}
		}
    }
    public static void DrawNode(DMC.Node n, int f) {
		Vector3 offset = new Vector3(0, 0, 0);
		float scale = 64;
		if(n.Children != null) {
			DrawNode(n.Children[0], f);
			DrawNode(n.Children[1], f);
		}
		for(int i = 0; i < 6; i++) {
			Vector3 pa = (n.Vertices[DMC.Lookups.EdgePairs[i, 0]] + offset) * scale;
			Vector3 pb = (n.Vertices[DMC.Lookups.EdgePairs[i, 1]] + offset) * scale;
			Gizmos.DrawLine(pa, pb);
		}
	}
    public static int[] ScrambleArray(int[] Arr) {
        int[] newArray = new int[Arr.Length];
        List<int> tempList = new List<int>(Arr);
        for(int i = 0; i < Arr.Length; i++) {
            int index = UnityEngine.Random.Range(0, tempList.Count);
            int n = tempList[index];
            tempList.RemoveAt(index);
            newArray[i] = n;
        }
        return newArray;
    }
    public static string ArrayToString(int[] Arr) {
			string s = "{";
			for(int f = 0; f < Arr.Length - 1; f++) s += Arr[f] + ", ";
			s += Arr[Arr.Length - 1] + "}";
			return s;
    }
        public static string ArrayToString(int[,] Arr) {
        string s = "";
        s += "{";
        for(int x = 0; x < Arr.GetLength(0); x++) {
            s+= "	{";
            for(int y = 0; y < Arr.GetLength(1); y++) {
                s += Arr[x, y];
                if(y != Arr.GetLength(1) - 1) s += ", ";
            }
            s+= "}, \n";
        }
        s += "};";
        return s;

    }
    public static string ArrayToString(int[,,] Arr) {
        string s = "";
        s += "{";
        for(int x = 0; x < Arr.GetLength(0); x++) {
            s+= "	{";
            for(int y = 0; y < Arr.GetLength(1); y++) {
                s += " {";
                for(int z = 0; z < Arr.GetLength(2) - 1; z++) {
                    s += Arr[x,y,z] + ", ";
                }
                s += Arr[x,y, Arr.GetLength(2) - 1];
                if(y == Arr.GetLength(1) - 1) s += "} ";
                else s += "}, ";
            }
            s+= "}, \n";
        }
        s += "};";
        return s;
    }
    public static Vector3 FindMidpoint(Vector3 A, Vector3 B) {
        return (A + B) / 2;
    }

}