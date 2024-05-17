using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIToolTipRect : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[TextArea]
	public string text;

	private Coroutine delayRoutine;

	public void OnPointerEnter(PointerEventData e)
	{
		delayRoutine = StartCoroutine(DisplayAfterDelay());
	}

	private IEnumerator DisplayAfterDelay()
	{
		yield return new WaitForSeconds(0.6f);
		if ((bool)UIToolTipManager.fetch)
		{
			UIToolTipManager.fetch.EnterTooltip(this);
		}
	}

	public void OnPointerExit(PointerEventData e)
	{
		if (delayRoutine != null)
		{
			StopCoroutine(delayRoutine);
		}
		if ((bool)UIToolTipManager.fetch)
		{
			UIToolTipManager.fetch.ExitTooltip(this);
		}
	}

	private void OnDisable()
	{
		OnPointerExit(null);
	}
}
