using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MFDPortalManager : MonoBehaviour, IQSVehicleComponent
{
	public GameObject poweredObject;

	public MFDPortalHalf halfLeft;

	public MFDPortalHalf halfRight;

	public MFDPHome homePageTemplate;

	public List<MFDPortalPage> pages;

	public Battery battery;

	private bool poweredOn;

	public AudioSource uiInputAudioSource;

	public AudioClip inputSound;

	private MFDPortalQuarter[] quarters;

	public PoseBounds portalsPoseBounds;

	private bool playInputSoundThisFrame;

	public bool hasPower { get; private set; }

	public void SetPower(int pwr)
	{
		poweredOn = pwr > 0;
	}

	private void Awake()
	{
		quarters = new MFDPortalQuarter[4] { halfLeft.quarterLeft, halfLeft.quarterRight, halfRight.quarterLeft, halfRight.quarterRight };
		if ((bool)portalsPoseBounds)
		{
			VRInteractable[] componentsInChildren = GetComponentsInChildren<VRInteractable>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].poseBounds = portalsPoseBounds;
			}
		}
	}

	private void Start()
	{
		foreach (MFDPortalPage page in pages)
		{
			page.gameObject.SetActive(value: false);
		}
		SetupHomepageInstances();
	}

	private void Update()
	{
		hasPower = poweredOn && (bool)battery && battery.Drain(0.01f * Time.deltaTime);
		poweredObject.SetActive(hasPower);
		if (playInputSoundThisFrame)
		{
			if (hasPower)
			{
				uiInputAudioSource.PlayOneShot(inputSound);
			}
			playInputSoundThisFrame = false;
		}
	}

	public void SwapHalves()
	{
		MFDPortalHalf mFDPortalHalf = halfLeft;
		Vector3 localPosition = halfLeft.transform.localPosition;
		Vector3 localPosition2 = halfRight.transform.localPosition;
		halfLeft.transform.localPosition = localPosition2;
		halfRight.transform.localPosition = localPosition;
		halfLeft = halfRight;
		halfRight = mFDPortalHalf;
	}

	private void SetupHomepageInstances()
	{
		SetupHomepageInstance(halfLeft.quarterLeft);
		SetupHomepageInstance(halfLeft.quarterRight);
		SetupHomepageInstance(halfRight.quarterLeft);
		SetupHomepageInstance(halfRight.quarterRight);
		homePageTemplate.gameObject.SetActive(value: false);
	}

	private void SetupHomepageInstance(MFDPortalQuarter qtr)
	{
		GameObject obj = Object.Instantiate(homePageTemplate.gameObject);
		obj.transform.SetParent(qtr.transform, worldPositionStays: true);
		MFDPHome component = obj.GetComponent<MFDPHome>();
		component.quarter = qtr;
		component.SetupButtons(qtr, pages);
		qtr.homepageInstance = component;
		qtr.DisplayMenu();
		qtr.SetSubLeftPage(null);
		qtr.SetSubRightPage(null);
	}

	public void OnInputAxis(Vector3 axis)
	{
		for (int i = 0; i < quarters.Length; i++)
		{
			quarters[i].OnInputAxis(axis);
		}
	}

	public void OnInputButtonDown()
	{
		for (int i = 0; i < quarters.Length; i++)
		{
			quarters[i].OnInputButtonDown();
		}
	}

	public void OnInputAxisReleased()
	{
		for (int i = 0; i < quarters.Length; i++)
		{
			quarters[i].OnInputAxisReleased();
		}
	}

	public void OnInputButtonUp()
	{
		for (int i = 0; i < quarters.Length; i++)
		{
			quarters[i].OnInputButtonUp();
		}
	}

	public void OnInputButton()
	{
		for (int i = 0; i < quarters.Length; i++)
		{
			quarters[i].OnInputButton();
		}
	}

	public void UpdateSOIUIs()
	{
		for (int i = 0; i < quarters.Length; i++)
		{
			quarters[i].UpdateSOIUI();
		}
	}

	public void DisableAllSOI()
	{
		for (int i = 0; i < pages.Count; i++)
		{
			pages[i].isSOI = false;
		}
		UpdateSOIUIs();
	}

	public void PlayInputSound()
	{
		playInputSoundThisFrame = true;
	}

	public void ResetPortals()
	{
		ResetHalf(halfLeft);
		ResetHalf(halfRight);
	}

	private void ResetHalf(MFDPortalHalf half)
	{
		if (half.quarterLeft.displayState == MFDPortalQuarter.DisplayStates.Maximized)
		{
			half.quarterLeft.HideSubs();
		}
		else if (half.quarterRight.displayState == MFDPortalQuarter.DisplayStates.Maximized)
		{
			half.quarterRight.HideSubs();
		}
		if (half.quarterLeft.displayState == MFDPortalQuarter.DisplayStates.HideSubs)
		{
			half.quarterLeft.ShowSubs();
		}
		if (half.quarterRight.displayState == MFDPortalQuarter.DisplayStates.HideSubs)
		{
			half.quarterRight.ShowSubs();
		}
		half.quarterLeft.DisplayPage(null);
		half.quarterLeft.SetSubLeftPage(null);
		half.quarterLeft.SetSubRightPage(null);
		half.quarterRight.DisplayPage(null);
		half.quarterRight.SetSubLeftPage(null);
		half.quarterRight.SetSubRightPage(null);
	}

	private ConfigNode SavePreset(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.AddNode(SaveHalf("HalfLeft", halfLeft));
		configNode.AddNode(SaveHalf("HalfRight", halfRight));
		return configNode;
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

	private void LoadPreset(ConfigNode savedPreset)
	{
		if (savedPreset != null)
		{
			ResetPortals();
			ConfigNode node = savedPreset.GetNode("HalfLeft");
			ConfigNode node2 = savedPreset.GetNode("HalfRight");
			LoadHalf(halfLeft, node);
			LoadHalf(halfRight, node2);
			if (halfLeft.quarterLeft.displayState == MFDPortalQuarter.DisplayStates.Maximized)
			{
				halfLeft.quarterLeft.HideSubs();
				halfLeft.quarterLeft.MaximizeDisplay();
			}
			if (halfRight.quarterLeft.displayState == MFDPortalQuarter.DisplayStates.Maximized)
			{
				halfRight.quarterLeft.HideSubs();
				halfRight.quarterLeft.MaximizeDisplay();
			}
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
		foreach (MFDPortalPage page in pages)
		{
			if (page.pageName == pageName)
			{
				return page;
			}
		}
		return null;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode(SavePreset("MFDLayout"));
		ConfigNode configNode = qsNode.AddNode("MFDPortalManager");
		MFDPortalPage mFDPortalPage = null;
		foreach (MFDPortalPage page in pages)
		{
			if (page.isSOI)
			{
				mFDPortalPage = page;
				break;
			}
		}
		configNode.SetValue("soiPage", (mFDPortalPage == null) ? string.Empty : mFDPortalPage.pageName);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("MFDLayout");
		ConfigNode node2 = qsNode.GetNode("MFDPortalManager");
		if (node != null)
		{
			StartCoroutine(PortalQuickloadRoutine(node, node2));
		}
	}

	private IEnumerator PortalQuickloadRoutine(ConfigNode sNode, ConfigNode mfdpNode)
	{
		yield return null;
		LoadPreset(sNode);
		yield return null;
		if (mfdpNode == null)
		{
			yield break;
		}
		string value = mfdpNode.GetValue("soiPage");
		if (string.IsNullOrEmpty(value))
		{
			yield break;
		}
		foreach (MFDPortalPage page in pages)
		{
			if (page.pageName == value)
			{
				page.ToggleInput();
				break;
			}
		}
	}
}
