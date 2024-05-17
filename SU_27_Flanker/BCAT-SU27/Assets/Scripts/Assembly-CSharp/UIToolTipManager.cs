using UnityEngine;

public class UIToolTipManager : MonoBehaviour
{
	public UIToolTip toolTip;

	private UIToolTipRect activeRect;

	public static UIToolTipManager fetch { get; private set; }

	private void Awake()
	{
		fetch = this;
	}

	public void EnterTooltip(UIToolTipRect tr)
	{
		toolTip.Display(tr.text);
		activeRect = tr;
		base.transform.SetAsLastSibling();
	}

	public void ExitTooltip(UIToolTipRect tr)
	{
		if (tr == activeRect)
		{
			toolTip.Hide();
		}
	}
}
