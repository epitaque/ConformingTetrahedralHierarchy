using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DMC {
public static class DMCLookupTableGenerator {
    public static void Run() {

    }

    public static void CreateDiamondChildLookupTable(bool copy = false) {
        // index: diamond's type
        // diamond's type is  
        Vector3[] childOffsets;

        // create a big hierarchy and loop through each diamond to find child offsets
        DebugAlgorithm.CreateHierarchy(new Vector3(0, 0, 0));

    }

    public static byte GetDiamondType(Vector3Int cv) {
        int xtz = CountTrailingZeroes(cv.x);
        int ytz = CountTrailingZeroes(cv.y);
        int ztz = CountTrailingZeroes(cv.z);
        int tz = xtz;
        if(ytz > xtz) { tz = ytz;
        if(ztz > ytz) tz = ztz; }
        
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