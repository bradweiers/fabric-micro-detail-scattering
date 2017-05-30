using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(FabricPatternTexture))]
public class FabricPatternTextureEditor : Editor
{
    Texture2D m_PatternTexture;
    int       m_Resolution;
    string    m_TextureName;
    float     m_NumRows;
    float     m_NumCols;
    int       m_PatternAlternation;
    float     m_WarpTwists;
    float     m_WeftTwists;
    float     m_WarpTwistSlope;
    float     m_WeftTwistSlope;
    float     m_FiberBumpScale;
    float     m_FiberTwistScale;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //Extract Texture Data from Object
        InitializeTextureParameters((FabricPatternTexture)target);

        if (GUILayout.Button("Build Fabric Pattern Map"))
        {
            Debug.Log("Building Pattern Map...");

            m_PatternTexture = new Texture2D(m_Resolution, m_Resolution, TextureFormat.ARGB32, true, false);
            var pixels = new Color[m_Resolution * m_Resolution];

            for(var i = 0; i < m_Resolution; ++i)
            {
                for(var j = 0; j < m_Resolution; ++j)
                {
                    pixels[m_Resolution * i + j] = WeaveElementDelta(/*u*/j, /*v*/i);
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
        var delta = new Color();

        var resScaledU = (float)u / m_Resolution;
        var resScaledV = (float)v / m_Resolution;

        var sc  = u / (m_Resolution / (int)m_NumRows); 
        var sr  = v / (m_Resolution / (int)m_NumCols);
        delta.a = (sc + sr) % m_PatternAlternation == 0 ? /*warp*/ 1 : /*weft*/ 0; //Every other for now.

        var xDivisionFactor = 1.0f / m_NumRows;
        var yDivisionFactor = 1.0f / m_NumCols;

        var a = xDivisionFactor * Mathf.Floor(resScaledV / xDivisionFactor); // [ x | > | > | x | - ] 5 cell example
        var b = yDivisionFactor * Mathf.Floor(resScaledU / xDivisionFactor);

        var sigmaU = (a + xDivisionFactor / 2 - resScaledV) * (m_NumRows * 2); // Current x of beginning cell + half width minus UV distance for relative axis distance.
        var sigmaV = (b + yDivisionFactor / 2 - resScaledU) * (m_NumCols * 2); // Scaled by 2x num divisions

        delta.r = Mathf.Approximately(delta.a, 1f) ? m_FiberTwistScale * (2 * Frac(m_WarpTwists * (resScaledU - m_WarpTwistSlope * sigmaU)) - 1) : m_FiberBumpScale * sigmaV;
        delta.g = delta.a == 1 ? m_FiberBumpScale * sigmaU : m_FiberTwistScale * (2 * Frac(m_WeftTwists * (resScaledV - m_WeftTwistSlope * sigmaV)) - 1);
        delta.b = 1.0f - Mathf.Sqrt(Mathf.Pow(delta.r, 2.0f) + Mathf.Pow(delta.g, 2.0f)); //Approx. Occlusion

        delta.r = delta.r * 0.5f + 0.5f; //Don't compress, store as RGBAFloat
        delta.g = delta.g * 0.5f + 0.5f;
        delta.b = delta.b * 0.5f + 0.5f;

        return delta;
    }

    static float Frac(float v)
    {
        return v - Mathf.Floor(v);
    }

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
