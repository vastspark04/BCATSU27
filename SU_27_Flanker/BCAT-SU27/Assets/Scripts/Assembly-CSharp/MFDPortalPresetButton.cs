using System.Collections;
using UnityEngine;

public class MFDPortalPresetButton : MonoBehaviour, IPersistentVehicleData
{
	public int id;

	public MFDPortalManager portals;

	public VRInteractable interactable;

	public AudioClip saveSound;

	public AudioClip loadSound;

	public MFDPortalPresetSaved savedMessage;

	private ConfigNode savedPreset;

	private float timePressed;

	private float saveHoldTime = 2f;

	private bool saved;

	private Coroutine holdingRoutine;

	private string NODE_NAME => "PortalPreset_" + id;

	private void Awake()
	{
		interactable.OnInteract.AddListener(OnPressDown);
		interactable.OnStopInteract.AddListener(OnPressUp);
	}

	private void OnPressDown()
	{
		timePressed = Time.time;
		saved = false;
		holdingRoutine = StartCoroutine(HoldingRoutine());
	}

	private IEnumerator HoldingRoutine()
	{
		yield return new WaitForSeconds(saveHoldTime);
		SavePreset();
		saved = true;
	}

	private void OnPressUp()
	{
		if (holdingRoutine != null)
		{
			StopCoroutine(holdingRoutine);
			if (!saved)
			{
				LoadPreset();
			}
		}
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (vDataNode.HasNode(NODE_NAME))
		{
			savedPreset = vDataNode.GetNode(NODE_NAME);
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		if (savedPreset != null)
		{
			if (vDataNode.HasNode(NODE_NAME))
			{
				vDataNode.RemoveNodes(NODE_NAME);
			}
			vDataNode.AddNode(savedPreset);
		}
	}

	private void SavePreset()
	{
		savedPreset = new ConfigNode(NODE_NAME);
		savedPreset.AddNode(SaveHalf("HalfLeft", portals.halfLeft));
		savedPreset.AddNode(SaveHalf("HalfRight", portals.halfRight));
		portals.uiInputAudioSource.PlayOneShot(saveSound);
		savedMessage.Display();
	}

	private ConfigNode SaveHalf(string nodeName, MFDPortalHalf half)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.AddNode(SaveQuarter("QuarterRight", half.quarterRight));
		configNode.AddNode(SaveQuarter("QuarterLeft", half.quarterLeft));
		return configNode;
	}

	private ConfigNode SaveQuarter(string nodeName, MFDPortalQuarter quarter)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		string value = string.Empty;
		string value2 = string.Empty;
		string value3 = string.Empty;
		if ((bool)quarter.displayedPage)
		{
			value = quarter.displayedPage.pageName;
			configNode.SetValue("pageState", quarter.displayedPage.pageState);
		}
		if ((bool)quarter.subLeftPage)
		{
			value2 = quarter.subLeftPage.pageName;
		}
		if ((bool)quarter.subRightPage)
		{
			value3 = quarter.subRightPage.pageName;
		}
		configNode.SetValue("dPage", value);
		configNode.SetValue("subLeftPage", value2);
		configNode.SetValue("subRightPage", value3);
		return configNode;
	}

	private void LoadPreset()
	{
		MFDPortalQuarter[] componentsInChildren = portals.gameObject.GetComponentsInChildren<MFDPortalQuarter>();
		foreach (MFDPortalQuarter mFDPortalQuarter in componentsInChildren)
		{
			if ((bool)mFDPortalQuarter.displayedPage && mFDPortalQuarter.displayedPage.locked)
			{
				return;
			}
		}
		if (savedPreset != null)
		{
			portals.ResetPortals();
			ConfigNode node = savedPreset.GetNode("HalfLeft");
			ConfigNode node2 = savedPreset.GetNode("HalfRight");
			LoadHalf(portals.halfLeft, node);
			LoadHalf(portals.halfRight, node2);
			if (portals.halfLeft.quarterLeft.displayState == MFDPortalQuarter.DisplayStates.Maximized)
			{
				portals.halfLeft.quarterLeft.HideSubs();
				portals.halfLeft.quarterLeft.MaximizeDisplay();
			}
			if (portals.halfRight.quarterLeft.displayState == MFDPortalQuarter.DisplayStates.Maximized)
			{
				portals.halfRight.quarterLeft.HideSubs();
				portals.halfRight.quarterLeft.MaximizeDisplay();
			}
			portals.uiInputAudioSource.PlayOneShot(loadSound);
		}
	}

	private void LoadHalf(MFDPortalHalf half, ConfigNode hNode)
	{
		if (hNode != null)
		{
			ConfigNode node = hNode.GetNode("QuarterLeft");
			ConfigNode node2 = hNode.GetNode("QuarterRight");
			LoadQuarter(half.quarterLeft, node);
			LoadQuarter(half.quarterRight, node2);
		}
	}

	private void LoadQuarter(MFDPortalQuarter quarter, ConfigNode qNode)
	{
		if (qNode == null)
		{
			return;
		}
		string value = qNode.GetValue("dPage");
		string value2 = qNode.GetValue("subLeftPage");
		string value3 = qNode.GetValue("subRightPage");
		if (quarter.displayState == MFDPortalQuarter.DisplayStates.Maximized)
		{
			quarter.HideSubs();
		}
		if (quarter.displayState == MFDPortalQuarter.DisplayStates.HideSubs)
		{
			quarter.ShowSubs();
		}
		quarter.DisplayPage(GetPage(value));
		quarter.SetSubLeftPage(GetPage(value2));
		quarter.SetSubRightPage(GetPage(value3));
		if ((bool)quarter.displayedPage)
		{
			switch (qNode.GetValue<MFDPortalPage.PageStates>("pageState"))
			{
			case MFDPortalPage.PageStates.Standard:
				quarter.ShowSubs();
				break;
			case MFDPortalPage.PageStates.FullHeight:
				quarter.HideSubs();
				break;
			case MFDPortalPage.PageStates.Maximized:
				quarter.MaximizeDisplay();
				break;
			case MFDPortalPage.PageStates.Uninitialized:
			case MFDPortalPage.PageStates.SubSized:
			case MFDPortalPage.PageStates.Minimized:
				break;
			}
		}
	}

	private MFDPortalPage GetPage(string pageName)
	{
		if (string.IsNullOrEmpty(pageName))
		{
			return null;
		}
		foreach (MFDPortalPage page in portals.pages)
		{
			if (page.pageName == pageName)
			{
				return page;
			}
		}
		return null;
	}
}
