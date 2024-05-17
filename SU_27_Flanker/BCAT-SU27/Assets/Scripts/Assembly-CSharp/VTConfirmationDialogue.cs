using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VTConfirmationDialogue : MonoBehaviour
{
	public Text titleText;

	public Text descriptionText;

	public ContentSizeFitter descriptionSizeFitter;

	private UnityAction onConfirm;

	private UnityAction onDeny;

	public Button confirmButton;

	public Button denyButton;

	private float height;

	private RectTransform rectTf;

	private RectTransform descriptRect;

	private void Awake()
	{
		rectTf = (RectTransform)base.transform;
		height = rectTf.rect.height;
		if ((bool)descriptionSizeFitter)
		{
			descriptRect = (RectTransform)descriptionSizeFitter.transform;
		}
	}

	public void DisplayConfirmation(string title, string description, UnityAction confirmAction, UnityAction denyAction)
	{
		onConfirm = confirmAction;
		onDeny = denyAction;
		titleText.text = title;
		descriptionText.text = description;
		base.gameObject.SetActive(value: true);
		OnDisplayPopup();
		base.transform.SetAsLastSibling();
		if ((bool)descriptionSizeFitter)
		{
			descriptionSizeFitter.SetLayoutVertical();
			rectTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + descriptRect.rect.height * descriptRect.localScale.y);
		}
		if ((bool)confirmButton && (bool)denyButton)
		{
			if (onConfirm == onDeny)
			{
				denyButton.gameObject.SetActive(value: false);
			}
			else
			{
				denyButton.gameObject.SetActive(value: true);
			}
		}
	}

	public void Okay()
	{
		OnClosePopup();
		base.gameObject.SetActive(value: false);
		if (onConfirm != null)
		{
			onConfirm();
		}
	}

	public void Cancel()
	{
		OnClosePopup();
		base.gameObject.SetActive(value: false);
		if (onDeny != null)
		{
			onDeny();
		}
	}

	protected virtual void OnDisplayPopup()
	{
	}

	protected virtual void OnClosePopup()
	{
	}
}
