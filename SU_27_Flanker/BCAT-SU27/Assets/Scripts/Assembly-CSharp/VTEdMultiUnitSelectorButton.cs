using System.Collections.Generic;
using UnityEngine;

public class VTEdMultiUnitSelectorButton : MonoBehaviour
{
	public VTBoolProperty boolProp;

	public GameObject subUnitTemplate;

	private VTEdUnitSelector selector;

	private int subIdx = -1;

	private UnitSpawner uSpawner;

	private float height;

	public float GetHeight()
	{
		return height;
	}

	public void Setup(VTEdUnitSelector selector, UnitSpawner uSpawner, UnitReferenceList selected, bool allowSubunits)
	{
		this.selector = selector;
		this.uSpawner = uSpawner;
		height = ((RectTransform)base.transform).rect.height;
		boolProp.SetLabel(uSpawner.GetUIDisplayName());
		boolProp.SetInitialValue(selected.ContainsUnit(uSpawner.unitInstanceID));
		boolProp.OnPropertyValueChanged += OnValueChanged;
		if (allowSubunits && uSpawner.prefabUnitSpawn is AIUnitSpawn)
		{
			float num = ((RectTransform)subUnitTemplate.transform).rect.height;
			List<Actor> subUnits = ((AIUnitSpawn)uSpawner.prefabUnitSpawn).subUnits;
			for (int i = 0; i < subUnits.Count; i++)
			{
				_ = subUnits[i];
				GameObject obj = Object.Instantiate(subUnitTemplate, subUnitTemplate.transform.parent);
				obj.transform.localPosition = subUnitTemplate.transform.localPosition + num * (float)i * Vector3.down;
				height += num;
				obj.GetComponent<VTEdMultiUnitSelectorButton>().SetupSubunit(selector, uSpawner, selected, i);
			}
		}
		subUnitTemplate.SetActive(value: false);
	}

	private void SetupSubunit(VTEdUnitSelector selector, UnitSpawner uSpawner, UnitReferenceList selected, int subIdx)
	{
		this.selector = selector;
		this.uSpawner = uSpawner;
		this.subIdx = subIdx;
		AIUnitSpawn aIUnitSpawn = (AIUnitSpawn)uSpawner.prefabUnitSpawn;
		boolProp.SetLabel(subIdx + 1 + ": " + aIUnitSpawn.subUnits[subIdx].actorName);
		boolProp.SetInitialValue(selected.ContainsUnit(uSpawner.unitInstanceID, subIdx));
		boolProp.OnPropertyValueChanged += OnValueChangedSubUnit;
	}

	private void OnValueChanged(object o)
	{
		if ((bool)o)
		{
			selector.SelectUnit(uSpawner);
		}
		else
		{
			selector.DeselectUnit(uSpawner);
		}
	}

	private void OnValueChangedSubUnit(object o)
	{
		if ((bool)o)
		{
			selector.SelectSubUnit(uSpawner, subIdx);
		}
		else
		{
			selector.DeselectSubUnit(uSpawner, subIdx);
		}
	}

	public void SetValue(bool selected)
	{
		boolProp.SetInitialValue(selected);
		if (subIdx >= 0)
		{
			OnValueChangedSubUnit(selected);
		}
		else
		{
			OnValueChanged(selected);
		}
	}
}
