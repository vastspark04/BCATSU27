using UnityEngine;

public class AH94PilotSwitcher : MonoBehaviour, IQSVehicleComponent
{
	public ABObjectToggler objectToggler;

	public PlayerVehicleSetup pvs;

	public FlybyCameraMFDPage sCam;

	public AudioListener frontListener;

	public AudioListener rearListener;

	public TargetingMFDPage tgpPage;

	public MFD[] rearMfds;

	public MFD[] frontMfds;

	private string[] frontPages;

	private string[] rearPages;

	private bool isFront;

	public VRDoor[] frontDoors;

	public VRDoor[] rearDoors;

	private bool wasHeadMode;

	private void Awake()
	{
		frontPages = new string[frontMfds.Length];
		rearPages = new string[rearMfds.Length];
		pvs.OnBeginRearming.AddListener(OpenDoorOnRearm);
		pvs.OnEndRearming.AddListener(OpenDoorOnRearm);
	}

	public void SetToFront()
	{
		SavePages(rearMfds, rearPages);
		objectToggler.SetToB();
		isFront = true;
		pvs.GetComponent<VisualTargetFinder>().fovReference = VRHead.instance.transform;
		sCam.SetPlayerAudioListener(frontListener);
		RestorePages(frontMfds, frontPages);
	}

	public void SetToRear()
	{
		SavePages(frontMfds, frontPages);
		objectToggler.SetToA();
		isFront = false;
		pvs.GetComponent<VisualTargetFinder>().fovReference = VRHead.instance.transform;
		sCam.SetPlayerAudioListener(rearListener);
		RestorePages(rearMfds, rearPages);
	}

	private void SavePages(MFD[] mfds, string[] pageNames)
	{
		for (int i = 0; i < mfds.Length; i++)
		{
			MFD mFD = mfds[i];
			if (mFD.activePage != null && mFD.activePage.pageName != "home")
			{
				pageNames[i] = mFD.activePage.pageName;
			}
			else
			{
				pageNames[i] = string.Empty;
			}
		}
		wasHeadMode = (bool)tgpPage && tgpPage.tgpMode == TargetingMFDPage.TGPModes.HEAD && tgpPage.isSOI;
	}

	private void RestorePages(MFD[] mfds, string[] pageNames)
	{
		for (int i = 0; i < mfds.Length; i++)
		{
			MFD mFD = mfds[i];
			if (!string.IsNullOrEmpty(pageNames[i]))
			{
				mFD.SetPage(pageNames[i]);
				if (wasHeadMode && mFD.activePage == tgpPage)
				{
					tgpPage.MFDHeadButton();
				}
			}
			else
			{
				mFD.GoHome();
			}
		}
	}

	private void OpenDoorOnRearm()
	{
		VRDoor[] array;
		if (isFront)
		{
			bool flag = false;
			array = frontDoors;
			foreach (VRDoor vRDoor in array)
			{
				if (!vRDoor.isLatched)
				{
					vRDoor.RemoteSetState(1f);
					flag = true;
				}
			}
			if (!flag)
			{
				frontDoors[0].RemoteSetState(1f);
			}
			return;
		}
		bool flag2 = false;
		array = rearDoors;
		foreach (VRDoor vRDoor2 in array)
		{
			if (!vRDoor2.isLatched)
			{
				vRDoor2.RemoteSetState(1f);
				flag2 = true;
			}
		}
		if (!flag2)
		{
			rearDoors[0].RemoteSetState(1f);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("AH94PilotSwitcher").SetValue("isFront", isFront);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("AH94PilotSwitcher");
		if (node != null)
		{
			bool target = false;
			ConfigNodeUtils.TryParseValue(node, "isFront", ref target);
			if (target)
			{
				SetToFront();
			}
		}
	}
}
