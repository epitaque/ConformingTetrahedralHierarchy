using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public static class Math {
    // Given tetrahedron ABCD, returns barycentric coordinates of point P
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
    // Given tetrahedron ABCD, returns cartesian coordinates of barycentric coordinates Bary
    public static Vector3 Tet_BaryToCart(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector4 Bary) {
        return Bary.x * A + Bary.y * B + Bary.z * C + Bary.w * D;
    }

	public static float ScTP(Vector3 A, Vector3 B, Vector3 C) {
		return Vector3.Dot(Vector3.Cross(A, B), C);
	}
}

public static class Utility {
    public static Color SinColor(float value) {
        float frequency = 0.3f;
        float red   = Mathf.Sin(frequency*value + 0) * 0.5f + 0.5f;
        float green = Mathf.Sin(frequency*value + 2) * 0.5f + 0.5f;
        float blue  = Mathf.Sin(frequency*value + 4) * 0.5f + 0.5f;
        return new Color(red, green, blue);
    }

    public static Color[] GetRandomColorArray(int length) {
        Color[] colors = new Color[length];
		for(int i = 0; i < colors.Length; i++) {
			colors[i] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
		}
        return colors;
    }
    public static void DrawHierarchy(DMC.Root root, float scale = 1) {
		for(int i = 0; i < 6; i++) {
			if(root.Children[i] != null) {
				DrawNode(root.Children[i], scale, i);
			}
		}
    }
    public static void DrawNode(DMC.Node node, float scale = 1, int f = 0) {
        Gizmos.color = Color.cyan;
		//Vector3 offset = new Vector3(f * 1.4f, 0, 0);
        Vector3 offset = new Vector3(f * 0, 0, 0);
		if(node.IsLeaf) {
            for(int i = 0; i < 6; i++) {
                Vector3 pa = (node.Vertices[DMC.Lookups.EdgePairs[i, 0]] + offset) * scale;
                Vector3 pb = (node.Vertices[DMC.Lookups.EdgePairs[i, 1]] + offset) * scale;
                Gizmos.DrawLine(pa, pb);
            }
		}
        else {
            DrawNode(node.Children[0], f);
            DrawNode(node.Children[1], f);
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
        return (A + B) / 2f;
    }
    public static Vector3 Lerp(float isolevel, Strucs.Point point1, Strucs.Point point2) {
        if (Mathf.Abs(isolevel-point1.Density) < 0.00001)
            return(point1.Position);
        if (Mathf.Abs(isolevel-point2.Density) < 0.00001)
            return(point2.Position);
        if (Mathf.Abs(point1.Density-point2.Density) < 0.00001)
            return(point2.Position);
        float mu = (isolevel - point1.Density) / (point2.Density - point1.Density); 
        return point1.Position + mu * (point2.Position - point1.Position); 
    }
    public static SE.OpenSimplexNoise noise = new SE.OpenSimplexNoise(2);
    public static float DebugNoise(Vector3 position) {
        float r = 1.8f;
        return (float)noise.Evaluate(position.x * r, position.y * r, position.z * r);
    }
    public static Sphere CalculateBoundingSphere(DMC.Node node) {
        Sphere s = new Sphere();

        Vector3 minExt = node.Vertices[0];
        Vector3 maxExt = node.Vertices[0];
        for(int i = 1; i < 4; i++) {
            minExt = Min(minExt, node.Vertices[i]);
            maxExt = Max(maxExt, node.Vertices[i]);
        }

        s.Center = new Vector3(0.5f * (minExt.x + maxExt.x), 0.5f * (minExt.y + maxExt.y), 0.5f * (minExt.z + maxExt.z));

        float maxDist = 0f;
        foreach(Vector3 p in node.Vertices) {
            float dist = Vector3.Distance(p, s.Center);
            if(dist > maxDist) {
                maxDist = dist;
            }
        }
        s.Radius = maxDist / 2f;

        return s;
    }
    private static Vector3 Min(Vector3 A, Vector3 B) {
        if(A.x > B.x) A.x = B.x;
        if(A.y > B.y) A.y = B.y;
        if(A.z > B.z) A.z = B.z;
        return A;
    }
    private static Vector3 Max(Vector3 A, Vector3 B) {
        if(A.x < B.x) A.x = B.x;
        if(A.y < B.y) A.y = B.y;
        if(A.z < B.z) A.z = B.z;
        return A;
    }
}

public class Sphere {
    public Vector3 Center;
    public float Radius;
}

namespace Strucs {
    public struct NoiseInfo {
        public Vector3 offset;
        public float frequency;
    }

    public struct Vector3i {
        public int x;
        public int y;
        public int z;

        public Vector3i(int x, int y, int z) { 
            this.x = x; this.y = y; this.z = z; 
        }
        public int getDimensionSigned(int dim) {
            switch(dim) {
                case 0: return -x;
                case 1: return x;
                case 2: return -y;
                case 3: return y;
                case 4: return -z;
                case 5: return z;
            }
            return -1;
        }
        public int getDimension(int dim) {
            switch(dim) {
                case 0: return x;
                case 1: return y;
                case 2: return z;
            }
            return -1;
        }
        public void setDimension(int dim, int val) {
            switch(dim) {
                case 0: x = val; break;
                case 1: y = val; break;
                case 2: z = val; break;
            }
        }
    }
    public struct GridCell {
        public Point[] Points;
        public GridCell Clone() {
            GridCell c = new GridCell();
            c.Points = new Point[Points.Length];
            for(int i = 0; i < Points.Length; i++) {
                c.Points[i] = Points[i];
            }
            return c;
        }
    }

    public struct Point {
        public Vector3 Position;
        public float Density;    
    }

}