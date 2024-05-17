using UnityEngine;
using UnityEngine.XR;

public class VTMapEditorMenu : MonoBehaviour
{
	public VTMapGenUI mapGenUI;

	public VTMapMenuOpenUI openUI;

	public VTECMenuConfirmDialogue confirmDialogue;

	private void Start()
	{
		if (XRSettings.enabled)
		{
			VRUtils.DisableVR();
			LoadingSceneController.ReloadSceneImmediately();
		}
		else
		{
			BGMManager.FadeOut();
		}
	}

	public void NewMapButton()
	{
		mapGenUI.gameObject.SetActive(value: true);
		base.gameObject.SetActive(value: false);
	}

	public void OpenButton()
	{
		openUI.gameObject.SetActive(value: true);
		base.gameObject.SetActive(value: false);
		openUI.Open();
	}

	public void BackButton()
	{
		LoadingSceneController.SwitchToVRScene("SamplerScene");
	}
}
