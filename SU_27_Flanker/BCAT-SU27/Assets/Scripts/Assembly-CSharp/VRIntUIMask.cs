using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class VRIntUIMask : MonoBehaviour
{
	public RectTransform mask;

	private VRInteractable vrInt;

	private void Start()
	{
		vrInt = GetComponent<VRInteractable>();
	}

	private void Update()
	{
		vrInt.enabled = mask.rect.Contains(mask.InverseTransformPoint(vrInt.transform.position));
	}
}
