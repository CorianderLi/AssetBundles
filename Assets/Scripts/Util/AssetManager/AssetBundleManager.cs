using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

public class LoadedAssetBundle
{
	//实例化一个资源包
    public AssetBundle m_AssetBundle;
    public int m_Reference;
    public LoadedAssetBundle(AssetBundle assetBundle)
    {
        m_AssetBundle = assetBundle;
        m_Reference = 1;
    }
}

public class AssetBundleManager : MonoBehaviour
{
	//重要步骤
	//存储资源包清单以及彼此之间的依赖关系
	static AssetBundleManifest s_AssetBundleManifest = null;
    public static AssetBundleManifest AssetBundleManifestObject
    {
        set { s_AssetBundleManifest = value; }
    }

	//获取硬件平台名字，作为资源包路经点
	public static string s_PlatformDir = "";//Windows
    static string s_DownloadDir = "";

   	//存储资源包
    static Dictionary<string, LoadedAssetBundle> s_LoadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
	//存储要管理加载的资源包清单以及依赖关系的类示例
    static List<AssetBundleLoadAssetOperation> s_DownloadingOps = new List<AssetBundleLoadAssetOperation>();
	//存储要加载的资源包清单获取，便于以后加载遍历
    static Dictionary<string, WWW> s_DownloadingWWWs = new Dictionary<string, WWW>();

    static Dictionary<string, string> s_DownloadingErrors = new Dictionary<string, string>();
	//存储依赖关系数组
    static Dictionary<string, string[]> s_Dependencies = new Dictionary<string, string[]>();

	IEnumerator Start()
	{
		//获取资源包在硬件平台文件目录，存储在s_PlatformDir
		Init();
		//获取AssetBundles清单
		AssetBundleLoadManifestOperation op = LoadManifestAsset();
		//开始获取清单
		//使用 yield 关键字，则意味着它在其中出现的方法、运算符或 get 访问器是迭代器
		yield return StartCoroutine(op);
	}

	static void Init()
	{
		s_PlatformDir =
		#if UNITY_EDITOR   //编辑器用户编译设置,当前激活编译目标
			GetPlatformFolderForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
		#else
		GetPlatformFolderForAssetBundles(Application.platform);
		#endif
		s_DownloadDir = GetRelativePath() + "/AssetBundles/" + s_PlatformDir + "/";
	}

	public static string GetPlatformFolderForAssetBundles(BuildTarget target)
	{
		switch (target)
		{
			case BuildTarget.Android:
				return "Android";
			case BuildTarget.iOS:
				return "iOS";
			case BuildTarget.WebPlayer:
				return "WebPlayer";
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return "Windows";
			default:
				return null;
		}
	}

	static string GetRelativePath()
	{
		if (Application.isEditor)
			return "file://" + System.Environment.CurrentDirectory.Replace("\\", "/"); // Use the build output folder directly.
		else
			return "";
	}

	static AssetBundleLoadManifestOperation LoadManifestAsset()
	{
		//s_PlatformDir = “Windows”
		LoadAssetBundle(s_PlatformDir, true);
		AssetBundleLoadManifestOperation operation = new AssetBundleLoadManifestOperation(s_PlatformDir, "AssetBundleManifest", typeof(AssetBundleManifest));
		s_DownloadingOps.Add(operation);
		
		return operation;
	}

	/// <summary>
	/// 两个功能：1.初始化的时候把资源包清单加载到s_DownloadingWWWs字典中；
	/// 		 2.加载资源的时候，根据名字遍历s_DownloadingWWWs字典，得到地址通过WWW加载实例化到游戏中
	/// </summary>
	static void LoadAssetBundle(string assetBundleName, bool isManifestFile = false)
	{
		bool isExisted = CheckExists(assetBundleName);
		if (!isExisted && !isManifestFile)
		{
			LoadDependencies(assetBundleName);
		}
	}

	static void LoadDependencies(string assetBundleName)
	{
		if (s_AssetBundleManifest == null)
		{
			Debug.Log("清单文件为空，开始前请先打包资源，检查初始化的时候是否把清单文件加载成功");
			return;
		}
		//获取所有assetBundleName下的依赖对象
		string[] dependencies = s_AssetBundleManifest.GetAllDependencies(assetBundleName);
		if (dependencies.Length > 0)
		{
			s_Dependencies.Add(assetBundleName, dependencies);
			for (int i = 0; i < dependencies.Length; ++i)
			{
				LoadAssetBundle(dependencies[i]);
			}
		}
	}

