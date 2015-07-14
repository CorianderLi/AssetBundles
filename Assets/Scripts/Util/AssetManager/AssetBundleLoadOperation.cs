using UnityEngine;
using System.Collections;


public abstract class AssetBundleLoadOperation :IEnumerator
{
    public object Current
    {
        get
        {
			Debug.Log ("2 Current");
            return null;
        }
    }

    public bool MoveNext()
    {
		Debug.Log ("1 MoveNext");
        return !IsDone();
        //return true;
    }

	public void Reset (){}

    abstract public bool Update12();

    abstract public bool IsDone();
}


public abstract class AssetBundleLoadAssetOperation:AssetBundleLoadOperation
{
    public abstract T GetAsset<T>() where T : UnityEngine.Object;
}

public class AssetBundleLoadCommonAsset:AssetBundleLoadAssetOperation
{
    protected string m_AssetBundleName;
    protected string m_AssetName;
    protected string m_DownloadingError;
    protected System.Type m_Type;
    protected AssetBundleRequest m_Request = null;

    public AssetBundleLoadCommonAsset(string bundleName,string assetName,System.Type type)
    {
        this.m_AssetBundleName = bundleName;
        this.m_AssetName = assetName;
        m_Type = type;
    }

    public override T GetAsset<T>()
    {
        if(m_Request!=null && m_Request.isDone)
        {
            return m_Request.asset as T;
        }
        else
        {
            return null;
        }
    }

    public override bool Update12()
    {
        if(m_Request!=null)
        {
            return false;
        }
        LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle(m_AssetBundleName,out m_DownloadingError);
		if(bundle!=null)
        {
			//重要步骤
            m_Request = bundle.m_AssetBundle.LoadAssetAsync(m_AssetName, m_Type);
            return false;
        }
        else
        {
            return true;
        }
    }

    public override bool IsDone()
    {
        if(m_Request == null && m_DownloadingError != null) 
        {
            return true;
        }
        return m_Request != null && m_Request.isDone;
    }
}

public class AssetBundleLoadManifestOperation:AssetBundleLoadCommonAsset
{
    public AssetBundleLoadManifestOperation(string bundleName,string assetName,System.Type type):
        base(bundleName,assetName,type)
    {
    }
    public override bool Update12()
    {
		base.Update12();
		if(m_Request!=null && m_Request.isDone)
		{
			//重要步骤
			AssetBundleManager.AssetBundleManifestObject = GetAsset<AssetBundleManifest>();
			return false;
		}
		else
		{
			return true;
		}
    }

}