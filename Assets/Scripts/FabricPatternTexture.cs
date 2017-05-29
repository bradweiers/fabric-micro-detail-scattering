using UnityEngine;
using System.Collections;

public class FabricPatternTexture : MonoBehaviour {
	public string textureName;
    public int    textureResolution;
    
    [Header("Function Parameters")]
    public float numRows;
    public float numCols;
    public int   patternAlternation;
    public float warpTwists;
    public float weftTwists;
    public float warpTwistSlope;
    public float weftTwistSlope;
    public float fiberBumpScale;
    public float fiberTwistScale;
}