    public static AssetBundleLoadAssetOperation LoadAssetAsync(string assetBundleName, string assetName, System.Type type)
    {
        LoadAssetBundle(assetBundleName);
        AssetBundleLoadAssetOperation operation = new AssetBundleLoadCommonAsset(assetBundleName, assetName, type);
        s_DownloadingOps.Add(operation);
        return operation;
    }

	static bool CheckExists(string assetBundleName)
	{
		LoadedAssetBundle bundle = null;
		//如果找到该键，便会返回与指定的键相关联的值,赋值给bundle；
		//否则，则会返回 value 参数的类型默认值,赋值给bundle。该参数未经初始化即被传递
		//如果 Dictionary 包含具有指定键的元素，则为 true；否则为 false
		s_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
		if (bundle != null)
		{
			bundle.m_Reference++;
			string[] dependencies = null;
			if(s_Dependencies.TryGetValue(assetBundleName, out dependencies))
			{
				foreach (string item in dependencies)
				{
					CheckExists(item);
				}
			}
			return true;
		}
		if (s_DownloadingWWWs.ContainsKey(assetBundleName)) // be sure only one AssetBundle
		{
			return true;
		}
		WWW www = new WWW(s_DownloadDir + assetBundleName);
		//添加要加载的资源包清单获取，便于以后加载遍历
		s_DownloadingWWWs.Add(assetBundleName, www);
		return false;
	}

    public static void UnloadAssetBundle(string assetBundleName)
    {
        string error;
        LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName, out error);
        if (bundle == null)
        {
            return;
        }
        if (--bundle.m_Reference == 0)
        {
            bundle.m_AssetBundle.Unload(true);
            Debug.Log("unload a assset bundle " + assetBundleName);
            s_LoadedAssetBundles.Remove(assetBundleName);
        }
        string[] dependencies = null;
        if (!s_Dependencies.TryGetValue(assetBundleName, out dependencies))
        {
            return;
        }
        foreach (string item in dependencies)
        {
            UnloadAssetBundle(item);
        }
        if (bundle.m_Reference == 0)
        {
            s_Dependencies.Remove(assetBundleName);
        }
    }

    public static LoadedAssetBundle GetLoadedAssetBundle(string assetBundleName, out string error)
    {
        if (s_DownloadingErrors.TryGetValue(assetBundleName, out error))
        {
            return null;
        }
        LoadedAssetBundle bundle = null;
        s_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
        if (bundle == null)
        {
            return null;
        }
        string[] dependencies = null;
        if (!s_Dependencies.TryGetValue(assetBundleName, out dependencies))
        {
            return bundle;
        }
        foreach (string item in dependencies)
        {
            if (s_DownloadingErrors.TryGetValue(item, out error))
            {
                return null;
            }
            LoadedAssetBundle dependentBundle;
            s_LoadedAssetBundles.TryGetValue(item, out dependentBundle);
            if (dependentBundle == null)
            {
                return null;
            }
        }
        return bundle;
    }

    void Update()
    {
        RemoveFinishedWWWs();
        RemoveFinishedOps();
    }

	/// <summary>
	/// 作为移除已经加载过的索引
	/// </summary>
    void RemoveFinishedWWWs()
    {
		//如果存在还没有加载的资源清单的个数大于0
		if (s_DownloadingWWWs.Count > 0) {
			List<string> keysToRemove = new List<string>();
			foreach (var item in s_DownloadingWWWs)//length = 1
			{
				WWW www = item.Value;
				if (www.error != null)
				{
					Debug.LogError(www.error);
					s_DownloadingErrors.Add(item.Key, www.error);
					keysToRemove.Add(item.Key);
					continue;
				}
				if (www.isDone)
				{
					//重要步骤
					s_LoadedAssetBundles.Add(item.Key, new LoadedAssetBundle(www.assetBundle));
					keysToRemove.Add(item.Key);
				}
			}
			foreach (string item in keysToRemove)
			{
				WWW www = s_DownloadingWWWs[item];
				s_DownloadingWWWs.Remove(item);
				www.Dispose();
			}
		}
    }

    void RemoveFinishedOps()
    {
        for (int i = 0; i < s_DownloadingOps.Count; )
        {
            if (!s_DownloadingOps[i].Update12())
            {
                s_DownloadingOps.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }
}
