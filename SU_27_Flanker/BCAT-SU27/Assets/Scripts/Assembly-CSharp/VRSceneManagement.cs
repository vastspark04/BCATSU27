using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(VRInteractable))]
public class VRSceneManagement : MonoBehaviour
{
	public int sceneIndex;

	public string sceneName;

	public bool immediate;

	private void Start()
	{
		GetComponent<VRInteractable>().OnStartInteraction += Vrint_OnStartInteraction;
		if (!string.IsNullOrEmpty(sceneName))
		{
			sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
		}
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		StartCoroutine(LoadSceneRoutine());
	}

	private IEnumerator LoadSceneRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 0.85f);
		yield return new WaitForSeconds(1f);
		if (immediate)
		{
			LoadingSceneController.LoadSceneImmediate(sceneIndex);
		}
		else
		{
			LoadingSceneController.LoadScene(sceneIndex);
		}
		ControllerEventHandler.UnpauseEvents();
	}
}
