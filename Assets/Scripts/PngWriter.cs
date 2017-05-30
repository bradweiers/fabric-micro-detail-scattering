using UnityEngine;
using System.IO;

public static class PngWriter
{
    public static void WritePng(string filename, Texture2D source)
    {
        var pngData = source.EncodeToPNG();
#if !UNITY_WEBPLAYER
        File.WriteAllBytes(filename, pngData);
#else
        Debug.LogError("Cannot write png in webplayer mode.  Please switch to standalone module.");
#endif
    }

}
