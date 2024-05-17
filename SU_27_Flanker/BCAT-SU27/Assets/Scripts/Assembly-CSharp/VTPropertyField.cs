using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public abstract class VTPropertyField : MonoBehaviour
{
	public struct VTOnChangeAttributeCallback
	{
		public string methodName;

		public UnitSpawner spawner;
	}

	[HideInInspector]
	public string fieldName;

	public FieldInfo fieldInfo;

	public Text labelText;

	public Type type;

	public List<VTOnChangeAttributeCallback> onChangeAttributeCallbacks = new List<VTOnChangeAttributeCallback>();

	public event UnityAction<object> OnPropertyValueChanged;

	public virtual void SetLabel(string label)
	{
		if ((bool)labelText)
		{
			labelText.text = label;
		}
	}

	public virtual void SetInitialValue(object value)
	{
	}

	public virtual object GetValue()
	{
		return null;
	}

	protected void ValueChanged()
	{
		if (this.OnPropertyValueChanged != null)
		{
			this.OnPropertyValueChanged(GetValue());
		}
		foreach (VTOnChangeAttributeCallback onChangeAttributeCallback in onChangeAttributeCallbacks)
		{
			onChangeAttributeCallback.spawner.prefabUnitSpawn.GetType().GetMethod(onChangeAttributeCallback.methodName).Invoke(null, new object[2]
			{
				onChangeAttributeCallback.spawner,
				GetValue()
			});
		}
	}
}
