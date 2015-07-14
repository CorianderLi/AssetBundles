using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

public class BuildAssetBundle
{
    const string m_outputDir = "AssetBundles";

    [MenuItem("UGameTools/BuildBundles/GenerateBundles")]
    static void BuildBundle()
    {
        string platformStr = AssetBundleManager.GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
		//利用平台名字，作为资源清单文件名
		string outputPath = Path.Combine(m_outputDir, platformStr);
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        BuildPipeline.BuildAssetBundles(outputPath, 0, EditorUserBuildSettings.activeBuildTarget);
    }
}
