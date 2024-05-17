using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class VTStaticObjectReference : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text currentValueText;

	public List<Func<VTStaticObject, bool>> objectFilters;

	private StaticObjectReference objectRef;

	public void SetFilters(params Func<VTStaticObject, bool>[] filters)
	{
		objectFilters = new List<Func<VTStaticObject, bool>>();
		foreach (Func<VTStaticObject, bool> func in filters)
		{
			if (func != null)
			{
				objectFilters.Add(func);
			}
		}
	}

	public override void SetInitialValue(object value)
	{
		if (value == null)
		{
			objectRef = default(StaticObjectReference);
		}
		else if (value is VTStaticObject)
		{
			VTStaticObject vTStaticObject = (VTStaticObject)value;
			if (vTStaticObject != null)
			{
				objectRef = new StaticObjectReference(vTStaticObject.id);
			}
			else
			{
				objectRef = default(StaticObjectReference);
			}
		}
		else
		{
			if (!(value is StaticObjectReference))
			{
				objectRef = default(StaticObjectReference);
				throw new ArgumentException("Invalid object type in SetInitialValue for VTStaticObjectReference");
			}
			objectRef = (StaticObjectReference)value;
		}
		UpdateValueText();
	}

	public override object GetValue()
	{
		return objectRef;
	}

	public void SelectButton()
	{
		List<object> list = new List<object>();
		List<string> list2 = new List<string>();
		int selected = -1;
		foreach (VTStaticObject allObject in editor.currentScenario.staticObjects.GetAllObjects())
		{
			bool flag = true;
			if (objectFilters != null)
			{
				for (int i = 0; i < objectFilters.Count && flag; i++)
				{
					Func<VTStaticObject, bool> func = objectFilters[i];
					if (func != null && !func(allObject))
					{
						flag = false;
					}
				}
			}
			if (flag)
			{
				list.Add(allObject);
				list2.Add(allObject.GetUIDisplayName());
				if (allObject.id == objectRef.objectID)
				{
					selected = list.Count - 1;
				}
			}
		}
		editor.optionSelector.Display("Select Object", list2.ToArray(), list.ToArray(), selected, OnSelectedObject);
	}

	private void OnSelectedObject(object o)
	{
		if (o != null)
		{
			VTStaticObject vTStaticObject = (VTStaticObject)o;
			new StaticObjectReference(vTStaticObject.id);
			SetInitialValue(vTStaticObject);
		}
		else
		{
			SetInitialValue(null);
		}
		ValueChanged();
	}

	private void UpdateValueText()
	{
		string text = "None";
		if (objectRef.objectID >= 0)
		{
			text = (objectRef.GetStaticObject() ? objectRef.GetDisplayName() : "Missing!");
		}
		currentValueText.text = text;
	}
}
