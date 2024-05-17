using UnityEngine;
using UnityEngine.UI;

public class MFDPortalPageSelectButton : MonoBehaviour
{
	public MFDPortalQuarter portalQtr;

	public MFDPortalPage page;

	public Text buttonText;

	public VRInteractable interactable;

	public GameObject hoverObj;

	private void Awake()
	{
		interactable = GetComponent<VRInteractable>();
		interactable.OnHover += VrInt_OnHover;
		interactable.OnUnHover += VrInt_OnUnHover;
		interactable.OnStartInteraction += PlaySound;
		hoverObj.gameObject.SetActive(value: false);
	}

	private void VrInt_OnUnHover(VRHandController controller)
	{
		hoverObj.gameObject.SetActive(value: false);
	}

	private void VrInt_OnHover(VRHandController controller)
	{
		hoverObj.gameObject.SetActive(value: true);
	}

	public void TouchButton()
	{
		if ((bool)page && (bool)portalQtr)
		{
			portalQtr.DisplayPage(page);
		}
		hoverObj.gameObject.SetActive(value: false);
	}

	private void PlaySound(VRHandController c)
	{
		if ((bool)portalQtr)
		{
			portalQtr.PlayInputSound();
		}
	}
}
