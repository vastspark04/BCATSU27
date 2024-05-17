using System.Collections.Generic;
using UnityEngine.UI;

public class VTEquipmentListProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text currentValueText;

	private PlayerVehicle vehicle;

	private VehicleEquipmentList currentValue;

	private PlayerVehicle lastVeh;

	private string[] allEquips;

	private string[] allEquipNames;

	public void SetVehicle(PlayerVehicle v)
	{
		vehicle = v;
		if (!(lastVeh != vehicle) && allEquips != null)
		{
			return;
		}
		lastVeh = vehicle;
		if ((bool)vehicle)
		{
			allEquips = new string[vehicle.allEquipPrefabs.Count];
			allEquipNames = new string[allEquips.Length];
			for (int i = 0; i < allEquips.Length; i++)
			{
				allEquips[i] = vehicle.allEquipPrefabs[i].gameObject.name;
				allEquipNames[i] = vehicle.allEquipPrefabs[i].GetComponentImplementing<HPEquippable>().fullName;
			}
		}
		UpdateValueText();
	}

	public override void SetInitialValue(object value)
	{
		if (value == null)
		{
			currentValue = new VehicleEquipmentList();
		}
		else
		{
			currentValue = (VehicleEquipmentList)value;
		}
		SetVehicle(vehicle);
		UpdateValueText();
	}

	private void UpdateValueText()
	{
		currentValueText.text = "Select Equipment";
	}

	public override object GetValue()
	{
		return currentValue;
	}

	public void SelectButton()
	{
		HackyUpdateVehicle();
		List<int> list = new List<int>();
		if (currentValue != null)
		{
			for (int i = 0; i < allEquips.Length; i++)
			{
				if (!currentValue.equipment.Contains(allEquips[i]))
				{
					list.Add(i);
				}
			}
		}
		editor.multiSelector.Display("Select Equips", allEquipNames, list, OnEquipsSelected);
	}

	private void HackyUpdateVehicle()
	{
		VTEnumProperty[] componentsInChildren = base.transform.parent.GetComponentsInChildren<VTEnumProperty>();
		foreach (VTEnumProperty vTEnumProperty in componentsInChildren)
		{
			if (vTEnumProperty.GetValue() is MultiplayerSpawn.Vehicles)
			{
				SetVehicle(VTResources.GetPlayerVehicle(MultiplayerSpawn.GetVehicleName((MultiplayerSpawn.Vehicles)vTEnumProperty.GetValue())));
			}
		}
	}

	private void OnEquipsSelected(int[] selectedIndices)
	{
		if (currentValue == null)
		{
			currentValue = new VehicleEquipmentList();
		}
		currentValue.equipment.Clear();
		for (int i = 0; i < allEquips.Length; i++)
		{
			if (!selectedIndices.Contains(i))
			{
				currentValue.equipment.Add(allEquips[i]);
			}
		}
		UpdateValueText();
	}
}
