using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class MFDGPSTargets : MonoBehaviour, ILocalizationUser
{
	[Header("Targets")]
	public Transform targetsParent;

	public GameObject targetTemplate;

	public RectTransform targetWindowRect;

	public Transform targetSelectTransform;

	private List<Text> targetTexts;

	private float targetEntryHeight;

	private float targetWindowHeight;

	[Header("Groups")]
	public Transform groupsParent;

	public GameObject groupTemplate;

	public RectTransform groupWindowRect;

	public Transform groupSelectTransform;

	private List<Text> groupTexts;

	private float groupEntryHeight;

	private float groupWindowHeight;

	public WeaponManager wm;

	public MFDPage mfdPage;

	public MFDPortalPage portalPage;

	private bool listeningToGpsSys;

	private string s_mfdGps_path = "PATH";

	private bool isSharingGroup;

	private GPSTargetGroup sharedGroupToRename;

	public bool remoteOnly { get; set; }

	public MultiUserVehicleSync muvs { get; set; }

	private void Awake()
	{
		ApplyLocalization();
		if (!wm)
		{
			wm = GetComponentInParent<WeaponManager>();
		}
		if (!mfdPage)
		{
			mfdPage = GetComponent<MFDPage>();
		}
		if (!portalPage)
		{
			portalPage = GetComponent<MFDPortalPage>();
		}
		targetTemplate.SetActive(value: false);
		targetEntryHeight = ((RectTransform)targetTemplate.transform).rect.height * targetTemplate.transform.localScale.y;
		targetWindowHeight = targetWindowRect.rect.height;
		targetTexts = new List<Text>();
		groupTemplate.SetActive(value: false);
		groupEntryHeight = ((RectTransform)groupTemplate.transform).rect.height * groupTemplate.transform.localScale.y;
		groupWindowHeight = groupWindowRect.rect.height;
		groupTexts = new List<Text>();
		if ((bool)mfdPage)
		{
			mfdPage.OnActivatePage.AddListener(OnActivatePage);
			mfdPage.OnDeactivatePage.AddListener(OnDeactivatePage);
		}
		if ((bool)portalPage)
		{
			portalPage.OnShowPage.AddListener(OnActivatePage);
			portalPage.OnHidePage.AddListener(OnDeactivatePage);
		}
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTOLMPDataLinkManager.instance.OnClearedGPSGroup += Instance_OnClearedGPSGroup;
			VTOLMPDataLinkManager.instance.OnReceivedGPSTarget += Instance_OnReceivedGPSTarget;
		}
	}

	private void OnDestroy()
	{
		if ((bool)VTOLMPDataLinkManager.instance)
		{
			VTOLMPDataLinkManager.instance.OnClearedGPSGroup -= Instance_OnClearedGPSGroup;
			VTOLMPDataLinkManager.instance.OnReceivedGPSTarget -= Instance_OnReceivedGPSTarget;
		}
	}

	private void Instance_OnReceivedGPSTarget(PlayerInfo owner, int groupId, int index, GPSTarget target)
	{
		if (owner.team != wm.actor.team)
		{
			return;
		}
		GPSTargetGroup gPSTargetGroup = null;
		foreach (GPSTargetGroup value in wm.gpsSystem.targetGroups.Values)
		{
			if (value.datalinkID == groupId)
			{
				Debug.Log($" >> found existing GPS group for datalinked target, datalink ID {groupId}");
				gPSTargetGroup = value;
				break;
			}
		}
		if (gPSTargetGroup == null)
		{
			Debug.Log($" >> creating new GPS group for a datalinked target, datalink ID {groupId}");
			string text = owner.pilotName;
			if (text.Length > 3)
			{
				text = owner.pilotName.Substring(0, 3);
			}
			else if (text.Length < 3)
			{
				for (int i = text.Length; i < 3; i++)
				{
					text += " ";
				}
			}
			text = text.ToUpper();
			gPSTargetGroup = wm.gpsSystem.CreateGroup(text, groupId);
			gPSTargetGroup.datalinkID = groupId;
		}
		gPSTargetGroup.AddTarget(target);
		wm.gpsSystem.TargetsChanged();
	}

	private void Instance_OnClearedGPSGroup(PlayerInfo owner, int groupId)
	{
		if (owner.team != wm.actor.team)
		{
			return;
		}
		GPSTargetGroup gPSTargetGroup = null;
		foreach (KeyValuePair<string, GPSTargetGroup> targetGroup in wm.gpsSystem.targetGroups)
		{
			if (targetGroup.Value.datalinkID == groupId)
			{
				gPSTargetGroup = targetGroup.Value;
			}
		}
		if (gPSTargetGroup == null)
		{
			string text = owner.pilotName;
			if (text.Length > 3)
			{
				text = owner.pilotName.Substring(0, 3);
			}
			text = text.ToUpper();
			gPSTargetGroup = wm.gpsSystem.CreateGroup(text, groupId);
			gPSTargetGroup.datalinkID = groupId;
		}
		else
		{
			gPSTargetGroup.RemoveAllTargets();
		}
		wm.gpsSystem.TargetsChanged();
	}

	private void OnActivatePage()
	{
		wm.gpsSystem.onGPSTargetsChanged.AddListener(OnTargetsChanged);
		listeningToGpsSys = true;
		OnTargetsChanged();
	}

	private void OnDeactivatePage()
	{
		if (wm.gpsSystem != null && listeningToGpsSys)
		{
			wm.gpsSystem.onGPSTargetsChanged.RemoveListener(OnTargetsChanged);
			listeningToGpsSys = false;
		}
	}

	public void NextTarget()
	{
		if (!wm.gpsSystem.noGroups)
		{
			wm.gpsSystem.NextTarget();
			SetTargetSelectorPosition();
		}
	}

	public void PreviousTarget()
	{
		if (!wm.gpsSystem.noGroups)
		{
			wm.gpsSystem.PreviousTarget();
			SetTargetSelectorPosition();
		}
	}

	public void NextGroup()
	{
		if (!wm.gpsSystem.noGroups && !isSharingGroup)
		{
			wm.gpsSystem.NextGroup();
			SetGroupSelectorPosition();
		}
	}

	public void PreviousGroup()
	{
		if (!wm.gpsSystem.noGroups && !isSharingGroup)
		{
			wm.gpsSystem.PreviousGroup();
			SetGroupSelectorPosition();
		}
	}

	public void CreateCustomGroup()
	{
		if (!isSharingGroup)
		{
			wm.gpsSystem.CreateCustomGroup();
		}
	}

	public void DeleteCurrentGroup()
	{
		if (!wm.gpsSystem.noGroups && !isSharingGroup)
		{
			wm.gpsSystem.RemoveCurrentGroup();
		}
	}

	public void DeleteCurrentTarget()
	{
		if (!wm.gpsSystem.noGroups && !isSharingGroup)
		{
			wm.gpsSystem.RemoveCurrentTarget();
		}
	}

	private void OnTargetsChanged()
	{
		if (targetTexts != null)
		{
			foreach (Text targetText in targetTexts)
			{
				Object.Destroy(targetText.gameObject);
			}
			targetTexts = new List<Text>();
		}
		if (groupTexts != null)
		{
			foreach (Text groupText in groupTexts)
			{
				Object.Destroy(groupText.gameObject);
			}
			groupTexts = new List<Text>();
		}
		if (wm.gpsSystem.noGroups)
		{
			targetSelectTransform.gameObject.SetActive(value: false);
			groupSelectTransform.gameObject.SetActive(value: false);
			if ((bool)mfdPage)
			{
				mfdPage.SetText("pathLabel", "");
			}
			else if ((bool)portalPage)
			{
				portalPage.SetText("pathLabel", "");
			}
		}
		else
		{
			PopulateTargetList();
			PopulateGroupsList();
			if ((bool)mfdPage)
			{
				mfdPage.SetText("pathLabel", s_mfdGps_path, wm.gpsSystem.currentGroup.isPath ? Color.green : Color.white);
			}
			else if ((bool)portalPage)
			{
				portalPage.SetText("pathLabel", s_mfdGps_path, wm.gpsSystem.currentGroup.isPath ? Color.green : Color.white);
			}
		}
		SetTargetSelectorPosition();
		SetGroupSelectorPosition();
	}

	private void PopulateTargetList()
	{
		int num = 0;
		foreach (GPSTarget target in wm.gpsSystem.currentGroup.targets)
		{
			GameObject gameObject = Object.Instantiate(targetTemplate, targetsParent);
			Text component = gameObject.GetComponent<Text>();
			targetTexts.Add(component);
			string text = (component.text = target.fullGpsLabel);
			float y = (float)num * (0f - targetEntryHeight);
			gameObject.transform.localPosition = new Vector3(0f, y, 0f);
			gameObject.SetActive(value: true);
			num++;
		}
		SetTargetSelectorPosition();
	}

	private void SetTargetSelectorPosition()
	{
		if (wm.gpsSystem.noGroups || wm.gpsSystem.currentGroup.targets.Count == 0)
		{
			targetSelectTransform.gameObject.SetActive(value: false);
			targetsParent.localPosition = Vector3.zero;
			return;
		}
		targetSelectTransform.gameObject.SetActive(value: true);
		float num = (float)wm.gpsSystem.currentGroup.currentTargetIdx * (0f - targetEntryHeight);
		targetSelectTransform.localPosition = new Vector3(0f, num, 0f);
		Vector3 localPosition = targetsParent.localPosition;
		if (num < 0f - targetWindowHeight + 1f - localPosition.y)
		{
			localPosition.y = 0f - num - targetWindowHeight + targetEntryHeight;
		}
		else if (num > 0f - localPosition.y - 1f)
		{
			localPosition.y = 0f - num;
		}
		targetsParent.localPosition = localPosition;
	}

	private void PopulateGroupsList()
	{
		for (int i = 0; i < wm.gpsSystem.targetGroups.Count; i++)
		{
			GameObject obj = Object.Instantiate(groupTemplate, groupsParent);
			Text component = obj.GetComponent<Text>();
			groupTexts.Add(component);
			component.text = wm.gpsSystem.GetGroupName(i);
			float y = (float)i * (0f - groupEntryHeight);
			obj.transform.localPosition = new Vector3(0f, y, 0f);
			obj.SetActive(value: true);
		}
		SetGroupSelectorPosition();
	}

	public void SetGroupSelectorPosition()
	{
		if (wm.gpsSystem.noGroups)
		{
			groupSelectTransform.gameObject.SetActive(value: false);
			groupsParent.localPosition = Vector3.zero;
			return;
		}
		groupSelectTransform.gameObject.SetActive(value: true);
		float num = (float)wm.gpsSystem.currGroupIdx * (0f - groupEntryHeight);
		groupSelectTransform.localPosition = new Vector3(0f, num, 0f);
		Vector3 localPosition = groupsParent.localPosition;
		if (num < 0f - groupWindowHeight + 1f - localPosition.y)
		{
			localPosition.y = 0f - num - groupWindowHeight + groupEntryHeight;
		}
		else if (num > 0f - localPosition.y - 1f)
		{
			localPosition.y = 0f - num;
		}
		groupsParent.localPosition = localPosition;
	}

	public void ToggleGroupPath()
	{
		if (!wm.gpsSystem.noGroups)
		{
			bool flag = wm.gpsSystem.TogglePathMode();
			if ((bool)mfdPage)
			{
				mfdPage.SetText("pathLabel", s_mfdGps_path, flag ? Color.green : Color.white);
			}
			else if ((bool)portalPage)
			{
				portalPage.SetText("pathLabel", s_mfdGps_path, flag ? Color.green : Color.white);
			}
		}
	}

	public void MoveTargetUp()
	{
		wm.gpsSystem.MoveCurrentTargetUp();
	}

	public void MoveTargetDown()
	{
		wm.gpsSystem.MoveCurrentTargetDown();
	}

	public void SetWaypoint()
	{
		if (remoteOnly)
		{
			muvs.RemoteGPS_SetWP();
		}
		else if (wm.gpsSystem.hasTarget)
		{
			WaypointManager.instance.SetWaypointGPS(wm.gpsSystem.currentGroup);
		}
	}

	public void ApplyLocalization()
	{
		s_mfdGps_path = VTLocalizationManager.GetString("s_mfdGps_path", "PATH", "MFD GPS page PATH toggle button/label");
	}

	public void ShareCurrentGroup()
	{
		if (remoteOnly)
		{
			muvs.RemoteGPS_Share();
		}
		else if (!wm.gpsSystem.noGroups && !isSharingGroup && wm.gpsSystem.currentGroup.targets != null && wm.gpsSystem.currentGroup.targets.Count != 0)
		{
			isSharingGroup = true;
			if (wm.gpsSystem.currentGroup.datalinkID <= 0)
			{
				sharedGroupToRename = wm.gpsSystem.currentGroup;
			}
			AsyncOpStatus asyncOpStatus = VTOLMPDataLinkManager.instance.ShareGPSGroup(wm.gpsSystem.currentGroup, wm.actor.team);
			if (asyncOpStatus.isDone)
			{
				OnFinishedShare();
			}
			else
			{
				asyncOpStatus.OnFinished += OnFinishedShare;
			}
		}
	}

	private void OnFinishedShare()
	{
		if (sharedGroupToRename != null)
		{
			string text = PilotSaveManager.current.pilotName;
			if (text.Length > 3)
			{
				text = PilotSaveManager.current.pilotName.Substring(0, 3);
			}
			text = text.ToUpper();
			wm.gpsSystem.RenameGroup(sharedGroupToRename.groupName, text, sharedGroupToRename.datalinkID);
		}
		sharedGroupToRename = null;
		isSharingGroup = false;
	}
}
