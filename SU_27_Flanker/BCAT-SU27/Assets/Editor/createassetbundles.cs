using UnityEditor;
using System.IO;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")] //FA26-b
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles"; 
        if(!Directory.Exists(assetBundleDirectory))
{
    Directory.CreateDirectory(assetBundleDirectory);
}
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, 
                                        BuildAssetBundleOptions.UncompressedAssetBundle, 
                                        BuildTarget.StandaloneWindows);
    }
}