using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class VTEditMainMenu : MonoBehaviour
{
	public VTConfirmationDialogue confirmDialogue;

	public GameObject mainMenuWindow;

	public VTEdCampaignMenu campaignMenu;

	public VTEdCampaignEditWindow standaloneWindow;

	public VTEdResourceBrowser resourceBrowser;

	public VTEdMultiSelector multiSelector;

	public GameObject swLoadingObj;

	public GameObject swBrowser;

	public GameObject sceneSwitchLoadingObj;

	private void Awake()
	{
		VTResources.LoadCustomScenarios();
		Shader.EnableKeyword("UNITY_UI_CLIP_RECT");
		MaskableGraphic[] componentsInChildren = GetComponentsInChildren<MaskableGraphic>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Material materialForRendering = componentsInChildren[i].materialForRendering;
			if (materialForRendering != null)
			{
				materialForRendering.EnableKeyword("UNITY_UI_CLIP_RECT");
			}
		}
	}

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

	public void Open()
	{
		mainMenuWindow.SetActive(value: true);
	}

	public void Close()
	{
		mainMenuWindow.SetActive(value: false);
	}

	public void CampaignEditorButton()
	{
		campaignMenu.Open();
		Close();
	}

	public void StandaloneMissionButton()
	{
		standaloneWindow.Open(null);
		Close();
	}

	public void QuitButton()
	{
		Close();
		sceneSwitchLoadingObj.SetActive(value: true);
		LoadingSceneController.SwitchToVRScene("SamplerScene");
	}

	public void OpenSteamWorkshop()
	{
		swBrowser.gameObject.SetActive(value: true);
		mainMenuWindow.SetActive(value: false);
	}

	public void CloseSteamWorkshop()
	{
		swBrowser.gameObject.SetActive(value: false);
		mainMenuWindow.SetActive(value: true);
	}
}
