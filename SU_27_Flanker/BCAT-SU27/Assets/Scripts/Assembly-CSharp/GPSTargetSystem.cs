using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class GPSTargetSystem
{
	public Dictionary<string, GPSTargetGroup> targetGroups;

	public List<string> groupNames;

	private int customGroupIdx = 1;

	private Dictionary<string, int> prefixes = new Dictionary<string, int>();

	public UnityEvent onGPSTargetsChanged;

	public bool remoteOnly { get; set; }

	public MultiUserVehicleSync muvs { get; set; }

	public GPSTargetGroup currentGroup
	{
		get
		{
			if (noGroups)
			{
				return null;
			}
			return targetGroups[currentGroupName];
		}
	}

	public string currentGroupName
	{
		get
		{
			if (noGroups)
			{
				return string.Empty;
			}
			return groupNames[currGroupIdx];
		}
	}

	public int currGroupIdx { get; private set; }

	public bool noGroups { get; private set; }

	public bool hasTarget
	{
		get
		{
			if (!noGroups)
			{
				return currentGroup.currentTargetIdx >= 0;
			}
			return false;
		}
	}

	public string GetGroupName(int groupIdx)
	{
		return groupNames[groupIdx];
	}

	public GPSTargetSystem()
	{
		targetGroups = new Dictionary<string, GPSTargetGroup>();
		noGroups = true;
		groupNames = new List<string>();
		onGPSTargetsChanged = new UnityEvent();
	}

	public void RenameGroup(string oldName, string denom, int numeral)
	{
		GPSTargetGroup gPSTargetGroup = targetGroups[oldName];
		int index = groupNames.IndexOf(oldName);
		targetGroups.Remove(oldName);
		gPSTargetGroup.denom = denom;
		gPSTargetGroup.numeral = numeral;
		groupNames[index] = gPSTargetGroup.groupName;
		targetGroups.Add(gPSTargetGroup.groupName, gPSTargetGroup);
		TargetsChanged();
	}

	public void AddTarget(Vector3 worldPosition, string prefix)
	{
		int num = 0;
		if (prefixes.ContainsKey(prefix))
		{
			num = prefixes[prefix];
			prefixes[prefix] = num + 1;
		}
		else
		{
			prefixes.Add(prefix, 1);
		}
		currentGroup.AddTarget(new GPSTarget(worldPosition, prefix, num));
		TargetsChanged();
	}

	public void RemoveCurrentTarget()
	{
		if (!noGroups)
		{
			if (remoteOnly)
			{
				muvs.RemoteGPS_DeleteTarget();
				return;
			}
			currentGroup.RemoveCurrentTarget();
			TargetsChanged();
		}
	}

	public GPSTargetGroup CreateGroup(string denom, int numeral)
	{
		string text = $"{denom} {numeral}";
		if (groupNames.Contains(text))
		{
			return targetGroups[text];
		}
		GPSTargetGroup gPSTargetGroup = new GPSTargetGroup(denom, numeral);
		targetGroups.Add(text, gPSTargetGroup);
		if (targetGroups.Count == 1)
		{
			currGroupIdx = 0;
		}
		groupNames.Add(text);
		noGroups = false;
		TargetsChanged();
		return gPSTargetGroup;
	}

	public void CreateCustomGroup(bool sendIfRemote = true)
	{
		if (remoteOnly && sendIfRemote)
		{
			muvs.RemoteGPS_CreateGroup();
			return;
		}
		GPSTargetGroup gPSTargetGroup = CreateGroup("GRP", customGroupIdx);
		customGroupIdx++;
		SetCurrentGroup(gPSTargetGroup.groupName);
	}

	public void RemoveCurrentGroup(bool sendIfRemote = true)
	{
		if (noGroups)
		{
			return;
		}
		if (sendIfRemote && remoteOnly)
		{
			muvs.RemoteGPS_DeleteGroup();
			return;
		}
		string text = currentGroupName;
		targetGroups.Remove(text);
		groupNames.Remove(text);
		int count = groupNames.Count;
		if (count == 0)
		{
			noGroups = true;
			currGroupIdx = 0;
		}
		else if (currGroupIdx == count)
		{
			currGroupIdx--;
		}
		TargetsChanged();
	}

	public bool SetCurrentGroup(string groupName)
	{
		if (noGroups)
		{
			return false;
		}
		int num = groupNames.IndexOf(groupName);
		if (num >= 0)
		{
			currGroupIdx = num;
			TargetsChanged();
			return true;
		}
		return false;
	}

	public void SetCurrentGroup(int idx)
	{
		if (!noGroups)
		{
			currGroupIdx = idx;
			TargetsChanged();
		}
	}

	public void NextTarget(bool sendIfRemote = true)
	{
		if (!noGroups)
		{
			if (remoteOnly && sendIfRemote)
			{
				muvs.RemoteGPS_NextTgt();
				return;
			}
			currentGroup.NextTarget();
			TargetsChanged();
		}
	}

	public void PreviousTarget(bool sendIfRemote = true)
	{
		if (!noGroups)
		{
			if (remoteOnly && sendIfRemote)
			{
				muvs.RemoteGPS_PrevTgt();
				return;
			}
			currentGroup.PreviousTarget();
			TargetsChanged();
		}
	}

	public void NextGroup(bool sendIfRemote = true)
	{
		if (!noGroups)
		{
			if (remoteOnly && sendIfRemote)
			{
				muvs.RemoteGPS_NextGrp();
				return;
			}
			currGroupIdx = (currGroupIdx + 1) % groupNames.Count;
			TargetsChanged();
		}
	}

	public void PreviousGroup(bool sendIfRemote = true)
	{
		if (noGroups)
		{
			return;
		}
		if (remoteOnly && sendIfRemote)
		{
			muvs.RemoteGPS_PrevGrp();
			return;
		}
		currGroupIdx--;
		if (currGroupIdx < 0)
		{
			currGroupIdx = groupNames.Count - 1;
		}
		TargetsChanged();
	}

	public void SelectTarget(GPSTargetGroup grp, int idx)
	{
		if (noGroups)
		{
			return;
		}
		int num = groupNames.IndexOf(grp.groupName);
		if (num < 0)
		{
			return;
		}
		grp = targetGroups[grp.groupName];
		if (grp.targets.Count > idx)
		{
			currGroupIdx = num;
			while (grp.currentTargetIdx != idx)
			{
				grp.NextTarget();
			}
			TargetsChanged();
		}
	}

	public void TargetsChanged()
	{
		if (onGPSTargetsChanged != null)
		{
			onGPSTargetsChanged.Invoke();
		}
	}

	public void MoveCurrentTargetUp()
	{
		if (!noGroups && currentGroup.currentTargetIdx != 0 && currentGroup.targets.Count >= 2)
		{
			if (remoteOnly)
			{
				muvs.RemoteGPS_MoveTgtUp();
				return;
			}
			GPSTarget value = currentGroup.targets[currentGroup.currentTargetIdx - 1];
			currentGroup.targets[currentGroup.currentTargetIdx - 1] = currentGroup.currentTarget;
			currentGroup.targets[currentGroup.currentTargetIdx] = value;
			currentGroup.currentTargetIdx--;
			TargetsChanged();
		}
	}

	public void MoveCurrentTargetDown()
	{
		if (!noGroups && currentGroup.currentTargetIdx != currentGroup.targets.Count - 1 && currentGroup.targets.Count >= 2)
		{
			if (remoteOnly)
			{
				muvs.RemoteGPS_MoveTgtDown();
				return;
			}
			GPSTarget value = currentGroup.targets[currentGroup.currentTargetIdx + 1];
			currentGroup.targets[currentGroup.currentTargetIdx + 1] = currentGroup.currentTarget;
			currentGroup.targets[currentGroup.currentTargetIdx] = value;
			currentGroup.currentTargetIdx++;
			TargetsChanged();
		}
	}

	public bool TogglePathMode()
	{
		if (noGroups)
		{
			return false;
		}
		currentGroup.isPath = !currentGroup.isPath;
		TargetsChanged();
		return currentGroup.isPath;
	}

	public void UpdateRemotelyModifiedGroups()
	{
		noGroups = groupNames.Count == 0;
	}
}
