using UnityEngine;

public class VTECMenuConfirmDialogue : VTConfirmationDialogue
{
	public Transform blockerTransform;

	protected override void OnDisplayPopup()
	{
		blockerTransform.gameObject.SetActive(value: true);
		blockerTransform.SetAsLastSibling();
	}

	protected override void OnClosePopup()
	{
		blockerTransform.gameObject.SetActive(value: false);
	}
}
