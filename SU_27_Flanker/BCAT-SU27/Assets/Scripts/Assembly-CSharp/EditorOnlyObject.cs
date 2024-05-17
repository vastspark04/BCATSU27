using UnityEngine;

public class EditorOnlyObject : MonoBehaviour
{
	public bool isTestFeature;

	public string testFeatureOptionName;

	public bool allowInDevTools = true;

	private void Awake()
	{
		if (isTestFeature && GameStartup.version.releaseType == GameVersion.ReleaseTypes.Testing && (string.IsNullOrEmpty(testFeatureOptionName) || GameSettings.CurrentSettings.GetBoolSetting(testFeatureOptionName) || (GameSettings.TryGetGameSettingValue<bool>(testFeatureOptionName, out var val) && val)))
		{
			return;
		}
		if (allowInDevTools)
		{
			if (!VTResources.isEditorOrDevTools)
			{
				Object.Destroy(base.gameObject);
			}
		}
		else if (!Application.isEditor)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
