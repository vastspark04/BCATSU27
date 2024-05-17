using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class VRIHoverToggle : MonoBehaviour
{
	public GameObject hoverObj;

	private void Awake()
	{
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnHover += Vri_OnHover;
		component.OnUnHover += Vri_OnUnHover;
		hoverObj.SetActive(value: false);
	}

	private void Vri_OnUnHover(VRHandController controller)
	{
		hoverObj.SetActive(value: false);
	}

	private void Vri_OnHover(VRHandController controller)
	{
		hoverObj.SetActive(value: true);
	}
}
