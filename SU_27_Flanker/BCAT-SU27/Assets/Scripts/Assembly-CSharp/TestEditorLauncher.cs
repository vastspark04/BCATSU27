using UnityEngine;

public class TestEditorLauncher : MonoBehaviour
{
	public void LaunchEditor()
	{
		VTScenarioEditor.LaunchEditor(string.Empty);
	}

	public void LaunchMapEditor()
	{
		LoadingSceneController.LoadSceneImmediate("VTMapEditMenu");
	}

	public void LaunchVoiceProfilesTest()
	{
		LoadingSceneController.LoadSceneImmediate("CommRadioTest");
	}

	public void LaunchLocalization()
	{
		LoadingSceneController.LoadSceneImmediate("LocalizationScene");
	}
}
