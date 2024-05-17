using UnityEngine;
using UnityEngine.UI;

public class VTFloatRangeProperty : VTPropertyField
{
	public UnitSpawnAttributeRange.RangeTypes rangeType;

	[HideInInspector]
	public float min;

	[HideInInspector]
	public float max;

	public InputField inputField;

	public Scrollbar slider;

	private float currentValue;

	public override void SetInitialValue(object value)
	{
		currentValue = Mathf.Clamp((float)value, min, max);
		if ((bool)slider)
		{
			slider.value = Mathf.InverseLerp(min, max, currentValue);
		}
		if ((bool)inputField)
		{
			if (rangeType == UnitSpawnAttributeRange.RangeTypes.Float)
			{
				inputField.contentType = InputField.ContentType.DecimalNumber;
				inputField.text = currentValue.ToString("0.00");
			}
			else
			{
				inputField.contentType = InputField.ContentType.IntegerNumber;
				inputField.text = Mathf.RoundToInt(currentValue).ToString();
			}
		}
	}

	public override object GetValue()
	{
		return currentValue;
	}

	public void OnEnterString(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			currentValue = min;
		}
		else
		{
			currentValue = Mathf.Clamp(float.Parse(str), min, max);
		}
		if (rangeType == UnitSpawnAttributeRange.RangeTypes.Int)
		{
			currentValue = Mathf.RoundToInt(currentValue);
		}
		inputField.text = currentValue.ToString();
		if ((bool)slider)
		{
			slider.value = Mathf.InverseLerp(min, max, currentValue);
		}
		ValueChanged();
	}

	public void OnSetScroll(float t)
	{
		currentValue = Mathf.Lerp(min, max, t);
		if (rangeType == UnitSpawnAttributeRange.RangeTypes.Int)
		{
			currentValue = Mathf.RoundToInt(currentValue);
		}
		if ((bool)inputField)
		{
			if (rangeType == UnitSpawnAttributeRange.RangeTypes.Float)
			{
				inputField.text = currentValue.ToString("0.00");
			}
			else
			{
				inputField.text = Mathf.RoundToInt(currentValue).ToString();
			}
		}
		ValueChanged();
	}
}
