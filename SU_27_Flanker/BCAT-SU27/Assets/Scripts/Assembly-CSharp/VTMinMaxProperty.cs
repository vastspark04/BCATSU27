using UnityEngine;
using UnityEngine.UI;

public class VTMinMaxProperty : VTPropertyField
{
	public float minLimit;

	public float maxLimit = 1f;

	public Scrollbar minScrollBar;

	public Scrollbar maxScrollBar;

	public InputField minInputField;

	public InputField maxInputField;

	public Image midBarImage;

	private MinMax currValue;

	private UnitSpawnAttributeRange.RangeTypes _rangeType;

	public UnitSpawnAttributeRange.RangeTypes rangeType
	{
		get
		{
			return _rangeType;
		}
		set
		{
			_rangeType = value;
			switch (_rangeType)
			{
			case UnitSpawnAttributeRange.RangeTypes.Float:
			{
				int num3 = (minScrollBar.numberOfSteps = (maxScrollBar.numberOfSteps = 0));
				break;
			}
			case UnitSpawnAttributeRange.RangeTypes.Int:
			{
				currValue.min = Mathf.Floor(currValue.min);
				currValue.max = Mathf.CeilToInt(currValue.max);
				int numberOfSteps = Mathf.RoundToInt(maxLimit - minLimit + 1f);
				minScrollBar.numberOfSteps = numberOfSteps;
				maxScrollBar.numberOfSteps = numberOfSteps;
				break;
			}
			}
			UpdateUI();
		}
	}

	private void Awake()
	{
		minScrollBar.onValueChanged.AddListener(OnMinValueChanged);
		maxScrollBar.onValueChanged.AddListener(OnMaxValueChanged);
		minInputField.onEndEdit.AddListener(OnMinTextEntered);
		maxInputField.onEndEdit.AddListener(OnMaxTextEntered);
	}

	private void OnMinTextEntered(string s)
	{
		float value = float.Parse(s);
		value = Mathf.InverseLerp(minLimit, maxLimit, value);
		OnMinValueChanged(value);
		minScrollBar.value = Mathf.InverseLerp(minLimit, maxLimit, currValue.min);
	}

	private void OnMaxTextEntered(string s)
	{
		float value = float.Parse(s);
		value = Mathf.InverseLerp(minLimit, maxLimit, value);
		OnMaxValueChanged(value);
		maxScrollBar.value = Mathf.InverseLerp(minLimit, maxLimit, currValue.max);
	}

	private void OnMinValueChanged(float v)
	{
		if (minScrollBar.numberOfSteps == 0)
		{
			if (v >= maxScrollBar.value)
			{
				v = maxScrollBar.value - 0.01f;
				minScrollBar.value = v;
			}
		}
		else if (v >= maxScrollBar.value)
		{
			v = maxScrollBar.value - 1f / (float)(minScrollBar.numberOfSteps - 1);
			minScrollBar.value = v;
		}
		currValue.min = Mathf.Lerp(minLimit, maxLimit, v);
		UpdateUI();
		ValueChanged();
	}

	private void OnMaxValueChanged(float v)
	{
		if (maxScrollBar.numberOfSteps == 0)
		{
			if (v <= minScrollBar.value)
			{
				v = minScrollBar.value + 0.01f;
				maxScrollBar.value = v;
			}
		}
		else if (v <= minScrollBar.value)
		{
			v = minScrollBar.value + 1f / (float)(maxScrollBar.numberOfSteps - 1);
			maxScrollBar.value = v;
		}
		currValue.max = Mathf.Lerp(minLimit, maxLimit, v);
		UpdateUI();
		ValueChanged();
	}

	private void Start()
	{
		SetLimits(minLimit, maxLimit);
	}

	public void SetLimits(float minLimit, float maxLimit)
	{
		this.minLimit = minLimit;
		this.maxLimit = maxLimit;
		currValue.min = Mathf.Max(minLimit, currValue.min);
		currValue.max = Mathf.Min(maxLimit, currValue.max);
		SetInitialValue(currValue);
	}

	public override void SetInitialValue(object value)
	{
		currValue = (MinMax)value;
		if (_rangeType == UnitSpawnAttributeRange.RangeTypes.Int)
		{
			currValue.min = Mathf.Floor(currValue.min);
			currValue.max = Mathf.CeilToInt(currValue.max);
			minScrollBar.numberOfSteps = Mathf.RoundToInt(currValue.max - currValue.min);
		}
		minScrollBar.value = Mathf.InverseLerp(minLimit, maxLimit, currValue.min);
		maxScrollBar.value = Mathf.InverseLerp(minLimit, maxLimit, currValue.max);
		UpdateUI();
		ValueChanged();
	}

	private void UpdateUI()
	{
		midBarImage.transform.position = minScrollBar.handleRect.position;
		float x = minScrollBar.handleRect.InverseTransformPoint(maxScrollBar.handleRect.position).x;
		midBarImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x);
		string text = ((_rangeType == UnitSpawnAttributeRange.RangeTypes.Int) ? "0" : "0.00");
		minInputField.text = currValue.min.ToString(text);
		maxInputField.text = currValue.max.ToString(text);
	}

	public override object GetValue()
	{
		return currValue;
	}
}
