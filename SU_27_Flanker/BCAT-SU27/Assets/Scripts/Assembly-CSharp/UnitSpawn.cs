using System;
using UnityEngine;
using VTOLVR.Multiplayer;

public class UnitSpawn : MonoBehaviour
{
	public enum PlacementModes
	{
		Any,
		Ground,
		Water,
		Air,
		GroundOrAir
	}

	public bool hideFromEditor;

	public bool createFloatingOriginTransform = true;

	[Header("Components")]
	public Actor actor;

	[Header("Unit Info")]
	public string unitName;

	[TextArea]
	public string unitDescription;

	public VTUnitGroup.GroupTypes groupType = VTUnitGroup.GroupTypes.Unassigned;

	[HideInInspector]
	public UnitSpawner unitSpawner;

	[Header("Editor Info")]
	public string category;

	public string editorSprite;

	public bool multiplayerOnly;

	public bool singleplayerOnly;

	[Header("Placement")]
	public PlacementModes placementMode;

	public bool alignToGround;

	public float heightFromSurface;

	private int _unitID = -1;

	public bool isLocal
	{
		get
		{
			if (VTOLMPUtils.IsMultiplayer())
			{
				return VTOLMPLobbyManager.isLobbyHost;
			}
			return true;
		}
	}

	public int unitID => _unitID;

	public bool quickloaded { get; set; }

	public event Action OnSpawnedUnit;

	public void InvokeSpawnedUnitEvent()
	{
		if (this.OnSpawnedUnit != null)
		{
			this.OnSpawnedUnit();
		}
	}

	public virtual void OnSpawnUnit()
	{
	}

	public virtual void OnPreSpawnUnit()
	{
	}

	public void SetUnitInstanceID(int id)
	{
		_unitID = id;
	}

	public virtual void OnEditorUpdate(VTScenarioEditor editor)
	{
	}

	public virtual void Quicksave(ConfigNode qsNode)
	{
		IQSVehicleComponent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>();
		foreach (IQSVehicleComponent iQSVehicleComponent in componentsInChildrenImplementing)
		{
			try
			{
				iQSVehicleComponent.OnQuicksave(qsNode);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error when quicksaving component: " + UIUtils.GetHierarchyString(((Component)iQSVehicleComponent).gameObject) + " \n" + ex);
				QuicksaveManager.instance.IndicateError();
			}
		}
	}

	public virtual void Quickload(ConfigNode qsNode)
	{
		IQSVehicleComponent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>();
		foreach (IQSVehicleComponent iQSVehicleComponent in componentsInChildrenImplementing)
		{
			try
			{
				iQSVehicleComponent.OnQuickload(qsNode);
			}
			catch (Exception ex)
			{
				Debug.LogError("Error when quickloading component: " + UIUtils.GetHierarchyString(((Component)iQSVehicleComponent).gameObject) + " \n" + ex);
				QuicksaveManager.instance.IndicateError();
			}
		}
	}
}
