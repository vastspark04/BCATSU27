using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTBoolProperty : VTPropertyField, IPointerClickHandler, IEventSystemHandler
{
	public Image checkImage;

	private bool currValue;

	public event UnityAction<bool> OnValueChanged;

	public override object GetValue()
	{
		return currValue;
	}

	public override void SetInitialValue(object value)
	{
		currValue = (bool)value;
		checkImage.enabled = currValue;
	}

	public void Toggle()
	{
		currValue = !currValue;
		checkImage.enabled = currValue;
		if (this.OnValueChanged != null)
		{
			this.OnValueChanged(currValue);
		}
		ValueChanged();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		Toggle();
	}
}
