using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor (typeof(FabricPatternTexture))]
public class FabricPatternTextureEditor : Editor {

    private Texture2D m_PatternTexture;
    private int       m_Resolution;
    private string    m_TextureName;
    private float     m_NumRows;
    private float     m_NumCols;
    private int       m_PatternAlternation;
    private float     m_WarpTwists;
    private float     m_WeftTwists;
    private float     m_WarpTwistSlope;
    private float     m_WeftTwistSlope;
    private float     m_FiberBumpScale;
    private float     m_FiberTwistScale;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //Extract Texture Data from Object
        InitializeTextureParameters((FabricPatternTexture)target);

        if (GUILayout.Button("Build Fabric Pattern Map"))
        {
            Debug.Log("Building Pattern Map...");

            m_PatternTexture = new Texture2D(m_Resolution, m_Resolution, TextureFormat.ARGB32, true, false);
            Color[] pixels = new Color[m_Resolution * m_Resolution];

            for(int i = 0; i < m_Resolution; ++i)
            {
                for(int j = 0; j < m_Resolution; ++j)
                {
                    pixels[(m_Resolution * i) + j] = WeaveElementDelta(/*u*/j, /*v*/i);
                }
            }

            m_PatternTexture.SetPixels(pixels);
            m_PatternTexture.Apply();
            AssetDatabase.CreateAsset(m_PatternTexture, "Assets/Textures/" + m_TextureName + ".asset");
            PngWriter.WritePng("Assets/Textures/" + m_TextureName + "PNG.png", m_PatternTexture);
        }
    }

    //https://www.researchgate.net/publication/221252079_Realtime_Rendering_of_Realistic_Fabric_with_Alternation_of_Deformed_Anisotropy
    Color WeaveElementDelta(int u, int v)
    {
        Color delta = new Color();

        float U = (float)u / m_Resolution;
        float V = (float)v / m_Resolution;

        int sc  = u / (m_Resolution / (int)m_NumRows); 
        int sr  = v / (m_Resolution / (int)m_NumCols);
        delta.a = ((sc + sr) % m_PatternAlternation == 0 ? /*warp*/ 1 : /*weft*/ 0); //Every other for now.

        float xDivisionFactor = 1.0f / m_NumRows;
        float yDivisionFactor = 1.0f / m_NumCols;

        float a = xDivisionFactor * Mathf.Floor(V / xDivisionFactor); // [ x | > | > | x | - ] 5 cell example
        float b = yDivisionFactor * Mathf.Floor(U / xDivisionFactor);

        float sigmaU = ((a + (xDivisionFactor / 2)) - V) * (m_NumRows * 2); // Current x of beginning cell + half width minus UV distance for relative axis distance.
        float sigmaV = ((b + (yDivisionFactor / 2)) - U) * (m_NumCols * 2); // Scaled by 2x num divisions

        delta.r = (delta.a == 1 ? m_FiberTwistScale * ((2 * frac(m_WarpTwists * (U - (m_WarpTwistSlope * sigmaU)))) - 1) : (m_FiberBumpScale * sigmaV));
        delta.g = (delta.a == 1 ? (m_FiberBumpScale * sigmaU) : m_FiberTwistScale * ((2 * frac(m_WeftTwists * (V - (m_WeftTwistSlope * sigmaV)))) - 1));
        delta.b = 1.0f - Mathf.Sqrt(Mathf.Pow(delta.r, 2.0f) + Mathf.Pow(delta.g, 2.0f)); //Approx. Occlusion

        delta.r = (delta.r * 0.5f) + 0.5f; //Don't compress, store as RGBAFloat
        delta.g = (delta.g * 0.5f) + 0.5f;
        delta.b = (delta.b * 0.5f) + 0.5f;

        return delta;
    }

    float frac(float v) { return v - Mathf.Floor(v); }

    void InitializeTextureParameters(FabricPatternTexture _textureData)
    {
        m_Resolution         = _textureData.textureResolution;
        m_TextureName        = _textureData.textureName;
        m_NumRows            = _textureData.numRows;
        m_NumCols            = _textureData.numCols;
        m_PatternAlternation = _textureData.patternAlternation;
        m_WarpTwists         = _textureData.warpTwists;
        m_WeftTwists         = _textureData.weftTwists;
        m_WarpTwistSlope     = _textureData.warpTwistSlope;
        m_WeftTwistSlope     = _textureData.weftTwistSlope;
        m_FiberBumpScale     = _textureData.fiberBumpScale;
        m_FiberTwistScale    = _textureData.fiberTwistScale;
    }
}
