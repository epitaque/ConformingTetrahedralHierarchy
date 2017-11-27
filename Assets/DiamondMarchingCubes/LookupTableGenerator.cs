using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DMC2 {
public static class DMCLookupTableGenerator {
	// index: diamond type | value: list of parent offsets
	public static Dictionary<byte, List<Vector3>> parentOffsets;

    public static void Run() {
		parentOffsets = new Dictionary<byte, List<Vector3>>();
		TestGetDiamondType();
		CreateDiamondChildLookupTable();
    }

    public static void CreateDiamondChildLookupTable(bool copy = false) {
        // index: diamond's type
        // diamond's type is  
        Vector3[] childOffsets;

        // create a big hierarchy and loop through each diamond to find child offsets
        Root hierarchy = DebugAlgorithm2.CreateTestHierarchy(AfterSplit);

		Debug.Log("done generating lookup table.");
		string table = "";
		foreach(KeyValuePair<byte, List<Vector3>> e in parentOffsets) {
			byte type = e.Key;
			List<Vector3> val = e.Value;

			table += "	{" + type + ", ";

			foreach(Vector3 off in val) {
				table += off + ", ";
			}

			table += "}\n";
		}
		Debug.Log("Table: " + table);
    }

	public static void AfterSplit(Vector3Int diamond, Vector3Int parent) {
		int scale1 = GetDiamondScale(diamond);
		int scale = (int)System.Math.Pow(2, (double)scale1);

		Debug.Log("AfterSplit scale1 (tz) " + scale1 + ", actual scale: " + scale);

		Vector3Int off = parent - diamond;
		Vector3 unscaled = new Vector3((float)((double)off.x / (double)scale), (float)((double)off.y / (double)scale), (float)((double)off.z / (double)scale));

		byte type = GetDiamondType(diamond);

		if(!parentOffsets.ContainsKey(type)) {
			parentOffsets.Add(type, new List<Vector3>());
		}
		if(!parentOffsets[type].Contains(unscaled)) {
			parentOffsets[type].Add(unscaled);
		}
	}

	public static int GetDiamondScale(Vector3Int cv) {
		//Debug.Log("Counting trailing zeroes of " + cv);

        int xtz = CountTrailingZeroes(cv.x);
        int ytz = CountTrailingZeroes(cv.y);
        int ztz = CountTrailingZeroes(cv.z);
        int tz = xtz;
        if(ytz < xtz) { tz = ytz;
        if(ztz < ytz) tz = ztz; }

		return tz; // minimum number of trailing zeroes is scale
	}

    public static void TestGetDiamondType() {
		Vector3Int testNum = new Vector3Int(72, 20, 20);
		Debug.Log("Testing diamond type detector. Input: " + testNum);
		Debug.Log("To binary: x: " + System.Convert.ToString(testNum.x, 2) + ", y: " +  System.Convert.ToString(testNum.y, 2));

		byte type = GetDiamondType(testNum);

		Debug.Log("Type: " + type);

		Debug.Log("Type: x: " + System.Convert.ToString(type & 3, 2) + ", y: " +  System.Convert.ToString( (type >> 2) & 3, 2) + ", z: " + System.Convert.ToString( (type >> 4) & 3, 2));
	} 

	public static byte GetDiamondType(Vector2Int cv) {
		int xtz = CountTrailingZeroes(cv.x);
		int ytz = CountTrailingZeroes(cv.y);
		int tz = xtz;
		if(ytz < xtz) { tz = ytz; }

		Debug.Log("Trailing zeroes: " + tz);

		int xtype = GetType(cv.x, tz);
		int ytype = GetType(cv.y, tz);

		int type = xtype | (ytype << 2);
		return (byte)type;
	}

    public static byte GetDiamondType(Vector3Int cv) {
        int xtz = CountTrailingZeroes(cv.x);
        int ytz = CountTrailingZeroes(cv.y);
        int ztz = CountTrailingZeroes(cv.z);
        int tz = xtz;
        if(ytz < xtz) { tz = ytz;
        if(ztz < ytz) tz = ztz; }
        
        int xtype = GetType(cv.x, tz);
        int ytype = GetType(cv.y, tz);
        int ztype = GetType(cv.z, tz);

        int type = xtype | (ytype << 2) | (ztype << 4);
        return (byte)type;
    }

    public static int CountTrailingZeroes(int n) {
        int count = 0;
        for(int s = 0; s < 32; s++) {
            if( (n >> s & 1) == 0) {
                count++;
            }
			else {
				break;
			}
        }
        return count;
    }

    public static int GetType(int coordinate, int trailingZeroes) {
        int shifted = coordinate >> trailingZeroes;
        int type =  shifted & 3; // 2 bits

        return type;
    }
}
}