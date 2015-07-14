using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TestLoadBundle : MonoBehaviour 
{
	//加载
    public void OnClickLoad()
    {
        StartCoroutine(LoadBundle());
    }
    IEnumerator LoadBundle()
    {
        AssetBundleLoadAssetOperation op2 = AssetBundleManager.LoadAssetAsync("cube.unity3d", "Cube", typeof(GameObject));
        yield return StartCoroutine(op2);
		Debug.Log ("3");
        GameObject cube = op2.GetAsset<GameObject>();
        if (cube != null)
        {
            GameObject.Instantiate(cube);
        }
    }

	//卸载
	public void UnloadBundle(GameObject obj)
	{
		Destroy(obj);
		AssetBundleManager.UnloadAssetBundle("cube.unity3d");
	}
}
