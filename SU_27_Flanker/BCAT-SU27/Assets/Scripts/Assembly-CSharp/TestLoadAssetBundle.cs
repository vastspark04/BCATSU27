using System.IO;
using UnityEngine;

public class TestLoadAssetBundle : MonoBehaviour
{
	public string assetPath;

	public string prefabName;

	private void Start()
	{
		string text = Path.Combine(VTResources.gameRootDirectory, assetPath);
		if (File.Exists(text))
		{
			AssetBundle assetBundle = AssetBundle.LoadFromFile(text);
			if (assetBundle == null)
			{
				Debug.LogError("Failed to load asset!");
			}
			else
			{
				Object.Instantiate(assetBundle.LoadAsset<GameObject>(prefabName));
			}
		}
		else
		{
			Debug.LogError("File does not exist: " + text);
		}
	}
}
