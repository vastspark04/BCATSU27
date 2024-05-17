using UnityEngine;
using VTOLVR.Multiplayer;

public class MFDGameMenu : MonoBehaviour
{
	private bool recentering;

	public GameObject recenterDisplayObject;

	public MFDPage page;

	public MFDPortalPage portalPage;

	public GameObject[] hideOnRecenter;

	private void Update()
	{
		if (!recentering)
		{
			return;
		}
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if (controller.thumbButtonPressed)
			{
				EndRecenter();
			}
		}
	}

	public void BeginRecenter()
	{
		recenterDisplayObject.SetActive(value: true);
		if ((bool)page)
		{
			page.locked = true;
		}
		if ((bool)portalPage)
		{
			portalPage.locked = true;
		}
		if (hideOnRecenter != null)
		{
			hideOnRecenter.SetActive(active: false);
		}
		recentering = true;
	}

	private void EndRecenter()
	{
		VRHead.ReCenter();
		recentering = false;
		recenterDisplayObject.SetActive(value: false);
		if ((bool)page)
		{
			page.locked = false;
		}
		if ((bool)portalPage)
		{
			portalPage.locked = false;
		}
		if (hideOnRecenter != null)
		{
			hideOnRecenter.SetActive(active: true);
		}
	}

	public void QuickSave()
	{
		if (QuicksaveManager.instance.CheckQsEligibility() && QuicksaveManager.instance.CheckScenarioQsLimits())
		{
			QuicksaveManager.instance.Quicksave();
		}
	}

	public void QuickLoad()
	{
		QuicksaveManager.instance.Quickload();
	}

	public void ReturnToMainButton()
	{
		FlightSceneManager.instance.ReturnToBriefingOrExitScene();
	}

	public void ReloadSceneButton()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			FlightSceneManager.instance.ReturnToBriefingOrExitScene();
		}
		else
		{
			FlightSceneManager.instance.ReloadScene();
		}
	}
}
