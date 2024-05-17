using System;
using UnityEngine;

public class VTEdPropertyFieldTemplates : MonoBehaviour
{
	public GameObject prefabsParent;

	[Header("Fields")]
	public GameObject unitField;

	public GameObject waypointField;

	public GameObject enumField;

	public GameObject pathField;

	public GameObject boolField;

	public GameObject floatRangeField;

	public GameObject unitListField;

	public GameObject unitGroupField;

	public GameObject wingmanVoiceField;

	public GameObject awacsVoiceField;

	public GameObject conditionalField;

	public GameObject airportField;

	public GameObject equipmentListField;

	[Header("Parameters")]
	public GameObject unitParameter;

	public GameObject unitListParameter;

	public GameObject waypointParameter;

	public GameObject enumParameter;

	public GameObject pathParameter;

	public GameObject boolParameter;

	public GameObject floatRangeParameter;

	public GameObject airportParameter;

	public GameObject audioRefParameter;

	public GameObject globalPosParameter;

	public GameObject unitGroupParameter;

	public GameObject conditionalActionParameter;

	public GameObject stringParameter;

	public GameObject vehicleControlParameter;

	public GameObject videoParameter;

	public GameObject globalValueParameter;

	private void Awake()
	{
		prefabsParent.SetActive(value: false);
	}

	public GameObject GetPropertyFieldForType(Type type, Transform parent)
	{
		GameObject gameObject = null;
		if (type == typeof(UnitReference))
		{
			gameObject = unitField;
		}
		else if (type == typeof(Waypoint))
		{
			gameObject = waypointField;
		}
		else if (type.IsEnum)
		{
			gameObject = enumField;
		}
		else if (type == typeof(FollowPath))
		{
			gameObject = pathField;
		}
		else if (type == typeof(bool))
		{
			gameObject = boolField;
		}
		else if (type == typeof(float) || type == typeof(int))
		{
			gameObject = floatRangeField;
		}
		else if (type == typeof(UnitReferenceList) || type.IsSubclassOf(typeof(UnitReferenceList)))
		{
			gameObject = unitListField;
		}
		else if (type == typeof(VTUnitGroup.UnitGroup))
		{
			gameObject = unitGroupField;
		}
		else if (type == typeof(WingmanVoiceProfile))
		{
			gameObject = wingmanVoiceField;
		}
		else if (type == typeof(AWACSVoiceProfile))
		{
			gameObject = awacsVoiceField;
		}
		else if (type == typeof(ScenarioConditional))
		{
			gameObject = conditionalField;
		}
		else if (type == typeof(AirportReference))
		{
			gameObject = airportField;
		}
		else if (type == typeof(VehicleEquipmentList))
		{
			gameObject = equipmentListField;
		}
		if (gameObject != null)
		{
			return UnityEngine.Object.Instantiate(gameObject, parent);
		}
		return null;
	}

	public GameObject GetParameterForType(Type type, Transform parent)
	{
		GameObject gameObject = null;
		if (type == typeof(UnitReference))
		{
			gameObject = unitParameter;
		}
		else if (type == typeof(Waypoint))
		{
			gameObject = waypointParameter;
		}
		else if (type.IsEnum)
		{
			gameObject = enumParameter;
		}
		else if (type == typeof(FollowPath))
		{
			gameObject = pathParameter;
		}
		else if (type == typeof(bool))
		{
			gameObject = boolParameter;
		}
		else if (type == typeof(float) || type == typeof(int))
		{
			gameObject = floatRangeParameter;
		}
		else if (type == typeof(AirportReference))
		{
			gameObject = airportParameter;
		}
		else if (type == typeof(VTSAudioReference))
		{
			gameObject = audioRefParameter;
		}
		else if (type == typeof(UnitReferenceList) || type.IsSubclassOf(typeof(UnitReferenceList)))
		{
			gameObject = unitListParameter;
		}
		else if (type == typeof(FixedPoint))
		{
			gameObject = globalPosParameter;
		}
		else if (type == typeof(VTUnitGroup.UnitGroup))
		{
			gameObject = unitGroupParameter;
		}
		else if (type == typeof(ConditionalActionReference))
		{
			gameObject = conditionalActionParameter;
		}
		else if (type == typeof(string))
		{
			gameObject = stringParameter;
		}
		else if (type == typeof(VehicleControlReference))
		{
			gameObject = vehicleControlParameter;
		}
		else if (type == typeof(VTSVideoReference))
		{
			gameObject = videoParameter;
		}
		else if (type == typeof(GlobalValue))
		{
			gameObject = globalValueParameter;
		}
		if (gameObject != null)
		{
			return UnityEngine.Object.Instantiate(gameObject, parent);
		}
		return null;
	}
}
