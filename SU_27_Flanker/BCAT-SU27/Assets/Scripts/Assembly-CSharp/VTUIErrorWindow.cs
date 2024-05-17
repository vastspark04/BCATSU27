using System;
using UnityEngine;
using UnityEngine.UI;

public class VTUIErrorWindow : MonoBehaviour
{
	public Text errorText;

	private Action OnOkayPressed;

	public void DisplayError(string errorMsg, Action OnOkayPressed)
	{
		if (base.gameObject.activeSelf)
		{
			Debug.LogError("Attempted to open error window but it was already open!");
			return;
		}
		base.gameObject.SetActive(value: true);
		this.OnOkayPressed = OnOkayPressed;
		errorText.text = errorMsg;
	}

	public void PressOkay()
	{
		base.gameObject.SetActive(value: false);
		if (OnOkayPressed != null)
		{
			OnOkayPressed();
		}
	}
}
